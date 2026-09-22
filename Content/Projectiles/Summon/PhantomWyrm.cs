using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 龙体节段接口（CE 的 iWyrmSeg）：让"头"（弹幕本体）与"身"（<see cref="WyrmSeg"/>）能用同一套链式跟随逻辑。
    /// </summary>
    public interface iWyrmSeg
    {
        float rot { get; set; }
        Vector2 Center { get; set; }
    }
    /// <summary>
    /// 龙体节段（CE 的 WyrmSeg）：每帧朝向前一节段、并保持 <see cref="spacing"/> 的间距。
    /// 它是纯 C# 对象（不是弹幕），由幻影妖龙在生成时串成一条 32 节的链。
    /// </summary>
    public class WyrmSeg:iWyrmSeg
    {
        public float rot { get; set; }
        public Vector2 Center { get; set; }
        /// <summary>前一节（头一节的前驱就是弹幕本体）</summary>
        public iWyrmSeg follow;
        /// <summary>与前一节的距离</summary>
        public int spacing = 48;
        /// <summary>每帧最多朝向前一节角度靠拢多少弧度</summary>
        public float rotC = 0.14f;
        /// <summary>true 时无条件跟到 spacing 距离上；false 时只在超过 spacing 才拉近</summary>
        public bool AlwaysFollow = true;
        public void update()
        {
            if (follow == null)
            {
                return;
            }
            rot = (follow.Center - Center).ToRotation();
            if (rotC > 0)
            {
                rot = CDUtil.RotateTowardsAngle(rot, follow.rot, rotC, false);
            }
            if (AlwaysFollow || Vector2.Distance(Center, follow.Center) > spacing)
            {
                Center = follow.Center - rot.ToRotationVector2() * spacing;
            }
        }
    }
    /// <summary>
    /// 幻影妖龙（PhantomWyrm） - 噬渊鞭挞（Ystralyn）命中敌人时给自己挂上虚无幻象（WyrmPhantom），
    /// 玩家侧据此在场上没有它时补生成一只（见 <c>CalamityDemutationPlayer.WyrmPhantom</c>）。
    /// 它**不占仆从栏**（CE 原样：没置 <c>Projectile.minion</c>），靠 buff 在场时不断把 timeLeft 顶回 3 来维持；
    /// buff 消失后 alpha 逐帧回落，归零即自然消亡。伤害取 <c>Ystralyn.PhantomDamage</c> 经玩家召唤伤害加成后的值。
    /// <para>
    /// 与 CE 原版的差异：① <c>rot</c> 属性的 setter —— CE 写成 <c>set { this.rot = value; }</c>（自赋值，
    /// 真调起来会无限递归栈溢出；只因链式跟随只读不写才没炸），本模组按显而易见的本意写成写回
    /// <c>Projectile.rotation</c>；② 节段链的初始化从 <c>AI</c> 挪到 <c>OnSpawn</c>（同中子脉冲那次踩过的
    /// "代生成的弹幕首帧可能不跑 AI"时序坑：否则 PreDraw 会拿到空的 <c>segs</c>）；
    /// ③ <c>CEUtils.findTarget</c> 内联（其内部的 MinionAttackTargetNPC 判定在 CE 里本就被 AI 末尾那段覆盖，
    /// 故只留 <c>FindTargetWithinRange</c>）；④ <c>CEUtils.getDistance</c>→<c>Vector2.Distance</c>、
    /// <c>CEUtils.RotateTowardsAngle</c>→<c>CDUtil.RotateTowardsAngle</c>；⑤ <c>Projectile.GetOwner()</c>→
    /// <c>Main.player[Projectile.owner]</c>；⑥ CE 那行切加法混合的 <c>UseBlendState</c> 本来就是注释掉的，
    /// 本模组照旧按默认批次画（贴图自带发光感，行为与 CE 一致）。
    /// </para>
    /// </summary>
    internal class PhantomWyrm:ModProjectile, iWyrmSeg
    {
        /// <summary>本体角度：直接读写 <c>Projectile.rotation</c>（见类注释差异①）</summary>
        public float rot
        {
            get { return Projectile.rotation; }
            set { Projectile.rotation = value; }
        }
        public Vector2 Center
        {
            get { return Projectile.Center; }
            set { Projectile.Center = value; }
        }
        /// <summary>贴图占位：本弹幕全程自绘 PreDraw 里的四张龙体图，故按工程惯例指向白图</summary>
        public override string Texture => "CalamityDemutation/Assets/ExtraTextures/white";
        /// <summary>龙体四张图（CE 的 Assets/Extra/pw_*）</summary>
        private const string HeadTexture = "CalamityDemutation/Content/Projectiles/Summon/PhantomWyrmHead";
        private const string TailTexture = "CalamityDemutation/Content/Projectiles/Summon/PhantomWyrmTail";
        private const string BodyTexture = "CalamityDemutation/Content/Projectiles/Summon/PhantomWyrmBody";
        private const string BodyAltTexture = "CalamityDemutation/Content/Projectiles/Summon/PhantomWyrmBodyAlt";
        /// <summary>节段数（CE 原样 32）</summary>
        private const int SegmentCount = 32;
        /// <summary>当前不透明度：有 buff 时每帧 +0.01，没 buff 时每帧 -0.01</summary>
        private float alpha = 0;
        /// <summary>节段链是否待生成（生成后置 false；本该由 AI 首次执行，现提前到 OnSpawn）</summary>
        private bool spawnSeg = true;
        /// <summary>节段链</summary>
        private List<WyrmSeg> segs;
        /// <summary>当前锁定的目标（会跨帧保留，CE 原样：是普通字段而非 ai 槽，多人下不保证各端一致）</summary>
        private NPC target = null;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 5000;
        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.width = 224;
            Projectile.height = 224;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 3;
            Projectile.MaxUpdates = 3;
            Projectile.usesLocalNPCImmunity = true;
            // 命中无敌帧：CE 原样是 30（约 2 次/秒），2026-09-22 用户拍板改成 10（约 6 次/秒）
            Projectile.localNPCHitCooldown = 10;
        }
        /// <summary>
        /// 生成时就把节段链串好（CE 放在 AI 首帧；挪到这里是为了保证 PreDraw 永远不会拿到空表，
        /// 见类注释差异②）。
        /// </summary>
        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            InitSegments();
        }
        /// <summary>以本体为头串出一条 32 节的链，每节都朝向前一节</summary>
        private void InitSegments()
        {
            spawnSeg = false;
            segs = new List<WyrmSeg>();
            iWyrmSeg seg = this;
            for (int i = 0; i < SegmentCount; i++)
            {
                WyrmSeg spawn = new WyrmSeg() { Center = Projectile.Center, follow = seg };
                segs.Add(spawn);
                seg = spawn;
            }
        }
        internal ref float Time => ref Projectile.ai[0];
        internal ref float FlyAcceleration => ref Projectile.ai[1];
        public override void AI()
        {
            if (spawnSeg)
            {
                InitSegments();
            }
            foreach (WyrmSeg seg in segs)
            {
                seg.update();
            }
            Player player = Main.player[Projectile.owner];
            // 有 buff 就渐显、没 buff 就渐隐；还有一点影子时持续续命，淡到 0 后自然消亡
            if (player.HasBuff(ModContent.BuffType<WyrmPhantom>()))
            {
                if (alpha < 1)
                {
                    alpha += 0.01f;
                }
            }
            else
            {
                if (alpha > 0)
                {
                    alpha -= 0.01f;
                }
            }
            if (alpha > 0)
            {
                Projectile.timeLeft = 3;
            }
            if (target == null || !target.active || !target.CanBeChasedBy(Projectile))
            {
                target = Projectile.FindTargetWithinRange(3600f, false);
            }
            if (target == null)
            {
                // 没目标就飘在玩家头顶 120 像素处；离得太远才慢慢靠过去（CE 原样的 300 阈值）
                Vector2 hover = player.Center + new Vector2(0, -120);
                if (Vector2.Distance(hover, Projectile.Center) > 300)
                {
                    Projectile.velocity *= 0.98f;
                    Projectile.velocity += (hover - Projectile.Center).SafeNormalize(Vector2.Zero) * 0.33f;
                }
            }
            else
            {
                AttackTarget(target);
            }
            // 玩家用召唤物标记指定的目标优先（原版右键/鞭子标记的 MinionAttackTargetNPC）
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
        }
        /// <summary>
        /// 全自绘：头部画在弹幕中心并跟着 <c>Projectile.rotation</c>，随后 32 节节段依次画（最后一节用尾巴图，
        /// 其余按下标奇偶交替用两张身体图），整体乘 <see cref="alpha"/> 做淡入淡出。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            if (segs == null || segs.Count == 0)
            {
                return false;
            }
            Texture2D head = ModContent.Request<Texture2D>(HeadTexture).Value;
            Texture2D tail = ModContent.Request<Texture2D>(TailTexture).Value;
            Texture2D body1 = ModContent.Request<Texture2D>(BodyTexture).Value;
            Texture2D body2 = ModContent.Request<Texture2D>(BodyAltTexture).Value;
            Main.EntitySpriteDraw(head, Projectile.Center - Main.screenPosition, null, Color.White * 0.8f * alpha, Projectile.rotation, head.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
            for (int i = 0; i < segs.Count; i++)
            {
                Texture2D draw = tail;
                if (i < segs.Count - 1)
                {
                    draw = (i % 2 == 0) ? body1 : body2;
                }
                Main.EntitySpriteDraw(draw, segs[i].Center - Main.screenPosition, null, Color.White * 0.8f * alpha, segs[i].rot, draw.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
            }
            return false;
        }
    }
}
