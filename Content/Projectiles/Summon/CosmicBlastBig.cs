using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 宇宙之灵的大爆裂（照 CI 的 <c>Content/Projectiles/ExoLore/CosmicBlastBigExoLore.cs</c> 移植，
    /// 即 ExoLore 分支那一枚）。24×24 判定、穿透 1、寿命 600；出场减速后加速追踪敌人，
    /// 一路撒彩虹尘与碎布（gore）。命中挂整套星云系减益，消失时撑到 288×288 再打一次范围伤害。
    /// </summary>
    internal class CosmicBlastBig:ModProjectile
    {
        /// <summary>属于仆从弹药；残影缓存 6 格</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }
        /// <summary>基础属性：照源（24×24、穿透 1、寿命 600、不占召唤栏、召唤伤害）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.minionSlots = 0f;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Summon;
        }
        /// <summary>零星音效/碎布 + 出场减速 + 加速追踪（照源）</summary>
        public override void AI()
        {
            if (Projectile.soundDelay == 0)
            {
                Projectile.soundDelay = 20 + Main.rand.Next(40);
                if (Main.rand.NextBool(5))
                {
                    SoundEngine.PlaySound(SoundID.Item9, Projectile.position);
                }
            }
            Projectile.rotation += (Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y)) * 0.01f * (float)Projectile.direction;
            // 彩虹火星
            if (Main.rand.NextBool(8))
            {
                Vector2 offset = Vector2.UnitX.RotatedByRandom(MathHelper.PiOver2).RotatedBy(Projectile.velocity.ToRotation());
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.RainbowTorch, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f, 150, new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB), 1.2f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = offset * 0.66f;
                Main.dust[dust].position = Projectile.Center + offset * 12f;
            }
            // 偶尔甩出一块碎布（源写裸值 16，即 gore 16 号）
            if (Main.rand.NextBool(24) && Main.netMode != NetmodeID.Server)
            {
                int gore = Gore.NewGore(Projectile.GetSource_FromAI(), Projectile.Center, new Vector2(Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f), 16, 1f);
                Main.gore[gore].velocity *= 0.66f;
                Main.gore[gore].velocity += Projectile.velocity * 0.3f;
            }
            // 被标记过（ai[1] == 1）时撒得更密
            if (Projectile.ai[1] == 1f)
            {
                if (Main.rand.NextBool(5))
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.RainbowTorch, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f, 150, new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB), 1.2f);
                    Main.dust[dust].noGravity = true;
                }
                if (Main.rand.NextBool(10) && Main.netMode != NetmodeID.Server)
                {
                    Gore.NewGore(Projectile.GetSource_FromAI(), Projectile.position, new Vector2(Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f), Main.rand.Next(16, 18), 1f);
                }
            }
            // 出生后 60 帧内持续减速，之后加速追踪 1500 像素内的敌人
            if (Projectile.timeLeft > 540)
            {
                Projectile.velocity *= 0.96f;
            }
            if (Projectile.timeLeft < 540)
            {
                float maxSpeed = 20f;
                float acceleration = 0.06f * 10f;
                float homeInSpeed = MathHelper.Clamp(Projectile.ai[0] += acceleration, 0f, maxSpeed);

                CDUtil.HomeInOnNPC(Projectile, !Projectile.tileCollide, 1500f, homeInSpeed, 15f);
            }
        }
        /// <summary>Lore 分支的写法：纯白</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(255, 255, 255, 255);
        /// <summary>命中挂整套星云系减益（照源 <c>target.ExoDebuffs()</c>）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.ExoDebuffs();
        /// <summary>PvP 命中挂奇迹枯萎（经典版没有该减益，退回原版燃烧）</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info) => CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "MiracleBlight", 300, BuffID.OnFire);
        /// <summary>自绘：只画残影（源同：本体不画，全靠残影与尘）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 3);
            return false;
        }
        /// <summary>消失：把自己撑到 288×288 打一次范围伤害，再喷一圈彩虹尘（照源）</summary>
        public override void OnKill(int timeLeft)
        {
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 288;
            Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
            Projectile.maxPenetrate = -1;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.Damage();
            SoundEngine.PlaySound(SoundID.Zombie103, Projectile.Center);
            for (int i = 0; i < 3; i++)
            {
                int dust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.RainbowTorch, 0f, 0f, 100, new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB), 1.5f);
                Main.dust[dust].noGravity = true;
            }
            for (int i = 0; i < 30; i++)
            {
                int dust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.RainbowTorch, 0f, 0f, 0, new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB), 2.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 3f;
                dust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.RainbowTorch, 0f, 0f, 100, new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB), 1.5f);
                Main.dust[dust].velocity *= 2f;
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
