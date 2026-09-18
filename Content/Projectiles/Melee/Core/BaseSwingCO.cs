using CalamityDemutation.Common.Effects;
using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Content.Projectiles.BaseProjectiles;
using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee.Core
{
    /// <summary>
    /// 挥砍类手持弹幕基类（移植自 CWR 的 BaseSwingProj 体系）：
    /// 统一处理「持握定位 → 挥舞弧线 → 弧光采样与渲染 → 发射子弹幕」的整条管线。
    /// 子类通常只需重写 SetSwingProperty/SwingAI/Shoot 与若干视觉参数即可形成一件挥砍武器。
    /// </summary>
    internal abstract class BaseSwingCO : BaseHeldProjCO
    {
        // ── 实例字段 ──
        /// <summary>
        /// 是否绘制弧光（受配置项 EnableSwordLight 的总开关约束），默认为 false
        /// </summary>
        private bool _canDrawSlashTrail;
        /// <summary>
        /// 是否跟随玩家进行朝向纠正，默认为 true
        /// </summary>
        protected bool canFormOwnerSetDir = true;
        /// <summary>
        /// 是否自动设置玩家手臂动作，默认为 true
        /// </summary>
        protected bool canSetOwnerArmBver = true;
        /// <summary>
        /// 本帧是否处于「发射窗口」（Time 到达 maxSwingTime × shootSengs），由 PreUpdate 每帧计算
        /// </summary>
        protected bool canShoot;
        /// <summary>
        /// 绘制刀光时是否应用高光渲染，默认为 true
        /// </summary>
        protected bool drawTrailHighlight = true;
        /// <summary>
        /// 自发光：为 true 时 DrawSwing 强制把刀身染成纯白，忽略环境光照。
        /// 本模组现有子类均未赋值，故恒为 false，
        /// 目前只有配置项 WeaponAdaptiveIllumination 关闭时才会走纯白分支；
        /// 将来某把武器要自发光，在它的 SetSwingProperty 里置位即可。
        /// </summary>
        public bool Incandescence = false;
        /// <summary>
        /// 绘制中是否进行对角线翻转
        /// </summary>
        protected bool inDrawFlipdiagonally;
        /// <summary>
        /// 刀光纹理倾斜采样：作为 shader 参数 obliqueSampling 传给 KnifeRendering。
        /// 本模组现有子类均未赋值，故恒为 false，即倾斜采样实际未启用。
        /// </summary>
        public bool ObliqueSampling = false;
        /// <summary>
        /// 玩家朝向将锁定到弹幕的初始朝向，默认为 false；启用后 canFormOwnerSetDir 将不再具备意义
        /// </summary>
        protected bool ownerOrientationLock = false;
        /// <summary>
        /// 最大动画帧数，默认为 1
        /// </summary>
        public int AnimationMaxFrme = 1;
        /// <summary>
        /// 动画帧切换间隔，默认为 5
        /// </summary>
        public int CuttingFrmeInterval = 5;
        /// <summary>
        /// 弹幕初始朝向，用于 ownerOrientationLock 时还原玩家朝向
        /// </summary>
        private int dirs;
        /// <summary>
        /// 绝对中心距离玩家的距离，默认为 75
        /// </summary>
        protected int distanceToOwner = 75;
        /// <summary>
        /// 弧光内宽度，默认为 70
        /// </summary>
        protected float drawTrailBtommWidth = 70;
        /// <summary>
        /// 弧光的采样点数，默认为 15 × <see cref="updateCount"/>
        /// </summary>
        protected int drawTrailCount = 15;
        /// <summary>
        /// 弧光宽度，默认为 50
        /// </summary>
        protected float drawTrailTopWidth = 50;
        /// <summary>
        /// 弹幕实体中心偏离值，默认为 60
        /// </summary>
        public float Length = 60;
        /// <summary>
        /// 一个挥舞周期的最大时间，默认为 22
        /// </summary>
        protected int maxSwingTime = 22;
        /// <summary>
        /// 弧光采样点各自距离玩家的距离
        /// </summary>
        protected float[] oldDistanceToOwner;
        /// <summary>
        /// 弧光采样点各自的长度
        /// </summary>
        protected float[] oldLength;
        /// <summary>
        /// 刀光弧度全局缩放，默认为 1
        /// </summary>
        public float oldLengthOffsetSizeValue = 1f;
        /// <summary>
        /// 上一帧的挥舞角，用于求出 rotSpeed
        /// </summary>
        private float oldRot;
        /// <summary>
        /// 弧光采样点各自的挥舞角（100f 表示该点尚未采样）
        /// </summary>
        protected float[] oldRotate;
        /// <summary>
        /// 弹幕实体中心偏离值，默认为 60；该值不应直接更新，仅用于还原
        /// </summary>
        public float OrigLength = 60;
        /// <summary>
        /// 额外的矫正刀光采点角度的值，默认为 0
        /// </summary>
        protected float overOffsetCachesRoting = 0;
        /// <summary>
        /// 旋转角度，默认为 MathHelper.ToRadians(3)
        /// </summary>
        public float Rotation;
        /// <summary>
        /// 旋转速度
        /// </summary>
        protected float rotSpeed;
        /// <summary>
        /// 射击时间比例，默认为 0.5f
        /// </summary>
        protected float shootSengs = 0.5f;
        /// <summary>
        /// 发射射弹的速度模长，默认为 6
        /// </summary>
        protected float ShootSpeed = 6f;
        /// <summary>
        /// 基本速度
        /// </summary>
        protected float speed;
        /// <summary>
        /// 挥舞起始方向向量
        /// </summary>
        protected Vector2 startVector;
        /// <summary>
        /// 总时间，记录更新，在每帧的最后更新中自行加 1
        /// </summary>
        protected int Time;
        /// <summary>
        /// 离心量的绘制矫正模长，默认为 48
        /// </summary>
        protected float toProjCoreMode = 48;
        /// <summary>
        /// 一个垂直于手臂的绘制矫正模长，默认为 0。
        /// 本模组现有子类均未赋值，故恒为 0，DrawSwing 里的 offsetOwnerPos 因此恒为零向量，
        /// 即该矫正当前不生效；将来需要在子类里赋非零值才会体现。
        /// </summary>
        protected float unitOffsetDrawZkMode = 0;
        /// <summary>
        /// 当前帧的刀尖偏移向量（起始方向旋转 Rotation 后乘 Length）
        /// </summary>
        protected Vector2 vector;
        // ── 属性 ──
        /// <summary>
        /// 是否绘制弧光：受配置项 EnableSwordLight 总开关约束，默认为 false
        /// </summary>
        protected bool CanDrawSlashTrail
        {
            get
            {
                if (!ConfigSystem.Instance.EnableSwordLight)
                {
                    return false;
                }
                return _canDrawSlashTrail;
            }
            set => _canDrawSlashTrail = value;
        }
        /// <summary>
        /// 颜色采样图
        /// </summary>
        public Texture2D GradientTexture => SwingSystem.GetGradientTexture(Type, gradientTexturePath).Value;
        /// <summary>
        /// 颜色采样图路径（子类可重写；为空则用默认色条）
        /// </summary>
        public virtual string gradientTexturePath => "";
        /// <summary>
        /// 这个手持刀对应的物品实例
        /// </summary>
        public Item Item => Owner.ActiveItem();
        /// <summary>
        /// 较为稳妥的获取一个正确的刀尖单位方向向量
        /// </summary>
        protected Vector2 safeInSwingUnit => Owner.Center.To(Projectile.Center).UnitVector();
        /// <summary>
        /// 子弹幕的生成位置（玩家稳定中心沿鼠标方向前推 Length 的一半）
        /// </summary>
        protected Vector2 ShootSpanPos => Owner.GetPlayerStabilityCenter() + UnitToMouseV * Length * 0.5f;
        /// <summary>
        /// 子弹幕的初速度（鼠标方向 × ShootSpeed，再按攻速折算）
        /// </summary>
        protected Vector2 ShootVelocity => UnitToMouseV * ShootSpeed / SetSwingSpeed(1);
        /// <summary>
        /// 子弹幕的生成源（由手持物品产生）
        /// </summary>
        protected IEntitySource Source => Owner.GetSource_ItemUse(Item);
        /// <summary>
        /// 挥舞索引（存于 ModPlayer，用于区分同一玩家连续的多段挥舞）
        /// </summary>
        public ref int SwingIndex => ref Owner.CWR().SwingIndex;
        /// <summary>
        /// 刀光贴图
        /// </summary>
        public virtual Texture2D TextureValue => CDUtil.GetT2DValue(Texture);
        /// <summary>
        /// 刀光流形采样图路径（子类可重写；为空则用默认刀光）
        /// </summary>
        public virtual string trailTexturePath => "";
        /// <summary>
        /// 刀光流形采样图
        /// </summary>
        public Texture2D TrailTexture => SwingSystem.GetTrailTexture(Type, trailTexturePath).Value;
        /// <summary>
        /// 更新率，值为 <see cref="Projectile.extraUpdates"/>+1；
        /// 使用之前请注意 <see cref="Projectile.extraUpdates"/> 是否已经被正确设置
        /// </summary>
        internal int updateCount => Projectile.extraUpdates + 1;
        // ── 嵌套类型 ──
        /// <summary>
        /// 挥舞参数包：把 SwingBehavior 的一长串参数打包，子类可构造一份再整体传入
        /// </summary>
        public struct SwingDataStruct
        {
            /// <summary>
            /// 挥舞起始角（度），默认 33
            /// </summary>
            public float starArg = 33;
            /// <summary>
            /// 基础挥舞角速度（度），默认 4
            /// </summary>
            public float baseSwingSpeed = 4;
            /// <summary>
            /// 前段长度增长率，默认 0.08f
            /// </summary>
            public float ler1_UpLengthSengs = 0.08f;
            /// <summary>
            /// 前段角速度增长率，默认 0.1f
            /// </summary>
            public float ler1_UpSpeedSengs = 0.1f;
            /// <summary>
            /// 前段缩放增长率，默认 0.012f
            /// </summary>
            public float ler1_UpSizeSengs = 0.012f;
            /// <summary>
            /// 后段长度衰减率，默认 0.01f
            /// </summary>
            public float ler2_DownLengthSengs = 0.01f;
            /// <summary>
            /// 后段角速度衰减率，默认 0.1f
            /// </summary>
            public float ler2_DownSpeedSengs = 0.1f;
            /// <summary>
            /// 后段缩放衰减率，默认 0
            /// </summary>
            public float ler2_DownSizeSengs = 0;
            /// <summary>
            /// 长度下限（0 表示按 OrigLength×1.2 自动取值），默认 0
            /// </summary>
            public int minClampLength = 0;
            /// <summary>
            /// 长度上限（0 表示按 OrigLength×1.4 自动取值），默认 0
            /// </summary>
            public int maxClampLength = 0;
            /// <summary>
            /// 前段持续时间（0 表示按 maxSwingTime×0.4 自动取值），默认 0
            /// </summary>
            public int ler1Time = 0;
            /// <summary>
            /// 挥舞周期（0 表示沿用基类的 maxSwingTime），默认 0
            /// </summary>
            public int maxSwingTime = 0;
            /// <summary>
            /// 攻速对挥舞速度的整体倍率，默认 1
            /// </summary>
            public float overSpeedUpSengs = 1;
            /// <summary>
            /// 各字段按上面的默认值初始化
            /// </summary>
            public SwingDataStruct() { }
        }
        // ── 生命周期方法 ──
        /// <summary>
        /// 基础属性与初始化：先把 Length 还原为 OrigLength，再依次走
        /// PreSetSwingProperty → SetSwingProperty → PostSwingProperty 三个子类钩子，
        /// 其中第一段返回 true 时才套用默认的判定箱/伤害类型等基础值；最后按帧数分配弧光采样数组。
        /// </summary>
        public sealed override void SetDefaults()
        {
            Length = OrigLength;
            if (PreSetSwingProperty())
            {
                Projectile.DamageType = DamageClass.Melee;
                Projectile.width = Projectile.height = 22;
                Projectile.tileCollide = false;
                Projectile.scale = 1f;
                Projectile.friendly = true;
                Projectile.penetrate = -1;
                Rotation = MathHelper.ToRadians(3);
                SetSwingProperty();
            }
            PostSwingProperty();
            LoadTrailCountData();
            OrigLength = Length;
        }
        /// <summary>
        /// 挥舞期间弹幕位置由 InOwner 直接接管，故不参与引擎的位置更新
        /// </summary>
        public override bool ShouldUpdatePosition() => false;
        /// <summary>
        /// 几乎所有的逻辑更新都在这里进行。返回 false 表示不执行引擎默认 AI。
        /// 顺序为：计算发射窗口 → 首帧 Initialize → PreInOwnerUpdate（可拦截）→
        /// InOwner 跟随玩家 → SwingAI 挥舞 → 本地玩家到点 Shoot → 更新弧光缓存 → 动画帧推进 → 记录 rotSpeed。
        /// </summary>
        public sealed override bool PreUpdate()
        {
            canShoot = Time == (int)(maxSwingTime * shootSengs);
            if (Time == 0)
            {
                Initialize();
            }
            if (PreInOwnerUpdate())
            {
                InOwner();
                SwingAI();
                if (Projectile.IsOwnedByLocalPlayer() && canShoot)
                {
                    Shoot();
                }
                UpdateCaches();
                if (AnimationMaxFrme > 1)
                {
                    CDUtil.ClockFrame(ref Projectile.frame, CuttingFrmeInterval, AnimationMaxFrme - 1);
                }
                rotSpeed = Rotation - oldRot;
                oldRot = Rotation;
                canShoot = false;
            }
            PostInOwnerUpdare();
            Time++;
            return false;
        }
        /// <summary>
        /// 暂时弃用的钩子：逻辑全部在 <see cref="PreUpdate"/> 中完成
        /// </summary>
        public sealed override void AI() { }
        /// <summary>
        /// 绘制入口：先按开关绘制弧光拖尾，再绘制挥舞中的刀身本体，返回 false 阻止默认贴图绘制
        /// </summary>
        public sealed override bool PreDraw(ref Color lightColor)
        {
            if (CanDrawSlashTrail)
            {
                DrawSlashTrail();
            }
            DrawSwing(Main.spriteBatch, lightColor);
            return false;
        }
        // ── 挥舞逻辑扩展点 ──
        /// <summary>
        /// 按帧数分配弧光采样数组（长度 = drawTrailCount），随后由 InitializeCaches 填初值
        /// </summary>
        public void LoadTrailCountData()
        {
            drawTrailCount *= updateCount;
            oldRotate = new float[drawTrailCount];
            oldDistanceToOwner = new float[drawTrailCount];
            oldLength = new float[drawTrailCount];
            InitializeCaches();
        }
        /// <summary>
        /// 把极坐标（半径 + 角度）转换为二维向量
        /// </summary>
        public Vector2 RodingToVer(float radius, float theta)
        {
            Vector2 vector2 = theta.ToRotationVector2();
            vector2.X *= radius;
            vector2.Y *= radius;
            return vector2;
        }
        /// <summary>
        /// 按玩家当前武器攻速折算挥舞/射击速度
        /// </summary>
        public float SetSwingSpeed(float speed) => speed / Owner.GetAttackSpeed(Projectile.DamageType);
        /// <summary>
        /// 弧光采样数组的初值：全部标记为「未采样」（角度 100f）并写入初始距离与长度
        /// </summary>
        protected virtual void InitializeCaches()
        {
            for (int j = drawTrailCount - 1; j >= 0; j--)
            {
                oldRotate[j] = 100f;
                oldDistanceToOwner[j] = distanceToOwner;
                oldLength[j] = Projectile.height * Projectile.scale;
            }
        }
        /// <summary>
        /// 模拟出一个勉强符合物理逻辑的命中粒子效果：
        /// 按挥砍方向与目标相对位置求出火花速度方向，并按场上已有火花总数分档缩减 sparkCount。
        /// </summary>
        protected void HitEffectValue(Entity target, int sparkCount, out Vector2 rotToTargetSpeedTrengsVumVer, out int newSparkCount)
        {
            Vector2 toTarget = Owner.Center.To(target.Center);
            Vector2 norlToTarget = toTarget.GetNormalVector();
            int ownerToTargetSetDir = Math.Sign(toTarget.X);
            ownerToTargetSetDir = ownerToTargetSetDir != DirSign ? -1 : 1;
            if (rotSpeed > 0)
            {
                norlToTarget *= -1;
            }
            if (rotSpeed < 0)
            {
                norlToTarget *= 1;
            }
            int pysCount = DRKLoader.GetParticlesCount(DRKLoader.GetParticleType(typeof(DRK_Spark)));
            if (pysCount > 120)
            {
                sparkCount = 10;
            }
            if (pysCount > 220)
            {
                sparkCount = 8;
            }
            if (pysCount > 350)
            {
                sparkCount = 6;
            }
            if (pysCount > 500)
            {
                sparkCount = 3;
            }
            newSparkCount = sparkCount;
            float rotToTargetSpeedSengs = rotSpeed * 3 * ownerToTargetSetDir;
            rotToTargetSpeedTrengsVumVer = norlToTarget.RotatedBy(-rotToTargetSpeedSengs) * 13;
        }
        /// <summary>
        /// 挥舞行为主体：首帧确定起始角与角速度，前 ler1Time 段加速胀大、之后减速收缩，
        /// 到达 maxSwingTime 后销毁弹幕；每整帧夹取一次 Length。
        /// 参数为空时按其默认值或由 OrigLength/maxSwingTime 推导。
        /// </summary>
        public virtual void SwingBehavior(float starArg = 33, float baseSwingSpeed = 4
            , float ler1_UpLengthSengs = 0.08f, float ler1_UpSpeedSengs = 0.1f, float ler1_UpSizeSengs = 0.012f
            , float ler2_DownLengthSengs = 0.01f, float ler2_DownSpeedSengs = 0.1f, float ler2_DownSizeSengs = 0
            , int minClampLength = 0, int maxClampLength = 0, int ler1Time = 0, int maxSwingTime = 0, float overSpeedUpSengs = 1)
        {
            if (minClampLength == 0)
            {
                minClampLength = (int)(OrigLength * 1.2f);
            }
            if (maxClampLength == 0)
            {
                maxClampLength = (int)(OrigLength * 1.4f);
            }
            if (maxSwingTime == 0)
            {
                maxSwingTime = this.maxSwingTime;
            }
            if (ler1Time == 0)
            {
                ler1Time = (int)(maxSwingTime * 0.4f);
            }
            float speedUp = SetSwingSpeed(1);
            speedUp *= overSpeedUpSengs;
            if (Time == 0)
            {
                Rotation = MathHelper.ToRadians(starArg * -Owner.direction);
                startVector = RodingToVer(1, Projectile.velocity.ToRotation() - MathHelper.PiOver2 * Projectile.spriteDirection);
                speed = MathHelper.ToRadians(baseSwingSpeed) / speedUp;
            }
            if (Time < ler1Time * speedUp)
            {
                Length *= 1 + ler1_UpLengthSengs / updateCount;
                Rotation += speed * Projectile.spriteDirection;
                speed *= 1 + ler1_UpSpeedSengs / updateCount;
                vector = startVector.RotatedBy(Rotation) * Length;
                Projectile.scale += ler1_UpSizeSengs;
            }
            else
            {
                Length *= 1 - ler2_DownLengthSengs / updateCount;
                Rotation += speed * Projectile.spriteDirection;
                speed *= 1 - ler2_DownSpeedSengs / updateCount / speedUp;
                vector = startVector.RotatedBy(Rotation) * Length;
                Projectile.scale -= ler2_DownSizeSengs;
            }
            if (Time >= maxSwingTime * updateCount * speedUp)
            {
                Projectile.Kill();
            }
            if (Time % updateCount == updateCount - 1)
            {
                Length = MathHelper.Clamp(Length, minClampLength, maxClampLength);
            }
        }
        /// <summary>
        /// 挥舞行为重载：把 SwingDataStruct 的各字段转发给完整参数版
        /// </summary>
        public virtual void SwingBehavior(SwingDataStruct swingData) =>
            SwingBehavior(swingData.starArg,
                          swingData.baseSwingSpeed,
                          swingData.ler1_UpLengthSengs,
                          swingData.ler1_UpSpeedSengs,
                          swingData.ler1_UpSizeSengs,
                          swingData.ler2_DownLengthSengs,
                          swingData.ler2_DownSpeedSengs,
                          swingData.ler2_DownSizeSengs,
                          swingData.minClampLength,
                          swingData.maxClampLength,
                          swingData.ler1Time,
                          swingData.maxSwingTime,
                          swingData.overSpeedUpSengs);
        /// <summary>
        /// SetDefaults 前置钩子；返回 false 时跳过默认的判定箱/伤害类型等基础设置，改由子类全权负责
        /// </summary>
        public virtual bool PreSetSwingProperty() { return true; }
        /// <summary>
        /// 子类在此设置挥舞相关的自定义属性（在基础属性之后调用）
        /// </summary>
        public virtual void SetSwingProperty() { }
        /// <summary>
        /// 子类在此做挥舞属性设置后的收尾
        /// </summary>
        public virtual void PostSwingProperty() { }
        /// <summary>
        /// 子类在此发射子弹幕（到达发射窗口且为本地玩家时由 PreUpdate 调用一次）
        /// </summary>
        public virtual void Shoot() { }
        /// <summary>
        /// 处理一些与玩家相关的逻辑，比如跟随和初始化一些基本数据，运行在 <see cref="SwingAI"/> 之前：
        /// 首帧记录朝向，重置穿透计数，把弹幕登记为手持物、锁住玩家的使用动画，
        /// 再把弹幕中心贴到玩家稳定中心 + vector，最后处理朝向纠正与手臂动作。
        /// </summary>
        public virtual void InOwner()
        {
            if (Time == 0)
            {
                dirs = Projectile.spriteDirection = Owner.direction;
            }
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Projectile.Center = Owner.GetPlayerStabilityCenter() + vector;
            if (canFormOwnerSetDir)
            {
                Projectile.spriteDirection = Owner.direction;
            }
            if (canSetOwnerArmBver)
            {
                Owner.SetCompositeArmFront(true, Length >= 80 ? Player.CompositeArmStretchAmount.Full : Player.CompositeArmStretchAmount.Quarter
                    , (Owner.Center - Projectile.Center).ToRotation() + MathHelper.PiOver2);
            }
            if (ownerOrientationLock)
            {
                Owner.direction = Projectile.spriteDirection = dirs;
            }
            Projectile.rotation = Projectile.spriteDirection == 1
                ? (Projectile.Center - Owner.Center).ToRotation() + MathHelper.PiOver4
                : (Projectile.Center - Owner.Center).ToRotation() - MathHelper.Pi - MathHelper.PiOver4;
        }
        /// <summary>
        /// InOwner 前置钩子；返回 false 时跳过本帧的跟随/挥舞/发射/缓存更新
        /// </summary>
        public virtual bool PreInOwnerUpdate() { return true; }
        /// <summary>
        /// InOwner 之后的收尾钩子，默认为空
        /// </summary>
        public virtual void PostInOwnerUpdare() { }
        /// <summary>
        /// 首帧初始化钩子，默认为空
        /// </summary>
        public virtual void Initialize() { }
        /// <summary>
        /// 用这个函数来处理挥舞相关的逻辑更新，运行在 <see cref="InOwner"/> 之后、<see cref="UpdateCaches"/> 之前
        /// </summary>
        public virtual void SwingAI() { }
        // ── 弧光与刀身绘制 ──
        /// <summary>
        /// 用 IDrawWarp 式的一次性顶点缓冲绘制扭曲弧光（非刀光拖尾主路径）：
        /// 按历史采样点构造 TriangleStrip，套用 KnifeDistortion 着色器与刀光贴图后直接绘制。
        /// </summary>
        public virtual void WarpDraw()
        {
            List<CustomVertexInfo> bars = [];
            GetCurrentTrailCount(out float count);
            float w = 1f;
            for (int i = 0; i < count; i++)
            {
                if (oldRotate[i] == 100f)
                    continue;
                float factor = 1f - i / count;
                Vector2 Center = Owner.GetPlayerStabilityCenter();
                float r = oldRotate[i] % 6.18f;
                float dir = (r >= 3.14f ? r - 3.14f : r + 3.14f) / MathHelper.TwoPi;
                Vector2 Top = Center + oldRotate[i].ToRotationVector2() * (oldLength[i] + drawTrailTopWidth + oldDistanceToOwner[i]);
                Vector2 Bottom = Center + oldRotate[i].ToRotationVector2() * (oldLength[i] - ControlTrailBottomWidth(factor) * 1.25f + oldDistanceToOwner[i]);
                bars.Add(new CustomVertexInfo(Top, new Color(dir, w, 0f, 15), new Vector3(factor, 0f, w)));
                bars.Add(new CustomVertexInfo(Bottom, new Color(dir, w, 0f, 15), new Vector3(factor, 1f, w)));
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.Default, RasterizerState.CullNone);
            Matrix projection = Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth, Main.screenHeight, 0f, 0f, 1f);
            Matrix model = Matrix.CreateTranslation(new Vector3(-Main.screenPosition.X, -Main.screenPosition.Y, 0f)) * Main.GameViewMatrix.TransformationMatrix;
            Effect effect = EffectLoader.KnifeDistortion.Value;
            effect.Parameters["uTransform"].SetValue(model * projection);
            Main.graphics.GraphicsDevice.Textures[0] = TrailTexture;
            Main.graphics.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
            effect.CurrentTechnique.Passes[0].Apply();
            if (bars.Count >= 3)
            {
                Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, bars.ToArray(), 0, bars.Count - 2);
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(0, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }
        /// <summary>
        /// 统计弧光中已采样的点数（角度不为 100f 的采样点），供绘制时决定条带长度
        /// </summary>
        public virtual void GetCurrentTrailCount(out float count)
        {
            count = 0f;
            if (oldRotate == null)
                return;
            for (int i = 0; i < oldRotate.Length; i++)
                if (oldRotate[i] != 100f)
                    count += 1f;
        }
        /// <summary>
        /// 在逻辑帧 <see cref="PreUpdate"/> 中被最后调用，用于更新弧光相关的点数据：
        /// 整体后移一位，再把当前位置/距离/长度写入 0 号采样点。
        /// </summary>
        public virtual void UpdateCaches()
        {
            if (Time < 2)
            {
                return;
            }
            for (int i = drawTrailCount - 1; i > 0; i--)
            {
                oldRotate[i] = oldRotate[i - 1];
                oldDistanceToOwner[i] = oldDistanceToOwner[i - 1];
                oldLength[i] = oldLength[i - 1];
            }
            oldRotate[0] = (Projectile.Center - Owner.Center).ToRotation() + overOffsetCachesRoting * Math.Sign(rotSpeed);
            oldDistanceToOwner[0] = distanceToOwner;
            oldLength[0] = Projectile.height * Projectile.scale * oldLengthOffsetSizeValue;
        }
        /// <summary>
        /// 临时切换混合/采样/光栅状态后调用 DrawTrail 画刀光，并还原全部图形状态
        /// </summary>
        public void DrawTrailHander(List<VertexPositionColorTexture> bars, GraphicsDevice device, BlendState blendState = null
            , SamplerState samplerState = null, RasterizerState rasterizerState = null)
        {
            RasterizerState originalState = Main.graphics.GraphicsDevice.RasterizerState;
            BlendState originalBlendState = Main.graphics.GraphicsDevice.BlendState;
            SamplerState originalSamplerState = Main.graphics.GraphicsDevice.SamplerStates[0];
            device.BlendState = blendState ?? originalBlendState;
            device.SamplerStates[0] = samplerState ?? originalSamplerState;
            device.RasterizerState = rasterizerState ?? originalState;
            DrawTrail(bars);
            device.RasterizerState = originalState;
            device.BlendState = originalBlendState;
            device.SamplerStates[0] = originalSamplerState;
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        }
        /// <summary>
        /// 用 KnifeRendering 着色器绘制刀光条带：绑定变换矩阵、高光/倾斜采样开关、
        /// 流形贴图与色带贴图，每个 pass 先按普通混合画一遍再按加法混合叠一遍以增亮
        /// </summary>
        public virtual void DrawTrail(List<VertexPositionColorTexture> bars)
        {
            Effect effect = CalamityDemutation.Instance.Assets.Request<Effect>(CalamityDemutationConstant.noEffects + "KnifeRendering").Value;
            effect.Parameters["transformMatrix"].SetValue(GetTransfromMaxrix());
            effect.Parameters["drawTrailHighlight"].SetValue(drawTrailHighlight);
            effect.Parameters["obliqueSampling"].SetValue(ObliqueSampling);
            effect.Parameters["sampleTexture"].SetValue(TrailTexture);
            effect.Parameters["gradientTexture"].SetValue(GradientTexture);
            //应用shader，并绘制顶点
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, bars.ToArray(), 0, bars.Count - 2);
                Main.graphics.GraphicsDevice.BlendState = BlendState.Additive;
                Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, bars.ToArray(), 0, bars.Count - 2);
            }
        }
        /// <summary>
        /// 计算刀光绘制用的变换矩阵（世界 × 视图 × 正交投影）
        /// </summary>
        public virtual Matrix GetTransfromMaxrix()
        {
            Matrix world = Matrix.CreateTranslation(-Main.screenPosition.Vec3());
            Matrix view = Main.GameViewMatrix.TransformationMatrix;
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);
            return world * view * projection;
        }
        /// <summary>
        /// 弧光下沿相对基准宽度的收缩量（默认按 drawTrailBtommWidth × 弹幕缩放）
        /// </summary>
        public virtual float ControlTrailBottomWidth(float factor)
        {
            return drawTrailBtommWidth * Projectile.scale;
        }
        /// <summary>
        /// 绘制刀光拖尾（默认路径）：按历史采样点构造上下沿顶点与渐变色，
        /// 条带足够长时交给 <see cref="DrawTrailHander"/> 用 KnifeRendering 着色器绘制
        /// </summary>
        public virtual void DrawSlashTrail()
        {
            List<VertexPositionColorTexture> bars = [];
            GetCurrentTrailCount(out float count);
            for (int i = 0; i < count; i++)
            {
                if (oldRotate[i] == 100f)
                    continue;
                float factor = 1f - i / count;
                Vector2 Center = Owner.GetPlayerStabilityCenter();
                Vector2 Top = Center + oldRotate[i].ToRotationVector2() * (oldLength[i] + drawTrailTopWidth + oldDistanceToOwner[i]);
                Vector2 Bottom = Center + oldRotate[i].ToRotationVector2() * (oldLength[i] - ControlTrailBottomWidth(factor) + oldDistanceToOwner[i]);
                var topColor = Color.Lerp(new Color(238, 218, 130, 200), new Color(167, 127, 95, 0), 1 - factor);
                var bottomColor = Color.Lerp(new Color(109, 73, 86, 200), new Color(83, 16, 85, 0), 1 - factor);
                bars.Add(new VertexPositionColorTexture(Top.Vec3(), topColor, new Vector2(factor, 0)));
                bars.Add(new VertexPositionColorTexture(Bottom.Vec3(), bottomColor, new Vector2(factor, 1)));
            }
            if (bars.Count > 2)
            {
                DrawTrailHander(bars, Main.graphics.GraphicsDevice, BlendState.NonPremultiplied, SamplerState.PointWrap, RasterizerState.CullNone);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            }
        }
        /// <summary>
        /// 绘制挥舞中的刀身贴图：按动画帧取切图，处理逆挥的垂直翻转与对角线翻转，
        /// 位置扣除离心量后再叠加手臂垂直方向的矫正偏移。
        /// 开启自发光（Incandescence）或关闭武器自适应光照时强制纯白。
        /// </summary>
        public virtual void DrawSwing(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = TextureValue;
            Rectangle rect = CDUtil.GetRec(texture, Projectile.frame, AnimationMaxFrme);
            Vector2 drawOrigin = rect.Size() / 2;
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;
            Vector2 offsetOwnerPos = safeInSwingUnit.GetNormalVector() * unitOffsetDrawZkMode * Projectile.spriteDirection;
            float drawRoting = Projectile.rotation;
            if (Projectile.spriteDirection == -1)
            {
                drawRoting += MathHelper.Pi;
            }
            //烦人的对角线翻转代码
            if (inDrawFlipdiagonally)
            {
                effects = Projectile.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                drawRoting += MathHelper.PiOver2;
                offsetOwnerPos *= -1;
            }
            Vector2 drawPosValue = Projectile.Center - RodingToVer(toProjCoreMode, (Projectile.Center - Owner.Center).ToRotation()) + offsetOwnerPos;
            Color color = Projectile.GetAlpha(lightColor);
            if (Incandescence || !ConfigSystem.Instance.WeaponAdaptiveIllumination)
            {
                color = Color.White;
            }
            Main.EntitySpriteDraw(texture, drawPosValue - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY, new Rectangle?(rect)
                , color, drawRoting, drawOrigin, Projectile.scale, effects, 0);
        }
    }
}
