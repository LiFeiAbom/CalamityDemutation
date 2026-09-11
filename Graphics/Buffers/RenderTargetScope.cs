using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace CalamityDemutation.Graphics.Buffers
{
    /// <summary>
    /// 渲染目标作用域（移植自灾厄 Daybreak 的 RenderTargetScope）：临时切换渲染目标，释放时还原。
    /// </summary>
    public readonly struct RenderTargetScope : IDisposable
    {
        private readonly GraphicsDevice graphicsDevice;
        private readonly RenderTargetBinding[] previous;

        /// <summary>
        /// 进入作用域：先记录当前绑定的渲染目标（previous），若 preserveContents 为真再尝试保留其内容
        /// （经 RenderTargetPreserver，目前为空实现），然后把 target 设为当前渲染目标；
        /// clearColor 有值时立即清屏。配合 using 使用，离开块时由 Dispose 还原绑定。
        /// </summary>
        public RenderTargetScope(RenderTarget2D target, bool preserveContents = true, Color? clearColor = null)
        {
            ArgumentNullException.ThrowIfNull(target);
            graphicsDevice = target.GraphicsDevice;
            previous = graphicsDevice.GetRenderTargets();
            if (preserveContents)
                RenderTargetPreserver.PreserveBindings(previous);
            graphicsDevice.SetRenderTarget(target);
            if (clearColor.HasValue)
                graphicsDevice.Clear(clearColor.Value);
        }

        /// <summary>
        /// 离开 using 作用域时还原进入前的渲染目标绑定（RAII 的关键一步，务必保证被执行）
        /// </summary>
        public void Dispose()
        {
            graphicsDevice.SetRenderTargets(previous);
        }
    }

    /// <summary>
    /// 渲染目标作用域扩展（移植自灾厄 Daybreak 的 RenderTargetScopeExtensions）
    /// </summary>
    public static class RenderTargetScopeExtensions
    {
        /// <summary>
        /// 扩展方法：对渲染目标开启一个 using 作用域，离开块时自动还原此前的绑定
        /// </summary>
        public static RenderTargetScope Scope(this RenderTarget2D target, bool preserveContents = true, Color? clearColor = null)
        {
            return new RenderTargetScope(target, preserveContents, clearColor);
        }
        /// <summary>
        /// 扩展方法：对租借到的渲染目标（RenderTargetLease）开启 using 作用域，
        /// 内部取其 Target；注意归还租约仍需另行 Dispose 租约本身
        /// </summary>
        public static RenderTargetScope Scope(this RenderTargetLease target, bool preserveContents = true, Color? clearColor = null)
        {
            return new RenderTargetScope(target.Target, preserveContents, clearColor);
        }
    }
}
