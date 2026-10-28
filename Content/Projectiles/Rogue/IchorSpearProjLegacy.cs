using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>CI 命运之矛普通投掷的中央穿透标枪，潜行时不参与生成。</summary>
    internal class IchorSpearProjLegacy : ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Projectiles/Rogue/IchorSpearProjLegacy";
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 4;
            ProjectileID.Sets.TrailingMode[Type] = 1;
        }
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 2;
            Projectile.aiStyle = ProjAIStyleID.StickProjectile;
            Projectile.timeLeft = 600;
            AIType = ProjectileID.BoneJavelin;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }
        public override void AI()
        {
            if (Main.rand.NextBool(4))
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.GoldCoin, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            if (!CDUtil.IsStealthStrike(Projectile, out _) || Projectile.timeLeft % 6 != 0 || Projectile.owner != Main.myPlayer)
                return;
            int projectileType = Main.rand.NextBool(2) ? ProjectileID.GoldenShowerFriendly : ProjectileID.IchorSplash;
            int ichor = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Main.rand.NextVector2Square(-14f, 14f),
                projectileType,
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner);
            if (ichor < 0 || ichor >= Main.maxProjectiles)
                return;
            Main.projectile[ichor].DamageType = CDUtil.GetRogueDamageClass();
            Main.projectile[ichor].usesLocalNPCImmunity = true;
            Main.projectile[ichor].localNPCHitCooldown = 10;
            Main.projectile[ichor].extraUpdates = 2;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 3);
            return false;
        }
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i <= 10; i++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.GoldCoin, Projectile.oldVelocity.X * 0.5f, Projectile.oldVelocity.Y * 0.5f);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Ichor, CDUtil.IsStealthStrike(Projectile, out _) ? 600 : 120);
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Ichor, CDUtil.IsStealthStrike(Projectile, out _) ? 600 : 120);
        }
    }
}
