using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 超新星炸弹（照灾厄 2.0 的 <c>SupernovaBomb</c>）：
    /// 34×34 判定、穿透 1、存活 200 帧、**会撞物块**、贴图借用物品贴图；
    /// 每帧 1/6 概率撒彩虹尘（107/234/269）并打一圈渐变色照明，10 帧后启动重力与空气阻力、随水平速度自转。
    /// </summary>
    /// <remarks>
    /// 潜行打击的那一枚：每 8 帧朝正上方喷一枚追踪能量（伤害 ×0.48，主人端生成）。
    /// 炸开（<c>OnKill</c>）：判定框撑到 128 + 爆炸音，然后（主人端）
    /// 爆出一发 <see cref="SupernovaBoom"/>（全额伤害）+ 3~4 枚 <see cref="SupernovaSpike"/>（伤害 ×0.6）
    /// + 6 枚 <see cref="SupernovaHoming"/>（三对、各向两侧、伤害 ×0.5），再撒彩虹尘、火星与烟。
    /// </remarks>
    internal class SupernovaBomb : ModProjectile
    {
        /// <summary>爆炸时撑到的判定框边长（照源）</summary>
        private const int ExplosionSize = 128;
        /// <summary>潜行弹喷追踪能量的间隔（帧，照源）</summary>
        private const int StealthEnergyInterval = 8;
        /// <summary>潜行弹喷出的追踪能量伤害倍率（照源）</summary>
        private const float StealthEnergyDamage = 0.48f;

        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Rogue/Supernova";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }
        /// <summary>34×34、穿透 1、存活 200 帧、撞物块，伤害类型取盗贼</summary>
        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 200;
            Projectile.tileCollide = true;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>彩虹尘 + 变色照明 + 10 帧后重力/阻力 + 自转 + 潜行弹定时喷追踪能量</summary>
        public override void AI()
        {
            int dustType = Main.rand.NextBool(2) ? DustID.Terra : DustID.BoneTorch;
            if (Main.rand.NextBool(4))
                dustType = DustID.Sandnado;
            if (Main.rand.NextBool(6))
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, dustType, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f);
            Lighting.AddLight(Projectile.Center, Main.DiscoR * 0.5f / 255f, Main.DiscoG * 0.5f / 255f, Main.DiscoB * 0.5f / 255f);

            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] > 10f)
            {
                Projectile.ai[0] = 10f;
                if (Projectile.velocity.Y == 0f && Projectile.velocity.X != 0f)
                {
                    Projectile.velocity.X *= 0.97f;
                    if (Math.Abs(Projectile.velocity.X) < 0.01f)
                    {
                        Projectile.velocity.X = 0f;
                        Projectile.netUpdate = true;
                    }
                }
                Projectile.velocity.Y += 0.2f;
            }
            Projectile.rotation += Projectile.velocity.X * 0.1f;

            if (CDUtil.IsStealthStrike(Projectile, out _) && Projectile.timeLeft % StealthEnergyInterval == 0 && Projectile.owner == Main.myPlayer)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.UnitY * 2f, ModContent.ProjectileType<SupernovaHoming>(), (int)(Projectile.damage * StealthEnergyDamage), Projectile.knockBack, Projectile.owner, 0f, 0f);
        }
        /// <summary>命中敌人挂整套星云系减益</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.ExoDebuffs();
        }
        /// <summary>PvP 同理</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.ExoDebuffs();
        }
        /// <summary>炸开：撑框 + 巨响 + 爆炸/尖刺/追踪能量（主人端）+ 尘与烟</summary>
        public override void OnKill(int timeLeft)
        {
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = ExplosionSize;
            Projectile.position -= Projectile.Size * 0.5f;
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);

            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<SupernovaBoom>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, 0f);
                int spikeAmount = Main.rand.Next(3, 5);
                for (int i = 0; i < spikeAmount; i++)
                {
                    Vector2 velocity = CDUtil.RandomVelocity(100f, 70f, 100f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<SupernovaSpike>(), (int)(Projectile.damage * 0.6f), 0f, Projectile.owner, 0f, 0f);
                }
                float spread = MathHelper.Pi / 3f;
                double startAngle = Math.Atan2(Projectile.velocity.X, Projectile.velocity.Y) - spread / 2;
                double deltaAngle = spread / 6f;
                for (int i = 0; i < 3; i++)
                {
                    double offsetAngle = startAngle + deltaAngle * (i + i * i) / 2f + 32f * i;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y, (float)(Math.Sin(offsetAngle) * 2f), (float)(Math.Cos(offsetAngle) * 2f), ModContent.ProjectileType<SupernovaHoming>(), (int)(Projectile.damage * 0.5f), Projectile.knockBack, Projectile.owner, 0f, 0f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y, (float)(-Math.Sin(offsetAngle) * 2f), (float)(-Math.Cos(offsetAngle) * 2f), ModContent.ProjectileType<SupernovaHoming>(), (int)(Projectile.damage * 0.5f), Projectile.knockBack, Projectile.owner, 0f, 0f);
                }
            }

            int explosionDust = Main.rand.NextBool(2) ? DustID.Terra : DustID.BoneTorch;
            if (Main.rand.NextBool(4))
                explosionDust = DustID.Sandnado;
            for (int i = 0; i < 5; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, explosionDust, 0f, 0f, 100, default, 2f);
                Main.dust[dust].velocity *= 3f;
                if (Main.rand.NextBool(2))
                {
                    Main.dust[dust].scale = 0.5f;
                    Main.dust[dust].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
            }
            for (int i = 0; i < 9; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 3f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 5f;
                dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 2f);
                Main.dust[dust].velocity *= 2f;
            }
            if (Main.netMode != NetmodeID.Server)
                SpawnExplosionSmoke();
        }
        /// <summary>拖影（照源调灾厄的 DrawAfterimagesCentered，本工程用同名 CDUtil 版本）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
        /// <summary>照源在非服务端撒 12 团原版烟（3 组 × 四方向、三种速度档）</summary>
        private void SpawnExplosionSmoke()
        {
            Vector2 source = Projectile.Center - new Vector2(24f, 24f);
            const int goreAmount = 3;
            for (int i = 0; i < goreAmount; i++)
            {
                float velocityMult = i < goreAmount / 3 ? 0.66f : (i >= 2 * goreAmount / 3 ? 1f : 0.33f);
                for (int direction = 0; direction < 4; direction++)
                {
                    int type = Main.rand.Next(61, 64);
                    int smoke = Gore.NewGore(Projectile.GetSource_Death(), source, default, type, 1f);
                    Gore gore = Main.gore[smoke];
                    gore.velocity *= velocityMult;
                    gore.velocity.X += direction == 0 || direction == 2 ? 1f : -1f;
                    gore.velocity.Y += direction < 2 ? 1f : -1f;
                }
            }
        }
    }
}
