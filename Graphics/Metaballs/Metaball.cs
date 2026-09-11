using System.Collections.Generic;
using System.Linq;
using CalamityDemutation.Common.Effects;
using CalamityDemutation.Enums;
using CalamityDemutation.Graphics.Buffers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Graphics.Metaballs
{
    /// <summary>
    /// Metaball 基类（移植自灾厄的 Metaball）
    /// </summary>
    public abstract class Metaball : ModType
    {
        internal List<RenderTargetLease> LayerTargets = new List<RenderTargetLease>();
        /// <summary>
        /// 当前是否有内容需要绘制：为 false 时管理器会跳过本元球的更新与渲染
        /// </summary>
        public abstract bool AnythingToDraw { get; }
        /// <summary>
        /// 参与叠加的图层贴图序列：每层对应一个离屏渲染目标，逐层用不同参数合成
        /// </summary>
        public abstract IEnumerable<Texture2D> Layers { get; }
        /// <summary>
        /// 该元球挂在哪个绘制层级（由 GeneralDrawLayerSystem 分发时按此过滤）
        /// </summary>
        public abstract GeneralDrawLayer DrawLayer { get; }
        /// <summary>
        /// 边缘色，作为着色器 edgeColor 参数传给边缘检测
        /// </summary>
        public abstract Color EdgeColor { get; }
        /// <summary>
        /// 逐层颜色覆盖（可空）：第 i 层取第 i 个值，用不满或未设置时回退为白色
        /// </summary>
        public virtual List<Vector4> LayerColors { get; set; } = new List<Vector4>();
        /// <summary>
        /// 为 true 时无需等待暂停/帧率限制，每帧 PostUpdateEverything 都更新
        /// </summary>
        public virtual bool IgnoreFPS => false;
        /// <summary>
        /// 为 true 时忽略世界滚动，图层按屏幕空间固定采样（不随屏幕位置偏移）
        /// </summary>
        public virtual bool FixedToScreen => false;
        /// <summary>
        /// 清空所有实例（世界卸载时由 MetaballManager.OnWorldUnload 调用）
        /// </summary>
        public virtual void ClearInstances() { }
        /// <summary>
        /// 每帧更新实例状态；FPS 类元球在 PostUpdateEverything 中更新，其余在准备渲染目标时更新
        /// </summary>
        public virtual void Update() { }
        /// <summary>
        /// 指定图层的额外手动滚动偏移，叠加在 screenPosition/screenSize 之上
        /// </summary>
        public virtual Vector2 CalculateManualOffsetForLayer(int layerIndex) => Vector2.Zero;
        /// <summary>
        /// 绘制实例前的 spriteBatch 定制钩子（例如切换混合状态），默认为空
        /// </summary>
        public virtual void PrepareSpriteBatch(SpriteBatch spriteBatch) { }
        /// <summary>
        /// 为第 layerIndex 层的合成准备默认边缘着色器：写入图层尺寸、屏幕尺寸、图层滚动偏移、
        /// 边缘色、逐帧屏幕偏移与图层颜色，并把该层贴图绑到 1 号纹理槽（线性循环采样）。
        /// 子类可重写以使用自己的着色器（如龙息用加法混合版）。
        /// </summary>
        public virtual void PrepareShaderForTarget(int layerIndex)
        {
            var metaballShader = EffectLoader.MetaballEdgeShader;
            var gd = Main.instance.GraphicsDevice;
            Texture2D layerTexture = Layers.ElementAt(layerIndex);
            Vector2 screenSize = new(Main.screenWidth, Main.screenHeight);
            Vector2 layerScrollOffset = Main.screenPosition / screenSize + CalculateManualOffsetForLayer(layerIndex);
            if (FixedToScreen)
                layerScrollOffset = Vector2.Zero;
            metaballShader.Value.Parameters["layerSize"]?.SetValue(layerTexture.Size());
            metaballShader.Value.Parameters["screenSize"]?.SetValue(screenSize);
            metaballShader.Value.Parameters["layerOffset"]?.SetValue(layerScrollOffset);
            metaballShader.Value.Parameters["edgeColor"]?.SetValue(EdgeColor.ToVector4());
            metaballShader.Value.Parameters["singleFrameScreenOffset"]?.SetValue((Main.screenLastPosition - Main.screenPosition) / screenSize);
            metaballShader.Value.Parameters["layerColor"]?.SetValue(LayerColors.Count() > layerIndex ? LayerColors[layerIndex] : Color.White.ToVector4());
            gd.Textures[1] = layerTexture;
            gd.SamplerStates[1] = SamplerState.LinearWrap;
            metaballShader.Value.CurrentTechnique.Passes[0].Apply();
        }
        /// <summary>
        /// 把实例绘制进当前渲染目标（由 MetaballManager 在准备阶段逐层调用，需自行保证 spriteBatch 已 Begin）
        /// </summary>
        public abstract void DrawInstances();
        /// <summary>
        /// tModLoader ModType 注册回调：注册到 ModTypeLookup、加入管理器列表，
        /// 并在主线程为每个图层从屏幕空间池租借一个渲染目标（尺寸比屏幕各多 4 像素，用于容纳边缘溢出）。
        /// 服务端（dedServ）直接跳过渲染目标分配。
        /// </summary>
        protected sealed override void Register()
        {
            ModTypeLookup<Metaball>.Register(this);
            if (!MetaballManager.metaballs.Contains(this))
                MetaballManager.metaballs.Add(this);
            if (Main.dedServ)
                return;
            Main.QueueMainThreadAction(() =>
            {
                int layerCount = Layers.Count();
                for (int i = 0; i < layerCount; i++)
                    LayerTargets.Add(ScreenspaceTargetPool.Shared.Rent(Main.instance.GraphicsDevice, (width, height) => (width + 4, height + 4)));
            });
        }
        /// <summary>
        /// 释放所有租借的图层渲染目标（归还至池）。由 MetaballManager.Unload 在主线程统一调用，
        /// 与 <see cref="Register"/> 中的 Rent 配对。
        /// </summary>
        public void Dispose()
        {
            for (int i = 0; i < LayerTargets.Count; i++)
                LayerTargets[i]?.Dispose();
        }
    }
}
