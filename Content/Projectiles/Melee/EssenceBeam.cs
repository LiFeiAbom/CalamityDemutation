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
    /// 宇宙精粹（Essence Beam）—— 移植自灾厄大修 Beta1.12 的 <c>EssenceBeam</c>（源放在 Spear 目录下，供宇宙暗流等长矛共用）。
    /// 粉色能量束：借用原版光束 AI 模板沿初速直飞，穿透 10 次、存活 600 帧，用 6 格残影拉出拖尾，
    /// 命中挂神裁狱火（GodSlayerInferno），消亡时炸开一圈暗影束尘。
    /// <para>
    /// 与 CI 源的差异：
    /// ① 残影绘制换成本工程的 <see cref="CDUtil.DrawAfterimagesCentered"/>（源用灾厄的 <c>CalamityUtils.DrawAfterimagesCentered</c>）；
    /// ② 命中减益走本工程的双版本容错封装（现代版 / 经典版 GodSlayerInferno，两版都取不到时退回原版诅咒地狱）；
    /// ③ 消亡尘的两处速度缩放改成直改 <c>Main.dust[d]</c>——源先取 <c>Dust dust = Main.dust[d]</c> 再改副本，
    ///    Dust 是结构体，那两句缩放其实一次都没生效（这里按源注释的意图还原）；
    /// ④ 源 <c>AIType = 156</c> 是写死的原版光束模板弹幕 ID，本工程按数值写成 <c>ProjectileID.LightBeam</c> 常量。
    /// </para>
    /// </summary>
    internal class EssenceBeam : ModProjectile
    {
        /// <summary>静态属性：预留 6 格残影缓存并启用模式 0（普通残影）——PreDraw 就靠它画拖尾</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }
        /// <summary>基础属性：26×26、借原版光束 AI 模板、近战伤害、穿透 10 次、5 层额外更新、存活 600 帧、逐帧可再命中</summary>
        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.aiStyle = ProjAIStyleID.Beam;   // 源里写死 27：原版光束 AI
            AIType = ProjectileID.LightBeam;           // 源里写死 156（= 原版光束模板弹幕 LightBeam）
            Projectile.ignoreWater = true;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 10;
            Projectile.extraUpdates = 5;
            Projectile.timeLeft = 600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 0;
        }
        /// <summary>每帧发出粉色光照（照源写法：不按 alpha 折算，直接给 0.6 / 0 / 0.2）</summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.6f, 0f, 0.2f);
        }
        /// <summary>固定灰白着色，透明度交回弹幕自身的 alpha（照源）</summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(200, 200, 200, Projectile.alpha);
        }
        /// <summary>
        /// 绘制：本弹幕不画本体，只用 6 格残影拼出拖尾（源如此，效果上就是一条渐隐的能量束）；
        /// 出生后的头 5 帧连残影也不画，避免拖尾从玩家手里硬拉出来。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.timeLeft > 595)
                return false;

            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
        /// <summary>命中：挂 300 帧神裁狱火，并把该目标对主人的受击免疫压到 2 帧（照源，配合穿透 10 次形成"无限穿透、无视无敌帧"的观感）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "GodSlayerInferno", 300, BuffID.CursedInferno);
            target.immune[Projectile.owner] = 2;
        }
        /// <summary>消亡：播放原版音效，并沿速度反方向甩出一串暗影束尘（照源：i 从 4 到 30，位移与速度按 30/i 衰减）</summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
            for (int i = 4; i < 31; i++)
            {
                float pVelX = Projectile.oldVelocity.X * (30f / i);
                float pVelY = Projectile.oldVelocity.Y * (30f / i);
                Vector2 dustPos = new Vector2(Projectile.oldPosition.X - pVelX, Projectile.oldPosition.Y - pVelY);
                // 主尘：暗影束尘 + 自身速度（源注释意图：速度减半）
                int d = Dust.NewDust(dustPos, 8, 8, DustID.ShadowbeamStaff, Projectile.oldVelocity.X, Projectile.oldVelocity.Y, 100, default, 1.8f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 0.5f;
                // 副尘：更小更慢的碎点（源注释意图：速度压到 5%）
                d = Dust.NewDust(dustPos, 8, 8, DustID.ShadowbeamStaff, Projectile.oldVelocity.X, Projectile.oldVelocity.Y, 100, default, 1.4f);
                Main.dust[d].velocity *= 0.05f;
            }
        }
    }
}
