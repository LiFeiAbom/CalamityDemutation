using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 腐臭倾泻者 2.0.4 的腐肉：最多 8 枚黏附同一目标，持续造成完整伤害并施加硫酸中毒。
    /// </summary>
    internal class EmesisGore : ModProjectile
    {
        private const int MaxStuckProjectiles = 8;
        private const int PoisonDuration = 60;
        public int HurtCounter;
        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.penetrate = 10;
            Projectile.timeLeft = 600;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }
        public override void SendExtraAI(BinaryWriter writer) => writer.Write(HurtCounter);
        public override void ReceiveExtraAI(BinaryReader reader) => HurtCounter = reader.ReadInt32();
        public override void AI()
        {
            UpdateSticking();
            if (!Projectile.active)
                return;
            if (Projectile.ai[0] == 1f)
            {
                Projectile.localAI[0] = 5f;
                Projectile.velocity = Vector2.Zero;
                HurtCounter++;
                if (HurtCounter % 10 == 0)
                    Main.npc[(int)Projectile.ai[1]].HitEffect(0, 50.0);
            }
            else
            {
                Projectile.rotation += (Projectile.velocity.X > 0).ToDirectionInt()
                    * MathHelper.ToRadians(8f);
            }
            if (Projectile.timeLeft % 12 == 11)
            {
                int dustCount = Projectile.ai[0] == 1f ? 3 : 1;
                for (int i = 0; i < dustCount; i++)
                {
                    Dust dust = Dust.NewDustDirect(Projectile.position, 10, 10, DustID.Shadowflame);
                    dust.velocity = Vector2.One.RotatedByRandom(MathHelper.TwoPi);
                    dust.noGravity = true;
                }
            }
        }
        private void UpdateSticking()
        {
            if (Projectile.ai[0] != 1f)
                return;
            int targetIndex = (int)Projectile.ai[1];
            if (targetIndex < 0 || targetIndex >= Main.maxNPCs)
            {
                Projectile.Kill();
                return;
            }
            NPC target = Main.npc[targetIndex];
            Projectile.tileCollide = false;
            Projectile.localAI[0]++;
            if (!target.active || target.dontTakeDamage)
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = target.Center - Projectile.velocity * 2f;
            Projectile.gfxOffY = target.gfxOffY;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.ai[0] == 1f || Projectile.owner != Main.myPlayer
                || !target.active || target.dontTakeDamage)
                return;
            int stuckCount = 0;
            int oldestIndex = -1;
            int oldestTime = int.MaxValue;
            foreach (Projectile other in Main.ActiveProjectiles)
            {
                if (other.whoAmI == Projectile.whoAmI || other.owner != Projectile.owner
                    || other.type != Projectile.type || other.ai[0] != 1f
                    || (int)other.ai[1] != target.whoAmI)
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
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (targetHitbox.Width > 8 && targetHitbox.Height > 8)
                targetHitbox.Inflate(-targetHitbox.Width / 8, -targetHitbox.Height / 8);
            return null;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
            => CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(
                target, "SulphuricPoisoning", PoisonDuration, BuffID.Venom);
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
            => CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(
                target, "SulphuricPoisoning", PoisonDuration, BuffID.Venom);
    }
}
