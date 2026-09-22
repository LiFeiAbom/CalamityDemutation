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
    /// 上屏合成系统（移植自 CWR 的 EffectsSystem）：钩入 FilterManager.EndCapture，在屏幕捕获末尾做四层后期——
    /// ① warp 扭曲：收集所有实现 IDrawWarp 的活跃弹幕，把它们的 Warp() 画到 screenTargetSwap 作为位移遮罩，
    /// 再用 WarpShader 合成扭曲后的屏幕；
    /// ② 深渊裂隙：收集活跃的 <see cref="AbyssalCrack"/> 与 <see cref="AbyssalParticle"/>，把裂隙折线与
    /// 粒子遮罩画到 screenTargetSwap，再用 cabyss 着色器合成为蓝色深渊裂缝叠回屏幕；
    /// ③ 全屏闪白：<c>CalamityDemutation.FlashEffectStrength</c> 为正时，以屏幕中心为轴叠 16 层逐级放大的
    /// 画面（CE 的 ApplyFinalShader，符文之歌收招放大招时用）；
    /// ④ 无星之夜剑体：把 <see cref="StarlessNightProj"/> 的剑体画在所有弹幕之上（CE 把它的 drawSword
    /// 放在这一层调用，见 ApplyFinalShader 末尾）。
    /// <para>
    /// 注意 EndCapture 只在**滤镜管线被激活时**才会被调用，因此本系统额外注册了一个以原版 FilterMiniTower
    /// 为背书的"透明滤镜"，仅在场上存在裂隙/深渊粒子/无星之夜弹幕，或有闪白待播时激活它
    /// （见 <see cref="PostUpdateEverything"/>），其余时间保持关闭，既保证管线跑到，又不对画面与性能产生影响。
    /// 后三项的效果强度也在这个每帧钩子里递减（分别对应 CE 的 PostUpdateNPCs / PostUpdateDusts 那两处衰减）。
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
            bool needed = HasAbyssContent() || HasStarlessNightContent() || CalamityDemutation.FlashEffectStrength > 0f;
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
            // 全屏闪白与无星之夜剑体：CE 把这两件事都放在它的 ApplyFinalShader 末尾按「先闪白、后剑体」执行
            if (CalamityDemutation.FlashEffectStrength > 0f)
            {
                DrawFlash();
            }
            if (HasStarlessNightContent())
            {
                DrawStarlessNightSwords();
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
                // 噬渊鞭挞挥砍中段拖出的深渊裂纹（CE 把这道折线也画进同一个深渊 RT 遮罩里）
                if (projectile.active && projectile.ModProjectile is YstralynProj ystralyn)
                {
                    ystralyn.draw_crack();
                }
                // 沧溟渊龙命中撕开的裂空（CE 同样把它画进这个深渊遮罩）
                if (projectile.active && projectile.ModProjectile is NxCrack nxCrack)
                {
                    nxCrack.drawCrack();
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
        /// 把所有活跃 <see cref="StarlessNightProj"/> 的剑体画在当前画面上（所有弹幕之后）。
        /// CE 在它的全局绘制层里逐个调 drawSword，本模组改到上屏阶段做同一件事，
        /// 这样剑体同样不会被别的弹幕/物块压住。
        /// </summary>
        private static void DrawStarlessNightSwords()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            foreach (Projectile projectile in Main.projectile)
            {
                if (projectile.active && projectile.ModProjectile is StarlessNightProj sword)
                {
                    sword.DrawSword();
                }
            }
            Main.spriteBatch.End();
        }
        /// <summary>
        /// 是否还有无星之夜的手持弹幕在场（决定要不要跑剑体的全局绘制层）。
        /// </summary>
        private static bool HasStarlessNightContent()
        {
            foreach (Projectile projectile in Main.projectile)
            {
                if (projectile.active && projectile.ModProjectile is StarlessNightProj)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 是否还有需要上屏合成的东西：任一活跃的深渊裂隙、任一存活的深渊粒子，或任一活跃的"在深渊遮罩里自绘"的弹幕
        /// （噬渊鞭挞的鞭身拖出的折线、沧溟渊龙命中撕开的裂空）。
        /// 深渊刃起手阶段还没撕出裂缝，但渊水弹一路都在吐粒子，所以粒子也要算进来。
        /// </summary>
        private static bool HasAbyssContent()
        {
            foreach (Projectile projectile in Main.projectile)
            {
                if (projectile.active && (projectile.ModProjectile is AbyssalCrack || projectile.ModProjectile is YstralynProj || projectile.ModProjectile is NxCrack))
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
