using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Content.Projectiles;
using CalamityDemutation.Content.Projectiles.Melee;
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
    /// ① warp 扭曲：收集所有实现 IDrawWarp 的活跃弹幕，把它们的 Warp() 画到 screenTargetSwap 作为位移遮罩，
    /// 再用 WarpShader 合成扭曲后的屏幕；
    /// ② 深渊裂隙：收集活跃的 <see cref="AbyssalCrack"/> 与 <see cref="AbyssalParticle"/>，把裂隙折线与
    /// 粒子遮罩画到 screenTargetSwap，再用 cabyss 着色器合成为蓝色深渊裂缝叠回屏幕。
    /// <para>
    /// 注意 EndCapture 只在**滤镜管线被激活时**才会被调用，因此本系统额外注册了一个以原版 FilterMiniTower
    /// 为背书的"透明滤镜"，仅在场上存在裂隙或深渊粒子时激活它（见 <see cref="PostUpdateEverything"/>），
    /// 其余时间保持关闭，既保证管线跑到，又不对画面与性能产生影响。
    /// </para>
    /// </summary>
    [Autoload(Side = ModSide.Client)]
    internal class EffectsSystem : ModSystem
    {
        // ── 常量 ──
        /// <summary>强制开启捕获管线的透明滤镜注册键</summary>
        private const string AbyssFilterKey = "CalamityDemutation:AbyssCrack";
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
        private static Filter abyssFilter;
        // ── 生命周期方法 ──
        /// <summary>
        /// 内容加载完成后注册那个"透明滤镜"：借原版 FilterMiniTower 着色器做背书（颜色透明、不透明度 0，
        /// 本身不改画面），作用只是让 FilterManager 的捕获管线有理由启动，从而让本系统的 EndCapture 钩子被调用。
        /// </summary>
        public override void PostSetupContent()
        {
            abyssFilter = new Filter(new ScreenShaderData("FilterMiniTower").UseColor(Color.Transparent).UseOpacity(0f), EffectPriority.VeryHigh);
            Filters.Scene[AbyssFilterKey] = abyssFilter;
        }
        /// <summary>
        /// 每帧世界更新完毕后按需开关透明滤镜：只要场上还有深渊裂隙或深渊粒子就保持激活，
        /// 两者都消失后立刻关闭（关闭后捕获管线不再启动，本系统的钩子自然也不会做事）。
        /// </summary>
        public override void PostUpdateEverything()
        {
            if (Main.dedServ || abyssFilter == null)
            {
                return;
            }
            bool needed = HasAbyssContent();
            if (needed && !abyssFilter.IsActive())
            {
                abyssFilter.Activate(Vector2.Zero);
            }
            else if (!needed && abyssFilter.IsActive())
            {
                abyssFilter.Deactivate();
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
            if (abyssFilter != null)
            {
                if (abyssFilter.IsActive())
                {
                    abyssFilter.Deactivate();
                }
                abyssFilter = null;
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
            // 深渊裂隙：与 warp 各自独立走一遍「备份屏幕 → 画遮罩 → 合成回屏」，先后顺序不影响结果
            if (HasAbyssContent())
            {
                DrawAbyssCrack();
            }
            orig.Invoke(self, finalTexture, screenTarget1, screenTarget2, clearColor);
        }
        /// <summary>
        /// 深渊裂隙的上屏合成：① 备份当前屏幕；② 把裂隙折线与深渊粒子按屏幕空间画到 screenTargetSwap 当遮罩
        /// （白色线条 + cvmask 贴图遮罩，都不带视图矩阵，位置自行减 screenPosition）；③ 先把屏幕还原回去，
        /// 再用 cabyss 着色器把遮罩合成为蓝色深渊裂缝叠在最上层。
        /// </summary>
        private void DrawAbyssCrack()
        {
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            // 1. 备份当前屏幕
            graphicsDevice.SetRenderTarget(screen);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            Main.spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
            // 2. 裂隙折线 + 深渊粒子遮罩画到 screenTargetSwap
            graphicsDevice.SetRenderTarget(Main.screenTargetSwap);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null);
            foreach (Projectile projectile in Main.projectile)
            {
                if (projectile.active && projectile.ModProjectile is AbyssalCrack crack)
                {
                    crack.DrawCrack();
                }
            }
            Texture2D cvmask = ModContent.Request<Texture2D>(AbyssMaskTexture).Value;
            foreach (BaseParticle particle in DRKLoader.particles)
            {
                if (particle is AbyssalParticle abyss)
                {
                    Main.spriteBatch.Draw(cvmask, abyss.Position - Main.screenPosition, null, Color.White * 0.06f, abyss.Rotation, cvmask.Size() / 2, (5.4f * abyss.Opacity) * 0.05f, SpriteEffects.None, 0f);
                }
            }
            Main.spriteBatch.End();
            // 3. 还原场景，再经 cabyss 把遮罩合成为蓝色裂缝叠回去
            graphicsDevice.SetRenderTarget(Main.screenTarget);
            graphicsDevice.Clear(Color.Transparent);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null);
            Main.spriteBatch.Draw(screen, Vector2.Zero, Color.White);
            // CE 的时间/偏移量都基于它的全局帧计数 cvcount，这里用等价的时间换算：cvcount ≈ 秒 × 60
            float frameCount = Main.GlobalTimeWrappedHourly * 60f;
            Effect cabyss = EffectLoader.AbyssShader.Value;
            cabyss.CurrentTechnique = cabyss.Techniques["Technique1"];
            cabyss.CurrentTechnique.Passes[0].Apply();
            cabyss.Parameters["clr"].SetValue(new Color(12, 50, 160).ToVector4());
            cabyss.Parameters["tex1"].SetValue(ModContent.Request<Texture2D>(AbyssNoiseTexture).Value);
            cabyss.Parameters["time"].SetValue(frameCount / 50f);
            cabyss.Parameters["scrsize"].SetValue(new Vector2(Main.screenWidth, Main.screenHeight));
            cabyss.Parameters["offset"].SetValue((Main.screenPosition + new Vector2(frameCount * 1.4f, frameCount * 1.4f)) / new Vector2(Main.screenWidth, Main.screenHeight));
            // 遮罩层是按世界坐标减 screenPosition 画的、不带视图矩阵，而重力反转时世界是翻转渲染的，
            // 所以要在这里把整层垂直翻转补偿回来（CE 原样）
            Main.spriteBatch.Draw(Main.screenTargetSwap, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 1f,
                Main.LocalPlayer.gravDir < 0f ? SpriteEffects.FlipVertically : SpriteEffects.None, 0f);
            Main.spriteBatch.End();
        }
        /// <summary>
        /// 是否还有需要上屏合成的东西：任一活跃的深渊裂隙，或任一存活的深渊粒子。
        /// 深渊刃起手阶段还没撕出裂缝，但渊水弹一路都在吐粒子，所以粒子也要算进来。
        /// </summary>
        private static bool HasAbyssContent()
        {
            foreach (Projectile projectile in Main.projectile)
            {
                if (projectile.active && projectile.ModProjectile is AbyssalCrack)
                {
                    return true;
                }
            }
            if (DRKLoader.particles != null)
            {
                foreach (BaseParticle particle in DRKLoader.particles)
                {
                    if (particle is AbyssalParticle)
                    {
                        return true;
                    }
                }
            }
            return false;
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
