using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 硫酸弹——取灾厄 2.0.7.2：追踪敌人，最多两发黏附同一目标；
    /// 黏附后持续造成完整伤害，并以硫磺中毒回退毒液。
    /// </summary>
    internal class SulphuricBlast : ModProjectile
    {
        private const int AnimationFrames = 20;
        private const int MaxStuckProjectiles = 2;
        private const int StuckDuration = 8 * 60;
        private const int PoisonDuration = 180;
        public override void SetStaticDefaults() => Main.projFrames[Type] = AnimationFrames;
        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.MaxUpdates = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = Projectile.MaxUpdates * 11;
        }
        public override void AI()
        {
            if (Projectile.numUpdates == -1)
                Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 4 % AnimationFrames;
            UpdateSticking();
            Projectile.Opacity = Utils.GetLerpValue(StuckDuration, StuckDuration * 0.5f, Projectile.localAI[0], true);
            if (Main.netMode != NetmodeID.Server && Projectile.numUpdates == -1 && Projectile.velocity.Length() > 3f)
            {
                Vector2 gasPosition = Projectile.Center + Main.rand.NextVector2Circular(8f, 8f);
                Vector2 gasVelocity = Projectile.velocity * 1.2f + Projectile.velocity.RotatedBy(0.75f) * 0.3f;
                gasVelocity *= Main.rand.NextFloat(0.24f, 0.6f);
                Dust gas = Dust.NewDustPerfect(gasPosition, DustID.FireworksRGB, gasVelocity, 0, new Color(136, 211, 113, 127), Main.rand.NextFloat(0.5f, 1f));
                gas.noGravity = true;
            }
            if (Projectile.ai[0] != 1f)
            {
                CDUtil.HomeInOnNPC(Projectile, true, 720f, 16f, Projectile.MaxUpdates * 20f);
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            }
        }
        private void UpdateSticking()
        {
            if (Projectile.ai[0] != 1f)
                return;
            Projectile.localAI[0]++;
            int targetIndex = (int)Projectile.ai[1];
            if (Projectile.localAI[0] >= StuckDuration || targetIndex < 0 || targetIndex >= Main.maxNPCs)
            {
                Projectile.Kill();
                return;
            }
            NPC target = Main.npc[targetIndex];
            if (!target.active || target.dontTakeDamage)
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = target.Center - Projectile.velocity * 2f;
            Projectile.gfxOffY = target.gfxOffY;
            if (Projectile.localAI[0] % 30f == 0f)
                target.HitEffect(0, 1f);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.ai[0] == 1f || !target.active || target.dontTakeDamage)
                return;
            int stuckCount = 0;
            int oldestIndex = -1;
            int oldestTime = int.MaxValue;
            foreach (Projectile other in Main.ActiveProjectiles)
            {
                if (other.whoAmI == Projectile.whoAmI || other.owner != Projectile.owner || other.type != Projectile.type || other.ai[0] != 1f || (int)other.ai[1] != target.whoAmI)
                    continue;
                stuckCount++;
                if (other.timeLeft < oldestTime)
                {
                    oldestTime = other.timeLeft;
                    oldestIndex = other.whoAmI;
                }
            }
            if (stuckCount >= MaxStuckProjectiles && oldestIndex >= 0)
                Main.projectile[oldestIndex].Kill();
            Projectile.ai[0] = 1f;
            Projectile.ai[1] = target.whoAmI;
            Projectile.velocity = (target.Center - Projectile.Center) * 0.75f;
            Projectile.netUpdate = true;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
            => CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "SulphuricPoisoning", PoisonDuration, BuffID.Venom);
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
            => CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "SulphuricPoisoning", PoisonDuration, BuffID.Venom);
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (targetHitbox.Width > 8 && targetHitbox.Height > 8)
                targetHitbox.Inflate(-targetHitbox.Width / 8, -targetHitbox.Height / 8);
            return null;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int frameHeight = texture.Height / AnimationFrames;
            Rectangle frame = new Rectangle(0, frameHeight * Projectile.frame, texture.Width, frameHeight);
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), frame,
                Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2(texture.Width / 2f, frameHeight / 2f), Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
