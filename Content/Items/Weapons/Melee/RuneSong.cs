using CalamityDemutation.Content.Buffs.NegativeBuffs;
using CalamityDemutation.Content.Dusts;
using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Players;
using CalamityDemutation.Sounds;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 符文之歌（RuneSong，移植自 CalamityEntropy）：灾厄之影系武器，由灾厄之影宝袋掉落。
    /// 本体既不显示也不判定（noUseGraphic / noMelee），持握与挥砍交给手持弹幕 <see cref="RuneSongHeld"/>；
    /// 用频道式持握（channel），按住左键才会一直维持。
    /// </summary>
    internal class RuneSong:ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 230;
            Item.DamageType = DamageClass.Melee;
            Item.width = 56;
            Item.height = 56;
            Item.noUseGraphic = true;
            Item.useTime = 40;
            Item.useAnimation = 40;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6;
            Item.value = Item.buyPrice(gold: 60);
            Item.rare = ItemRarityID.Yellow;
            Item.UseSound = null;                  // 挥砍音由手持弹幕播放
            Item.channel = true;                   // 频道式：按住左键维持手持弹幕
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<RuneSongHeld>();
            Item.shootSpeed = 6f;
        }
        /// <summary>虽用 Swing 姿势，但伤害类型是近战，允许吃近战前缀的速度加成</summary>
        public override bool MeleePrefix() => true;
    }
    /// <summary>
    /// 符文之歌手持弹幕（RuneSongHeld，移植自 CalamityEntropy）：<c>ai[1]</c> 当状态机用，五个阶段循环——
    /// <b>0</b> 短暂蓄势（剑往身后压）；<b>1</b> 大范围单次挥舞；命中则转 <b>4</b>（剑身弹开的过渡，此间剑体胀大）；
    /// <b>2</b> 更大幅度的多段斩击；<b>3</b> 收招回正。没命中时从 <b>1</b> 直接进 <b>3</b>，
    /// 并在收招末尾朝光标方向打出一道符文脉冲束 <see cref="RuneBolt"/>。
    /// 命中的敌人挂护甲碎裂类减益。
    /// <para>
    /// 与 CE 原版的差异：① CE 的 <c>SoulDisorder</c> 是它自研的易伤减益，两版灾厄都没有，
    /// 改用两版都有的 ArmorCrunch（经 <c>CalamityDemutationPlayer.ApplyCalamityBuff</c> 挂）；
    /// ② <c>FriendlySetDefaults</c>（CE 工具）按净结果展开成 SetDefaults 里的逐条赋值；
    /// ③ <c>Entropy().FirstFrames</c> 换成本类的 <c>firstFrames</c> 字段，在 <see cref="AI"/> 开头取出后立刻置假；
    /// ④ <c>StickToPlayer</c>/<c>SetHandRotWithDir</c>/<c>RotateTowardsAngle</c>/<c>Parabola</c>/
    /// <c>randomPointInCircle</c> 在 CE 侧属于工具库，这里内联；<c>player.mouseWorld()</c> 走本模组同口径的
    /// <see cref="CalamityDemutationPlayer.GetMouseWorld"/>（跨端可见的鼠标世界坐标）；
    /// ⑤ <c>SpawnHeavenSpark</c> 用本模组的 <see cref="HeavenfallStarCal"/> 内联（CE 的 PRT_HeavenfallStar 的精确移植版）；
    /// ⑥ <c>FlashEffectStrength</c> 走本模组主类上的同名全局量，由 <c>EffectsSystem</c> 在上屏阶段做白闪；
    /// ⑦ <c>CEUtils.SyncProj</c> 换成本机原版的 <c>NetMessage.SendData(MessageID.SyncProjectile)</c>；
    /// ⑧ <c>ModifyHitNPC</c> 在 CE 里只有一句 <c>modifiers.SourceDamage *= 1f</c>（无操作），删掉；
    /// ⑨ <c>localAI[1]</c> 在 CE 里只写不读（累计剑体转过多少弧度），删掉；
    /// ⑩ <c>CEUtils.UseAdditive</c>/<c>ExitShaderRegion</c> 本机 tML 没有，改用 End + Begin(Immediate, Additive)、
    /// 画完再恢复默认批次；⑪ <c>CEUtils.WeapSound</c> 按 1.0 处理。
    /// </para>
    /// </summary>
    internal class RuneSongHeld:ModProjectile
    {
        /// <summary>剑体拖尾的圆形烟贴图（CE 的 CEExtraAssets.CircularSmearSmokey）</summary>
        private const string SmearTexture = "CalamityDemutation/Assets/ExtraTextures/CircularSmearSmokey";
        /// <summary>灵魂紊乱减益类型（本模组自有，见 <c>Content/Buffs/NegativeBuffs/SoulDisorder.cs</c>）</summary>
        internal static int soulDisorderBuffType = -1;
        /// <summary>朝向（±1），取出手时的水平速度方向，之后在收招时朝鼠标重定向</summary>
        private int dir = 1;
        /// <summary>当前每帧的旋转角速度</summary>
        private float rotVel = 0f;
        /// <summary>相对鼠标方向的额外旋转偏移（叠在 StickToPlayer 给出的朝向上）</summary>
        private float rotOffset = 0f;
        /// <summary>剑体辉光强度</summary>
        private float glow = 0f;
        /// <summary>圆形拖尾的透明度</summary>
        private float smearAlpha = 0f;
        /// <summary>剑身绘制缩放（多段斩阶段会胀大）</summary>
        private float scaleE = 1f;
        /// <summary>命中判定线段长度</summary>
        private const int Length = 250;
        /// <summary>首帧标记：CE 用全局的 Entropy().FirstFrames，这里自持一份，AI 开头取出后立刻置假</summary>
        private bool firstFrames = true;
        /// <summary><c>ai[1]</c> 的状态机槽位：0 蓄势 / 1 挥舞 / 4 弹开过渡 / 2 多段斩 / 3 收招</summary>
        private int flag
        {
            get => (int)Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }
        public override void SetStaticDefaults()
        {
            // 灵魂紊乱是本模组自有的减益（CE 原版挂的就是它），内容注册完成后直接取 ID
            soulDisorderBuffType = ModContent.BuffType<SoulDisorder>();
        }
        public override void SetDefaults()
        {
            // CE 的 FriendlySetDefaults(Melee, false, -1) 展开，后续几行是它紧随其后的覆写
            Projectile.DamageType = DamageClass.Melee;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.MaxUpdates = 2;                   // 配合 40 帧使用时间 → 蓄势/挥舞一共 80 帧
            Projectile.timeLeft = 800;                   // 占位值，真正的结束判据是状态机
            Projectile.localNPCHitCooldown = -1;         // 一开始同一次挥砍只结算一次，多段斩阶段会改成 8
        }
        /// <summary>只有挥舞（1）与多段斩（2）阶段参与判定，蓄势和收招不伤人</summary>
        public override bool? CanDamage() => flag == 1 || flag == 2;
        public override void AI()
        {
            bool isFirstFrame = firstFrames;
            firstFrames = false;
            if (isFirstFrame)
            {
                // 首帧套用玩家的近战尺寸加成（原版 Player.ApplyMeleeScale）
                float meleeScale = Main.player[Projectile.owner].HeldItem.scale;
                Main.player[Projectile.owner].ApplyMeleeScale(ref meleeScale);
                Projectile.scale *= meleeScale;
            }
            Player player = Main.player[Projectile.owner];
            // 全局攻速加成只按武器自己声明的倍率吃（原版 ItemID.Sets.BonusAttackSpeedMultiplier）
            float speedMelee = 1 + ((player.GetTotalAttackSpeed(DamageClass.Melee) - 1) * ItemID.Sets.BonusAttackSpeedMultiplier[player.HeldItem.type]);
            float speedTrueMelee = player.GetWeaponAttackSpeed(player.HeldItem);
            player.itemTime = player.itemAnimation = 2;
            if (flag == 0)
            {
                // 蓄势：剑往身后压，进度按真近战攻速累计到 1 转挥舞
                rotVel *= (float)Math.Pow(0.9f, speedTrueMelee);
                if (isFirstFrame)
                {
                    dir = Projectile.velocity.X > 0 ? 1 : -1;
                    rotOffset = -1.2f;
                    rotVel = -0.2f;
                }
                Projectile.ai[0] += speedTrueMelee / 60f;
                if (Projectile.ai[0] >= 1)
                {
                    Projectile.ai[0] = 1;
                    flag = 1;
                    smearAlpha = 1f;
                    SoundEngine.PlaySound(CalamityDemutationSounds.RuneSongCharge with { Pitch = Main.rand.NextFloat(0.6f, 0.8f) - 1f }, Projectile.Center);
                }
                glow = Projectile.ai[0];
            }
            if (flag == 1)
            {
                // 挥舞：先给一个正角速度甩出去，再按真近战攻速衰减；拖到进度 2 转收招
                rotVel *= (float)Math.Pow(0.875f, speedTrueMelee);
                if (Projectile.ai[0] == 1)
                {
                    rotVel = 0.64f * speedMelee;
                }
                if (Projectile.ai[0] > 1.3f)
                {
                    smearAlpha *= 0.86f;
                }
                Projectile.ai[0] += speedTrueMelee / 42f;
                if (Projectile.ai[0] >= 2)
                {
                    flag = 3;
                    Projectile.ai[0] = 2;
                    // 松开左键就立刻收招（频道式）
                    if (Main.myPlayer == Projectile.owner && !Main.mouseLeft)
                    {
                        Projectile.Kill();
                        return;
                    }
                }
                if (Projectile.ai[0] < 1.56f)
                {
                    SpawnRunes(3);
                }
            }
            if (flag == 4)
            {
                // 弹开过渡：命中后剑身被弹开，此间剑体从 1 胀到 1.5 倍
                rotVel *= (float)Math.Pow(0.9f, speedTrueMelee);
                Projectile.ai[0] += speedTrueMelee / 34f;
                if (Projectile.ai[0] >= 2)
                {
                    Projectile.ai[0] = 2;
                    flag = 2;
                }
                scaleE = 1 + Parabola(0.5f * (Projectile.ai[0] - 1), 1f) * 0.5f;
            }
            if (flag == 2)
            {
                // 多段斩：重置无敌帧、把冷却压到 8 帧，好让一次挥舞多次命中
                if (Projectile.ai[0] == 2)
                {
                    smearAlpha = 1;
                    rotVel = 0.6f * speedMelee;
                    Projectile.ResetLocalNPCHitImmunity();
                    Projectile.localNPCHitCooldown = 8;
                }
                if (Projectile.ai[0] > 2.5f)
                {
                    smearAlpha *= 0.86f;
                }
                if (Projectile.ai[0] > 2.4f)
                {
                    rotVel *= (float)Math.Pow(0.87f, speedTrueMelee);
                }
                Projectile.ai[0] += speedTrueMelee / 60f;
                // localAI[2] 兼作「是否走过多段斩路线」的标记，也是斩击节奏音的冷却计时器
                if (Projectile.localAI[2] <= 0 && Projectile.ai[0] < 2.7f)
                {
                    SoundEngine.PlaySound((Main.rand.Next(1, 3) == 1 ? CalamityDemutationSounds.RuneSongSwing1 : CalamityDemutationSounds.RuneSongSwing2)
                        with { Pitch = Main.rand.NextFloat(2.4f, 2.8f) - 1f, Volume = 0.5f }, Projectile.Center);
                    Projectile.localAI[2] = 1f;
                }
                Projectile.localAI[2] -= speedTrueMelee / 14f;
                if (Projectile.ai[0] >= 3)
                {
                    Projectile.Kill();
                    return;
                }
                if (Projectile.ai[0] < 2.6f)
                {
                    SpawnRunes(3);
                }
            }
            if (smearAlpha < 0.02f)
            {
                smearAlpha = 0;
            }
            if (flag == 3)
            {
                // 收招：旋转偏移归零（先比例后定速两段夹紧），进度推到 3 后放出收尾表现
                rotOffset = CDUtil.RotateTowardsAngle(rotOffset, 0, 0.08f, false);
                rotOffset = CDUtil.RotateTowardsAngle(rotOffset, 0, 0.01f, true);
                Projectile.ai[0] += speedMelee / 60f;
                if (Projectile.ai[0] >= 3)
                {
                    glow *= 0.7f;
                    // localAI[2] 仍为 0 → 说明没命中过（没走过弹开/多段斩），放符文脉冲束
                    if (Projectile.localAI[2] == 0)
                    {
                        glow = 2f;
                        SoundEngine.PlaySound(CalamityDemutationSounds.RuneSongBoltImpact with { Pitch = Main.rand.NextFloat(0.8f, 1.2f) - 1f }, Projectile.Center);
                        SoundEngine.PlaySound(CalamityDemutationSounds.FractalSwingHit with { Pitch = Main.rand.NextFloat(1.1f, 1.15f) - 1f }, Projectile.Center);
                        SoundEngine.PlaySound(CalamityDemutationSounds.FractalSwingHit with { Pitch = Main.rand.NextFloat(1.1f, 1.15f) - 1f }, Projectile.Center);
                        Vector2 boltDir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                        for (int i = 0; i < 128; i++)
                        {
                            RuneParticle rune = new RuneParticle();
                            DRKLoader.NewParticle(rune, Projectile.Center + RandomPointInCircle(12) + boltDir * 160,
                                boltDir * Main.rand.NextFloat(4, 80), Color.Aqua, Main.rand.NextFloat(0.8f, 1.4f));
                            rune.Configure(42);
                        }
                        if (Main.myPlayer == Projectile.owner)
                        {
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + Projectile.rotation.ToRotationVector2() * 180,
                                boltDir * 8, ModContent.ProjectileType<RuneBolt>(), (int)(Projectile.damage * 2.5f), Projectile.knockBack, Projectile.owner);
                            CalamityDemutation.FlashEffectStrength = 0.24f;
                        }
                    }
                    Projectile.localAI[2] += speedMelee / 10f;
                    if (Projectile.localAI[2] >= 1)
                    {
                        Projectile.Kill();
                        return;
                    }
                }
            }
            rotOffset += rotVel;
            StickToPlayer(player);
            Projectile.rotation += rotOffset * dir;
            player.direction = dir;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.None, Projectile.rotation - MathHelper.PiOver2);
        }
        /// <summary>命中时播命中音、在敌怪身上撒一圈天降星屑；挥舞阶段命中还会把剑弹开转入多段斩</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(CalamityDemutationSounds.RuneSongHit with { Pitch = Main.rand.NextFloat(1f, 1.4f) - 1f }, target.Center);
            SpawnHeavenSpark(target.Center, Main.rand.NextFloat(MathHelper.TwoPi), 1.2f, 1f, Color.LightBlue * 1.4f, 14);
            if (flag == 1)
            {
                // 挥舞阶段命中 → 弹开，转多段斩（音效与粒子再放一遍）
                SoundEngine.PlaySound(CalamityDemutationSounds.RuneSongHit with { Pitch = Main.rand.NextFloat(1f, 1.4f) - 1f }, target.Center);
                SpawnHeavenSpark(target.Center, Main.rand.NextFloat(MathHelper.TwoPi), 1.2f, 1f, Color.LightBlue * 1.4f, 14);
                Projectile.ai[0] = 1;
                flag = 4;
                rotVel = -0.4f;
                smearAlpha = 0;
                if (Main.netMode != NetmodeID.SinglePlayer && Main.myPlayer == Projectile.owner)
                {
                    NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, Projectile.whoAmI);
                }
            }
            if (soulDisorderBuffType >= 0)
            {
                target.AddBuff(soulDisorderBuffType, 200);
            }
        }
        /// <summary>联机同步旋转速度与拖尾透明度：两者都由本端按攻速累加，不传别的端会散架</summary>
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(rotVel);
            writer.Write(smearAlpha);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            rotVel = reader.ReadSingle();
            smearAlpha = reader.ReadSingle();
        }
        /// <summary>命中判定是从剑心沿朝向伸出的一条线段，不是碰撞箱自带的圆形判定</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CDUtil.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * Length * Projectile.scale * scaleE, targetHitbox, 100);
        }
        public override void CutTiles()
        {
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.rotation.ToRotationVector2() * Length * Projectile.scale * scaleE, 40, DelegateMethods.CutTiles);
        }
        /// <summary>自绘：先画剑体本体与辉光，再叠一张圆形烟拖尾，最后还原默认批次</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D glowTexture = ModContent.Request<Texture2D>(Texture + "Glow").Value;
            Texture2D smear = ModContent.Request<Texture2D>(SmearTexture).Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition + Projectile.rotation.ToRotationVector2() * 20;
            float rot = Projectile.rotation + MathHelper.PiOver4;
            Main.spriteBatch.Draw(texture, drawPos, null, Color.White, rot, new Vector2(17, 81), Projectile.scale * scaleE * 2, SpriteEffects.None, 0);
            // CE 的 UseAdditive()：Immediate + Additive + LinearWrap + CullNone
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(glowTexture, drawPos, null, Color.Aqua * glow * 0.6f, rot, new Vector2(17, 81), Projectile.scale * scaleE * 2, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(smear, Projectile.Center - Main.screenPosition, null, new Color(120, 120, 190) * smearAlpha,
                Projectile.rotation + MathHelper.PiOver4 * 3 + -0.6f * dir, smear.Size() / 2f, 3.16f * Projectile.scale * scaleE, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        /// <summary>
        /// 把剑体钉在玩家身上，朝向跟着鼠标走（CE 的 StickToPlayer 无参重载）。
        /// 鼠标取跨端可见的 <c>GetMouseWorld</c>——这个弹幕在所有端都会跑 AI，直读 Main.MouseWorld
        /// 会让别端看到的挥砍方向是本机鼠标的方向。
        /// </summary>
        private void StickToPlayer(Player owner)
        {
            Projectile.Center = owner.MountedCenter + owner.gfxOffY * Vector2.UnitY;
            Vector2 mouseWorld = owner.GetModPlayer<CalamityDemutationPlayer>().GetMouseWorld();
            Projectile.rotation = (mouseWorld - Projectile.Center).ToRotation();
            Projectile.velocity = Projectile.rotation.ToRotationVector2() * owner.HeldItem.shootSpeed;
            owner.heldProj = Projectile.whoAmI;
        }
        /// <summary>沿剑身方向撒符文粒子（CE 的 PRT_RuneParticle，寿命 38 帧）</summary>
        private void SpawnRunes(int count)
        {
            for (int i = 0; i < count; i++)
            {
                RuneParticle rune = new RuneParticle();
                DRKLoader.NewParticle(rune, Projectile.Center + Projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(40, Length * scaleE),
                    RandomPointInCircle(2), Color.LightBlue, 1f);
                rune.Configure(38);
            }
        }
        /// <summary>CalamityEntropy.SpawnHeavenSpark 的等价实现：沿指定方向的正反两侧各铺 53 颗天降星屑</summary>
        private static void SpawnHeavenSpark(Vector2 pos, float rot, float length, float scale, Color color, int lifeTime)
        {
            Vector2 direction = rot.ToRotationVector2();
            for (int j = 0; j < 53; j++)
            {
                SpawnHeavenStar(pos, direction * (0.1f + j * 0.34f) * length, direction.ToRotation(), scale, color, lifeTime);
                SpawnHeavenStar(pos, direction * -(0.1f + j * 0.34f) * length, (-direction).ToRotation(), scale, color, lifeTime);
            }
        }
        /// <summary>铺一颗天降星屑；朝向取所在那一侧的飞行方向（CE 的 PRT_HeavenfallStar 也是这么传的）</summary>
        private static void SpawnHeavenStar(Vector2 pos, Vector2 velocity, float rotation, float scale, Color color, int lifetime)
        {
            HeavenfallStarCal star = new HeavenfallStarCal();
            DRKLoader.NewParticle(star, pos, velocity, color, Main.rand.NextFloat(0.6f, 1.3f) * scale);
            star.Configure(rotation, lifetime);
        }
        /// <summary>CEUtils.Parabola 的等价实现：开口向下的抛物线，t=0.5 时取到 height</summary>
        private static float Parabola(float t, float height) => 4f * height * t * (1f - t);
        /// <summary>CEUtils.randomPointInCircle 的等价实现：随机角度 × [-r, r] 的随机半径</summary>
        private static Vector2 RandomPointInCircle(float r) => Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(-r, r);
    }
    /// <summary>
    /// 符文脉冲束（RuneBolt，移植自 CalamityEntropy）：符文之歌没命中敌人时的收招，会朝光标方向甩出这道束。
    /// 本体是一道随时间收束的光带，<c>ai[1]</c> 管粗细（前 5 帧变粗、之后线性变细），
    /// 前 5 帧内参与判定，命中后伤害衰减到 85% 并继续穿透。
    /// <para>
    /// 与 CE 原版的差异：① 贴图指向本模组的 Assets/ExtraTextures/white（CE 的 CEUtils.WhiteTexPath 同物）；
    /// ② <c>PRT_LineCal</c> 换成 <see cref="DRK_Spark"/>——两者是同源物（都是灾厄 LineParticle 的搬运版，
    /// 逻辑逐条一致），仅底图不同（CE 用 DrainLineBloom，本模组用 StarProj）；
    /// ③ CE 的 <c>DrawGlow</c> 内联，辉光贴图取 Assets/ExtraTextures/Glow2；
    /// ④ CE 的 <c>SquashDust</c> 已一并移植为本模组的 <see cref="SquashDust"/>，用法与 CE 一致；
    /// ⑤ <c>CEUtils.LineThroughRect</c>/<c>getDistance</c> 内联；⑥ 减益同 <see cref="RuneSongHeld"/> 用 ArmorCrunch；
    /// ⑦ <c>UseBlendState</c>/<c>ExitShaderRegion</c> 按 CE 的实现逐字等价替换成 End + Begin（采样器与
    /// 光栅化状态都对齐 CE：<c>UseBlendState(blend, sampler)</c> 用的是 <c>RasterizerState.CullNone</c>）。
    /// </para>
    /// </summary>
    internal class RuneBolt:ModProjectile
    {
        /// <summary>光带的条纹贴图（CE 的 CEExtraAssets.Streak1）</summary>
        private const string StreakTexture = "CalamityDemutation/Assets/ExtraTextures/Streak1";
        /// <summary>叠在光带上的符文飘带贴图（CE 的 Assets/Extra/RuneRibbon2）</summary>
        private const string RuneRibbonTexture = "CalamityDemutation/Assets/ExtraTextures/RuneRibbon2";
        /// <summary>端点辉光贴图（CE 的 CEExtraAssets.Glow2）</summary>
        private const string BoltGlowTexture = "CalamityDemutation/Assets/ExtraTextures/Glow2";
        /// <summary>光带长度（像素）</summary>
        private const int LaserLength = 2400;
        /// <summary>占位贴图：光带实际用 streak/ribbon 两张图，本体只按白图走 TrivialDraw 以外的路径</summary>
        public override string Texture => "CalamityDemutation/Assets/ExtraTextures/white";
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 12;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 5;
            Projectile.ai[1] = 1;
            Projectile.ArmorPenetration = 36;
        }
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            // 前 5 帧变粗（绽开），之后按 1/8 的步长收细
            if (Projectile.ai[0] > 4)
            {
                Projectile.ai[1] -= 1 / 8f;
            }
            else
            {
                Projectile.ai[1] += 0.25f;
            }
            Projectile.ai[0]++;
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.netUpdate = true;
            }
        }
        public override bool ShouldUpdatePosition() => false;
        /// <summary>只在绽开的头 5 帧内判定，之后光带纯演出</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.ai[0] > 4)
            {
                return false;
            }
            float laserLength = Projectile.scale * LaserLength;
            return CDUtil.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.One) * laserLength, targetHitbox, 90);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(CalamityDemutationSounds.RuneBoltHit with { Pitch = Main.rand.NextFloat(1.4f, 1.8f) - 1f }, target.Center);
            // 沿束的方向撒一片拉丝火花
            for (int i = 0; i < 16; i++)
            {
                Vector2 sparkVelocity = Projectile.velocity.RotatedByRandom(0.2f) * Main.rand.NextFloat(0.5f, 1.8f);
                Vector2 pos = target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f) + Projectile.velocity * 1.2f;
                LineParticleCal spark = new LineParticleCal();
                DRKLoader.NewParticle(spark, pos, sparkVelocity, Main.rand.NextBool() ? Color.Aqua : Color.LightBlue, Main.rand.NextFloat(0.95f, 1.8f));
                spark.Configure(false, Main.rand.Next(20, 24));
            }
            // 命中在束的末端炸开一圈压扁光球尘（CE 的 SquashDust，已一并移植）
            for (int i = 0; i < 29; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Projectile.rotation.ToRotationVector2() * Vector2.Distance(target.Center, Projectile.Center),
                    ModContent.DustType<SquashDust>(), -Projectile.velocity);
                dust.scale = Main.rand.NextFloat(3f, 3.5f);
                dust.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(0.4f) * Main.rand.NextFloat(8, 36);
                dust.noGravity = true;
                dust.color = Color.LightBlue;
                dust.fadeIn = 2f;
            }
            Projectile.damage = (int)(Projectile.damage * 0.85f);   // 穿透时逐次衰减
            if (RuneSongHeld.soulDisorderBuffType >= 0)
            {
                target.AddBuff(RuneSongHeld.soulDisorderBuffType, 200);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D streak = ModContent.Request<Texture2D>(StreakTexture).Value;
            Texture2D ribbon = ModContent.Request<Texture2D>(RuneRibbonTexture).Value;
            Texture2D glow = ModContent.Request<Texture2D>(BoltGlowTexture).Value;
            // CE 的 UseBlendState(Additive, LinearWrap)：Immediate + Additive + LinearWrap + CullNone
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            // 端点辉光三层：白核、白内层、青色外层
            DrawGlow(glow, Projectile.Center, Color.White * Projectile.ai[1], 2f * Projectile.scale);
            DrawGlow(glow, Projectile.Center, Color.White * Projectile.ai[1], 1.6f * Projectile.scale);
            DrawGlow(glow, Projectile.Center, Color.Aqua * Projectile.ai[1], 4f * Projectile.scale);
            // 光带三层：两层滚动条纹（快慢不同）+ 一层符文飘带
            Main.spriteBatch.Draw(streak, Projectile.Center - Main.screenPosition,
                new Rectangle(-(int)(Main.GlobalTimeWrappedHourly * 900), 0, (int)(Projectile.scale * LaserLength), streak.Height),
                new Color(90, 90, 140), Projectile.rotation, new Vector2(0, streak.Height * 0.5f), new Vector2(1, Projectile.scale * Projectile.ai[1] * 0.37f), SpriteEffects.None, 0);
            Main.spriteBatch.Draw(streak, Projectile.Center - Main.screenPosition,
                new Rectangle(-(int)(Main.GlobalTimeWrappedHourly * 1400), 0, (int)(Projectile.scale * LaserLength), streak.Height),
                new Color(255, 255, 255), Projectile.rotation, new Vector2(0, streak.Height * 0.5f), new Vector2(1, Projectile.scale * Projectile.ai[1] * 0.22f), SpriteEffects.None, 0);
            Main.spriteBatch.Draw(ribbon, Projectile.Center - Main.screenPosition,
                new Rectangle(-(int)(Main.GlobalTimeWrappedHourly * 1400), 0, (int)(Projectile.scale * LaserLength), ribbon.Height),
                new Color(160, 160, 200), Projectile.rotation, new Vector2(0, ribbon.Height * 0.5f), new Vector2(1.2f, Projectile.scale * Projectile.ai[1] * 1f), SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        /// <summary>CEUtils.DrawGlow 的等价实现（setState 为假的那条路径，批次由调用方开好）</summary>
        private static void DrawGlow(Texture2D glow, Vector2 worldPos, Color color, float scale)
        {
            Main.spriteBatch.Draw(glow, worldPos - Main.screenPosition, null, color, 0, glow.Size() * 0.5f, scale * 0.4f, SpriteEffects.None, 0);
        }
    }
}
