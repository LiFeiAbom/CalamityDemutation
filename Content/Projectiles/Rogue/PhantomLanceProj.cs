using CalamityDemutation.Content.Projectiles.Magic;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 幻影投矛实体——普通投矛逐渐减速并淡出，潜伏投矛保持速度；
    /// 每 18 次更新生成一枚 <see cref="Phantom"/>，普通状态下后续召唤物的伤害逐步衰减。
    /// </summary>
    internal class PhantomLanceProj : ModProjectile
    {
        private const int Lifetime = 300;
        private const int SoulInterval = 18;
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Rogue/PhantomLance";
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = 1;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        public override void AI()
        {
            bool stealthStrike = CDUtil.IsStealthStrike(Projectile, out _);
            if (!stealthStrike)
            {
                if (Projectile.timeLeft <= 255)
                    Projectile.alpha++;
                if (Projectile.timeLeft >= 75)
                    Projectile.velocity *= 0.995f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.SpectreStaff, Projectile.velocity.X * 0.25f, Projectile.velocity.Y * 0.25f, 0, default, 0.85f);
            Projectile.ai[0]++;
            if (Projectile.owner != Main.myPlayer || Projectile.ai[0] % SoulInterval != 0f)
                return;
            float damageMultiplier = stealthStrike ? 0.7f : Projectile.timeLeft * 0.7f / Lifetime;
            int phantom = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Phantom>(), (int)(Projectile.damage * damageMultiplier), Projectile.knockBack, Projectile.owner);
            if (phantom >= 0 && phantom < Main.maxProjectiles)
            {
                Main.projectile[phantom].DamageType = CDUtil.GetRogueDamageClass();
                Main.projectile[phantom].usesLocalNPCImmunity = true;
                Main.projectile[phantom].localNPCHitCooldown = -2;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = texture.Size() * 0.5f;
            Color drawColor = Projectile.GetAlpha(lightColor);
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                float opacity = (1f - i / (float)Projectile.oldPos.Length) * 0.35f;
                Vector2 position = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(texture, position, null, drawColor * opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            }
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, drawColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i <= 10; i++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.SpectreStaff, Projectile.oldVelocity.X * 0.5f, Projectile.oldVelocity.Y * 0.5f);
        }
    }
}
