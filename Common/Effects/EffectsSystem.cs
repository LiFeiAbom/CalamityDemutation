using CalamityDemutation.Content.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
namespace CalamityDemutation.Common.Effects
{
    /// <summary>
    /// 屏幕扭曲系统（移植自 CWR 的 EffectsSystem，仅保留 warp 扭曲路径）：
    /// 钩入 FilterManager.EndCapture，收集所有实现 IDrawWarp 的活跃弹幕，
    /// 把它们的 Warp() 画到 screenTargetSwap 作为位移遮罩，再用 WarpShader 合成扭曲后的屏幕。
    /// </summary>
    [Autoload(Side = ModSide.Client)]
    internal class EffectsSystem : ModSystem
    {
        internal static RenderTarget2D screen;

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
        /// 卸载时解绑两个钩子，并把 screen 的释放投递到主线程执行，避免后台线程调用图形 API 崩溃
        /// </summary>
        public override void Unload()
        {
            On_FilterManager.EndCapture -= FilterManager_EndCapture;
            Main.OnResolutionChanged -= Main_OnResolutionChanged;
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

        /// <summary>
        /// 分辨率变化回调：释放旧 screen 并按新的屏幕宽高重建，供扭曲合成当作屏幕备份
        /// </summary>
        private void Main_OnResolutionChanged(Vector2 obj)
        {
            screen?.Dispose();
            screen = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight);
        }

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
            if (HasWarpEffect(out List<IDrawWarp> warpSets))
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
                effect.Parameters["i"].SetValue(0.02f);
                effect.CurrentTechnique.Passes[0].Apply();
                Main.spriteBatch.Draw(screen, Vector2.Zero, Color.White);
                Main.spriteBatch.End();
                // 4. 弹幕本体画在扭曲结果之上
                Main.spriteBatch.Begin();
                foreach (IDrawWarp p in warpSets)
                {
                    if (p.canDraw())
                    {
                        p.costomDraw(Main.spriteBatch);
                    }
                }
                Main.spriteBatch.End();
            }
            orig.Invoke(self, finalTexture, screenTarget1, screenTarget2, clearColor);
        }

        /// <summary>
        /// 扫描全部活跃弹幕，收集其 ModProjectile 实现了 IDrawWarp 的实例到 warpSets；
        /// 返回是否存在（数量大于 0）
        /// </summary>
        private bool HasWarpEffect(out List<IDrawWarp> warpSets)
        {
            warpSets = new List<IDrawWarp>();
            foreach (Projectile p in Main.projectile)
            {
                if (p.active && p.ModProjectile is IDrawWarp drawWarp)
                {
                    warpSets.Add(drawWarp);
                }
            }
            return warpSets.Count > 0;
        }
    }
}
