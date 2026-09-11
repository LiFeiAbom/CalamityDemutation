using System.Collections.Generic;
using System.Linq;
using CalamityDemutation.Enums;
using CalamityDemutation.Graphics.Buffers;
using CalamityDemutation.Systems.Graphic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Graphics.Metaballs
{
    /// <summary>
    /// Metaball 管理器（移植自灾厄的 MetaballManager，trippy 幻视分支裁剪为固定白色）
    /// </summary>
    public class MetaballManager : ModSystem
    {
        // ── 静态字段 ──
        /// <summary>
        /// 全部已注册的元球（由 Metaball.Register 在注册时加入）
        /// </summary>
        internal static readonly List<Metaball> metaballs = new List<Metaball>();
        // ── 生命周期方法 ──
        /// <summary>
        /// 订阅绘制层系统的两个事件：OnPrepareDraw 用于把实例预渲染到离屏目标，
        /// OnDrawLayer 用于在对应层级把离屏目标合成回屏幕
        /// </summary>
        public override void Load()
        {
            GeneralDrawLayerSystem.OnPrepareDraw += PrepareMetaballTargets;
            GeneralDrawLayerSystem.OnDrawLayer += DrawMetaballs;
        }
        /// <summary>
        /// 世界卸载时清空所有元球的实例数据，避免带着旧世界的粒子进入新世界
        /// </summary>
        public override void OnWorldUnload()
        {
            foreach (Metaball metaball in metaballs)
                metaball.ClearInstances();
        }
        /// <summary>
        /// 每帧末更新：只有 IgnoreFPS（不受帧率限制）的元球在此更新；
        /// 其余元球延迟到准备渲染目标时、且游戏未暂停才更新
        /// </summary>
        public override void PostUpdateEverything()
        {
            var activeMetaballs = metaballs.Where(m => m.AnythingToDraw);
            foreach (Metaball metaball in activeMetaballs)
            {
                if (metaball.IgnoreFPS)
                    metaball.Update();
            }
        }
        /// <summary>
        /// 卸载时释放所有元球的图层渲染目标。释放动作投递到主线程执行，
        /// 因为图形资源释放必须在主线程进行（与 Metaball.Register 的分配配对）
        /// </summary>
        public override void Unload()
        {
            Main.QueueMainThreadAction(() =>
            {
                foreach (Metaball metaball in metaballs)
                    metaball?.Dispose();
            });
        }
        // ── 公开方法 ──
        /// <summary>
        /// 查询指定绘制层级上是否存在活跃元球（供其他系统按层级裁剪绘制，当前工程内暂无调用方）
        /// </summary>
        internal static bool AnyActiveMetaballsAtLayer(GeneralDrawLayer layerType) =>
            metaballs.Any(m => m.AnythingToDraw && m.DrawLayer == layerType);
        // ── 私有工具 ──
        /// <summary>
        /// 合成阶段（由 GeneralDrawLayerSystem.OnDrawLayer 在对应层级触发）：
        /// 用 Immediate 模式把每个元球的各层离屏目标以 (-2,-2) 偏移贴回屏幕，贴图前先调用
        /// <see cref="Metaball.PrepareShaderForTarget"/> 应用边缘着色器。
        /// 龙息元球声明的是 AfterProjectiles 层，由 GeneralDrawLayerSystem 在 On_Main.DrawProjectiles 之后触发。
        /// </summary>
        private static void DrawMetaballs(GeneralDrawLayer layerType)
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullCounterClockwise);
            foreach (Metaball metaball in metaballs.Where(m => m.DrawLayer == layerType && m.AnythingToDraw))
            {
                for (int i = 0; i < metaball.LayerTargets.Count; i++)
                {
                    var offset = new Vector2(-2, -2);   // 抵消渲染目标的 2 像素边距，使画面与屏幕对齐
                    Main.screenPosition += offset;
                    metaball.PrepareShaderForTarget(i);
                    Main.screenPosition -= offset;
                    Main.spriteBatch.Draw(metaball.LayerTargets[i].Target, offset, Color.White);
                }
            }
            Main.spriteBatch.End();
        }
        /// <summary>
        /// 准备阶段（由 GeneralDrawLayerSystem.OnPrepareDraw 触发）：
        /// 先把画布抬高 2 像素（与渲染目标比屏幕大 4 像素对应），逐层进入目标的 using 作用域清为透明后调用
        /// <see cref="Metaball.DrawInstances"/> 把实例画进离屏目标；每次切换渲染目标都要 End/重新 Begin spriteBatch。
        /// 未受 IgnoreFPS 标记的元球在此更新，但游戏暂停时跳过
        /// </summary>
        private void PrepareMetaballTargets()
        {
            var activeMetaballs = metaballs.Where(m => m.AnythingToDraw);
            if (!activeMetaballs.Any())
                return;
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, Main.Rasterizer, null, Main.Transform);
            var gd = Main.instance.GraphicsDevice;
            foreach (Metaball metaball in activeMetaballs)
            {
                if (!Main.gamePaused && !metaball.IgnoreFPS)
                    metaball.Update();
                metaball.PrepareSpriteBatch(Main.spriteBatch);
                foreach (var target in metaball.LayerTargets)
                {
                    using (target.Scope(clearColor: Color.Transparent))
                    {
                        var offset = new Vector2(-2, -2);   // 抵消渲染目标四周多出的 2 像素边距
                        Main.screenPosition += offset;
                        metaball.DrawInstances();
                        Main.screenPosition -= offset;
                        Main.spriteBatch.End();
                        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, Main.Rasterizer, null, Main.Transform);
                    }
                }
            }
            Main.spriteBatch.End();
        }
    }
}
