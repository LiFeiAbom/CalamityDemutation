using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>环绕目标旋转的 2.0 平底锅火球；源码类名 NiceCock，本工程改为中性名称。</summary>
    internal class SearedPanFireball : ModProjectile
    {
        private static readonly Color[] DustColors =
        {
            new Color(255, 0, 0, 50), new Color(255, 128, 0, 50), new Color(255, 255, 0, 50),
            new Color(128, 255, 0, 50), new Color(0, 255, 0, 50), new Color(0, 255, 128, 50),
            new Color(0, 255, 255, 50), new Color(0, 128, 255, 50), new Color(0, 0, 255, 50),
            new Color(128, 0, 255, 50), new Color(255, 0, 255, 50), new Color(255, 0, 128, 50)
        };
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
        }
        public override void SetDefaults()
        {
            Projectile.width = 44;
            Projectile.height = 70;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
            Projectile.timeLeft = 180;
            Projectile.alpha = 255;
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
            writer.Write(Projectile.localAI[1]);
            writer.Write(Projectile.localAI[2]);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
            Projectile.localAI[1] = reader.ReadSingle();
            Projectile.localAI[2] = reader.ReadSingle();
        }
        public override bool? CanHitNPC(NPC target)
        {
            if (Projectile.alpha >= 10)
                return false;
            int targetIndex = (int)Projectile.ai[1];
            if (targetIndex < 0 || targetIndex >= Main.maxNPCs)
                return false;
            NPC assignedTarget = Main.npc[targetIndex];
            if (!assignedTarget.active || assignedTarget.life <= 0 || assignedTarget.whoAmI != target.whoAmI)
                return false;
            return null;
        }
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (CDUtil.GetRogueStealthMax(owner) <= 0f)
            {
                Projectile.Kill();
                return;
            }
            Projectile.alpha -= 5;
            if (Projectile.localAI[2] == 0f)
            {
                Projectile.frame = Main.rand.Next(Main.projFrames[Projectile.type]);
                Projectile.localAI[2] = 1f;
            }
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 8)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= Main.projFrames[Projectile.type])
                Projectile.frame = 0;
            int targetIndex = (int)Projectile.ai[1];
            if (targetIndex < 0 || targetIndex >= Main.maxNPCs)
            {
                Projectile.Kill();
                return;
            }
            NPC target = Main.npc[targetIndex];
            if (!target.active || target.life <= 0)
            {
                Projectile.Kill();
                return;
            }
            if (Projectile.localAI[1] > 1f)
                Projectile.localAI[1]--;
            if (Projectile.localAI[1] == 1f && Projectile.localAI[0] != 1f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.netUpdate = true;
            }
            if (Projectile.localAI[0] == 1f)
            {
                Vector2 moveDirection = Projectile.SafeDirectionTo(target.Center, Vector2.UnitY);
                Projectile.velocity = (Projectile.velocity * 20f + moveDirection * 15f) / 21f;
                return;
            }
            float width = target.width;
            float height = target.height;
            float circleDistance = MathHelper.Min((height > width ? height : width) * 3f, Main.LogicCheckScreenWidth * Main.LogicCheckScreenHeight / 2);
            if (circleDistance > Main.LogicCheckScreenWidth / 3)
                circleDistance = Main.LogicCheckScreenWidth / 3;
            Projectile.Center = target.Center + Projectile.ai[0].ToRotationVector2() * circleDistance;
            Projectile.ai[0] -= MathHelper.ToRadians(4f);
        }
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item74, Projectile.Center);
            ExpandHitbox(50);
            for (int i = 0; i < 5; i++)
            {
                int dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.RainbowMk2, 0f, 0f, 150, DustColors[Main.rand.Next(DustColors.Length)], 2f);
                Main.dust[dustIndex].velocity *= 3f;
                Main.dust[dustIndex].noGravity = true;
                if (Main.rand.NextBool(2))
                {
                    Main.dust[dustIndex].scale = 0.5f;
                    Main.dust[dustIndex].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
            }
            for (int i = 0; i < 8; i++)
            {
                int dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.RainbowMk2, 0f, 0f, 150, DustColors[Main.rand.Next(DustColors.Length)], 3f);
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].velocity *= 5f;
                dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.RainbowMk2, 0f, 0f, 150, DustColors[Main.rand.Next(DustColors.Length)], 2f);
                Main.dust[dustIndex].velocity *= 2f;
                Main.dust[dustIndex].noGravity = true;
            }
        }
        private void ExpandHitbox(int newSize)
        {
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = newSize;
            Projectile.position -= Projectile.Size * 0.5f;
        }
    }
}
