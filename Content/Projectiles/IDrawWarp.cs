using Microsoft.Xna.Framework.Graphics;
namespace CalamityDemutation.Content.Projectiles
{
    /// <summary>
    /// 屏幕扭曲绘制接口（移植自 CWR 的 IDrawWarp）：
    /// 实现该接口的弹幕会被 EffectsSystem 收集，参与屏幕扭曲管线。
    /// </summary>
    internal interface IDrawWarp
    {
        /// <summary>
        /// 是否进行 costomDraw 额外绘制
        /// </summary>
        bool canDraw() => false;
        /// <summary>
        /// 额外自定义绘制，绘制内容不会被扭曲影响（画在扭曲结果之上）
        /// </summary>
        void costomDraw(SpriteBatch spriteBatch);
        /// <summary>
        /// 绘制扭曲遮罩，决定屏幕哪些像素如何位移
        /// </summary>
        void Warp();
    }
}
