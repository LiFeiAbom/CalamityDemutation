using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 元素球 - 元素方舟散射的自动追踪弹幕
    /// 本体不可见（使用隐形贴图），仅以彩虹粉尘表现，向附近敌人追踪飞行
    /// </summary>
    internal class ElementBall:ModProjectile
    {
        // 使用隐形贴图，视觉效果完全由粉尘承担
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 基础属性：8x8 全透明碰撞箱；友方、近战伤害、单次穿透；
        /// 每帧额外更新 2 次（弹速更快）、无视地形、存活 150 帧
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 2;    // 每帧额外更新 2 次，弹速更快
            Projectile.tileCollide = false; // 无视地形
            Projectile.timeLeft = 150;
        }
        /// <summary>
        /// AI：生成当前派对色（彩虹）粉尘作为弹幕外观，并向 1600 像素内最近的敌人平滑追踪
        /// </summary>
        public override void AI()
        {
            // 生成彩虹色粉尘作为弹幕外观
            int rainbowDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.RainbowTorch, Projectile.direction * 2, 0f, 150, new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB), 1f);
            Main.dust[rainbowDust].noGravity = true;
            Main.dust[rainbowDust].velocity *= 0f;
            // 向 1600 像素内的敌人追踪（转向速度 24，惯性 20）
            CDUtil.HomeInNPC(Projectile, 1600f, 24f, 20f);
        }
    }
}
