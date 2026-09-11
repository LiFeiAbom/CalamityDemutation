using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Magic
{
    /// <summary>
    /// 光环之雨（AuraRain）- 魔法武器生成的隐形"雨滴"弹幕
    /// 贴图不可见，仅以恶魔（暗影）粉尘表现；设置雨云 AI 作为模板但未调用 base.AI()，
    /// 实际行为仅保持生成时初速度直线飞行，300 帧（约 5 秒）内持续喷尘后自然消散。
    /// </summary>
    internal class AuraRain:ModProjectile
    {
        // 使用隐形贴图，视觉效果完全由粉尘承担
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 基础属性：2x2 微小碰撞箱，友方、无限穿透、撞实心块销毁（原版默认行为，无反弹）；魔法伤害、存活 300 帧、全透明并放大 1.1 倍；每帧额外更新 1 次
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.aiStyle = ProjAIStyleID.RainCloud;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 300;
            Projectile.alpha = 255;
            Projectile.scale = 1.1f;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.extraUpdates = 1;
            AIType = ProjectileID.RainFriendly;
        }
        /// <summary>
        /// AI：每帧在自身位置生成一粒静止的恶魔粉尘，构成"雨滴下坠"的视觉假象；无实际运动/攻击逻辑
        /// </summary>
        public override void AI()
        {
            // 在弹幕位置生成恶魔粉尘，速度清零并沿速度反向偏移 1/5，形成"雨滴拖尾"的视觉效果
            Dust dust4 = Main.dust[Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Demonite, Projectile.velocity.X, Projectile.velocity.Y, 100, default(Color), 1f)];
            dust4.velocity = Vector2.Zero;
            dust4.position -= Projectile.velocity / 5f;
            dust4.noGravity = true;
            dust4.scale = 0.8f;
            dust4.noLight = true;
        }
    }
}
