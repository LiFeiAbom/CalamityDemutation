using CalamityDemutation.Graphics.Primitives;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 绯红恶魔主长枪——玩法取 Public 2.0.4，贴图取经典 1.4.2 系列的独立长枪图。
    /// 普通投掷留下侧向追踪弹；潜行命中生成弹幕星阵并按实际伤害的 1% 回血。
    /// </summary>
    internal class ScarletDevilProjectile : ModProjectile
    {
        private const int LifeStealCap = 100;
        private const float LifeStealRange = 3000f;
        private ref float ShootTimer => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 45;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 108;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 300;
            Projectile.extraUpdates = 1;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        public override void AI()
        {
            bool stealthStrike = CDUtil.IsStealthStrike(Projectile, out _);
            Lighting.AddLight(Projectile.Center, 0.55f, 0.25f, 0f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            if (!Main.dedServ)
            {
                int dustCount = stealthStrike && Main.rand.NextBool() ? 2 : 1;
                for (int i = 0; i < dustCount; i++)
                {
                    Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height,
                        DustID.Firework_Red, Projectile.velocity.X * 0.25f, Projectile.velocity.Y * 0.25f,
                        0, new Color(255, 255, 255), 0.85f);
                }
            }
            ShootTimer++;
            if (!stealthStrike && Projectile.oldPos.Length != 6)
                Projectile.oldPos = new Vector2[6];
            if ((ShootTimer %= 5f) == 0f && !stealthStrike && Projectile.owner == Main.myPlayer)
                GenerateSideBullets(2, MathHelper.ToRadians(15f));
        }
        private void GenerateSideBullets(int totalBullets, float rotationalOffset)
        {
            for (int i = 0; i < totalBullets; i++)
            {
                Vector2 perturbedSpeed = new Vector2(-Projectile.velocity.X / 3f, -Projectile.velocity.Y / 3f)
                    .RotatedBy(MathHelper.Lerp(-rotationalOffset, rotationalOffset, i / (totalBullets - 1f)));
                for (int j = 0; j < 2; j++)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, perturbedSpeed,
                        ModContent.ProjectileType<ScarletDevilBullet>(), (int)(Projectile.damage * 0.03f),
                        0f, Projectile.owner);
                    perturbedSpeed *= 1.05f;
                }
            }
        }
        private void SpawnOnStealthStrikeBullets()
        {
            if (Projectile.owner != Main.myPlayer)
                return;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<ScarletDevilStealthBurst>(), (int)(Projectile.damage * 0.01f),
                0f, Projectile.owner);
        }
        public override Color? GetAlpha(Color lightColor) => new Color(250, 250, 250);
        public override void OnKill(int timeLeft) => SoundEngine.PlaySound(SoundID.Item122, Projectile.position);
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            HitEffects();
            if (!CDUtil.IsStealthStrike(Projectile, out _))
                return;
            SpawnOnStealthStrikeBullets();
            TrySpawnLifeSteal((int)Math.Round(hit.Damage * 0.01), target.lifeMax > 5);
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            HitEffects();
            if (!CDUtil.IsStealthStrike(Projectile, out _))
                return;
            SpawnOnStealthStrikeBullets();
            TrySpawnLifeSteal((int)Math.Round(info.Damage * 0.01), true);
        }
        private void HitEffects()
        {
            Projectile.ExpandHitboxBy(150);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<ScarletBlast>(), (int)(Projectile.damage * 0.0075f), 0f, Projectile.owner);
        }
        private void TrySpawnLifeSteal(int heal, bool targetCanHeal)
        {
            if (heal > LifeStealCap)
                heal = LifeStealCap;
            if (Main.player[Main.myPlayer].lifeSteal <= 0f || heal <= 0 || !targetCanHeal)
                return;
            SpawnLifeStealProjectile(heal);
        }
        private void SpawnLifeStealProjectile(int heal)
        {
            if (Main.LocalPlayer.moonLeech)
                return;
            Main.LocalPlayer.lifeSteal -= heal;
            float largestMissingLife = 0f;
            int healTarget = Projectile.owner;
            Player owner = Main.player[Projectile.owner];
            foreach (Player other in Main.ActivePlayers)
            {
                bool friendly = (!owner.hostile && !other.hostile) || owner.team == other.team;
                if (other.dead || !friendly || Vector2.Distance(Projectile.Center, other.Center) >= LifeStealRange)
                    continue;
                float missingLife = other.statLifeMax2 - other.statLife;
                if (missingLife > largestMissingLife)
                {
                    largestMissingLife = missingLife;
                    healTarget = other.whoAmI;
                }
            }
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ProjectileID.VampireHeal, 0, 0f, Projectile.owner, healTarget, heal);
        }
        private float WidthFunction(float completionRatio, Vector2 _)
        {
            float widthRatio = Utils.GetLerpValue(0f, 0.1f, completionRatio, true);
            return MathHelper.Lerp(0f, 110f, widthRatio)
                * MathHelper.Clamp(1f - (float)Math.Pow(completionRatio, 0.4D), 0.37f, 1f);
        }
        private Color ColorFunction(float completionRatio, Vector2 _)
        {
            float colorIncrement = (float)Math.Pow(completionRatio, 0.5f);
            float colorFade = 1f - Utils.GetLerpValue(0.6f, 0.98f, completionRatio, true);
            Color baseColor = CDUtil.MulticolorLerp(colorIncrement,
                Color.White, Color.DarkRed, Color.Wheat, Color.IndianRed)
                * MathHelper.Lerp(0f, 1.4f, colorFade);
            return Color.Lerp(baseColor, Color.DarkRed, (float)Math.Pow(completionRatio, 3D));
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (!CDUtil.IsStealthStrike(Projectile, out _))
            {
                CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type],
                    new Color(100, 100, 100));
                return true;
            }
            GameShaders.Misc["CalamityDemutation:TrailStreak"]
                .UseImage1(ModContent.Request<Texture2D>("CalamityDemutation/ExtraTextures/Trails/ScarletDevilStreak"));
            PrimitiveRenderer.RenderTrail(Projectile.oldPos,
                new PrimitiveSettings(
                    WidthFunction,
                    ColorFunction,
                    (float _, Vector2 _) => Projectile.Size * 0.5f
                        + Projectile.velocity.SafeNormalize(Vector2.Zero) * 86f,
                    smoothen: false,
                    pixelate: false,
                    shader: GameShaders.Misc["CalamityDemutation:TrailStreak"]),
                60);
            Texture2D spearTexture = TextureAssets.Projectile[Type].Value;
            for (int i = 0; i < 7; i++)
            {
                Color drawColor = Color.Lerp(lightColor, Color.White, 0.8f) * 0.2f;
                drawColor.A = 0;
                Vector2 drawOffset = (i / 7f * MathHelper.TwoPi).ToRotationVector2() * 2f;
                Main.EntitySpriteDraw(spearTexture,
                    Projectile.Center - Main.screenPosition + drawOffset,
                    null,
                    drawColor,
                    Projectile.rotation,
                    spearTexture.Size() * 0.5f,
                    Projectile.scale,
                    SpriteEffects.None,
                    0);
            }
            return false;
        }
    }
}
