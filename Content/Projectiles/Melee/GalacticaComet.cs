using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 星河彗星（GalacticaComet）—— 照搬灾厄 2.0.4 的 <c>Projectiles/Melee/GalacticaComet.cs</c>：
    /// 34×34、穿透 1、extraUpdates 1、存活 600 帧；出生 90 帧内不撞地形，减到 0 才开 <c>tileCollide</c>；
    /// 每 18 帧朝四周铺 12 颗双色尘（传送药水色/天柱色），另有随机尘与 gore 作点缀；
    /// 出生后每帧淡入（alpha 从 100 每帧 −15，夹到 0）；击杀时把自身放大到半径 68、爆两色尘并再结算一次伤害。
    /// <para>
    /// 与源的差异：① 神圣火焰走软依赖施加（缺灾厄该 buff 时静默跳过）；
    /// ② 拖影调本工程的 <c>CDUtil.DrawAfterimagesCentered</c>；③ 补一份 PvP 命中（本工程约定）。
    /// </para>
    /// </summary>
    internal class GalacticaComet:ModProjectile
    {
        /// <summary>撞地形倒计时：出生时 90，每帧减 1，减到 0 才打开 tileCollide</summary>
        private int noTileHitCounter = 90;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 1;
        }
        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.alpha = 100;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 600;
            Projectile.ignoreWater = true;
        }
        public override void AI()
        {
            noTileHitCounter -= 1;
            if (noTileHitCounter == 0)
            {
                Projectile.tileCollide = true;
            }
            if (Projectile.soundDelay == 0)
            {
                Projectile.soundDelay = 20 + Main.rand.Next(40);
                if (Main.rand.NextBool(5))
                {
                    SoundEngine.PlaySound(SoundID.Item9, Projectile.position);
                }
            }
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] == 18f)
            {
                Projectile.localAI[0] = 0f;
                // 12 颗尘按 12 等分角铺在弹幕前方的一圈上，速度指向弹幕自身（拖尾收束感）
                for (int l = 0; l < 12; l++)
                {
                    Vector2 dustRotate = Vector2.UnitX * (float)-(float)Projectile.width / 2f;
                    dustRotate += -Vector2.UnitY.RotatedBy((double)((float)l * MathHelper.Pi / 6f), default) * new Vector2(8f, 16f);
                    dustRotate = dustRotate.RotatedBy((double)(Projectile.rotation - MathHelper.PiOver2), default);
                    int galactic = Dust.NewDust(Projectile.Center, 0, 0, Main.rand.NextBool() ? DustID.TeleportationPotion : DustID.Vortex, 0f, 0f, 160, default, 1f);
                    Main.dust[galactic].noGravity = true;
                    Main.dust[galactic].position = Projectile.Center + dustRotate;
                    Main.dust[galactic].velocity = Projectile.velocity * 0.1f;
                    Main.dust[galactic].velocity = Vector2.Normalize(Projectile.Center - Projectile.velocity * 3f - Main.dust[galactic].position) * 1.25f;
                }
            }
            // 淡入：每帧减 15，落到 y 低于 ai[1] 之前先停在 150，越过后降到 0
            Projectile.alpha -= 15;
            int alphaControl = 150;
            if (Projectile.Center.Y >= Projectile.ai[1])
            {
                alphaControl = 0;
            }
            if (Projectile.alpha < alphaControl)
            {
                Projectile.alpha = alphaControl;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            if (Main.rand.NextBool(16))
            {
                Vector2 rotation = Vector2.UnitX.RotatedByRandom(MathHelper.PiOver2).RotatedBy((double)Projectile.velocity.ToRotation(), default);
                int pinkDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.TeleportationPotion, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f, 150, default, 1f);
                Main.dust[pinkDust].velocity = rotation * 0.66f;
                Main.dust[pinkDust].position = Projectile.Center + rotation * 12f;
            }
            if (Main.rand.NextBool(48) && Main.netMode != NetmodeID.Server)
            {
                int gored = Gore.NewGore(Projectile.GetSource_FromAI(), Projectile.Center, new Vector2(Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f), 16, 1f);
                Main.gore[gored].velocity *= 0.66f;
                Main.gore[gored].velocity += Projectile.velocity * 0.3f;
            }
            // ai[1] 为 1 的彗星（约 1/10）额外发光并多洒天柱尘/云 gore
            if (Projectile.ai[1] == 1f)
            {
                Projectile.light = 0.5f;
                if (Main.rand.NextBool(10))
                {
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Vortex, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f, 150, default, 1f);
                }
                if (Main.rand.NextBool(20) && Main.netMode != NetmodeID.Server)
                {
                    Gore.NewGore(Projectile.GetSource_FromAI(), Projectile.position, new Vector2(Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f), Main.rand.Next(16, 18), 1f);
                }
            }
        }
        /// <summary>自绘：出生 5 帧内不画（避免在天上凭空出现），之后只画拖影（2 帧抽稀间隔）、本体不单独绘制</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.timeLeft > 595)
            {
                return false;
            }
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 2);
            return false;
        }
        /// <summary>染色：彩虹 R 通道 + 固定 100/255 + 随 alpha 淡入，呈蓝紫渐变的星河色</summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(Main.DiscoR, 100, 255, Projectile.alpha);
        }
        public override void OnKill(int timeLeft)
        {
            // ai[0] 为 1 的变体不做爆炸（源保留的分支）
            if (Projectile.ai[0] == 1f)
            {
                return;
            }
            SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 68;
            Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
            for (int i = 0; i < 4; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.TeleportationPotion, 0f, 0f, 50, default, 1.5f);
            }
            for (int j = 0; j < 20; j++)
            {
                int galaxyDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Vortex, 0f, 0f, 0, default, 2.5f);
                Main.dust[galaxyDust].noGravity = true;
                Main.dust[galaxyDust].velocity *= 3f;
                galaxyDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Vortex, 0f, 0f, 50, default, 1.5f);
                Main.dust[galaxyDust].velocity *= 2f;
                Main.dust[galaxyDust].noGravity = true;
            }
            Projectile.maxPenetrate = -1;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.Damage();
        }
        /// <summary>命中敌人：挂 180 帧神圣火焰（非远程弹幕才施加，照抄源的守卫）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.DamageType != DamageClass.Ranged)
            {
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "HolyFlames", 180);
            }
        }
        /// <summary>命中玩家（PvP）：与 OnHitNPC 同构，挂同样的神圣火焰</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Projectile.DamageType != DamageClass.Ranged)
            {
                CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "HolyFlames", 180);
            }
        }
    }
}
