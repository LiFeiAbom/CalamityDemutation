using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 火花：受重力下坠并反弹的近战弹幕，飞行时洒落光尘
    /// </summary>
    internal class Spark:ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 基础属性：6x12 碰撞箱；友方、无限穿透（穿透为 -1）、近战伤害、存活 120 帧；
        /// 每个敌人独立 10 帧命中冷却
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.DamageType = DamageClass.Melee;
        }
        /// <summary>
        /// 重力下落与反弹逻辑，并持续生成光尘拖尾
        /// </summary>
        public override void AI()
        {
            // 照抄经典版的 NaN 自愈分支：NaN × 负数仍是 NaN，无法纠偏（该分支实际无效，属反编译遗留）
            if (Projectile.velocity.X != Projectile.velocity.X)
            {
                Projectile.velocity.X = Projectile.velocity.X * -0.1f;
            }
            if (Projectile.velocity.X != Projectile.velocity.X)
            {
                Projectile.velocity.X = Projectile.velocity.X * -0.5f;
            }
            if (Projectile.velocity.Y != Projectile.velocity.Y && Projectile.velocity.Y > 1f)
            {
                Projectile.velocity.Y = Projectile.velocity.Y * -0.5f;
            }
            // ai[0] 计数：前 5 帧为弹射启动期；之后每帧施加 0.2 重力
            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] > 5f)
            {
                Projectile.ai[0] = 5f;
                // 贴地（Y 速度为 0）仍水平滑动时，水平速度每帧 ×0.97，接近零则停住并同步网络
                if (Projectile.velocity.Y == 0f && Projectile.velocity.X != 0f)
                {
                    Projectile.velocity.X = Projectile.velocity.X * 0.97f;
                    if ((double)Projectile.velocity.X > -0.01 && (double)Projectile.velocity.X < 0.01)
                    {
                        Projectile.velocity.X = 0f;
                        Projectile.netUpdate = true;
                    }
                }
                Projectile.velocity.Y = Projectile.velocity.Y + 0.2f;
            }
            Projectile.rotation += Projectile.velocity.X * 0.1f;
            int num199 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.UnusedWhiteBluePurple, 0f, 0f, 100, default(Color), 1f);
            Dust expr_8976_cp_0 = Main.dust[num199];
            expr_8976_cp_0.position.X = expr_8976_cp_0.position.X - 2f;
            Dust expr_8994_cp_0 = Main.dust[num199];
            expr_8994_cp_0.position.Y = expr_8994_cp_0.position.Y + 2f;
            Main.dust[num199].scale += (float)Main.rand.Next(50) * 0.01f;
            Main.dust[num199].noGravity = true;
            Dust expr_89E7_cp_0 = Main.dust[num199];
            expr_89E7_cp_0.velocity.Y = expr_89E7_cp_0.velocity.Y - 2f;
            if (Main.rand.Next(2) == 0)
            {
                int num200 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.UnusedWhiteBluePurple, 0f, 0f, 100, default(Color), 1f);
                Dust expr_8A4E_cp_0 = Main.dust[num200];
                expr_8A4E_cp_0.position.X = expr_8A4E_cp_0.position.X - 2f;
                Dust expr_8A6C_cp_0 = Main.dust[num200];
                expr_8A6C_cp_0.position.Y = expr_8A6C_cp_0.position.Y + 2f;
                Main.dust[num200].scale += 0.3f + (float)Main.rand.Next(50) * 0.01f;
                Main.dust[num200].noGravity = true;
                Main.dust[num200].velocity *= 0.1f;
            }
            // 下落速度处于 0.15~0.25 的"将落地"区间时，水平速度衰减 20%，模拟触地摩擦
            if ((double)Projectile.velocity.Y < 0.25 && (double)Projectile.velocity.Y > 0.15)
            {
                Projectile.velocity.X = Projectile.velocity.X * 0.8f;
            }
            Projectile.rotation = -Projectile.velocity.X * 0.05f;
            // 限制最大下落速度为 16，避免火花加速过快而穿模/穿透地板
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
            }
        }
        /// <summary>
        /// 照抄经典版：penetrate 恒为 -1（无限穿透），penetrate == 0 分支不可达，撞墙实际不销毁
        /// </summary>
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.penetrate == 0)
            {
                Projectile.Kill();
            }
            return false;
        }
    }
}
