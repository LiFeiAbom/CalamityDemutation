using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Typeless
{
    /// <summary>
    /// 血液爆炸（BloodBombExplosion） - BloodBomb 落地/命中后的范围爆炸（移植自经典版灾厄同名弹幕）。
    /// 250x250 的大判定框、穿透无限、存活 60 帧、本地无敌帧 2 帧；每帧按递减的总量向四周喷洒血尘，
    /// 首帧播放爆炸音效。源里用 ai[0] 同时当"已存活帧数"和"目标坐标"两用（ai[0]/ai[1] 为 0 时即纯计时），
    /// 本工程只按 BloodBomb 的用法传入 0，故保留计时那一半语义。
    /// </summary>
    internal class BloodBombExplosion:ModProjectile
    {
        /// <summary>基础属性：250x250、友方、穿透无限、存活 60 帧、本地无敌帧 2 帧（照经典版）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 250;
            Projectile.height = 250;
            Projectile.friendly = true;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 2;
        }
        /// <summary>AI：红光 + 首帧爆炸音 + 每帧按 (25 - 超时量) × 0.7 的颗数向外撒血尘，ai[0] 计到 180 后自毁</summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) / 255f, 0f, 0f);
            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item74, Projectile.position);
                Projectile.localAI[0] += 1f;
            }
            float dustCount = 25f;
            if (Projectile.ai[0] > 180f)
                dustCount -= (Projectile.ai[0] - 180f) / 2f;
            if (dustCount <= 0f)
            {
                dustCount = 0f;
                Projectile.Kill();
            }
            dustCount *= 0.7f;
            Projectile.ai[0] += 4f;
            for (int i = 0; (float)i < dustCount; i++)
            {
                float vx = Main.rand.Next(-30, 31);
                float vy = Main.rand.Next(-30, 31);
                float speed = Main.rand.Next(9, 27);
                float len = (float)System.Math.Sqrt(vx * vx + vy * vy);
                if (len != 0f)
                {
                    vx = vx * speed / len;
                    vy = vy * speed / len;
                }
                int bloody = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Blood, 0f, 0f, 100, default, 1.8f);
                Main.dust[bloody].noGravity = true;
                Main.dust[bloody].position = Projectile.Center + new Vector2(Main.rand.Next(-10, 11), Main.rand.Next(-10, 11));
                Main.dust[bloody].velocity = new Vector2(vx, vy);
            }
        }
    }
}
