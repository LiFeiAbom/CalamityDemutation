using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.BaseProjectiles
{
    /// <summary>
    /// 激光光束基类（移植自灾厄 BaseLaserbeamProjectile）：统一处理"跟随、伸缩、碰撞、切割地形"的激光管线。
    /// 子类只需给出 Lifetime/MaxScale/MaxLaserLength 与三截贴图，并按需覆写
    /// UpdateLaserMotion（默认做匀速扫射）、DetermineScale（默认按寿命取正弦）、AttachToSomething（默认空）、
    /// DetermineLaserLength（默认不撞地形，可用 DetermineLaserLength_CollideWithTiles 走精确撞墙）与 ExtraBehavior（相当于 PostAI）。
    /// 与灾厄原版的区别：DetermineLaserLength_CollideWithTiles 改用本工程的 CDUtil.PreciseDistanceToTileCollisionHit。
    /// </summary>
    internal abstract class BaseLaserbeamCO : ModProjectile
    {
        // ── 属性映射（与灾厄一致地复用 ai[0] / localAI[0] / localAI[1]） ──
        /// <summary>每帧转动的角速度（存于 ai[0]）</summary>
        public float RotationalSpeed
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }
        /// <summary>存活帧数（存于 localAI[0]）</summary>
        public float Time
        {
            get => Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }
        /// <summary>当前激光长度（存于 localAI[1]，以 0.9 的系数快速逼近理想长度）</summary>
        public float LaserLength
        {
            get => Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }
        // ── 主流程 ──
        /// <summary>
        /// 激光的全部 AI：先处理附着（AttachToSomething）→ 把速度单位化（带 NaN 兜底）→ 计帧并在到寿时自毁 →
        /// 算缩放、更新指向、逼近理想长度，最后沿激光线投光。
        /// </summary>
        public virtual void Behavior()
        {
            // 可选的附着逻辑（灾厄把 ai[1] 预留给原版死亡射线那类附着用）
            AttachToSomething();
            // 速度单位化，NaN 时回退到正上方
            Projectile.velocity = Projectile.velocity.SafeNormalize(-Vector2.UnitY);
            Time++;
            if (Time >= Lifetime)
            {
                Projectile.Kill();
                return;
            }
            DetermineScale();
            UpdateLaserMotion();
            float idealLaserLength = DetermineLaserLength();
            LaserLength = MathHelper.Lerp(LaserLength, idealLaserLength, 0.9f);   // 以很快的速度逼近理想长度
            if (LightCastColor != Color.Transparent)
            {
                DelegateMethods.v3_1 = LightCastColor.ToVector3();
                Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, Projectile.width * Projectile.scale, DelegateMethods.CastLight);
            }
        }
        /// <summary>
        /// 更新激光指向。默认做匀速扫射：把速度转成角度后加上 RotationalSpeed 再转回速度；
        /// 同帧把 Projectile.rotation 对齐为"角度 - 90°"（多数激光贴图是竖排的）。
        /// </summary>
        public virtual void UpdateLaserMotion()
        {
            float updatedVelocityDirection = Projectile.velocity.ToRotation() + RotationalSpeed;
            Projectile.rotation = updatedVelocityDirection - MathHelper.PiOver2;
            Projectile.velocity = updatedVelocityDirection.ToRotationVector2();
        }
        /// <summary>
        /// 计算激光缩放。默认按寿命取正弦，再用 ScaleExpandRate 放大并钳在 MaxScale 以内
        /// </summary>
        public virtual void DetermineScale()
        {
            Projectile.scale = (float)Math.Sin(Time / Lifetime * MathHelper.Pi) * ScaleExpandRate * MaxScale;
            if (Projectile.scale > MaxScale)
                Projectile.scale = MaxScale;
        }
        /// <summary>附着到某个对象/位置（灾厄把 ai[1] 预留给这项用途）。默认什么都不做。</summary>
        public virtual void AttachToSomething() { }
        /// <summary>
        /// 计算当前激光长度。默认不撞地形（返回 MaxLaserLength）；
        /// 需要撞墙截断时改用 <see cref="DetermineLaserLength_CollideWithTiles"/>
        /// </summary>
        public virtual float DetermineLaserLength() => MaxLaserLength;
        /// <summary>常规 AI 全部走完后的额外逻辑（相当于 PostAI）。默认什么都不做。</summary>
        public virtual void ExtraBehavior() { }
        // ── 工具 ──
        /// <summary>
        /// 计算"撞到地形为止"的激光长度（改用本工程的精确撞墙距离）
        /// </summary>
        public float DetermineLaserLength_CollideWithTiles()
        {
            return CDUtil.PreciseDistanceToTileCollisionHit(Projectile.Center, Projectile.velocity.ToRotation(), MaxLaserLength);
        }
        /// <summary>
        /// 用三截贴图（起始 / 中段循环 / 末端）画出整条光束：先画起始段，再按中段贴图高度逐段铺满，
        /// 末端只在"当前长度已接近理想长度"时补上，避免截断处露出突兀的断口。
        /// </summary>
        protected internal void DrawBeamWithColor(Color beamColor, float scale, int startFrame = 0, int middleFrame = 0, int endFrame = 0)
        {
            Rectangle startFrameArea = LaserBeginTexture.Frame(1, Main.projFrames[Type], 0, startFrame);
            Rectangle middleFrameArea = LaserMiddleTexture.Frame(1, Main.projFrames[Type], 0, middleFrame);
            Rectangle endFrameArea = LaserEndTexture.Frame(1, Main.projFrames[Type], 0, endFrame);
            Main.EntitySpriteDraw(LaserBeginTexture, Projectile.Center - Main.screenPosition, startFrameArea, beamColor, Projectile.rotation, LaserBeginTexture.Size() / 2f, scale, SpriteEffects.None, 0);
            // 中段：总长扣掉起始段半高与末端整高后逐段铺
            float laserBodyLength = LaserLength;
            laserBodyLength -= (startFrameArea.Height / 2 + endFrameArea.Height) * scale;
            Vector2 centerOnLaser = Projectile.Center;
            centerOnLaser += Projectile.velocity * scale * startFrameArea.Height / 2f;
            if (laserBodyLength > 0f)
            {
                float laserOffset = middleFrameArea.Height * scale;
                float incrementalBodyLength = 0f;
                while (incrementalBodyLength + 1f < laserBodyLength)
                {
                    Main.EntitySpriteDraw(LaserMiddleTexture, centerOnLaser - Main.screenPosition, middleFrameArea, beamColor, Projectile.rotation, LaserMiddleTexture.Width * 0.5f * Vector2.UnitX, scale, SpriteEffects.None, 0);
                    incrementalBodyLength += laserOffset;
                    centerOnLaser += Projectile.velocity * laserOffset;
                }
            }
            // 末端：只在长度已基本收敛时画
            if (Math.Abs(LaserLength - DetermineLaserLength()) < 30f)
            {
                Vector2 laserEndCenter = centerOnLaser - Main.screenPosition;
                Main.EntitySpriteDraw(LaserEndTexture, laserEndCenter, endFrameArea, beamColor, Projectile.rotation, LaserEndTexture.Frame(1, 1, 0, 0).Top(), scale, SpriteEffects.None, 0);
            }
        }
        // ── 钩子覆写 ──
        /// <summary>每帧先把绘制判定范围放宽到 10000（超长光束在屏幕外也要参与绘制），再跑 Behavior + ExtraBehavior</summary>
        public override void AI()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
            Behavior();
            ExtraBehavior();
        }
        /// <summary>沿激光线切割草等地形（近战攻击的切割上下文）</summary>
        public override void CutTiles()
        {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackMelee;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, Projectile.Size.Length() * Projectile.scale, DelegateMethods.CutTiles);
        }
        /// <summary>自绘三截光束；速度为 0（不应该发生）时直接不画</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.velocity == Vector2.Zero)
                return false;
            DrawBeamWithColor(LaserOverlayColor, Projectile.scale);
            return false;
        }
        /// <summary>线判定：命中框相交，或从激光中心沿指向、宽度取弹幕对角线长度做线段检测</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (projHitbox.Intersects(targetHitbox))
                return true;
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, Projectile.Size.Length() * Projectile.scale, ref _);
        }
        /// <summary>位置每帧由 AI 自行指定，不走原版的位置积分</summary>
        public override bool ShouldUpdatePosition() => false;
        // ── 子类必须/可选提供 ──
        /// <summary>存活帧数上限</summary>
        public abstract float Lifetime { get; }
        /// <summary>缩放上限</summary>
        public abstract float MaxScale { get; }
        /// <summary>激光长度上限（过大会引起卡顿）</summary>
        public abstract float MaxLaserLength { get; }
        /// <summary>起始段贴图</summary>
        public abstract Texture2D LaserBeginTexture { get; }
        /// <summary>中段循环贴图</summary>
        public abstract Texture2D LaserMiddleTexture { get; }
        /// <summary>末端贴图</summary>
        public abstract Texture2D LaserEndTexture { get; }
        /// <summary>正弦缩放的放大倍率（默认 4）</summary>
        public virtual float ScaleExpandRate => 4f;
        /// <summary>沿激光线投出的光照色（默认白色；给 Transparent 可完全关闭投光）</summary>
        public virtual Color LightCastColor => Color.White;
        /// <summary>绘制光束用的叠加色（默认白色 0.9 倍）</summary>
        public virtual Color LaserOverlayColor => Color.White * 0.9f;
    }
}
