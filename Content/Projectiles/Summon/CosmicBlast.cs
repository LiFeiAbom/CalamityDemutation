using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 宇宙之灵的小爆裂（照 CI 的 <c>Content/Projectiles/ExoLore/CosmicBlastExoLore.cs</c> 移植，
    /// 即 ExoLore 分支那一枚；本工程常驻 Lore 模式，故不留普通版）。
    /// 18×18 判定、4 帧动画、穿透 1、寿命 300；出场先减速，之后加速追踪 1500 像素内的敌人。
    /// 命中挂整套星云系减益（<see cref="CDUtil.ExoDebuffs"/>），消失时把自己撑到 144×144 再打一次范围伤害。
    /// </summary>
    internal class CosmicBlast:ModProjectile
    {
        /// <summary>4 帧动画；属于仆从弹药；残影缓存 4 格</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }
        /// <summary>基础属性：照源（18×18、穿透 1、寿命 300、不占召唤栏、召唤伤害）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.minionSlots = 0f;
            Projectile.minion = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Summon;
        }
        /// <summary>出场减速 → 加速追踪（照源）</summary>
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 3)
            {
                Projectile.frame = 0;
            }
            // 彩虹火星（源写 DustID.RainbowTorch + 当帧彩虹色）
            if (Main.rand.NextBool(8))
            {
                Vector2 offset = Vector2.UnitX.RotatedByRandom(MathHelper.PiOver2).RotatedBy(Projectile.velocity.ToRotation());
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.RainbowTorch, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f, 150, new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB), 1.2f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = offset * 0.66f;
                Main.dust[dust].position = Projectile.Center + offset * 12f;
            }

            // 出生后 40 帧内持续减速
            if (Projectile.timeLeft > 260)
            {
                Projectile.velocity *= 0.96f;
            }
            // 之后把追踪速度逐步提到上限 20，并追踪 1500 像素内的敌人
            if (Projectile.timeLeft < 260)
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
        /// <summary>自绘：4 帧本体 + 残影（照源用灾厄的 DrawAfterimagesCentered，本工程用同名移植件）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            int frameY = frameHeight * Projectile.frame;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Rectangle?(new Rectangle(0, frameY, texture.Width, frameHeight)), Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2((float)texture.Width / 2f, (float)frameHeight / 2f), Projectile.scale, SpriteEffects.None, 0);

            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 3);
            return false;
        }
        /// <summary>消失：把自己撑到 144×144 打一次范围伤害，再喷一圈彩虹尘（照源）</summary>
        public override void OnKill(int timeLeft)
        {
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 144;
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
