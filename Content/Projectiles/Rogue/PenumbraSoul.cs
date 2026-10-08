using CalamityDemutation.Content.Items.Weapons.Rogue;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 半影炸出的暗影魂（照灾厄 2.0 的 <c>PenumbraSoul</c>）：
    /// 18×18 判定、存活 150 帧、穿透 2、同一敌人每 10 帧可再吃一次、不撞物块、半透明（alpha 80）。
    /// </summary>
    /// <remarks>
    /// 每帧沿速度方向撒一颗暗影火尘（颜色 (38,30,43)、去重力）；<c>ai[1] == 0</c> 时在 200 像素内归航
    /// （速度 = 半影弹速 × 1.5 = 12、惯性 35，照源调 <c>CalamityUtils.HomeInOnNPC</c>，
    /// 本工程用同签名的 <c>CDUtil.HomeInOnNPC</c>）。
    /// <para>
    /// 命中后立刻"急刹"（速度 ×0.4）、关掉归航（<c>ai[1] = 1</c>）、每次命中淡出 20 点不透明度，
    /// 并炸出 6~10 颗暗影火尘；消失时再炸 30~40 颗。
    /// </para>
    /// </remarks>
    internal class PenumbraSoul : ModProjectile
    {
        /// <summary>归航索敌半径（照源）</summary>
        private const float HomingRange = 200f;
        /// <summary>归航惯性（照源；数值越大转向越钝）</summary>
        private const float HomingInertia = 35f;
        /// <summary>暗影火尘色（源的固定色）</summary>
        private static readonly Color ShadowColor = new Color(38, 30, 43);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }
        /// <summary>18×18、存活 150 帧、穿透 2、同一敌人每 10 帧一次、不撞物块不撞水、alpha 80</summary>
        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.timeLeft = 150;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 80;
            Projectile.penetrate = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.extraUpdates = 1;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>朝向速度 + 绘制偏移修正 + 沿路撒尘 + 未命中时归航</summary>
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            DrawOffsetX = 1;
            DrawOriginOffsetY = 4;
            int dust = Dust.NewDust(Projectile.position - Projectile.velocity, Projectile.width, Projectile.height, DustID.Shadowflame, 0f, 0f, 0, ShadowColor);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity += Projectile.velocity * 0.8f;
            if (Projectile.ai[0] > 0f)
                Projectile.ai[0] -= 1f;
            if (Projectile.ai[1] == 0f)
                CDUtil.HomeInOnNPC(Projectile, true, HomingRange, Penumbra.ShootSpeed * 1.5f, HomingInertia);
        }
        /// <summary>拖影（照源调灾厄的 DrawAfterimagesCentered，本工程用同名 CDUtil 版本）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
        /// <summary>命中：急刹 + 关归航 + 淡出 20 点 + 炸 6~10 颗暗影火尘</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.velocity *= 0.4f;
            Projectile.ai[1] = 1f;
            Projectile.alpha += 20;
            if (Projectile.alpha > 255)
                Projectile.alpha = 255;
            int onHitDust = Main.rand.Next(6, 11);
            for (int i = 0; i < onHitDust; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, 0f, 0f, 0, ShadowColor);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= Main.rand.NextFloat(1.4f, 2.6f);
                Main.dust[dust].scale = Main.rand.NextFloat(1.0f, 1.8f);
            }
        }
        /// <summary>消失：炸 30~40 颗暗影火尘</summary>
        public override void OnKill(int timeLeft)
        {
            int killDust = Main.rand.Next(30, 41);
            for (int i = 0; i < killDust; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, 0f, 0f, 0, ShadowColor);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= Main.rand.NextFloat(2.0f, 3.1f);
                Main.dust[dust].scale = Main.rand.NextFloat(1.0f, 1.8f);
            }
        }
    }
}
