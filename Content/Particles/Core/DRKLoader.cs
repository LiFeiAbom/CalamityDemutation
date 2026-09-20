using CalamityDemutation.Content.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Particles.Core
{
    /// <summary>
    /// DRK 粒子系统加载器（DRKLoader） - 移植自灾厄本体粒子体系的核心处理器（ModSystem）。
    /// 职责：
    /// 1) Load 时初始化各容器、注册内置粒子 DRK_Spark，并挂上 On_Main.DrawInfernoRings 绘制钩子；Unload 时逆向清理与解绑。
    /// 2) PostUpdateEverything 每帧驱动全部粒子：叠加速度、累加存活时间、调用粒子 AI，并回收寿命耗尽或被 Kill 的粒子。
    /// 3) DrawAll 按混合模式（AlphaBlend / NonPremultiplied / Additive）分组批量绘制，避免逐粒子切换渲染状态。
    /// 粒子上限由 CalamityDemutationConstant.MaxParticleCount（10000）控制，超限时忽略非 Important 粒子。
    /// 注：类名与部分成员名沿用旧版移植（CWR 系列已废弃），仅作为容器保留。
    /// </summary>
    internal class DRKLoader : ModSystem
    {
        // ── 静态字段 ──
        /// <summary>
        /// 绘制批次：加法混合粒子
        /// </summary>
        private static List<BaseParticle> batched_AdditiveBlend_DRK;
        /// <summary>
        /// 绘制批次：普通 alpha 混合粒子
        /// </summary>
        private static List<BaseParticle> batched_AlphaBlend_DRK;
        /// <summary>
        /// 绘制批次：半透明非预乘混合粒子
        /// </summary>
        private static List<BaseParticle> batched_NonPremultiplied_DRK;
        /// <summary>
        /// 旧版移植遗留容器（当前无读写方）
        /// </summary>
        internal static List<BaseParticle> ParticleCoreInds;
        /// <summary>
        /// 类型 ID → 贴图资源
        /// </summary>
        internal static Dictionary<int, Asset<Texture2D>> ParticleIDToTexturesDic;
        /// <summary>
        /// 当前存活粒子（对模组内可见：深渊裂隙的上屏合成需要遍历它筛出 <see cref="AbyssalParticle"/>）
        /// </summary>
        internal static List<BaseParticle> particles;
        /// <summary>
        /// 待删除粒子（由 RemoveParticle / Kill 登记，下一帧 Update 统一回收）
        /// </summary>
        private static List<BaseParticle> particlesToKill;
        /// <summary>
        /// 粒子类型 → 类型 ID
        /// </summary>
        internal static Dictionary<Type, int> ParticleTypesDic;
        // ── 生命周期方法 ──
        /// <summary>
        /// 模组加载时初始化所有粒子容器、注册内置粒子（DRK_Spark / DRK_HeavenfallStar / FlameParticle / ManaDrainStreak / GlowSpark），并挂上绘制钩子。
        /// </summary>
        public override void Load()
        {
            particles = [];
            particlesToKill = [];
            ParticleTypesDic = [];
            ParticleIDToTexturesDic = [];
            ParticleCoreInds = [];
            batched_AlphaBlend_DRK = [];
            batched_NonPremultiplied_DRK = [];
            batched_AdditiveBlend_DRK = [];
            RegisterParticle<DRK_Spark>();
            RegisterParticle<DRK_HeavenfallStar>();
            RegisterParticle<FlameParticle>();
            RegisterParticle<ManaDrainStreak>();
            RegisterParticle<GlowSpark>();
            RegisterParticle<GlowSparkCal>();
            RegisterParticle<StarTrailParticle>();
            RegisterParticle<AbyssalParticle>();
            RegisterParticle<HeavySmokeParticle>();
            RegisterParticle<RuneParticle>();
            RegisterParticle<HeavenfallStarCal>();
            RegisterParticle<LineParticleCal>();
            RegisterParticle<ImpactParticle>();
            RegisterParticle<ShineParticle>();
            RegisterParticle<LightParticle>();
            RegisterParticle<SparkleParticle>();
            RegisterParticle<AltSparkParticle>();
            RegisterParticle<VoidImpactParticle>();
            On_Main.DrawInfernoRings += DrawForegroundParticles;
        }
        /// <summary>
        /// 每帧在世界更新完毕后驱动一次粒子系统（内部调用 Update）。
        /// </summary>
        public override void PostUpdateEverything() => Update();
        /// <summary>
        /// 卸载时清空全部静态容器并解绑绘制钩子，防止热重载后残留引用。
        /// </summary>
        public override void Unload()
        {
            particles = null;
            particlesToKill = null;
            ParticleTypesDic = null;
            ParticleIDToTexturesDic = null;
            ParticleCoreInds = null;
            batched_AlphaBlend_DRK = null;
            batched_NonPremultiplied_DRK = null;
            batched_AdditiveBlend_DRK = null;
            On_Main.DrawInfernoRings -= DrawForegroundParticles;
        }
        // ── 公开方法 ──
        /// <summary>
        /// 生成提供给世界的粒子实例。达到粒子上限且非 Important 时忽略
        /// </summary>
        public static void AddParticle(BaseParticle particle)
        {
            if (Main.gamePaused || Main.dedServ || particles == null)
            {
                return;
            }
            if (particles.Count >= CalamityDemutationConstant.MaxParticleCount && !particle.Important)
            {
                return;
            }
            particles.Add(particle);
            particle.Type = GetParticleType(particle.GetType());
            particle.SetDRK();
        }
        /// <summary>
        /// 挂在 On_Main.DrawInfernoRings 上的绘制钩子：先绘制所有粒子，再执行原版方法。
        /// </summary>
        public static void DrawForegroundParticles(Terraria.On_Main.orig_DrawInfernoRings orig, Main self)
        {
            DrawAll(Main.spriteBatch);
            orig(self);
        }
        /// <summary>
        /// 绘制全部粒子：先按混合模式把粒子分到三个批次，再逐批 Begin/End（各批次使用对应的 BlendState、
        /// 采样器与剪刀测试）；UseCustomDraw 的粒子走 CustomDraw，否则按贴图帧默认绘制；
        /// 最后清空批次并恢复默认渲染状态，避免影响原版后续绘制。
        /// </summary>
        public static void DrawAll(SpriteBatch sb)
        {
            if (particles.Count == 0)
            {
                return;
            }
            sb.End();
            var rasterizer = Main.Rasterizer;
            rasterizer.ScissorTestEnable = true;
            Main.instance.GraphicsDevice.RasterizerState.ScissorTestEnable = true;
            Main.instance.GraphicsDevice.ScissorRectangle = new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);
            foreach (BaseParticle particle in particles)
            {
                if (particle == null)
                {
                    continue;
                }
                if (particle.UseAdditiveBlend)
                {
                    batched_AdditiveBlend_DRK.Add(particle);
                }
                else if (particle.UseHalfTransparency)
                {
                    batched_NonPremultiplied_DRK.Add(particle);
                }
                else
                {
                    batched_AlphaBlend_DRK.Add(particle);
                }
            }
            if (batched_AlphaBlend_DRK.Count > 0)
            {
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                void defaultDraw(BaseParticle particle)
                {
                    Rectangle frame = ParticleIDToTexturesDic[particle.Type].Value.Frame(1, particle.FrameVariants, 0, particle.Variant);
                    sb.Draw(ParticleIDToTexturesDic[particle.Type].Value, particle.Position - Main.screenPosition, frame, particle.Color, particle.Rotation, frame.Size() * 0.5f,
                        particle.Scale, SpriteEffects.None, 0f);
                }
                foreach (BaseParticle particle in batched_AlphaBlend_DRK)
                {
                    if (particle.UseCustomDraw)
                    {
                        particle.CustomDraw(sb);
                    }
                    else
                    {
                        defaultDraw(particle);
                    }
                }
                sb.End();
            }
            if (batched_NonPremultiplied_DRK.Count > 0)
            {
                rasterizer = Main.Rasterizer;
                rasterizer.ScissorTestEnable = true;
                Main.instance.GraphicsDevice.RasterizerState.ScissorTestEnable = true;
                Main.instance.GraphicsDevice.ScissorRectangle = new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);
                sb.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, DepthStencilState.Default, rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                void defaultDraw(BaseParticle particle)
                {
                    Rectangle frame = ParticleIDToTexturesDic[particle.Type].Value.Frame(1, particle.FrameVariants, 0, particle.Variant);
                    sb.Draw(ParticleIDToTexturesDic[particle.Type].Value, particle.Position - Main.screenPosition, frame, particle.Color, particle.Rotation, frame.Size() * 0.5f, particle.Scale, SpriteEffects.None, 0f);
                }
                foreach (BaseParticle particle in batched_NonPremultiplied_DRK)
                {
                    if (particle.UseCustomDraw)
                        particle.CustomDraw(sb);
                    else
                    {
                        defaultDraw(particle);
                    }
                }
                sb.End();
            }
            if (batched_AdditiveBlend_DRK.Count > 0)
            {
                rasterizer = Main.Rasterizer;
                rasterizer.ScissorTestEnable = true;
                Main.instance.GraphicsDevice.RasterizerState.ScissorTestEnable = true;
                Main.instance.GraphicsDevice.ScissorRectangle = new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);
                sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.Default, rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                void defaultDraw(BaseParticle particle)
                {
                    Rectangle frame = ParticleIDToTexturesDic[particle.Type].Value.Frame(1, particle.FrameVariants, 0, particle.Variant);
                    sb.Draw(ParticleIDToTexturesDic[particle.Type].Value, particle.Position - Main.screenPosition, frame, particle.Color, particle.Rotation, frame.Size() * 0.5f, particle.Scale, SpriteEffects.None, 0f);
                }
                foreach (BaseParticle particle in batched_AdditiveBlend_DRK)
                {
                    if (particle.UseCustomDraw)
                    {
                        particle.CustomDraw(sb);
                    }
                    else
                    {
                        defaultDraw(particle);
                    }
                }
                sb.End();
            }
            batched_AlphaBlend_DRK.Clear();
            batched_NonPremultiplied_DRK.Clear();
            batched_AdditiveBlend_DRK.Clear();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        }
        /// <summary>
        /// 把批次接回该粒子所在混合桶的样式（CE 的 <c>PRTLoader.BeginDrawingWithMode</c> 等价物）：
        /// 粒子在 CustomDraw 里为自己的采样需求 End/Begin 过批次之后必须调它，否则同桶里后面的粒子
        /// 会画进错误的批次（表现为那一帧后续粒子花屏）。
        /// </summary>
        internal static void BeginDrawingWithMode(BaseParticle particle, SpriteBatch sb)
        {
            RasterizerState rasterizer = Main.Rasterizer;
            rasterizer.ScissorTestEnable = true;
            Main.instance.GraphicsDevice.RasterizerState.ScissorTestEnable = true;
            Main.instance.GraphicsDevice.ScissorRectangle = new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);
            if (particle.UseAdditiveBlend)
            {
                sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.Default, rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }
            else if (particle.UseHalfTransparency)
            {
                sb.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, DepthStencilState.Default, rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }
            else
            {
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }
        }
        /// <summary>
        /// 可用粒子槽数量
        /// </summary>
        public static int FreeSpacesAvailable()
        {
            return Main.dedServ || particles == null ? 0 : CalamityDemutationConstant.MaxParticleCount - particles.Count();
        }
        /// <summary>
        /// 取指定粒子类对应的类型 ID（注册顺序即 ID）。
        /// </summary>
        public static int GetParticleType<T>() where T : BaseParticle => ParticleTypesDic[typeof(T)];
        /// <summary>
        /// 按 Type 对象取粒子类型 ID（供非泛型场合使用）。
        /// </summary>
        public static int GetParticleType(Type sType) => ParticleTypesDic[sType];
        /// <summary>
        /// 当前存活粒子总数
        /// </summary>
        public static int GetParticlesCount() => particles.Count;
        /// <summary>
        /// 指定类型（fxType）的存活粒子数
        /// </summary>
        public static int GetParticlesCount(int fxType)
        {
            int num = 0;
            foreach (var particle in particles)
            {
                if (particle.Type == fxType)
                {
                    num++;
                }
            }
            return num;
        }
        /// <summary>
        /// 距 targetPos 不超过 maxFindDistance 的粒子数
        /// </summary>
        public static int GetParticlesCount(Vector2 targetPos, float maxFindDistance)
        {
            int num = 0;
            foreach (var particle in particles)
            {
                if (particle.Position.Distance(targetPos) <= maxFindDistance)
                {
                    num++;
                }
            }
            return num;
        }
        /// <summary>
        /// 指定范围内且类型匹配的粒子数
        /// </summary>
        public static int GetParticlesCount(Vector2 targetPos, float maxFindDistance, int fxType)
        {
            int num = 0;
            foreach (var particle in particles)
            {
                if (particle.Position.Distance(targetPos) <= maxFindDistance && particle.Type == fxType)
                {
                    num++;
                }
            }
            return num;
        }
        /// <summary>
        /// 便捷入口：填好位置、速度、颜色、缩放与 ai 参数后交给 AddParticle 登记。
        /// </summary>
        public static void NewParticle(BaseParticle particle, Vector2 position, Vector2 velocity
            , Color color = default, float scale = 1f, int ai0 = 0, int ai1 = 0, int ai2 = 0)
        {
            particle.Position = position;
            particle.Velocity = velocity;
            particle.Scale = scale;
            particle.Color = color;
            particle.ai[0] = ai0;
            particle.ai[1] = ai1;
            particle.ai[2] = ai2;
            AddParticle(particle);
        }
        /// <summary>
        /// 批量回收判定：寿命耗尽（且 SetLifetime 为 true）或已被 RemoveParticle 标记的粒子。
        /// </summary>
        public static void ParticleGarbageCollection(ref List<BaseParticle> particles)
        {
            bool isGC(BaseParticle p) => p.Time >= p.Lifetime && p.SetLifetime || particlesToKill.Contains(p);
            particles.RemoveAll(isGC);
        }
        /// <summary>
        /// 标记粒子为待删除（下一帧 Update 时统一回收）。
        /// </summary>
        public static void RemoveParticle(BaseParticle particle) => particlesToKill.Add(particle);
        /// <summary>
        /// 粒子主循环：逐个叠加速度、累加存活时间、执行 AI，然后回收寿命到期（且允许自动移除）或已被 Kill 的粒子。
        /// </summary>
        public static void Update()
        {
            if (Main.dedServ)
            {
                return;
            }
            // 用计数上限的 for 循环：粒子 AI 可能通过 AddParticle 向同一列表追加，foreach 会抛“集合已修改”。
            // 本帧只更新进入循环前已存在的粒子，本帧新加的粒子留到下一帧处理。
            int particleCount = particles.Count;
            for (int i = 0; i < particleCount; i++)
            {
                BaseParticle particle = particles[i];
                if (particle == null)
                {
                    continue;
                }
                UpdateParticleVelocity(particle);
                UpdateParticleTime(particle);
                particle.AI();
            }
            ParticleGarbageCollection(ref particles);
            particlesToKill.Clear();
        }
        /// <summary>
        /// 粒子存活时间 +1 帧。
        /// </summary>
        public static void UpdateParticleTime(BaseParticle particle) => particle.Time++;
        /// <summary>
        /// 把速度叠加到位置（每帧一次）。
        /// </summary>
        public static void UpdateParticleVelocity(BaseParticle particle) => particle.Position += particle.Velocity;
        // ── 私有工具 ──
        /// <summary>
        /// 显式注册单个粒子类型（替代 CWR 的反射枚举，DRK_Spark 无无参构造，故用未初始化对象读取 Texture）
        /// </summary>
        private static void RegisterParticle<T>() where T : BaseParticle
        {
            BaseParticle sample = (BaseParticle)RuntimeHelpers.GetUninitializedObject(typeof(T));
            Type type = typeof(T);
            int ID = ParticleTypesDic.Count;
            ParticleTypesDic[type] = ID;
            string texturePath = sample.Texture;
            if (texturePath == "")
            {
                texturePath = type.Namespace.Replace('.', '/') + "/" + type.Name;
            }
            // 默认异步加载：ImmediateLoad 会在模组加载期阻塞(日志里的 "blocking on asset loading" 警告)
            ParticleIDToTexturesDic[ID] = ModContent.Request<Texture2D>(texturePath);
        }
    }
}
