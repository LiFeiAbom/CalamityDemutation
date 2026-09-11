using Microsoft.Xna.Framework.Graphics;
using Terraria;
namespace CalamityDemutation.Graphics.Buffers
{
    /// <summary>
    /// 渲染目标内容保留辅助（移植自灾厄 Daybreak 的 RenderTargetPreserver）
    /// </summary>
    public static class RenderTargetPreserver
    {
        /// <summary>
        /// 读取当前设备的渲染目标绑定并对其调用 PreserveBindings，最后把绑定数组返回给调用方
        /// </summary>
        public static RenderTargetBinding[] GetAndPreserveCurrentBindings()
        {
            var bindings = Main.instance.GraphicsDevice.GetRenderTargets();
            PreserveBindings(bindings);
            return bindings;
        }
        /// <summary>
        /// 尝试为传入的渲染目标绑定保留内容。当前为空实现：
        /// tML2026 的 RenderTarget2D.RenderTargetUsage 为只读，无法强制改为 PreserveContents，
        /// 保留此方法仅为与灾厄 Daybreak 的调用点保持一致的接口。
        /// </summary>
        public static void PreserveBindings(RenderTargetBinding[] bindings)
        {
            // tML2026 的 RenderTarget2D.RenderTargetUsage 为只读，无法强制保留内容，故此处不做处理
        }
    }
}
