using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Common.Effects
{
    /// <summary>
    /// 中子星扭曲绘制工具（移植自 CWR 的 NeutronWarpHelper）：把 <c>NeutronWarp</c> 着色器
    /// 算出的位移场，一次性画进当前渲染目标。
    /// <para>
    /// 与旧写法的区别：过去是 CPU 侧把一张遮罩贴图叠几十层（近战小爆点 33 层、大爆点 133 层）来糊出
    /// 干涉环带，现在是着色器按技法程序化生成——画质更好且开销恒定为一层。CWR 原文注释即写作
    /// "NeutronWarp 替 CPU 叠绘"。
    /// </para>
    /// <para>
    /// 调用时机：必须在 <see cref="CalamityDemutation.Content.Projectiles.IDrawWarp.Warp"/> 里调用，
    /// 此时 EffectsSystem 已经把 <c>Main.screenTargetSwap</c> 设为当前渲染目标、并开着一个
    /// spriteBatch。本方法会先 <c>End()</c> 掉那个 batch，用自己的批次画，最后再按**与调用方逐项相同**
    /// 的参数 <c>Begin()</c> 回去，保证 EffectsSystem 的循环能继续遍历下一个扭曲源。
    /// </para>
    /// </summary>
    internal static class NeutronWarpHelper
    {
        /// <summary>画那个被着色器铺满的 1×1 白像素（工程通用占位贴图）</summary>
        private const string WhitePixel = "CalamityDemutation/Assets/ExtraTextures/white";
        /// <summary>
        /// 绘制一次中子星扭曲。
        /// <para>
        /// 可用技法（NeutronWarp.fx 里定义的 technique 名）：
        /// GravitationalVortex 引力漩涡 / ShockwaveRing 冲击波环 / RelativisticJet 相对论性喷流 /
        /// GravitationalLens 引力透镜 / KamuiLine 沿线拉扯。本工程目前只用后两个里的
        /// ShockwaveRing（爆炸）与 GravitationalLens（弹丸）。
        /// </para>
        /// </summary>
        /// <param name="worldCenter">扭曲场中心的世界坐标</param>
        /// <param name="screenWidth">扭曲场矩形宽（屏幕像素）</param>
        /// <param name="screenHeight">扭曲场矩形高（屏幕像素）</param>
        /// <param name="intensity">位移强度 0~1</param>
        /// <param name="progress">生命进度 0~1，控制扩张/收缩</param>
        /// <param name="rotation">极角图案的自转量（弧度）</param>
        /// <param name="technique">技法名，见上方列表</param>
        /// <param name="radius">UV 归一化半径，默认 0.45</param>
        public static void DrawWarp(Vector2 worldCenter, float screenWidth, float screenHeight
            , float intensity, float progress, float rotation, string technique, float radius = 0.45f)
        {
            if (Main.dedServ || EffectLoader.NeutronWarp is null)
                return;
            Effect effect = EffectLoader.NeutronWarp.Value;
            if (effect is null)
                return;
            effect.Parameters["uTime"]?.SetValue(Main.GameUpdateCount * 0.05f);
            effect.Parameters["uIntensity"]?.SetValue(intensity);
            effect.Parameters["uProgress"]?.SetValue(MathHelper.Clamp(progress, 0f, 1f));
            effect.Parameters["uRadius"]?.SetValue(radius);
            effect.Parameters["uRotation"]?.SetValue(rotation);
            effect.CurrentTechnique = effect.Techniques[technique];
            // 先把调用方（EffectsSystem 的扭曲遮罩循环）开着的批次收掉，换成带本着色器的批次
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend
                , Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone
                , effect, Main.GameViewMatrix.TransformationMatrix);
            effect.CurrentTechnique.Passes[0].Apply();
            Vector2 screenPos = worldCenter - Main.screenPosition;
            Texture2D pixel = ModContent.Request<Texture2D>(WhitePixel).Value;
            Rectangle destRect = new Rectangle((int)(screenPos.X - screenWidth * 0.5f)
                , (int)(screenPos.Y - screenHeight * 0.5f), (int)screenWidth, (int)screenHeight);
            Main.spriteBatch.Draw(pixel, destRect, new Rectangle(0, 0, 1, 1), Color.White);
            Main.spriteBatch.End();
            // 按调用方原来的参数把批次还回去，EffectsSystem 才能接着遍历下一个扭曲源
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend
                , Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone
                , null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
