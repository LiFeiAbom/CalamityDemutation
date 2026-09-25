using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 星流爆炸（Exoboomold）—— 星流之刃命中低血量目标时原地炸出的隐形爆炸区
    /// （照搬 CI 的 <c>Exoboomold</c>，即改版前旧 Exoboom 的复刻）。
    /// 本身无贴图，靠 250×250 的大判定框 + 连续 30 帧的青色尘爆表现；命中挂整套星云系减益。
    /// </summary>
    internal class Exoboomold : ModProjectile
    {
        /// <summary>无贴图：借用工程内的隐形贴图</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>基础属性：250×250、友方近战、穿透无限、可入水、不碰撞物块、存活 30 帧、本地免疫 3 帧</summary>
        public override void SetDefaults()
        {
            Projectile.width = 250;
            Projectile.height = 250;
            Projectile.friendly = true;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 30;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 3;
        }
        /// <summary>青色照明、出场音效（仅一次）、每帧朝四周随机迸发 40 颗青色尘</summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0f / 255f, (255 - Projectile.alpha) * 0.75f / 255f, (255 - Projectile.alpha) * 0.75f / 255f);   // 红色分量刻意乘 0，只发青（绿+蓝）光
            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item74, Projectile.position);
                Projectile.localAI[0] += 1f;
            }
            for (int i = 0; i < 40; i++)
            {
                // 循环只当"发 40 颗"的计数器用：方向取 (-30~30, -30~30) 后归一化，再乘 9~26 的线速度（num466 在此复用为缩放系数）
                float num463 = Main.rand.Next(-30, 31);
                float num464 = Main.rand.Next(-30, 31);
                float num465 = Main.rand.Next(9, 27);
                float num466 = (float)Math.Sqrt(num463 * num463 + num464 * num464);
                num466 = num465 / num466;
                num463 *= num466;
                num464 *= num466;
                int num467 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.TerraBlade, 0f, 0f, 100, new Color(0, 255, 255), 1.5f);
                Dust dust = Main.dust[num467];
                dust.noGravity = true;
                dust.position.X = Projectile.Center.X;
                dust.position.Y = Projectile.Center.Y;
                dust.position.X += Main.rand.Next(-10, 11);
                dust.position.Y += Main.rand.Next(-10, 11);
                dust.velocity.X = num463;
                dust.velocity.Y = num464;
            }
        }
        /// <summary>命中敌人：挂上整套星云系减益</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.ExoDebuffs();
        }
    }
}
