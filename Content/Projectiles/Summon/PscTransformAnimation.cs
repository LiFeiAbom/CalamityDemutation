using System;
using CalamityDemutation.Content.Items.Accessories.Comprehensive;
using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Particles;
using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 亵渎之魂水晶的变身动画弹幕（移植自灾厄 2.2.2 的 PscTransformAnimation）：
    /// 首次装备水晶时由 ProfanedSoulCrystal.UpdateAccessory 生成，存活 120 帧（ProfanedSoulCrystal.maxPscAnimTime）。
    /// 期间把自己锁在主人身上、每帧刷新主人侧的变身计时 profanedCrystalAnim，并向四周喷神圣粉尘、
    /// 定期从远处召回岩石弹幕（PscTransformRocks）把主人包裹起来；最后一帧重置计时并放脉冲环 + 音效。
    /// 本身无贴图（InvisibleProj）也无伤害；主人卸下水晶时立刻消散并复位计时。
    /// </summary>
    internal class PscTransformAnimation:ModProjectile
    {
        /// <summary>弹幕主人：跟随、粉尘与岩石都以主人为参照</summary>
        public Player Owner => Main.player[Projectile.owner];
        /// <summary>无贴图弹幕，使用工程通用的 InvisibleProj</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>注册 4 帧动画（本弹幕不可见，仅为对齐原版），并禁用液体扭曲</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.NoLiquidDistortion[Type] = true;
        }
        /// <summary>基础属性：26x26 碰撞箱、忽略水面、不碰地形、存活 120 帧、全透明</summary>
        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = ProfanedSoulCrystal.maxPscAnimTime;
            Projectile.alpha = 255;
        }
        /// <summary>
        /// AI：发光并跟随主人，把剩余时间写进主人的 profanedCrystalAnim；
        /// 主人卸下水晶则消散复位；未到最后一帧时按进度喷尘并召回岩石，最后一帧放脉冲环与音效
        /// </summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.3f, 0.225f, 0f);
            CalamityDemutationPlayer modPlayer = Owner.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.profanedCrystalAnim = Projectile.timeLeft;
            Projectile.Center = Owner.Center;
            if (!modPlayer.profanedCrystal)
            {
                modPlayer.profanedCrystalAnim = -1;
                Projectile.active = false;
                return;
            }
            if (Projectile.timeLeft > 1)
            {
                // 动画进度 0→1 的缓动：粉尘数量 1→3 颗、外扩半径 40→75、尺寸 0.45→1
                float progress = ((float)ProfanedSoulCrystal.maxPscAnimTime - modPlayer.profanedCrystalAnim) / ProfanedSoulCrystal.maxPscAnimTime;
                int dustCount = (int)Math.Round(MathHelper.SmoothStep(1f, 3f, progress));
                float outwardness = MathHelper.SmoothStep(40f, 75f, progress);
                float dustScale = MathHelper.Lerp(0.45f, 1f, progress);
                int[] validRockTypes = new int[] { 1, 3, 4, 5, 6 };
                int projectileCount = Owner.ownedProjectileCounts[ModContent.ProjectileType<PscTransformRocks>()];
                bool shouldStickAround = projectileCount <= 20;   // 场上岩石不多时让新岩石留下，否则淡出
                for (int i = 0; i < dustCount; i++)
                {
                    Vector2 spawnPosition = Projectile.Center + Main.rand.NextVector2Unit() * outwardness * Main.rand.NextFloat(0.75f, 1.1f);
                    Vector2 dustVelocity = (Projectile.Center - spawnPosition) * 0.085f + Owner.velocity;
                    Dust dust = Dust.NewDustPerfect(spawnPosition, MiniGuardianHealer.HolyDustType(!Main.dayTime));
                    dust.velocity = dustVelocity;
                    dust.scale = dustScale * Main.rand.NextFloat(0.75f, 1.15f);
                    dust.noGravity = true;
                    dust.noLight = true;
                    if (Projectile.timeLeft % 3 == 0)
                    {
                        // 每 3 帧从主人身上拉出一条流动拖线（对齐 2.2.2 的 ManaDrainStreak）：
                        // 起点取 250 半径圆周上的随机点、终点 50，颜色白天橙 / 夜晚碧蓝、末端提亮 1.5 倍，寿命 20~30 帧
                        if (!Main.dedServ)
                        {
                            Vector2 streakStart = Main.rand.NextVector2CircularEdge(250f, 250f);
                            Color streakColor = Main.dayTime ? Color.Orange : Color.Aquamarine;
                            DRKLoader.AddParticle(new ManaDrainStreak(Owner, Main.rand.NextFloat(0.3f, 0.6f), streakStart, 50f, streakColor, streakColor * 1.5f, Main.rand.Next(20, 31), Owner.Center));
                        }
                        if (Owner.whoAmI == Main.myPlayer)
                        {
                            spawnPosition = Owner.Center;
                            spawnPosition.X += Main.rand.NextFloat(-500f, 500f);
                            spawnPosition.Y += Main.rand.NextFloat(-500f, 500f);
                            dustVelocity = (Projectile.Center - spawnPosition) * 0.085f + Owner.velocity;
                            dustVelocity.Normalize();
                            dustVelocity *= 16f;
                            int rockType = validRockTypes[Main.rand.Next(0, validRockTypes.Length)];
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPosition, dustVelocity, ModContent.ProjectileType<PscTransformRocks>(), 0, 0f, Projectile.owner, shouldStickAround ? 1f : 0f, rockType);
                        }
                    }
                }
            }
            else
            {
                modPlayer.profanedCrystalAnim = -1;
                // 原灾厄此处 owner.SetScreenshake(5f)，本工程没有屏幕震动 API，略
                // 原灾厄此处调 DetermineTransformationEligibility(owner)：本工程四态每帧在
                // PostUpdateMiscEffects 重算，无需在此刷新
                if (!Main.dedServ)
                {
                    // 原注释：该粒子的 alpha 是反的，故此处显式补 255
                    Color color = ProfanedSoulCrystal.GetColorForPsc(modPlayer.pscState, Main.dayTime) with { A = 255 };
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Owner.Center, Vector2.Zero, color, Vector2.One, 0f, 0f, 2.5f, 75));
                }
                OnKill(1);
            }
        }
        /// <summary>不造成伤害</summary>
        public override bool? CanDamage() => false;
        /// <summary>
        /// 消亡时放结束音效（原为灾厄 Providence.SpawnSound，本工程改用原版 SoundID.Item29）并喷 20 颗神圣尘
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item29, Projectile.position);
            for (int i = 0; i < 20; i++)
            {
                Vector2 dustPos = new Vector2(Owner.Center.X + Main.rand.NextFloat(-10, 10), Owner.Center.Y + Main.rand.NextFloat(-10, 10));
                Vector2 velocity = (Owner.Center - dustPos).SafeNormalize(Vector2.Zero);
                velocity *= Main.dayTime ? 3f : 6.9f;
                var dust = Dust.NewDustPerfect(Owner.Center, MiniGuardianHealer.HolyDustType(!Main.dayTime), velocity, 0, default(Color), 2f);
                if (!Main.dayTime)
                    dust.noGravity = true;
            }
            Projectile.active = false;
        }
    }
}
