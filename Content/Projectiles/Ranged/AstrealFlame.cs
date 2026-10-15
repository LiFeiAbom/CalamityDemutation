using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 星穹烈焰（Astreal Flame） - 星穹箭飞行途中喷出的追踪火焰，＝灾厄 2.0 的
    /// <c>Projectiles/Ranged/AstrealFlame.cs</c>（逐行照搬）。
    /// <para>
    /// 判定 20×20、穿透 1、寿命 180 帧、初始全透明（每帧 -5 alpha 淡入）、**4 帧循环动画**（每 5 次更新推一帧）。
    /// **贴图借用工程已有的 `Melee/EssenceFlame2.png`**（源就是借灾厄 Healing/EssenceFlame 的同一张 4 帧表，
    /// 本工程那份 EssenceFlame2 正是从那张表拷来的）。
    /// </para>
    /// <para>
    /// 前 30 帧**不可命中**（<c>CanHitNPC</c> 要求 <c>timeLeft &lt; 150</c>）且不做追踪；
    /// 之后每帧朝 600 像素内最近的敌人追踪（追踪速度 12、惯性 20，走工程既有的
    /// <c>CDUtil.HomeInOnNPC</c>，等价于源的 <c>CalamityGlobalProjectile.HomeInOnNPC</c>）。
    /// 消亡：判定框撑到 **48×48**（以中心为轴）、响 `Item74`，并撒 10 颗（缩放 2、速度 ×3，半数改小缩放 + 淡入）
    /// 与 15 组（缩放 3 无重力 ×5 + 缩放 2 ×2）暗影束尘。
    /// </para>
    /// </summary>
    internal class AstrealFlame : ModProjectile
    {
        /// <summary>4 帧动画</summary>
        private const int FrameCount = 4;
        /// <summary>每帧的推进间隔（照源 <c>frameCounter &gt; 4</c>）</summary>
        private const int FrameAdvanceInterval = 4;
        /// <summary>淡入速度（照源每帧 -5 alpha）</summary>
        private const int AlphaFadeStep = 5;
        /// <summary>寿命剩多少以内才开始可命中 / 追踪（照源 <c>timeLeft &lt; 150</c>，即前 30 帧不做）</summary>
        private const int DelayFrames = 150;
        /// <summary>追踪半径 / 追踪速度 / 惯性（照源 600 / 12 / 20）</summary>
        private const float HomingRange = 600f;
        private const float HomingVelocity = 12f;
        private const float HomingInertia = 20f;
        /// <summary>消亡时判定框撑到多大（照源 48×48）</summary>
        private const int DeathHitboxSize = 48;
        /// <summary>消亡的几组尘：枚数 / 缩放（照源 10 颗、缩放 2 与 3 与 2）</summary>
        private const int DeathDustCount = 10;
        private const float DeathDustScaleA = 2f;
        private const float DeathDustScaleB = 3f;
        private const int DeathDustRadialCount = 15;
        /// <summary>贴图借用工程已有的精魂烈焰表（源同样借 Healing/EssenceFlame）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/Melee/EssenceFlame2";
        /// <summary>4 帧动画（照源）</summary>
        public override void SetStaticDefaults() => Main.projFrames[Projectile.type] = FrameCount;
        /// <summary>基础属性：20×20 判定、穿透 1、寿命 180 帧、初始全透明（照源）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.alpha = 255;
        }
        /// <summary>前 30 帧不给命中判定（照源）</summary>
        public override bool? CanHitNPC(NPC target) => Projectile.timeLeft < DelayFrames && target.CanBeChasedBy(Projectile);
        /// <summary>AI：淡入 + 推帧动画 + 满 30 帧后开始追踪（照源）</summary>
        public override void AI()
        {
            Projectile.alpha -= AlphaFadeStep;

            Projectile.frameCounter++;
            if (Projectile.frameCounter > FrameAdvanceInterval)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > FrameCount - 1)
                Projectile.frame = 0;

            if (Projectile.timeLeft < DelayFrames)
                CDUtil.HomeInOnNPC(Projectile, !Projectile.tileCollide, HomingRange, HomingVelocity, HomingInertia);
        }
        /// <summary>消亡：判定框撑到 <see cref="DeathHitboxSize"/> 见方（以中心为轴）+ `Item74` + 撒两组暗影束尘（照源）</summary>
        public override void OnKill(int timeLeft)
        {
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = DeathHitboxSize;
            Projectile.position.X -= Projectile.width / 2;
            Projectile.position.Y -= Projectile.height / 2;

            SoundEngine.PlaySound(SoundID.Item74, Projectile.Center);

            for (int i = 0; i < DeathDustCount; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default, DeathDustScaleA);
                Main.dust[dust].velocity *= 3f;
                if (Main.rand.NextBool(2))
                {
                    Main.dust[dust].scale = 0.5f;
                    Main.dust[dust].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
            }

            for (int i = 0; i < DeathDustRadialCount; i++)
            {
                int bigDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default, DeathDustScaleB);
                Main.dust[bigDust].noGravity = true;
                Main.dust[bigDust].velocity *= 5f;

                int smallDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default, DeathDustScaleA);
                Main.dust[smallDust].velocity *= 2f;
            }
        }
    }
}
