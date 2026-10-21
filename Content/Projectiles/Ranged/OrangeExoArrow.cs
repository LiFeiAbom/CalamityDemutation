using CalamityDemutation.Particles;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 橙色星流箭（OrangeExoArrow） - 天堂之风把木箭转化出的四色星流箭之一，＝旧版支线的
    /// <c>Content/Projectiles/ExoLore/ExoArrowOrangeExoLore</c>（本工程常驻 Lore 模式，故不留非 Lore 版）。
    /// <para>
    /// 定位是"落点炸出火焰"：判定 5×5、穿透 1、额外更新 3 帧、寿命 300 帧；
    /// **Lore 分支让它一路追踪敌人**（`CDUtil.HomeInOnNPC`，1000 像素内 / 16 / 18），
    /// 并且出生帧先把速度乘 0.7（"出场减速再追"的观感，照源）。
    /// </para>
    /// <para>
    /// 消亡：先把碰撞箱撑到 188（`CDUtil.ExpandHitboxBy`）、重置穿透再打一次范围伤害，播 `Item14` 爆炸音，
    /// 再炸出本工程既有的 <c>DetailedExplosion</c> + <c>GenericSparkle</c> 两枚粒子与三组尘。
    /// 源这一处用的是灾厄的 <c>SparkleParticle</c>——与工程既有的 <c>GenericSparkle</c> 属同一族
    /// （同款 33×33 星形贴图 + 200×200 光晕底、同按寿命取正弦亮度、同 0.95 阻尼），故沿用既有件不再补一份。
    /// 非主人端调用 <c>Projectile.Damage()</c> 会被引擎直接跳过，无需额外判据。
    /// </para>
    /// </summary>
    internal class OrangeExoArrow : ModProjectile
    {
        /// <summary>光束渐隐总长（像素），照源 <c>DrawBeam(40f, 1.5f, …)</c> 的第一个参数</summary>
        private const float BeamLength = 40f;
        /// <summary>相邻两次盖印的间距（像素），同时是 <c>localAI[0]</c> 每帧的增长量（照源第二个参数）</summary>
        private const float BeamStep = 1.5f;
        /// <summary>爆炸整体放大倍率（照源 <c>SizeBonus = 2</c>）</summary>
        private const float SizeBonus = 2f;
        /// <summary>配色分支（出生帧随机）：true 用纯橙、false 用橙红——影响爆炸粒子与火焰尘的颜色</summary>
        private bool colorStyle;
        /// <summary>爆炸尺寸的随机系数（照源：0.95~1.05）</summary>
        private float sizeVariance;
        /// <summary>共用的光束贴图（见 <see cref="TealExoArrow"/> 的说明）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/LaserProj";
        /// <summary>基础属性：5×5、穿透 1、额外更新 3 帧、寿命 300 帧、射手伤害类型（照源）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 5;
            Projectile.height = 5;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 3;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.arrow = true;
        }
        /// <summary>
        /// AI：出生帧抽定配色与尺寸系数、速度乘 0.7；随后淡入 + 光束长度增长到 <see cref="BeamLength"/>
        /// （<c>ai[1]</c> 非 0 时反向收缩、缩到 0 自毁）；末尾照 Lore 分支追踪 1000 像素内最近的敌人
        /// </summary>
        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                sizeVariance = Main.rand.NextFloat(0.95f, 1.05f);
                colorStyle = Main.rand.NextBool();
                Projectile.velocity *= 0.7f;
            }

            if (Projectile.alpha > 0)
                Projectile.alpha -= 25;
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;

            if (Projectile.ai[1] == 0f)
            {
                Projectile.localAI[0] += BeamStep;
                if (Projectile.localAI[0] > BeamLength)
                    Projectile.localAI[0] = BeamLength;
            }
            else
            {
                Projectile.localAI[0] -= BeamStep;
                if (Projectile.localAI[0] <= 0f)
                    Projectile.Kill();
            }

            // Lore 分支专属：追踪（照源 CalamityUtils.HomeInOnNPC(Projectile, !tileCollide, 1000f, 16f, 18f)）
            CDUtil.HomeInOnNPC(Projectile, !Projectile.tileCollide, 1000f, 16f, 18f);
        }
        /// <summary>橙色（照源 <c>new Color(250, 100, 0)</c>）</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(250, 100, 0, Projectile.alpha);
        /// <summary>自绘光束（顶掉默认绘制）</summary>
        public override bool PreDraw(ref Color lightColor) => Projectile.DrawBeam(BeamLength, BeamStep, lightColor);
        /// <summary>命中敌人：挂整套星云系减益（照源 <c>target.ExoDebuffs()</c>）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.ExoDebuffs();
        /// <summary>PvP 命中：同上（源的 <c>OnHitPvp</c> 在 1.4.4 起并入此钩子）</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.ExoDebuffs();
        /// <summary>消亡：撑大判定补打一次范围伤害 + 爆炸音 + 两枚粒子 + 三组火焰尘（照源）</summary>
        public override void OnKill(int timeLeft)
        {
            Projectile.ExpandHitboxBy(188);
            Projectile.maxPenetrate = -1;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 0;
            Projectile.Damage();
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            Color mainColor = colorStyle ? Color.Orange : Color.OrangeRed;
            Vector2 offset = Main.rand.NextVector2Circular(15f, 15f);

            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(Projectile.Center + offset, Vector2.Zero,
                mainColor * 0.9f, Vector2.One, Main.rand.NextFloat(-5f, 5f), 0f, 0.28f * sizeVariance * SizeBonus, 10));
            GeneralParticleHandler.SpawnParticle(new GenericSparkle(Projectile.Center + offset, Vector2.Zero,
                Color.White, mainColor, 2.5f * (SizeBonus * 0.3f), 7, 0f, 2f));

            // 两组主尘（照源：两组各 9 颗，第二组 1/2 概率）
            for (int k = 0; k < 9; k++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool() ? DustID.AmberBolt : DustID.GemTopaz,
                    new Vector2(4f, 4f).RotatedByRandom(100f) * Main.rand.NextFloat(0.5f, 1.5f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.7f, 1.25f);
                dust.alpha = 235;
                if (Main.rand.NextBool())
                {
                    Dust dust2 = Dust.NewDustPerfect(Projectile.Center, DustID.SteampunkSteam,
                        new Vector2(3f, 3f).RotatedByRandom(100f) * Main.rand.NextFloat(0.5f, 1.5f));
                    dust2.noGravity = true;
                    dust2.scale = Main.rand.NextFloat(0.8f, 1.5f);
                    dust2.alpha = 70;
                }
            }
            // 带色的飘散尘（照源：3 颗，受重力、带上抛初速）
            for (int k = 0; k < 3; k++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.FireworksRGB,
                    new Vector2(4f, 4f).RotatedByRandom(100f) * Main.rand.NextFloat(0.5f, 1.5f) + new Vector2(0f, -3f));
                dust.noGravity = false;
                dust.scale = Main.rand.NextFloat(0.85f, 1f);
                dust.color = mainColor;
            }
        }
    }
}
