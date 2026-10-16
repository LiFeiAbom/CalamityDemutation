using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Magic
{
    /// <summary>
    /// SHPC 的能量束（源：本体 2.0 <c>Projectiles/Magic/SHPL</c>）——右键打出，是一条会伸缩的光束条。
    /// <para>
    /// 本体是工程共用的 6×10 白条 <c>Content/Projectiles/LaserProj</c>，靠 <c>DrawBeam</c> 沿速度方向盖印成长条：
    /// 出场时 <c>localAI[0]</c> 每帧涨 3 直到 100（`ai[1]` 一旦置 1 就反向缩回、缩到 0 自毁）。
    /// 判定 5×5、穿透 1、`extraUpdates = 3`（照源，高速飞行）、寿命 600 帧；死亡时撒 246 / 73 / 187 三色尘。
    /// </para>
    /// </summary>
    internal class SHPCLaser : ModProjectile
    {
        /// <summary>光束渐隐总长（照源 <c>DrawBeam(100f, 3f, …)</c> 的第一个参数）</summary>
        private const float BeamLength = 100f;
        /// <summary>每帧的长度变化量（照源 3）</summary>
        private const float BeamLengthChangeRate = 3f;
        /// <summary>盖印间距（照源 <c>DrawBeam(100f, 3f, …)</c> 的第二个参数）</summary>
        private const float BeamStampSpacing = 3f;
        /// <summary>光束条贴图走工程共用的 LaserProj（照源同样借共用的 LaserProj）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/LaserProj";
        /// <summary>判定 5×5、穿透 1、额外更新 3 帧、寿命 600 帧、法师伤害、出场全透明（照源）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 5;
            Projectile.height = 5;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 3;
            Projectile.timeLeft = 600;
            Projectile.DamageType = DamageClass.Magic;
        }
        /// <summary>淡入、发光、长度伸缩（照源）</summary>
        public override void AI()
        {
            if (Projectile.alpha > 0)
                Projectile.alpha -= 25;
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;
            Lighting.AddLight((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16, 0.5f, 0.2f, 0.5f);
            if (Projectile.ai[1] == 0f)
            {
                Projectile.localAI[0] += BeamLengthChangeRate;
                if (Projectile.localAI[0] > BeamLength)
                    Projectile.localAI[0] = BeamLength;
            }
            else
            {
                Projectile.localAI[0] -= BeamLengthChangeRate;
                if (Projectile.localAI[0] <= 0f)
                {
                    Projectile.Kill();
                    return;
                }
            }
        }
        /// <summary>彩虹色（照源）</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(255, Main.DiscoG, 155, Projectile.alpha);
        /// <summary>用工程共用的 DrawBeam 画光束条（照源）</summary>
        public override bool PreDraw(ref Color lightColor) => Projectile.DrawBeam(BeamLength, BeamStampSpacing, lightColor);
        /// <summary>消散时撒 246 / 73 / 187 三色尘（照源）</summary>
        public override void OnKill(int timeLeft)
        {
            int dustAmount = Main.rand.Next(3, 7);
            for (int i = 0; i < dustAmount; i++)
            {
                int dustType = Utils.SelectRandom(Main.rand, [246, 73, 187]);
                int dustIndex = Dust.NewDust(Projectile.Center - Projectile.velocity / 2f, 0, 0, dustType, 0f, 0f, 100, default, 2.1f);
                Main.dust[dustIndex].velocity *= 2f;
                Main.dust[dustIndex].noGravity = true;
            }
        }
    }
}
