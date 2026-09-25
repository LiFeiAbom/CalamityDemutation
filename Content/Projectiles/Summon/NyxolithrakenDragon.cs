using CalamityDemutation.Content.Buffs.NegativeBuffs;
using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 沧溟渊龙（NyxolithrakenDragon，移植自 CalamityEntropy 的 Content/Projectiles/NyxolithrakenDragon.cs）：
    /// 沧溟龙契召唤的 5 节龙形仆从（头 + 5 节身尾），占 5 个仆从栏位。
    /// 常态飘在主人头顶 120 像素处；锁定目标后按 CE 那套"远处盘旋靠近、近身不再调整速度"的追击逻辑撞上去，
    /// 命中时挂生命压制与灾厄的窃语之死，并按 28 帧冷却朝目标前方 300 像素处撕一道 <see cref="NxCrack"/> 裂空。
    /// <para>
    /// 与 CE 原版的差异：① 节段链 <c>segs</c> 的初始化从 <c>AI</c> 首帧挪到 <c>OnSpawn</c>
    /// （同 <see cref="PhantomWyrm"/> 那次踩过的"代生成的弹幕首帧可能不跑 AI"时序坑——否则
    /// <c>AI</c>/<c>Colliding</c>/<c>PreDraw</c> 三处都会拿到空的 <c>segs</c>）；
    /// ② <c>PRT_Abyssal</c> → 本模组既有的 <see cref="AbyssalParticle"/>（同名同名物，已在上屏深渊 RT 里绘制），
    /// <c>CEUtils.randomPointInCircle</c> → 本类私有 <see cref="RandomPointInCircle"/>；
    /// ③ <c>CEUtils.FindTarget_HomingProj(proj, center, 2600)</c> → 本类私有 <see cref="FindTarget"/> 等价内联
    /// （保留 CE 的 <c>CanBeChasedBy(proj) &amp;&amp; !friendly</c> 判定与"纯中心距"口径，没用工程那个会算半身位的 FindClosestNPC）；
    /// ④ <c>target.AddBuff&lt;LifeOppress&gt;()</c> → 本模组自己的生命压制；CE 的 <c>WhisperingDeath</c>
    /// 是灾厄减益（两版都有）→ 走软依赖 <c>ApplyCalamityBuff</c> 各注册一次；
    /// ⑤ <c>CEUtils.getDistance</c>→<c>Vector2.Distance</c>、<c>ToNPC()</c>→带边界检查的 <c>Main.npc[]</c>、
    /// <c>getRectCentered</c>→原版 <c>Utils.CenteredRectangle</c>、<c>Projectile.GetTexture()</c>→<c>TextureAssets</c>、
    /// <c>CEUtils.getExtraTex("NxDragonGlow")</c>→同目录同前缀的 <c>NyxolithrakenDragonGlow</c>；
    /// ⑥ 节段的 <c>WyrmSeg</c>/<c>iWyrmSeg</c> 直接复用 <see cref="PhantomWyrm"/> 文件里已移植的那两个类（CE 侧同源）。
    /// </para>
    /// </summary>
    internal class NyxolithrakenDragon:ModProjectile, iWyrmSeg
    {
        // ── 状态与属性 ──
        /// <summary>本体角度：直接读写 <c>Projectile.rotation</c>（供节段链当作"前驱"读取）</summary>
        public float rot
        {
            get { return Projectile.rotation; }
            set { Projectile.rotation = value; }
        }
        /// <summary>本体位置：直接读写 <c>Projectile.Center</c>（供节段链当作"前驱"读取）</summary>
        public Vector2 Center
        {
            get { return Projectile.Center; }
            set { Projectile.Center = value; }
        }
        /// <summary>裂空冷却：<c>ai[2]</c>，大于 0 时命中不再撕裂缝</summary>
        public float CrackCd => Projectile.ai[2];
        /// <summary>出场帧数：<c>ai[0]</c>（供盘旋相位与"已出场 45 帧"判据使用）</summary>
        internal ref float Time => ref Projectile.ai[0];
        /// <summary>飞行加速度（平滑后的当前值）：<c>ai[1]</c></summary>
        internal ref float FlyAcceleration => ref Projectile.ai[1];
        /// <summary>当前锁定目标（CE 原样：普通字段、跨帧保留，多人下不保证各端一致）</summary>
        private NPC target = null;
        /// <summary>节段链是否待生成（本模组提前到 OnSpawn 执行，保留该标记以防 AI 先跑到）</summary>
        private bool spawnSeg = true;
        /// <summary>节段链（5 节，间距 34/46/34/34/16）</summary>
        private List<WyrmSeg> segs;
        /// <summary>
        /// 龙体发光贴图（CE 的 Assets/Extra/NxDragonGlow）。
        /// 常量名刻意不叫 <c>GlowTexture</c>——<see cref="ModProjectile"/> 已有同名成员，那样会 CS0108（本工程警告基线是 0）
        /// </summary>
        private const string GlowTexturePath = "CalamityDemutation/Content/Projectiles/Summon/NyxolithrakenDragonGlow";
        // ── 生命周期方法 ──
        /// <summary>单帧贴图、可被牺牲、参与原版仆从索敌目标机制</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
            ProjectileID.Sets.MinionSacrificable[Type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        }
        /// <summary>
        /// 基础属性：68x68 碰撞箱（节段的 36×36 判定另算）、友方、无限穿透、不撞地形、
        /// 占用 5 个仆从栏位、每个敌人独立命中冷却；命中无敌帧取 9（见下方注释）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 68;
            Projectile.height = 68;
            Projectile.friendly = true;
            Projectile.aiStyle = -1;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.minion = true;
            Projectile.minionSlots = 5;
            Projectile.usesLocalNPCImmunity = true;
            // 命中无敌帧：CE 原样是 26（约 2.3 次/秒），2026-09-22 用户拍板改成 9（约 6.7 次/秒）
            Projectile.localNPCHitCooldown = 9;
            Projectile.extraUpdates = 1;
        }
        // ── 覆写方法 ──
        /// <summary>不能砍草</summary>
        public override bool? CanCutTiles()
        {
            return false;
        }
        /// <summary>生成时就把 5 节节段串好（CE 放在 AI 首帧，理由见类注释差异①）</summary>
        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            InitSegments();
        }
        /// <summary>以本体为头串出 5 节，节间距按 CE 的 34/46/34/34/16，转向速度 0.06</summary>
        private void InitSegments()
        {
            spawnSeg = false;
            segs = new List<WyrmSeg>();
            iWyrmSeg seg = this;
            List<int> spacings = new List<int>() { 34, 46, 34, 34, 16 };
            for (int i = 0; i < 5; i++)
            {
                WyrmSeg spawn = new WyrmSeg() { Center = Projectile.Center, follow = seg, rotC = 0.06f, spacing = spacings[i] };
                segs.Add(spawn);
                seg = spawn;
            }
        }
        /// <summary>
        /// AI：漂在主人头顶 120 像素处待机（<c>target</c> 为空时），锁定目标后交给 <see cref="AttackTarget"/> 追击；
        /// 每帧把本体沿速度前挪一格跑完节段链再挪回来（避免头身错开）、离主人超 4000 像素直接传送回身边；
        /// 主人持有沧溟龙契增益（<see cref="NyxolithrakenBuff"/>）时不断续命实现常驻，否则按 timeLeft 自然消亡。
        /// ai 槽位：ai[0] = 出场帧数 Time、ai[1] = 飞行加速度 FlyAcceleration、ai[2] = 裂空冷却 CrackCd；
        /// localAI[0]：首帧为 0 时喷一圈深渊粒子（由 AI 末尾自增，故只触发一次）。
        /// </summary>
        public override void AI()
        {
            if (spawnSeg)
            {
                InitSegments();
            }
            if (Projectile.localAI[0] == 0 && !Main.dedServ)
            {
                for (int i = 0; i < 32; i++)
                {
                    // 深渊粒子（CE 的 PRT_Abyssal）不进常规粒子桶，走上屏深渊 RT 绘制
                    AbyssalParticle particle = new AbyssalParticle();
                    DRKLoader.NewParticle(particle, Projectile.Center, RandomPointInCircle(18f), Color.White, 1f);
                    particle.Opacity = Main.rand.NextFloat(0.35f, 0.7f);
                    particle.vd = 0.9f;
                }
            }
            Player player = Main.player[Projectile.owner];
            if (target == null)
            {
                // 没目标就飘在主人头顶 120 像素处，离得远才慢慢靠过去
                Vector2 hover = player.Center + new Vector2(0, -120);
                if (Vector2.Distance(hover, Projectile.Center) > 300)
                {
                    Projectile.velocity *= 0.96f;
                    Projectile.velocity += (hover - Projectile.Center).SafeNormalize(Vector2.Zero) * 0.6f;
                }
            }
            else
            {
                AttackTarget(target);
            }
            // 先让本体按速度前进一帧、节段跟着摆好，再把本体挪回来，避免本体与身节错开一格
            Projectile.Center += Projectile.velocity;
            foreach (WyrmSeg seg in segs)
            {
                seg.update();
            }
            Projectile.Center -= Projectile.velocity;
            Projectile.localAI[0]++;
            if (Vector2.Distance(Projectile.Center, player.Center) > 4000)
            {
                Projectile.Center = player.Center;
            }
            if (player.HasBuff(ModContent.BuffType<NyxolithrakenBuff>()))
            {
                Projectile.timeLeft = 3;
            }
            if (target == null || !target.active || target.dontTakeDamage || Vector2.Distance(Projectile.Center, target.Center) > 3000)
            {
                target = FindTarget(2600f);
            }
            if (player.MinionAttackTargetNPC >= 0 && player.MinionAttackTargetNPC < Main.maxNPCs && Main.npc[player.MinionAttackTargetNPC].active)
            {
                target = Main.npc[player.MinionAttackTargetNPC];
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        /// <summary>
        /// 追击逻辑（逐字照搬 CE）：平时以 0.18 的加速度转向目标；目标在 725 像素外则叠一圈半径 145 的盘旋偏移
        /// 并把加速度 ×2.5；追击超过 1500 像素且已出场 45 帧后加速度上限抬到 6。
        /// 距离 320 像素内不再调整速度（交给惯性撞上去），之外按"当前速度朝向与目标夹角"做加减速与转向。
        /// 末尾递减裂空冷却。
        /// </summary>
        private void AttackTarget(NPC target)
        {
            float acceleration = 0.18f;
            Vector2 center = target.Center;
            float distance = Projectile.Distance(center);
            if (distance > 725f)
            {
                center += (Time % 30f / 30f * (MathF.PI * 2f)).ToRotationVector2() * 145f;
                distance = Projectile.Distance(center);
                acceleration *= 2.5f;
            }
            if (distance > 1500f && Time > 45f)
            {
                acceleration = MathHelper.Min(6f, FlyAcceleration + 1f);
            }
            FlyAcceleration = MathHelper.Lerp(FlyAcceleration, acceleration, 0.3f);
            float angleDot = Vector2.Dot(Projectile.velocity.SafeNormalize(Vector2.Zero), Projectile.SafeDirectionTo(center));
            if (distance > 320f)
            {
                float speed = Projectile.velocity.Length();
                if (speed < 23f)
                {
                    speed += 0.08f;
                }
                if (speed > 32f)
                {
                    speed -= 0.08f;
                }
                if (angleDot < 0.85f && angleDot > 0.5f)
                {
                    speed += 6f;
                }
                if (angleDot < 0.5f && angleDot > -0.7f)
                {
                    speed -= 10f;
                }
                speed = MathHelper.Clamp(speed, 16f, 34f);
                Projectile.velocity = Projectile.velocity.ToRotation().AngleTowards(Projectile.AngleTo(center), FlyAcceleration * 1.3f).ToRotationVector2() * speed;
            }
            Projectile.ai[2]--;
        }
        /// <summary>
        /// CE 的 <c>CEUtils.FindTarget_HomingProj</c> 等价实现：在半径内取最近的"可被本弹幕追踪且非友方"的敌怪，
        /// 按纯中心距比较（不含半身位补偿）。
        /// </summary>
        private NPC FindTarget(float maxDistance)
        {
            NPC found = null;
            float best = maxDistance;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile) || npc.friendly)
                {
                    continue;
                }
                float distance = Vector2.Distance(npc.Center, Projectile.Center);
                if (distance <= best)
                {
                    best = distance;
                    found = npc;
                }
            }
            return found;
        }
        /// <summary>
        /// 命中：挂生命压制与灾厄的窃语之死；裂空冷却好了就朝目标前 300 像素处生成一道 <see cref="NxCrack"/>
        /// （伤害取本体的一半），并把冷却重置为 28。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<LifeOppress>(), 600);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "WhisperingDeath", 300);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "WhisperingDeath", 300);
            if (CrackCd <= 0)
            {
                Projectile.ai[2] = 28;
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), target.Center - direction * 300, direction, ModContent.ProjectileType<NxCrack>(), Projectile.damage / 2, 0, Projectile.owner);
            }
        }
        /// <summary>命中判定：除本体框外，每一节身子的 36×36 方框也算（CE 原样，否则只有龙头能打到人）</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            foreach (WyrmSeg seg in segs)
            {
                if (Utils.CenteredRectangle(seg.Center, new Vector2(36f, 36f)).Intersects(targetHitbox))
                {
                    return true;
                }
            }
            return base.Colliding(projHitbox, targetHitbox);
        }
        /// <summary>本体的占位贴图（实际由 <see cref="DrawSeg"/> 按头/身/尾六个帧区绘制）</summary>
        public Texture2D tex => TextureAssets.Projectile[Type].Value;
        /// <summary>龙体发光贴图（叠在每一节本体之上）</summary>
        public Texture2D texGlow => ModContent.Request<Texture2D>(GlowTexturePath).Value;
        /// <summary>
        /// 全自绘：把贴图条切成六段（头 80×80 起于 x=278，随后四节身子与 x=0 的尾巴），
        /// 依次画在本体与 5 个节段中心，每段再叠一层发光贴图。朝向朝左时改走"垂直翻转 + 原点镜像"那一路。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Main.instance.LoadProjectile(Type);
            Rectangle head = new Rectangle(278, 0, 80, 80);
            Vector2 ohead = new Vector2(46, 54);
            Rectangle seg1 = new Rectangle(190, 0, 86, 80);
            Vector2 oseg1 = new Vector2(48, 54);
            Rectangle seg2 = new Rectangle(128, 0, 60, 80);
            Vector2 oseg2 = new Vector2(36, 54);
            Rectangle seg3 = new Rectangle(90, 0, 36, 80);
            Vector2 oseg3 = new Vector2(18, 54);
            Rectangle seg4 = new Rectangle(50, 0, 38, 80);
            Vector2 oseg4 = new Vector2(18, 54);
            Rectangle tail = new Rectangle(0, 0, 48, 80);
            Vector2 otail = new Vector2(48, 54);
            DrawSeg(Center, head, rot, ohead, lightColor);
            DrawSeg(segs[0].Center, seg1, segs[0].rot, oseg1, lightColor);
            DrawSeg(segs[1].Center, seg2, segs[1].rot, oseg2, lightColor);
            DrawSeg(segs[2].Center, seg3, segs[2].rot, oseg3, lightColor);
            DrawSeg(segs[3].Center, seg4, segs[3].rot, oseg4, lightColor);
            DrawSeg(segs[4].Center, tail, segs[4].rot, otail, lightColor);
            return false;
        }
        /// <summary>画一节龙身与其发光层；朝左时用 FlipVertically + 翻转的原点把贴图上下倒过来贴</summary>
        public void DrawSeg(Vector2 pos, Rectangle frame, float rot, Vector2 origin, Color color)
        {
            if (Projectile.velocity.X > 0)
            {
                Main.EntitySpriteDraw(tex, pos - Main.screenPosition, frame, color, rot, origin, Projectile.scale, SpriteEffects.None);
                Main.EntitySpriteDraw(texGlow, pos - Main.screenPosition, frame, Color.White, rot, origin, Projectile.scale, SpriteEffects.None);
            }
            else
            {
                Main.EntitySpriteDraw(tex, pos - Main.screenPosition, frame, color, rot, new Vector2(origin.X, tex.Height - origin.Y), Projectile.scale, SpriteEffects.FlipVertically);
                Main.EntitySpriteDraw(texGlow, pos - Main.screenPosition, frame, Color.White, rot, new Vector2(origin.X, tex.Height - origin.Y), Projectile.scale, SpriteEffects.FlipVertically);
            }
        }
        // ── 私有工具 ──
        /// <summary>CEUtils.randomPointInCircle 的等价实现：随机角度 × [-r, r] 的随机半径</summary>
        private static Vector2 RandomPointInCircle(float r) => Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(-r, r);
    }
}
