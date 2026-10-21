using CalamityDemutation.Content.Items.Weapons.Rogue;
using CalamityDemutation.Players;
using CalamityDemutation.Sounds;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    internal class SearedPanProjectile : ModProjectile
    {
        private static int superDummyType = -1;
        private enum PanType
        {
            Normal = 1,
            Golden = 2,
            StealthStrike = 3
        }
        private PanType CurrentPanType
        {
            get => (PanType)(int)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) &&
                calamity.TryFind<ModNPC>("SuperDummyNPC", out ModNPC superDummy))
                superDummyType = superDummy.Type;
        }
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 420;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
            Projectile.extraUpdates = 2;
        }
        public override void AI()
        {
            if (Main.rand.NextBool(5))
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.Blood, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f);
            Projectile.spriteDirection = Projectile.direction = (Projectile.velocity.X > 0).ToDirectionInt();
            Projectile.rotation = Projectile.velocity.ToRotation() + (Projectile.spriteDirection == 1 ? 0f : MathHelper.Pi);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "BurningBlood", 180);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "BurningBlood", 180);
            bool countsForChain = target.type != NPCID.TargetDummy && target.type != superDummyType;
            OnHitEffects(target.whoAmI, target.life, countsForChain);
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Bleeding, 300);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "BurningBlood", 180);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "BurningBlood", 180);
            OnHitEffects(-1, target.statLife, true);
        }
        private void OnHitEffects(int targetIndex, int targetHealth, bool specialEffects)
        {
            Player owner = Main.player[Projectile.owner];
            if (CDUtil.GetRogueStealthMax(owner) <= 0f)
                return;
            CalamityDemutationPlayer modPlayer = owner.GetModPlayer<CalamityDemutationPlayer>();
            if (specialEffects)
                modPlayer.RegisterSearedPanHit();
            switch (CurrentPanType)
            {
                case PanType.StealthStrike:
                    modPlayer.ResetSearedPanCounter();
                    if (Projectile.owner == Main.myPlayer)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            Vector2 velocity = CDUtil.RandomVelocity(100f, 70f, 100f);
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<PanSpark>(), (int)(Projectile.damage * 0.2f), 0f, Projectile.owner);
                        }
                    }
                    SoundEngine.PlaySound(CalamityDemutationSounds.SearedPanSmash, Projectile.position);
                    MakeFireballsHome();
                    if (specialEffects && targetIndex >= 0 && targetIndex < Main.maxNPCs && Projectile.owner == Main.myPlayer)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            int fireball = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<SearedPanFireball>(), (int)(Projectile.damage * 0.1f), 0f, Projectile.owner, 0f, targetIndex);
                            if (fireball >= 0 && fireball < Main.maxProjectiles)
                            {
                                Main.projectile[fireball].localAI[1] = 61f;
                                Main.projectile[fireball].netUpdate = true;
                            }
                        }
                        ArrangeFireballs(targetIndex);
                    }
                    break;
                case PanType.Golden:
                    modPlayer.ResetSearedPanCounter();
                    SoundEngine.PlaySound(CalamityDemutationSounds.SearedPanSmash, Projectile.position);
                    MakeFireballsHome();
                    break;
                case PanType.Normal:
                    if (specialEffects && targetIndex >= 0 && targetIndex < Main.maxNPCs && targetHealth > 0 && Projectile.owner == Main.myPlayer)
                    {
                        for (int i = 0; i < 3; i++)
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<SearedPanFireball>(), (int)(Projectile.damage * 0.1f), 0f, Projectile.owner, 0f, targetIndex);
                        ArrangeFireballs(targetIndex);
                    }
                    break;
            }
        }
        private void MakeFireballsHome()
        {
            if (Projectile.owner != Main.myPlayer)
                return;
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != Projectile.owner || projectile.ModProjectile is not SearedPanFireball)
                    continue;
                projectile.localAI[0] = 1f;
                projectile.extraUpdates += 2;
                projectile.netUpdate = true;
            }
        }
        private void ArrangeFireballs(int targetIndex)
        {
            if (Projectile.owner != Main.myPlayer)
                return;
            int fireballCount = 0;
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != Projectile.owner || (int)projectile.ai[1] != targetIndex || projectile.ModProjectile is not SearedPanFireball || projectile.localAI[0] == 1f)
                    continue;
                fireballCount++;
            }
            if (fireballCount == 0)
                return;
            float angleVariance = MathHelper.TwoPi / fireballCount;
            float angle = 0f;
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != Projectile.owner || (int)projectile.ai[1] != targetIndex || projectile.ModProjectile is not SearedPanFireball || projectile.localAI[0] == 1f)
                    continue;
                projectile.ai[0] = angle;
                projectile.netUpdate = true;
                angle += angleVariance;
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (CurrentPanType == PanType.Golden || CurrentPanType == PanType.StealthStrike)
            {
                if (Projectile.owner == Main.myPlayer)
                {
                    foreach (Projectile projectile in Main.ActiveProjectiles)
                    {
                        if (projectile.owner == Projectile.owner && projectile.ModProjectile is SearedPanFireball)
                            projectile.Kill();
                    }
                }
            }
            return true;
        }
        public override Color? GetAlpha(Color lightColor)
        {
            if (CurrentPanType == PanType.Golden || CurrentPanType == PanType.StealthStrike)
                return new Color(255, 222, 0);
            return null;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
    }
}
