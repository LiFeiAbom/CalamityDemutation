using System.Collections.Generic;
using CalamityDemutation.Enums;
using CalamityDemutation.Systems.Graphic;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Particles
{
    /// <summary>
    /// 粒子管理器（移植自灾厄的 GeneralParticleHandler）。
    /// 裁剪掉像素化、自定义着色器、trippy（吃蘑菇幻视）、反射自动注册等本工程用不到的部分。
    /// 每帧 PostUpdateEverything 更新，按混合模式分组绘制。
    /// </summary>
    [Autoload(Side = ModSide.Client)]
    public sealed class GeneralParticleHandler : ModSystem
    {
        private const int ParticleLimit = 5000;
        private static List<Particle> activeParticles;
        private static List<Particle> particlesToKill;
        private static Dictionary<BlendState, List<Particle>> particlesToDraw;
        /// <summary>
        /// 模组加载时初始化三类容器（存活、待删除、按混合模式分组的绘制列表），
        /// 并订阅 <see cref="GeneralDrawLayerSystem.OnDrawLayer"/>，即挂到绘制层系统上做粒子绘制
        /// </summary>
        public override void Load()
        {
            activeParticles = new List<Particle>();
            particlesToKill = new List<Particle>();
            particlesToDraw = new Dictionary<BlendState, List<Particle>>();
            GeneralDrawLayerSystem.OnDrawLayer += DrawParticleCollectionsAtSpecificLayer;
        }
        /// <summary>
        /// 卸载时把静态容器置空，避免热重载后残留旧程序集引用。
        /// 此处无需手动解绑 OnDrawLayer：GeneralDrawLayerSystem.Unload 会把该事件整体置空，订阅随之一并清除
        /// </summary>
        public override void Unload()
        {
            activeParticles = null;
            particlesToKill = null;
            particlesToDraw = null;
        }
        /// <summary>
        /// 世界卸载时清空所有粒子容器，防止上一世界的粒子残留到新世界
        /// </summary>
        public override void OnWorldUnload()
        {
            activeParticles?.Clear();
            particlesToKill?.Clear();
            particlesToDraw?.Clear();
        }
        /// <summary>
        /// 每帧在世界更新完毕后驱动一次粒子系统（服务器端跳过，仅客户端运行）
        /// </summary>
        public override void PostUpdateEverything()
        {
            if (!Main.dedServ)
                Update();
        }
        /// <summary>
        /// 生成一个粒子。游戏暂停、服务器端、或达到粒子上限（且非关键粒子）时不生成。
        /// </summary>
        public static void SpawnParticle(Particle particle)
        {
            if (Main.gamePaused || Main.dedServ || activeParticles == null)
                return;
            if (activeParticles.Count >= ParticleLimit && !particle.Important)
                return;
            activeParticles.Add(particle);
            GetDrawList(particle.UseAdditiveBlend).Add(particle);
        }
        /// <summary>
        /// 标记粒子为待删除：登记到 particlesToKill，由本帧 Update 的 RemoveAll 统一回收（Particle.Kill 调用于此）。
        /// 服务器端不处理
        /// </summary>
        public static void RemoveParticle(Particle particle)
        {
            if (!Main.dedServ)
                particlesToKill.Add(particle);
        }
        /// <summary>
        /// 取出（不存在则创建）指定混合模式对应的绘制列表，供生成/移除粒子时同步维护
        /// </summary>
        private static List<Particle> GetDrawList(bool additive)
        {
            BlendState state = additive ? BlendState.Additive : BlendState.AlphaBlend;
            if (!particlesToDraw.ContainsKey(state))
                particlesToDraw[state] = new List<Particle>();
            return particlesToDraw[state];
        }
        /// <summary>
        /// 粒子主循环：逐个叠加速度到位置、累加存活帧数并调用粒子自身的 Update；
        /// 随后一次性回收「寿命耗尽且允许自动移除」或「已被 RemoveParticle 标记」的粒子，
        /// 回收时同步从对应混合模式的绘制列表移除，最后清空待删除表
        /// </summary>
        private static void Update()
        {
            if (Main.dedServ)
                return;
            foreach (Particle particle in activeParticles)
            {
                if (particle == null)
                    continue;
                particle.Position += particle.Velocity;
                particle.Time++;
                particle.Update();
            }
            activeParticles.RemoveAll(particle =>
            {
                if ((particle.Time >= particle.Lifetime && particle.SetLifetime) || particlesToKill.Contains(particle))
                {
                    GetDrawList(particle.UseAdditiveBlend).Remove(particle);
                    return true;
                }
                return false;
            });
            particlesToKill.Clear();
        }
        /// <summary>
        /// 订阅到绘制层系统的绘制回调：仅绘制 DrawLayer 等于当前层级的自定义绘制粒子。
        /// 按混合模式逐批 Begin/End（避免逐粒子切换渲染状态），批次内再按绘制层过滤
        /// </summary>
        private static void DrawParticleCollectionsAtSpecificLayer(GeneralDrawLayer drawLayer)
        {
            if (Main.dedServ)
                return;
            foreach (var pair in particlesToDraw)
            {
                if (pair.Value.Count == 0)
                    continue;
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, pair.Key, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                foreach (Particle particle in pair.Value)
                {
                    if (particle.DrawLayer != drawLayer)
                        continue;
                    if (particle.UseCustomDraw)
                        particle.CustomDraw(Main.spriteBatch);
                }
                Main.spriteBatch.End();
            }
        }
    }
}
