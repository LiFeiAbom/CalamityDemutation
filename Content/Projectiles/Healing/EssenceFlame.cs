using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Healing
{
    /// <summary>精华镰刀击杀敌人后生成的治疗火焰；追踪玩家并固定回复 8 生命。</summary>
    internal class EssenceFlame : ModProjectile
    {
        private const int HealAmount = 8;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 600;
            Projectile.alpha = 255;
            Projectile.extraUpdates = 3;
        }
        public override void AI()
        {
            Projectile.alpha -= 5;
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 16)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 3)
                Projectile.frame = 0;
            int playerIndex = (int)Projectile.ai[0];
            if (playerIndex < 0 || playerIndex >= Main.maxPlayers || !Main.player[playerIndex].active)
                playerIndex = Projectile.owner;
            Player target = Main.player[playerIndex];
            Vector2 delta = target.Center - Projectile.Center;
            float distance = delta.Length();
            if (distance < 50f && Projectile.Hitbox.Intersects(target.Hitbox))
            {
                if (Projectile.owner == Main.myPlayer)
                {
                    target.HealEffect(HealAmount, false);
                    target.statLife += HealAmount;
                    if (target.statLife > target.statLifeMax2)
                        target.statLife = target.statLifeMax2;
                    NetMessage.SendData(MessageID.PlayerHeal, -1, -1, null, playerIndex, HealAmount, 0f, 0f, 0, 0, 0);
                }
                Projectile.Kill();
                return;
            }
            if (distance > 0f)
            {
                Vector2 desiredVelocity = delta * (3.5f / distance);
                Projectile.velocity = (Projectile.velocity * 15f + desiredVelocity) / 16f;
            }
        }
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item74, Projectile.position);
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 50;
            Projectile.position.X -= Projectile.width / 2f;
            Projectile.position.Y -= Projectile.height / 2f;
            for (int i = 0; i < 10; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default, 2f);
                Main.dust[dust].velocity *= 3f;
                if (Main.rand.NextBool(2))
                {
                    Main.dust[dust].scale = 0.5f;
                    Main.dust[dust].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
            }
            for (int i = 0; i < 15; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default, 3f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 5f;
                dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default, 2f);
                Main.dust[dust].velocity *= 2f;
            }
        }
    }
}
