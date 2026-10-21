using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Magic
{
    /// <summary>
    /// 光之舞的主火焰 —— 移植自 旧版支线的 <c>WhiteFlameLegacy</c>。
    /// <para>
    /// 12×12、隐身贴图、<c>extraUpdates 2</c>、不撞图格、120 帧；每帧 5 颗钻石尘；
    /// 10 帧后在 700 像素内追踪（追踪速度 16、惯性 20）；消亡时分裂 2 枚
    /// <see cref="WhiteFlameAlt"/>，命中挂圣火 360 帧（缺灾厄退回原版燃烧）。
    /// </para>
    /// </summary>
    internal class WhiteFlame : ModProjectile
    {
        /// <summary>与工程其它隐形弹幕共用占位贴图</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>基础属性照旧版支线</summary>
        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 2;
            Projectile.timeLeft = 120;
            Projectile.DamageType = DamageClass.Magic;
        }
        /// <summary>白色微光、钻石尘、10 帧后开始追踪；主人死亡时缩短寿命</summary>
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.5f / 255f,
                (255 - Projectile.alpha) * 0.5f / 255f, (255 - Projectile.alpha) * 0.5f / 255f);
            for (int i = 0; i < 5; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.GemDiamond, 0f, 0f, 100, default, 1.2f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.5f;
                Main.dust[dust].velocity += Projectile.velocity * 0.1f;
            }

            Projectile.localAI[0] += 1f;
            if (owner.active && !owner.dead)
            {
                if (Projectile.Distance(owner.Center) > 1000f)
                {
                    return;
                }
                if (Projectile.localAI[0] > 10f)
                {
                    CDUtil.HomeInOnNPC(Projectile, true, 700f, 16f, 20f);
                }
            }
            else if (Projectile.timeLeft > 30)
            {
                Projectile.timeLeft = 30;
            }
        }
        /// <summary>消亡音与 2 枚随机方向的分裂火焰（仅在主人端生成，照旧版支线）</summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item122, Projectile.position);
            if (Projectile.owner != Main.myPlayer)
            {
                return;
            }
            for (int i = 0; i < 2; i++)
            {
                Vector2 speed = new(Main.rand.Next(-100, 101), Main.rand.Next(-100, 101));
                while (speed.X == 0f && speed.Y == 0f)
                {
                    speed = new Vector2(Main.rand.Next(-100, 101), Main.rand.Next(-100, 101));
                }
                speed.Normalize();
                speed *= Main.rand.Next(70, 101) * 0.1f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                    Projectile.oldPosition.X + Projectile.width / 2, Projectile.oldPosition.Y + Projectile.height / 2,
                    speed.X, speed.Y, ModContent.ProjectileType<WhiteFlameAlt>(), Projectile.damage, 0f, Projectile.owner, 0f, 0f);
            }
        }
        /// <summary>命中挂圣火 360 帧（照旧版支线）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "HolyFlames", 360, BuffID.OnFire);
        }
    }
}
