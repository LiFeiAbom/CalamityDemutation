using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 精准弩箭（PrecisionBolt） - 发条弓齐射的弩箭，＝灾厄 2.0 的
    /// <c>Projectiles/Ranged/PrecisionBolt.cs</c>（逐行照搬）。
    /// <para>
    /// 判定 **72×72**（贴图只有 14×70，判定远大于贴图）/ 穿透 1 / 寿命 119 帧 /
    /// **额外更新 1 帧**（源写 1.3 口径的 <c>MaxUpdates = 2</c>）/ 射手伤害类型 / 不受水阻 / **穿墙**。
    /// </para>
    /// <para>
    /// 追踪两段式（照源）：平时朝 512 像素内最近的敌人**轻追踪**（2.5°/帧）；
    /// 每 6 帧做一次**重整**——按"越接近消亡、转向幅度越小"的四次方权重算出转向角，
    /// 左右各试一次取更接近目标的那边（响 `Item93`）。
    /// 尾迹是 `DustID.LastPrism`（源写裸值 267）染成黄色；消亡再爆 10 颗同样的尘并响 `Item94`。
    /// </para>
    /// <para>
    /// 与源的一点差别：源的 <c>ClosestNPCAt</c> / <c>AngleBetween</c> 是灾厄工具函数，
    /// 本工程分别用既有的 <c>FindClosestNPC</c> 与下面的本地 <see cref="AngleBetween(Vector2, Vector2)"/> 顶。
    /// </para>
    /// </summary>
    internal class PrecisionBolt : ModProjectile
    {
        /// <summary>找目标的半径（照源 512）</summary>
        private const float TargetSearchRange = 512f;
        /// <summary>轻追踪的每帧转向上限（照源 2.5°）</summary>
        private const float HomingTurnRadians = MathHelper.Pi * 2.5f / 180f;
        /// <summary>重整的间隔帧数（照源 6）</summary>
        private const int RecalibrateInterval = 6;
        /// <summary>重整的最大转向角（照源 75°）</summary>
        private const float RecalibrateMaxTurnRadians = MathHelper.Pi * 75f / 180f;
        /// <summary>消亡时爆出的尘数（照源 10）</summary>
        private const int DeathDustCount = 10;
        /// <summary>当前锁定的目标（源用实例字段缓存，命中前才会重选）</summary>
        private NPC potentialTarget;
        /// <summary>
        /// 基础属性：72×72 判定、穿透 1、寿命 119 帧、额外更新 1 帧、射手伤害类型、不受水阻、穿墙（照源）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 72;
            Projectile.height = 72;
            Projectile.friendly = true;
            Projectile.timeLeft = 119;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 1;              // 源写 1.3 口径的 MaxUpdates = 2（＝额外更新 1 帧）
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
        }
        /// <summary>
        /// 重整一次的转向结果（照源）：转向幅度按"剩余寿命越短越温柔"的四次方权重衰减，
        /// 左右各试一次，取与本目标夹角更小的那一边
        /// </summary>
        private Vector2 Recalibrate()
        {
            float turnSpeedFactor = (float)Math.Pow(MathHelper.Clamp(Projectile.timeLeft - 40, 0f, 120f) / 120f, 4D);
            float turnAngle = turnSpeedFactor * RecalibrateMaxTurnRadians;

            Vector2 leftTurnVelocity = Projectile.velocity.RotatedBy(-turnAngle);
            Vector2 rightTurnVelocity = Projectile.velocity.RotatedBy(turnAngle);
            Vector2 toTarget = potentialTarget.Center - Projectile.Center;
            float leftDirectionImprecision = AngleBetween(leftTurnVelocity, toTarget);
            float rightDirectionImprecision = AngleBetween(rightTurnVelocity, toTarget);
            potentialTarget = Projectile.Center.FindClosestNPC(TargetSearchRange, true);   // 每次重整顺手重选目标（照源）

            return leftDirectionImprecision < rightDirectionImprecision ? leftTurnVelocity : rightTurnVelocity;
        }
        /// <summary>
        /// AI（照源）：点一盏灯 + 按速度摆正贴图 → 没目标就选 512 像素内最近的敌人 →
        /// 轻追踪 + 每 6 帧重整一次 → 撒一颗黄色 `DustID.LastPrism` 尾迹
        /// </summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Color.LightSteelBlue.ToVector3());
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (potentialTarget == null)
                potentialTarget = Projectile.Center.FindClosestNPC(TargetSearchRange, true);

            if (potentialTarget != null)
            {
                float idealDirection = Projectile.AngleTo(potentialTarget.Center);
                float updatedDirection = Projectile.velocity.ToRotation().AngleTowards(idealDirection, HomingTurnRadians);
                Projectile.velocity = updatedDirection.ToRotationVector2() * Projectile.velocity.Length();

                if (Projectile.timeLeft % RecalibrateInterval == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item93, Projectile.Center);
                    Projectile.velocity = Recalibrate();
                }
            }

            Dust trail = Dust.NewDustPerfect(Projectile.Center, DustID.LastPrism);
            trail.velocity = Vector2.Zero;
            trail.color = Color.Yellow;
            trail.scale = Main.rand.NextFloat(1f, 1.1f);
            trail.noGravity = true;
        }
        /// <summary>消亡：服务端不画尘；客户端响 `Item94` 并爆 10 颗黄色 `DustID.LastPrism`（照源）</summary>
        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.Item94, Projectile.Center);
            for (int i = 0; i < DeathDustCount; i++)
            {
                Dust zap = Dust.NewDustPerfect(Projectile.Center, DustID.LastPrism);
                zap.velocity = Projectile.velocity;
                zap.color = Color.Yellow;
                zap.scale = Main.rand.NextFloat(1f, 1.1f);
                zap.noGravity = true;
            }
        }
        /// <summary>
        /// 两个方向的夹角（移植自灾厄 <c>CalamityUtils.AngleBetween</c>；tML 的 <c>Utils</c> 里没有对应件）。
        /// 源是扩展方法，本工程写成普通静态方法（扩展方法必须住在非嵌套静态类里）。
        /// </summary>
        private static float AngleBetween(Vector2 v1, Vector2 v2) => (float)Math.Acos(Vector2.Dot(v1.SafeNormalize(Vector2.Zero), v2.SafeNormalize(Vector2.Zero)));
    }
}
