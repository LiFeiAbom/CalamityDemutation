using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 审判光束命中的飞散白球（移植自灾厄大修 0.4.0.1.3 的 OrderbringerWhiteOrbs）：
    /// 本体隐形，仅靠彩虹照明与尘表现。命中冷却走"全局共享"口径
    /// （usesIDStaticNPCImmunity + localNPCHitCooldown = -1）：多颗同时打同一敌人只结算一次。
    /// </summary>
    internal class OrderbringerWhiteOrbs : ModProjectile
    {
        /// <summary>本体隐形：视觉全部由 AI 中的彩虹尘表现</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 基础属性：8×8、友方近战、每帧额外更新 2 次、无限穿透、存活 60 帧、不撞地形；
        /// 命中冷却为全局共享（原版大修口径）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.MaxUpdates = 2;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 60;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.tileCollide = false;
        }
        /// <summary>
        /// AI：按迪斯科色照明，并每帧喷 2 颗彩虹火把尘（尘色取当前迪斯科色）
        /// </summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Main.DiscoColor.ToVector3());
            for (int i = 0; i < 2; i++)
            {
                int rainbow = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.RainbowTorch, 0f, 0f, 100, new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB), 1f);
                Main.dust[rainbow].noGravity = true;
                Main.dust[rainbow].velocity *= 0.5f;
                Main.dust[rainbow].velocity += Projectile.velocity * 0.1f;
            }
        }
    }
}
