using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 熵之飞刃（大号）—— 熵之舞挥砍时撒出的扇形飞刀之一（照搬灾厄 2.0.3.9 的 <c>EntropicFlechetteLarge</c>）。
    /// 与中号飞刃代码逐行相同，只有碰撞箱放大到 18×18（伤害倍率 1.0，见 <see cref="EntropicClaymore"/>）。
    /// </summary>
    internal class EntropicFlechetteLarge : ModProjectile
    {
        /// <summary>残影缓存 10 点、TrailingMode 1（只记录位置，供残影尾迹采样）</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 1;
        }
        /// <summary>基础属性：18×18 碰撞箱、友方近战、穿透 1、存活 600 帧</summary>
        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 600;
        }
        /// <summary>朝速度方向摆正、绕剑身喷两股暗影焰尘（每 7.5° 推进一帧、相位相差半圈）、朝敌人追踪</summary>
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Vector2 rotateVector = new Vector2(6f, 12f);
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] == 48f)
                Projectile.localAI[0] = 0f;
            else
            {
                for (int i = 0; i < 2; i++)
                {
                    // 0.1308997f = π/24，即每帧转 7.5°；两股尘相差半圈形成对称的旋转尾迹
                    Vector2 dustRotate = -Vector2.UnitY.RotatedBy(Projectile.localAI[0] * 0.1308997f + i * MathHelper.Pi) * rotateVector;
                    int darkDust = Dust.NewDust(Projectile.Center, 0, 0, DustID.Shadowflame, 0f, 0f, 160, default, 1f);
                    Main.dust[darkDust].scale = 1f;
                    Main.dust[darkDust].noGravity = true;
                    Main.dust[darkDust].position = Projectile.Center + dustRotate;
                    Main.dust[darkDust].velocity = Projectile.velocity;
                }
            }
            // 剑身正中再补一缕几乎不动的暗影焰尘，强调飞刀本体的位置
            int darkestDust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.Shadowflame, 0f, 0f, 100, default, 0.8f);
            Main.dust[darkestDust].noGravity = true;
            Main.dust[darkestDust].velocity *= 0f;
            CDUtil.HomeInOnNPC(Projectile, !Projectile.tileCollide, 200f, 12f, 20f);
        }
        /// <summary>画青色残影拖尾（每 2 格取 1 帧），替换默认绘制</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 2);
            return false;
        }
    }
}
