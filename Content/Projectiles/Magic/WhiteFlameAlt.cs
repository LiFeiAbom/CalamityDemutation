using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Magic
{
    /// <summary>
    /// 光之舞的分裂火焰 —— 移植自 CI 的 <c>WhiteFlameAltLegacy</c>（源文件名为 <c>WhiteFlame2.cs</c>）。
    /// 6×6、穿透 3、局部无敌 1 帧、120 帧；每帧 5 颗钻石尘；
    /// 源里 <c>homeTimer</c> 初值 100、条件 &gt;60 恒真，因此等效为**从第一帧起全局追踪**
    /// （1800 像素、速度 16、惯性 5）；命中挂圣火 360 帧。
    /// </summary>
    internal class WhiteFlameAlt : ModProjectile
    {
        /// <summary>与工程其它隐形弹幕共用占位贴图</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>基础属性照 CI</summary>
        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 1;
            Projectile.timeLeft = 120;
            Projectile.DamageType = DamageClass.Magic;
        }
        /// <summary>白色微光、钻石尘、从第一帧起全局追踪（等效 CI 的恒真 homeTimer 分支）</summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.5f / 255f,
                (255 - Projectile.alpha) * 0.5f / 255f, (255 - Projectile.alpha) * 0.5f / 255f);
            for (int i = 0; i < 5; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.GemDiamond, 0f, 0f, 100, default, 0.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.5f;
                Main.dust[dust].velocity += Projectile.velocity * 0.1f;
            }
            CDUtil.HomeInOnNPC(Projectile, true, 1800f, 16f, 5f);
        }
        /// <summary>命中挂圣火 360 帧（照 CI）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "HolyFlames", 360, BuffID.OnFire);
        }
    }
}
