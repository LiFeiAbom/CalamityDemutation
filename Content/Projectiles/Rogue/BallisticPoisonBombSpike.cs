using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 弹道毒炸弹炸出的尖刺（照灾厄 2.0 的 <c>BallisticPoisonBombSpike</c>）：
    /// 10×10 判定、初始全透明（每帧 -10 淡入）、穿透 3、<c>extraUpdates = 2</c>；
    /// 走原版钉子 AI（<c>aiStyle = 93</c> + <c>AIType = ProjectileID.NailFriendly</c>），
    /// 命中挂 2 秒「毒液」并把该玩家的无敌帧压到 1 帧（源写法，让连发尖刺不互相挡）。
    /// </summary>
    internal class BallisticPoisonBombSpike : ModProjectile
    {
        /// <summary>毒尘类型（源里是裸数字 14）</summary>
        private const int PoisonDust = 14;
        /// <summary>火尘类型（源里是裸数字 6）</summary>
        private const int FireDust = 6;

        /// <summary>10×10、全透明起步、穿透 3、两倍额外更新，伤害类型取盗贼</summary>
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.alpha = 255;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 3;
            Projectile.extraUpdates = 2;
            Projectile.aiStyle = ProjAIStyleID.Nail;
            AIType = ProjectileID.NailFriendly;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>每帧淡入 10 点；每 5 帧在原地留一颗不动的小毒尘</summary>
        public override void AI()
        {
            Projectile.alpha -= 10;
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;
            Projectile.localAI[1] += 1f;
            if (Projectile.localAI[1] > 4f)
            {
                int dust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, PoisonDust, 0f, 0f, 100, default, 0.75f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0f;
            }
        }
        /// <summary>命中：挂 2 秒「毒液」，并把命中者的无敌帧压到 1 帧（照源）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Venom, 120);
            target.immune[Projectile.owner] = 1;
        }
        /// <summary>PvP 只挂「毒液」（源里没有那行无敌帧压制）</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Venom, 120);
        }
        /// <summary>消失：把判定框撑到 32 再撒毒尘与火尘（颜色按源取 DiscoR 混 203/103）</summary>
        public override void OnKill(int timeLeft)
        {
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 32;
            Projectile.position -= Projectile.Size * 0.5f;
            Color sparkColor = new Color(Main.DiscoR, 203, 103);
            for (int i = 0; i < 2; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, PoisonDust, 0f, 0f, 100, sparkColor, 1.2f);
                Main.dust[dust].velocity *= 3f;
                if (Main.rand.NextBool(2))
                {
                    Main.dust[dust].scale = 0.5f;
                    Main.dust[dust].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
            }
            for (int i = 0; i < 2; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, FireDust, 0f, 0f, 100, sparkColor, 1.7f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 5f;
                dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, FireDust, 0f, 0f, 100, sparkColor, 1f);
                Main.dust[dust].velocity *= 2f;
            }
        }
    }
}
