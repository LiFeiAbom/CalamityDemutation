using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// CI 命运之矛弹幕：沿 800 像素范围平滑追踪；潜行弹不碰墙、额外穿透一次并带发光层。
    /// </summary>
    internal class SpearofDestinyProjectile : ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Rogue/SpearofDestiny";
        private bool initialized;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 5;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 600;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }
        public override void SendExtraAI(System.IO.BinaryWriter writer) => writer.Write(initialized);
        public override void ReceiveExtraAI(System.IO.BinaryReader reader) => initialized = reader.ReadBoolean();
        public override void AI()
        {
            bool stealthStrike = CDUtil.IsStealthStrike(Projectile, out _);
            if (!initialized)
            {
                if (stealthStrike)
                {
                    Projectile.penetrate++;
                    Projectile.tileCollide = false;
                }
                initialized = true;
            }
            if (Main.rand.NextBool(4))
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.GoldCoin, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Vector2 destination = Projectile.Center;
            float maxDistance = 800f;
            bool foundTarget = false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC target = Main.npc[i];
                if (!target.CanBeChasedBy(Projectile, false))
                    continue;
                float extraDistance = target.width * 0.5f + target.height * 0.5f;
                bool canHit = stealthStrike || Collision.CanHit(Projectile.Center, 1, 1, target.Center, 1, 1);
                if (Vector2.Distance(target.Center, Projectile.Center) < maxDistance + extraDistance && canHit)
                {
                    destination = target.Center;
                    foundTarget = true;
                    break;
                }
            }
            if (!foundTarget)
                return;
            Projectile.extraUpdates = 2;
            Vector2 direction = Projectile.SafeDirectionTo(destination, Vector2.UnitY);
            Projectile.velocity = (Projectile.velocity * 20f + direction * 12f) / 21f;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
            return false;
        }
        public override void PostDraw(Color lightColor)
        {
            if (!CDUtil.IsStealthStrike(Projectile, out _))
                return;
            Texture2D glow = ModContent.Request<Texture2D>(Texture + "Glow").Value;
            Main.EntitySpriteDraw(
                glow,
                Projectile.Center - Main.screenPosition,
                null,
                Color.White,
                Projectile.rotation,
                glow.Size() * 0.5f,
                Projectile.scale,
                SpriteEffects.None);
        }
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i <= 10; i++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.GoldCoin, Projectile.oldVelocity.X * 0.5f, Projectile.oldVelocity.Y * 0.5f);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.Ichor, 120);
        public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.AddBuff(BuffID.Ichor, 120);
    }
}
