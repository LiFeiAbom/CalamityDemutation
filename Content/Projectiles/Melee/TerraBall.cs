using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 大地弹 - 真·远古方舟召唤的绿色追踪弹幕
    /// 自动追踪敌人，命中/消失时产生大范围的绿色光尘与星形残片
    /// </summary>
    internal class TerraBall:ModProjectile
    {
        /// <summary>
        /// 静态属性：预留 6 格残影缓存（配合 PreDraw 的残影绘制）
        /// </summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6; // 残影数量
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }
        /// <summary>
        /// 基础属性：20x20 碰撞箱；友方、近战伤害、单次穿透、存活 180 帧
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
        }
        /// <summary>
        /// AI：持续生成绿色妖精粉尘拖尾；周期性播放闪烁音效；概率生成星形残片；朝 1600 像素内敌人追踪
        /// </summary>
        public override void AI()
        {
            // 持续生成绿色妖精粉尘作拖尾
            int num469 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GreenFairy, 0f, 0f, 100, default, 0.8f);
            Main.dust[num469].noGravity = true;
            // 周期性播放闪烁音效
            if (Projectile.soundDelay == 0)
            {
                Projectile.soundDelay = 20 + Main.rand.Next(40);
                if (Main.rand.NextBool(5))
                {
                    SoundEngine.PlaySound(SoundID.Item9, Projectile.position);
                }
            }
            // 1/48 概率生成星形残片
            if (Main.rand.NextBool(48))
            {
                int num60 = Gore.NewGore(Projectile.GetSource_FromThis(), Projectile.Center, new Vector2(Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f), 16, 1f);
                Main.gore[num60].velocity *= 0.66f;
                Main.gore[num60].velocity += Projectile.velocity * 0.3f;
            }
            // 向 1600 像素内的敌人追踪
            CDUtil.HomeInNPC(Projectile, 1600f, 30f, 20f, null, !Projectile.tileCollide);
        }
        /// <summary>
        /// 消亡：把碰撞箱临时扩大到 50x50，分三轮生成大范围绿色粉尘（受重力/无重力）与星形残片，模拟爆裂消散
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            // 先将碰撞箱放大到 50x50，使爆炸特效范围更大
            Projectile.position.X = Projectile.position.X + Projectile.width / 2;
            Projectile.position.Y = Projectile.position.Y + Projectile.height / 2;
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.position.X = Projectile.position.X - Projectile.width / 2;
            Projectile.position.Y = Projectile.position.Y - Projectile.height / 2;
            // 第一轮：5 个受重力影响的绿色粉尘（部分缩小并渐入）
            for (int num621 = 0; num621 < 5; num621++)
            {
                int num622 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.GreenFairy, 0f, 0f, 100, default, 1.2f);
                Main.dust[num622].velocity *= 3f;
                if (Main.rand.NextBool(2))
                {
                    Main.dust[num622].scale = 0.5f;
                    Main.dust[num622].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
            }
            // 第二轮：5 个快速扩散的无重力粉尘（成对生成大/小尺度）
            for (int num623 = 0; num623 < 5; num623++)
            {
                int num624 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.GreenFairy, 0f, 0f, 100, default, 1.7f);
                Main.dust[num624].noGravity = true;
                Main.dust[num624].velocity *= 5f;
                num624 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.GreenFairy, 0f, 0f, 100, default, 1f);
                Main.dust[num624].velocity *= 2f;
            }
            // 第三轮：3 个星形残片
            for (int num480 = 0; num480 < 3; num480++)
            {
                Gore.NewGore(Projectile.GetSource_FromThis(), Projectile.position, new Vector2(Projectile.velocity.X * 0.05f, Projectile.velocity.Y * 0.05f), Main.rand.Next(16, 18), 1f);
            }
        }
        /// <summary>
        /// 自定义绘制：绘制淡灰色残影拖尾
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimages(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
        /// <summary>
        /// 浅灰色着色，透明度跟随 alpha
        /// </summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(200, 200, 200, Projectile.alpha);
        }
    }
}
