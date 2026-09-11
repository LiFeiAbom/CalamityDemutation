using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 沙之矢：由沙之老婆（SandyWaifu）发射的不可见弹丸，命中或超时后留下一个沙之印记（SandMark）。
    /// </summary>
    internal class SandBolt:ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 基础属性：友方、单次穿透，本身不绘制贴图（仅靠尾迹粒子表现）。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.light = 1f;
            Projectile.timeLeft = 300;
        }
        /// <summary>
        /// 弹幕 AI：沿飞行路径抛洒沙尘粒子形成尾迹。
        /// </summary>
        public override void AI()
        {
            Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.Sand, Projectile.velocity.X * 0.1f, Projectile.velocity.Y * 0.1f);
        }
        /// <summary>
        /// 命中/超时后仅在服务端（owner 为本地玩家时）生成沙之印记。
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y, 0f, 0f, ModContent.ProjectileType<SandMark>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, 0f);
            }
        }
    }
}
