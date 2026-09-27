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
    /// 焚灭流星（BurningMeteor）—— 焚灭天惩（TheBurningSky）从天上砸下的火流星。
    /// 照搬灾厄 2.0.3.9 的 <c>Projectiles/Melee/BurningMeteor.cs</c>：46×46、穿透 1、extraUpdates 2、
    /// 存活 180 帧；出生后 120 帧内不撞地形（<c>noTileHitCounter</c> 每帧随机减 1~3，减到 0 才开 <c>tileCollide</c>），
    /// 每 30 帧朝四周喷 12 颗铜币色尘，命中挂 180 帧龙焰。
    /// 与源的差异：① 灾厄本家龙焰改走软依赖施加（缺灾厄该 buff 时静默跳过）；
    /// ② 拖影调本工程的 <c>CDUtil.DrawAfterimagesCentered</c>；③ 补一份 PvP 命中（本工程约定）。
    /// </summary>
    internal class BurningMeteor:ModProjectile
    {
        /// <summary>撞地形倒计时：出生时 120，每帧随机减 1~3，减到 0 才打开 tileCollide</summary>
        private int noTileHitCounter = 120;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 5;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }
        public override void SetDefaults()
        {
            Projectile.width = 46;
            Projectile.height = 46;
            Projectile.alpha = 150;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 2;
            Projectile.timeLeft = 180;
            Projectile.ignoreWater = true;
        }
        public override void AI()
        {
            int randomToSubtract = Main.rand.Next(1, 4);
            noTileHitCounter -= randomToSubtract;
            if (noTileHitCounter == 0)
            {
                Projectile.tileCollide = true;
            }
            if (Projectile.soundDelay == 0)
            {
                Projectile.soundDelay = 20 + Main.rand.Next(40);
                if (Main.rand.NextBool(5))
                {
                    SoundEngine.PlaySound(SoundID.Item20, Projectile.position);
                }
            }
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] == 30f)
            {
                Projectile.localAI[0] = 0f;
                // 12 颗尘按 12 等分角铺在弹幕前方的一圈上，速度指向弹幕自身（拖尾收束感）
                for (int l = 0; l < 12; l++)
                {
                    Vector2 dustRotate = Vector2.UnitX * (float)-(float)Projectile.width / 2f;
                    dustRotate += -Vector2.UnitY.RotatedBy((double)((float)l * 3.14159274f / 6f), default) * new Vector2(8f, 16f);
                    dustRotate = dustRotate.RotatedBy((double)(Projectile.rotation - 1.57079637f), default);
                    int burntDust = Dust.NewDust(Projectile.Center, 0, 0, DustID.CopperCoin, 0f, 0f, 160, default, 1f);
                    Main.dust[burntDust].scale = 1.1f;
                    Main.dust[burntDust].noGravity = true;
                    Main.dust[burntDust].position = Projectile.Center + dustRotate;
                    Main.dust[burntDust].velocity = Projectile.velocity * 0.1f;
                    Main.dust[burntDust].velocity = Vector2.Normalize(Projectile.Center - Projectile.velocity * 3f - Main.dust[burntDust].position) * 1.25f;
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
        }
        public override void OnKill(int timeLeft)
        {
            for (int k = 0; k < 5; k++)
            {
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.CopperCoin, 0f, 0f);
            }
        }
        /// <summary>命中敌人：挂 180 帧龙焰（灾厄本家 debuff，缺该 buff 时静默跳过）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "Dragonfire", 180);
        }
        /// <summary>命中玩家（PvP）：与 OnHitNPC 同构，挂同样的龙焰</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "Dragonfire", 180);
        }
        /// <summary>自绘：出生 5 帧内不画（避免在天上凭空出现），之后只画拖影、本体不单独绘制</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.timeLeft > 175)
                return false;
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
            return false;
        }
    }
}
