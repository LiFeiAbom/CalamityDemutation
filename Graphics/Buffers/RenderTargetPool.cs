using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
namespace CalamityDemutation.Graphics.Buffers
{
    /// <summary>
    /// 渲染目标创建参数（移植自灾厄 Daybreak 的 RenderTargetDescriptor）
    /// </summary>
    public readonly record struct RenderTargetDescriptor(
        SurfaceFormat Format,
        DepthFormat Depth,
        int MultiSampleCount,
        RenderTargetUsage Usage,
        bool GenerateMipmaps
    )
    {
        /// <summary>
        /// 默认描述符：彩色格式、无深度、不做多重采样、切换目标时丢弃内容、不生成 mipmap
        /// </summary>
        public static RenderTargetDescriptor Default { get; } = new(SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents, false);
        /// <summary>
        /// 默认描述符但改用 PreserveContents：切换渲染目标时保留其已有内容
        /// </summary>
        public static RenderTargetDescriptor DefaultPreserveContents { get; } = new(SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents, false);
        /// <summary>
        /// 按本描述符的参数在指定设备上创建一个新的渲染目标
        /// </summary>
        public RenderTarget2D Create(GraphicsDevice device, int width, int height)
        {
            return new RenderTarget2D(device, width, height, GenerateMipmaps, Format, Depth, MultiSampleCount, Usage);
        }
        /// <summary>
        /// 根据一个已存在的渲染目标反推描述符（内容丢弃模式；LevelCount&gt;1 视为生成 mipmap），
        /// 用于归还时计算缓存键
        /// </summary>
        public static RenderTargetDescriptor From(RenderTarget2D target)
        {
            return new RenderTargetDescriptor(target.Format, target.DepthStencilFormat, target.MultiSampleCount, RenderTargetUsage.DiscardContents, target.LevelCount > 1);
        }
    }
    /// <summary>
    /// 从池中租借的渲染目标（移植自灾厄 Daybreak 的 RenderTargetLease）
    /// </summary>
    public sealed class RenderTargetLease : IDisposable
    {
        // ── 实例字段 ──
        /// <summary>
        /// 目标所属的池，Dispose 时据此归还
        /// </summary>
        private readonly RenderTargetPool pool;
        // ── 属性 ──
        /// <summary>
        /// 被租借的渲染目标本体
        /// </summary>
        public RenderTarget2D Target { get; set; }
        // ── 构造函数 ──
        /// <summary>
        /// 记录被租借的目标与所属池，使 Dispose 能把目标归还到正确的池
        /// </summary>
        public RenderTargetLease(RenderTarget2D target, RenderTargetPool pool)
        {
            Target = target;
            this.pool = pool;
        }
        // ── 公开方法 ──
        /// <summary>
        /// 归还目标（IDisposable 形式，便于 using 作用域自动归还）；由池决定回收缓存还是真正释放
        /// </summary>
        public void Dispose()
        {
            pool.Return(this);
        }
    }
    /// <summary>
    /// 渲染目标资源池基类（移植自灾厄 Daybreak 的 RenderTargetPool）
    /// </summary>
    public abstract class RenderTargetPool : IDisposable
    {
        // ── 公开方法 ──
        /// <summary>
        /// 借出一个渲染目标：优先复用池中同规格对象，无可用对象时新建，返回的租约需用 Dispose 归还
        /// </summary>
        public abstract RenderTargetLease Rent(GraphicsDevice device, int width, int height, RenderTargetDescriptor descriptor);
        /// <summary>
        /// 归还租约：池自行决定缓存复用或立即释放
        /// </summary>
        public abstract void Return(RenderTargetLease lease);
        /// <summary>
        /// 释放池本身及其缓存的所有渲染目标
        /// </summary>
        public abstract void Dispose();
    }
}
