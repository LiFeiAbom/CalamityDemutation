using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Magic
{
    /// <summary>
    /// 星火凤凰雨的火焰弹幕—— 移植自 CI Beta1.12 的
    /// <c>Content/Projectiles/Magic/DragonStaffProj.cs</c>。
    /// <para>
    /// 机制照 CI：20×20 判定、穿透 1、<c>extraUpdates 3</c>、不撞图格、不惧水、存活 600 帧；
    /// 出场时完全透明，每帧 <c>alpha -= 3</c> 淡入（源没有"alpha 归零即自毁"的那句，故保持到寿命结束）；
    /// 4 帧火焰动画；消亡时播放 <c>Item14</c> 并炸出 20 颗金币尘 + 35 颗铂金币尘（源用
    /// <c>DustID.GoldCoin</c> / <c>DustID.PlatinumCoin</c>）；命中挂圣火 300 帧，缺灾厄时退回原版燃烧。
    /// </para>
    /// <para>
    /// 与原源的两处差异：① 源的 <c>CIFunction.FramesChanger(Projectile, 4, 3)</c> 只轮播 3 帧，
    /// 而贴图与 <c>Main.projFrames</c> 都是 4 帧，这里按图集循环 0~3；② 圣火改走本工程的
    /// <see cref="CalamityDemutationPlayer.ApplyCalamityBuffWithFallback"/>，不硬引用灾厄类型。
    /// </para>
    /// </summary>
    internal class PhoenixFlame : ModProjectile
    {
        /// <summary>贴图共 4 帧（20×120，每帧 20×30）</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }
        /// <summary>基础属性全部照 CI 的 <c>DragonStaffProj</c></summary>
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 3;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 600;
        }
        /// <summary>淡入、火焰 4 帧轮播、橙色微光（照 CI；帧数按图集修正为 4）</summary>
        public override void AI()
        {
            Projectile.alpha -= 3;
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= Main.projFrames[Type])
            {
                Projectile.frame = 0;
            }
            Lighting.AddLight(Projectile.Center, 0.5f, 0.5f, 0f);
        }
        /// <summary>消亡：照 CI 把判定临时放大到 50×50，再分别喷金币尘与铂金币尘</summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
            Projectile.position.X += Projectile.width / 2;
            Projectile.position.Y += Projectile.height / 2;
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.position.X -= Projectile.width / 2;
            Projectile.position.Y -= Projectile.height / 2;
            for (int i = 0; i < 20; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldCoin, 0f, 0f, 100, default, 2f);
                Main.dust[dust].velocity *= 3f;
                if (Main.rand.NextBool(2))
                {
                    Main.dust[dust].scale = 0.5f;
                    Main.dust[dust].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
            }
            for (int i = 0; i < 35; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.PlatinumCoin, 0f, 0f, 100, default, 3f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 5f;
                dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldCoin, 0f, 0f, 100, default, 2f);
                Main.dust[dust].velocity *= 2f;
            }
        }
        /// <summary>命中挂圣火 300 帧（照 CI；经典版灾厄没有 HolyFlames 时退回原版燃烧）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "HolyFlames", 300, BuffID.OnFire);
        }
    }
}
