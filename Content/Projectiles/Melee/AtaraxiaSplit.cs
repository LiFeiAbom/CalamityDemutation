using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 禅心剑 · 针叶弹（AtaraxiaSplit）—— 侧弹消亡时放射状分裂出的 6 枚细长碎片
    /// （照搬灾厄 2.0.4 的 <c>AtaraxiaSplit</c>）。
    /// 贴图只有 18×10，是本武器整套弹幕里最细最短的一枚，「针叶」之名由此而来。
    /// 行为：每帧指数减速（×0.93）并淡出（alpha +5），25 帧即消失；靠 <c>usesIDStaticNPCImmunity</c> + 6 帧冷却
    /// 让多枚碎片各自独立命中（灾厄注释写的 "ignore iframes" 实际指的是这个，而不是穿透）。
    /// </summary>
    internal class AtaraxiaSplit : ModProjectile
    {
        /// <summary>基础属性：8×8、友方近战、穿透无限、每帧更新 3 格（extraUpdates 2）、不碰撞物块、存活 25 帧、静态免疫 6 帧</summary>
        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 2;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 6;
            Projectile.timeLeft = 25;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 0;
        }
        /// <summary>每帧：摆正旋转角、按 ×0.93 指数减速并 +5 淡出，光强随透明度等比衰减，并按剩余透明度撒尘（越淡越稀）</summary>
        public override void AI()
        {
            DrawOffsetX = -5;
            DrawOriginOffsetY = -1;
            DrawOriginOffsetX = 0;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 0.93f;
            Projectile.alpha += 5;
            float lightFactor = (255f - (float)Projectile.alpha) / 255f;
            Lighting.AddLight(Projectile.Center, 0.3f * lightFactor, 0.05f * lightFactor, 0.2f * lightFactor);
            if (Main.rand.Next(256) > Projectile.alpha - 60)
            {
                int idx = Dust.NewDust(Projectile.Center, 1, 1, DustID.UndergroundHallowedEnemies);
                Main.dust[idx].position = Projectile.Center - Projectile.velocity * 0.7f;
                Main.dust[idx].noGravity = true;
                Main.dust[idx].velocity *= 0.3f;
                Main.dust[idx].velocity += Projectile.velocity * 0.4f;
                Main.dust[idx].scale = Main.rand.NextFloat(0.5f, 1.0f);
                Main.dust[idx].alpha = Main.rand.Next(80, 200);
            }
        }
    }
}
