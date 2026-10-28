using CalamityDemutation.Sounds;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>2.0.3.9 灵液长矛：高速穿透，潜行弹首次命中后钉在敌人身上持续伤害。</summary>
    internal class IchorSpearProj : ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Rogue/IchorSpear";
        private bool postHit;
        private int framesInAir;
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 6;
            Projectile.timeLeft = 900;
            Projectile.aiStyle = 0;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 50;
        }
        public override void AI()
        {
            if (Projectile.ai[0] == 1f)
            {
                UpdateStuckProjectile();
                return;
            }
            framesInAir++;
            bool stealthStrike = CDUtil.IsStealthStrike(Projectile, out _);
            if (framesInAir > 90 && !stealthStrike)
            {
                Projectile.velocity.X *= 0.998f;
                Projectile.velocity.Y += 0.3f;
            }
            Projectile.scale = 1.2f;
            if (!stealthStrike)
                Projectile.extraUpdates = 1;
            if (Main.rand.NextBool() && !postHit)
            {
                Vector2 position = Projectile.Center + Vector2.Normalize(Projectile.velocity);
                for (int direction = -1; direction <= 1; direction += 2)
                {
                    Dust dust = Dust.NewDustPerfect(
                        position,
                        DustID.IchorTorch,
                        Projectile.velocity.RotatedBy(1.9707963f * direction) * 0.1f + Projectile.velocity / 8f,
                        0,
                        default,
                        stealthStrike ? Main.rand.NextFloat(2.1f, 3.2f) : Main.rand.NextFloat(1.2f, 1.5f));
                    dust.position += Projectile.velocity.RotatedBy(0.3f * direction);
                    dust.fadeIn = 0.5f;
                    dust.noGravity = true;
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            if (stealthStrike)
            {
                Projectile.tileCollide = false;
                Projectile.extraUpdates = 2;
                if (!postHit)
                {
                    Projectile.penetrate = 10;
                    Projectile.localNPCHitCooldown = 60;
                }
            }
        }
        private void UpdateStuckProjectile()
        {
            int npcIndex = (int)Projectile.ai[1];
            if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
            {
                Projectile.Kill();
                return;
            }
            NPC target = Main.npc[npcIndex];
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] >= 600f || !target.active || target.dontTakeDamage)
            {
                Projectile.Kill();
                return;
            }
            Projectile.tileCollide = false;
            Projectile.Center = target.Center - Projectile.velocity * 2f;
            Projectile.gfxOffY = target.gfxOffY;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            if (Projectile.localAI[0] % 30f == 0f)
                target.HitEffect(0, 1.0);
        }
        private void StickTo(NPC target)
        {
            int attachedCount = 0;
            int oldestProjectile = -1;
            int oldestTimeLeft = int.MaxValue;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile attached = Main.projectile[i];
                if (i == Projectile.whoAmI || !attached.active || attached.owner != Projectile.owner
                    || attached.type != Projectile.type || attached.ai[0] != 1f || (int)attached.ai[1] != target.whoAmI)
                    continue;
                attachedCount++;
                if (attached.timeLeft < oldestTimeLeft)
                {
                    oldestTimeLeft = attached.timeLeft;
                    oldestProjectile = i;
                }
            }
            if (attachedCount >= 2 && oldestProjectile >= 0)
                Main.projectile[oldestProjectile].Kill();
            Projectile.ai[0] = 1f;
            Projectile.ai[1] = target.whoAmI;
            Projectile.velocity = (target.Center - Projectile.Center) * 0.75f;
            Projectile.netUpdate = true;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int i = 0; i < Main.rand.Next(4, 6); i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.IchorTorch,
                    Projectile.velocity.RotatedByRandom(0.2f) * Main.rand.NextFloat(0.6f, 1.1f),
                    0,
                    Color.Gold,
                    Main.rand.NextFloat(0.8f, 1f));
                dust.noGravity = true;
            }
            SoundEngine.PlaySound(CalamityDemutationSounds.WulfrumKnifeTileHit2, Projectile.position);
            target.AddBuff(BuffID.Ichor, CDUtil.IsStealthStrike(Projectile, out _) ? 900 : 180);
            if (CDUtil.IsStealthStrike(Projectile, out _))
            {
                for (int i = 0; i <= 17; i++)
                {
                    Dust dust = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f), DustID.IchorTorch);
                    dust.noGravity = true;
                    dust.velocity.Y -= Main.rand.NextFloat(2.5f, 10.5f);
                    dust.velocity.X += Main.rand.NextFloat(-3f, 3f);
                }
                SoundEngine.PlaySound(SoundID.NPCHit18, Projectile.position);
            }
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Ichor, CDUtil.IsStealthStrike(Projectile, out _) ? 600 : 120);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (CDUtil.IsStealthStrike(Projectile, out _) && !postHit)
            {
                StickTo(target);
                postHit = true;
            }
            if (Projectile.numHits > 0)
                Projectile.damage = System.Math.Max(1, (int)(Projectile.damage * 0.9f));
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (targetHitbox.Width > 8 && targetHitbox.Height > 8 && CDUtil.IsStealthStrike(Projectile, out _))
                targetHitbox.Inflate(-targetHitbox.Width / 8, -targetHitbox.Height / 8);
            return projHitbox.Intersects(targetHitbox);
        }
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i <= 9; i++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.IchorTorch, Projectile.oldVelocity.X * 0.3f, Projectile.oldVelocity.Y * 0.3f, 0, default, Main.rand.NextFloat(1.2f, 1.6f));
        }
    }
}
