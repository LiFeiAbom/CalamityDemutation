using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Content.Projectiles;
using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Content.Projectiles.Summon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;
namespace CalamityDemutation.Common.Effects
{
    /// <summary>
    /// 上屏合成系统（移植自 CWR 的 EffectsSystem）：钩入 FilterManager.EndCapture，在屏幕捕获末尾做两层后期——
    /// ① warp 扭曲：收集所有实现 IDrawWarp 的活跃弹幕，按 noBlueshift 分成两桶，各自把 Warp() 画到
    /// screenTargetSwap 作为位移遮罩，再用 WarpShader 合成扭曲后的屏幕（两桶分别设 blueValue，互不干扰）；
    /// ③ 全屏闪白：<c>CalamityDemutation.FlashEffectStrength</c> 为正时，以屏幕中心为轴叠 16 层逐级放大的
    /// 画面（CE 的 ApplyFinalShader，符文之歌收招放大招时用）；
    /// <para>
    /// 注意 EndCapture 只在**滤镜管线被激活时**才会被调用，因此本系统额外注册了一个以原版 FilterMiniTower
    /// 为背书的"透明滤镜"，仅在场上存在实现 IDrawWarp 的弹幕、或有闪白待播时激活它
    /// （见 <see cref="PostUpdateEverything"/>），其余时间保持关闭，既保证管线跑到，又不对画面与性能产生影响。
    /// 闪白的强度也在这个每帧钩子里递减（对应 CE 的 PostUpdateDusts 那处衰减）。
    /// </para>
    /// </summary>
    [Autoload(Side = ModSide.Client)]
    internal class EffectsSystem : ModSystem
    {
        // ── 常量 ──
        /// <summary>强制开启捕获管线的透明滤镜注册键</summary>
        private const string OverlayFilterKey = "CalamityDemutation:ScreenOverlay";
        /// <summary>屏幕震动的每帧衰减量（CE 在 PostUpdateNPCs 里减 0.5）</summary>
        private const float ScreenShakeDecay = 0.5f;
        /// <summary>全屏闪白的每帧衰减量（CE 在 PostUpdateDusts 里减 0.02）</summary>
        private const float FlashDecay = 0.02f;
        /// <summary>深渊粒子的遮罩贴图（CE 的 cvmask）</summary>
        private const string AbyssMaskTexture = "CalamityDemutation/Assets/ExtraTextures/cvmask";
        /// <summary>cabyss 着色器采样用的噪声贴图（CE 的 AwSky1）</summary>
        private const string AbyssNoiseTexture = "CalamityDemutation/Assets/ExtraTextures/AwSky1";
        // ── 静态字段 ──
        /// <summary>
        /// 屏幕备份渲染目标：扭曲/裂隙合成前先把 Main.screenTarget 拷到这里，之后作为采样源。
        /// 惰性创建（首次 EndCapture 或分辨率变化时），内容加载期不分配
        /// </summary>
        internal static RenderTarget2D screen;
        /// <summary>上面那个透明滤镜的实例（注册后持有，用于判活与开关）</summary>
        private static Filter overlayFilter;
        // ── 生命周期方法 ──
        /// <summary>
        /// 内容加载完成后注册那个"透明滤镜"：借原版 FilterMiniTower 着色器做背书（颜色透明、不透明度 0，
        /// 本身不改画面），作用只是让 FilterManager 的捕获管线有理由启动，从而让本系统的 EndCapture 钩子被调用。
        /// </summary>
        public override void PostSetupContent()
        {
            overlayFilter = new Filter(new ScreenShaderData("FilterMiniTower").UseColor(Color.Transparent).UseOpacity(0f), EffectPriority.VeryHigh);
            Filters.Scene[OverlayFilterKey] = overlayFilter;
        }
        /// <summary>
        /// 每帧世界更新完毕后：先衰减屏幕震动与全屏闪白这两个全局强度，再按需开关透明滤镜——
        /// 只要场上还有深渊裂隙/深渊粒子/无星之夜弹幕，或还有闪白待播，就保持激活（关闭后捕获管线不再启动，
        /// 本系统的钩子自然也不会做事）。
        /// </summary>
        public override void PostUpdateEverything()
        {
            if (CalamityDemutation.ScreenShakeAmp > 0f)
            {
                CalamityDemutation.ScreenShakeAmp -= ScreenShakeDecay;
            }
            if (CalamityDemutation.FlashEffectStrength > 0f)
            {
                CalamityDemutation.FlashEffectStrength -= FlashDecay;
            }
            if (Main.dedServ || overlayFilter == null)
            {
                return;
            }
            bool needed = HasAnyWarpProjectile()
                || CalamityDemutation.FlashEffectStrength > 0f;
            if (needed && !overlayFilter.IsActive())
            {
                overlayFilter.Activate(Vector2.Zero);
            }
            else if (!needed && overlayFilter.IsActive())
            {
                overlayFilter.Deactivate();
            }
        }
        /// <summary>
        /// 加载时挂上两个钩子：On_FilterManager.EndCapture（屏幕捕获末尾做扭曲合成）
        /// 与 Main.OnResolutionChanged（分辨率变化时重建 screen 渲染目标）
        /// </summary>
        public override void Load()
        {
            On_FilterManager.EndCapture += FilterManager_EndCapture;
            Main.OnResolutionChanged += Main_OnResolutionChanged;
        }
        /// <summary>
        /// 卸载时解绑两个钩子、关掉那个透明滤镜（避免模组卸载后仍强制开启捕获管线），
        /// 并把 screen 的释放投递到主线程执行，避免后台线程调用图形 API 崩溃
        /// </summary>
        public override void Unload()
        {
            On_FilterManager.EndCapture -= FilterManager_EndCapture;
            Main.OnResolutionChanged -= Main_OnResolutionChanged;
            if (overlayFilter != null)
            {
                if (overlayFilter.IsActive())
                {
                    overlayFilter.Deactivate();
                }
                overlayFilter = null;
            }
            // Unload() 由后台线程调用，而 FNA3D 的图形调用(含 RenderTarget2D.Dispose)必须在主线程执行，
            // 否则会抛 ThreadStateException 导致本模组卸载失败、tModLoader 被迫重启。
            // 所以这里不直接 Dispose，改为把释放动作投递到主线程。
            if (screen != null)
            {
                RenderTarget2D toDispose = screen;
                screen = null;
                Main.RunOnMainThread(toDispose.Dispose);
            }
        }
        // ── 私有工具 ──
        /// <summary>
        /// 屏幕捕获末尾的钩子：先确保 screen 已创建；若存在实现 IDrawWarp 的活跃弹幕，
        /// 则依次执行「备份屏幕 → 画 warp 遮罩到 screenTargetSwap → 用 WarpShader 合成回 screenTarget → 弹幕本体叠画在上层」，
        /// 最后无论是否扭曲都调用 orig 走完原版流程
        /// </summary>
        private void FilterManager_EndCapture(On_FilterManager.orig_EndCapture orig, FilterManager self
            , RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
        {
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            screen ??= new RenderTarget2D(graphicsDevice, Main.screenWidth, Main.screenHeight);
            if (HasWarpEffect(out List<IDrawWarp> warpSets, out List<IDrawWarp> warpSetsNoBlueshift))
            {
                // 有蓝移桶：WarpShader 的 blueValue 维持 30.11，扭曲区域会泛蓝
                if (warpSets.Count > 0)
                {
                    // 1. 备份当前屏幕到 screen
                    graphicsDevice.SetRenderTarget(screen);
                    graphicsDevice.Clear(Color.Transparent);
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                    Main.spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
                    Main.spriteBatch.End();
                    // 2. 把 warp 遮罩画到 screenTargetSwap
                    graphicsDevice.SetRenderTarget(Main.screenTargetSwap);
                    graphicsDevice.Clear(Color.Transparent);
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None
                        , RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                    foreach (IDrawWarp p in warpSets)
                    {
                        p.Warp();
                    }
                    Main.spriteBatch.End();
                    // 3. 用 WarpShader 把遮罩应用到屏幕上，写回 screenTarget
                    graphicsDevice.SetRenderTarget(Main.screenTarget);
                    graphicsDevice.Clear(Color.Transparent);
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                    Effect effect = EffectLoader.WarpShader.Value;
                    effect.Parameters["tex0"].SetValue(Main.screenTargetSwap);
                    effect.Parameters["noBlueshift"].SetValue(false);
                    effect.Parameters["i"].SetValue(0.02f);
                    effect.CurrentTechnique.Passes[0].Apply();
                    Main.spriteBatch.Draw(screen, Vector2.Zero, Color.White);
                    Main.spriteBatch.End();
                    // 4. 弹幕本体画在扭曲结果之上
                    // 这里必须带上 GameViewMatrix：costomDraw 喂进来的是「世界坐标 - screenPosition」，
                    // 少了视图矩阵在非 1 倍缩放下位置与大小都会错（大修原文即带 ViewMatrix）
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState
                        , DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                    foreach (IDrawWarp p in warpSets)
                    {
                        if (p.canDraw())
                        {
                            p.costomDraw(Main.spriteBatch);
                        }
                    }
                    Main.spriteBatch.End();
                }
                // 无蓝移桶：WarpShader 的 blueValue 降为 0.11，扭曲区域不泛蓝（大修的 noBlueshift 分支）
                if (warpSetsNoBlueshift.Count > 0)
                {
                    // 1. 备份当前屏幕到 screen
                    graphicsDevice.SetRenderTarget(screen);
                    graphicsDevice.Clear(Color.Transparent);
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                    Main.spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
                    Main.spriteBatch.End();
                    // 2. 把 warp 遮罩画到 screenTargetSwap
                    graphicsDevice.SetRenderTarget(Main.screenTargetSwap);
                    graphicsDevice.Clear(Color.Transparent);
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None
                        , RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                    foreach (IDrawWarp p in warpSetsNoBlueshift)
                    {
                        p.Warp();
                    }
                    Main.spriteBatch.End();
                    // 3. 用 WarpShader 把遮罩应用到屏幕上，写回 screenTarget
                    graphicsDevice.SetRenderTarget(Main.screenTarget);
                    graphicsDevice.Clear(Color.Transparent);
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                    Effect effect = EffectLoader.WarpShader.Value;
                    effect.Parameters["tex0"].SetValue(Main.screenTargetSwap);
                    effect.Parameters["noBlueshift"].SetValue(true);
                    effect.Parameters["i"].SetValue(0.02f);
                    effect.CurrentTechnique.Passes[0].Apply();
                    Main.spriteBatch.Draw(screen, Vector2.Zero, Color.White);
                    Main.spriteBatch.End();
                    // 4. 弹幕本体画在扭曲结果之上
                    // 这里必须带上 GameViewMatrix：costomDraw 喂进来的是「世界坐标 - screenPosition」，
                    // 少了视图矩阵在非 1 倍缩放下位置与大小都会错（大修原文即带 ViewMatrix）
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState
                        , DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                    foreach (IDrawWarp p in warpSetsNoBlueshift)
                    {
                        if (p.canDraw())
                        {
                            p.costomDraw(Main.spriteBatch);
                        }
                    }
                    Main.spriteBatch.End();
                }
            }
            // 全屏闪白与无星之夜剑体：CE 把这两件事都放在它的 ApplyFinalShader 末尾按「先闪白、后剑体」执行
            if (CalamityDemutation.FlashEffectStrength > 0f)
            {
                DrawFlash();
            }
            orig.Invoke(self, finalTexture, screenTarget1, screenTarget2, clearColor);
        }
        /// <summary>
        /// 全屏闪白（CE 的 ApplyFinalShader）：① 把当前画面备份到 screen；② 把屏幕本体原样还原回去；
        /// ③ 以屏幕中心为轴、把备份叠 16 层——每层透明度按 16/i 递减、缩放按 1+强度×0.08×i 递增，
        /// 由内向外糊开一层白雾。强度越高叠得越大越亮。
        /// </summary>
        private void DrawFlash()
        {
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            // 1. 备份当前屏幕
            graphicsDevice.SetRenderTarget(screen);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
            // 2. 还原屏幕本体（CE 用 RT 中转了一遍，画面上等于不变）
            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(screen, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
            // 3. 以中心为轴加法叠加放大的备份，形成由中心扩散的白闪
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            Vector2 center = screen.Size() / 2f;
            for (float i = 1; i <= 16; i++)
            {
                Main.spriteBatch.Draw(screen, center, null,
                    Color.White * ((16f / i) * 0.1f * CalamityDemutation.FlashEffectStrength), 0f, center,
                    1 + CalamityDemutation.FlashEffectStrength * 0.08f * i, SpriteEffects.None, 0);
            }
            Main.spriteBatch.End();
        }
        /// <summary>
        /// 扫描全部活跃弹幕，收集其 ModProjectile 实现了 IDrawWarp 的实例：
        /// 按 <see cref="IDrawWarp.noBlueshift"/> 分成"有蓝移"与"无蓝移"两桶，两者会被分别合成。
        /// 返回是否存在任意一个（两桶总数大于 0）
        /// </summary>
        private bool HasWarpEffect(out List<IDrawWarp> warpSets, out List<IDrawWarp> warpSetsNoBlueshift)
        {
            warpSets = new List<IDrawWarp>();
            warpSetsNoBlueshift = new List<IDrawWarp>();
            foreach (Projectile p in Main.projectile)
            {
                if (p.active && p.ModProjectile is IDrawWarp drawWarp)
                {
                    if (drawWarp.noBlueshift())
                    {
                        warpSetsNoBlueshift.Add(drawWarp);
                    }
                    else
                    {
                        warpSets.Add(drawWarp);
                    }
                }
            }
            return warpSets.Count > 0 || warpSetsNoBlueshift.Count > 0;
        }
        /// <summary>
        /// 是否存在实现了 IDrawWarp 的活跃弹幕。与 <see cref="HasWarpEffect"/> 同义但不分配容器，
        /// 供每帧调用的滤镜开关判据使用——否则这个判据每帧都会 new 两个 List 造成 GC 压力。
        /// </summary>
        private static bool HasAnyWarpProjectile()
        {
            foreach (Projectile p in Main.projectile)
            {
                if (p.active && p.ModProjectile is IDrawWarp)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 分辨率变化回调：释放旧 screen 并按新的屏幕宽高重建，供扭曲合成当作屏幕备份
        /// </summary>
        private void Main_OnResolutionChanged(Vector2 obj)
        {
            screen?.Dispose();
            screen = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);
        }
    }
}
