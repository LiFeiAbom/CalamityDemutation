using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
namespace CalamityDemutation.Graphics.Buffers
{
    /// <summary>
    /// 屏幕空间渲染目标池（移植自灾厄 Daybreak 的 ScreenspaceTargetPool）。
    /// 与基类 <see cref="RenderTargetPool"/> 的共享池不同，本池不做“复用缓存”：每次 Rent 都新建一个渲染目标，
    /// 仅用字典登记尚未归还的租约，供整池 Dispose 时兜底释放。
    /// 配对约束：调用方拿到 RenderTargetLease 后必须在用完后 Dispose（其内部即调用 Return 归还），
    /// 否则 RenderTarget 会一直占用显存直到整池释放。
    /// </summary>
    internal sealed class ScreenspaceTargetPool : RenderTargetPool
    {
        // ── 实例字段 ──
        /// <summary>
        /// 是否已释放；置位后任何 Rent/Return 都会抛 ObjectDisposedException
        /// </summary>
        private bool disposed;
        /// <summary>
        /// 尚未归还的租约登记表（租约 → 其尺寸回调），仅用于整池 Dispose 时兜底释放
        /// </summary>
        private readonly Dictionary<RenderTargetLease, GetTargetSize> cache = new Dictionary<RenderTargetLease, GetTargetSize>();
        // ── 属性 ──
        /// <summary>
        /// 本池的单例实例
        /// </summary>
        public static ScreenspaceTargetPool Shared { get; } = new();
        // ── 嵌套类型 ──
        /// <summary>
        /// 渲染目标尺寸回调：由调用方根据当前屏幕状态决定要创建的宽高。
        /// 四个参数依次为后缓冲宽、后缓冲高、离屏目标（Main.instance.tileTarget）宽、离屏目标高。
        /// </summary>
        public delegate (int Width, int Height) GetTargetSize(int backbufferWidth, int backbufferHeight, int offscreenTargetWidth, int offscreenTargetHeight);
        // ── 构造函数 ──
        /// <summary>
        /// 私有构造：本类为单例池，外部只能通过静态属性 Shared 获取唯一实例
        /// </summary>
        private ScreenspaceTargetPool() { }
        // ── 公开方法 ──
        /// <summary>
        /// 借出指定固定宽高的渲染目标（基类抽象方法的实现）。
        /// 做参数与池状态校验后委托给回调重载，回调无视屏幕实际尺寸恒返回 (width, height)。
        /// </summary>
        public override RenderTargetLease Rent(GraphicsDevice device, int width, int height, RenderTargetDescriptor descriptor)
        {
            ArgumentNullException.ThrowIfNull(device);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(width, 0);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(height, 0);
            ObjectDisposedException.ThrowIf(disposed, this);
            return Rent(device, (_, _, _, _) => (width, height), descriptor);
        }
        /// <summary>
        /// 借出与后缓冲（backbuffer）同尺寸的渲染目标，是最常用的无参形式。
        /// 尺寸回调直接原样返回后缓冲宽高。
        /// </summary>
        public RenderTargetLease Rent(GraphicsDevice device, RenderTargetDescriptor? descriptor = null)
        {
            return Rent(device, (width, height) => (width, height), descriptor);
        }
        /// <summary>
        /// 借出渲染目标，尺寸由回调仅依据后缓冲宽高计算。
        /// 适用于不需要离屏尺寸的场合：把该回调转接为完整版 GetTargetSize（忽略后两个离屏参数）。
        /// </summary>
        public RenderTargetLease Rent(GraphicsDevice device, Func<int, int, (int, int)> targetSizeCallback, RenderTargetDescriptor? descriptor = null)
        {
            return Rent(device, (width, height, _, _) => targetSizeCallback(width, height), descriptor);
        }
        /// <summary>
        /// 核心借出实现（其余三个 Rent 重载最终都汇聚到这里）。
        /// 校验设备、回调与池状态；descriptor 为空时取 RenderTargetDescriptor.Default；
        /// 读取当前屏幕尺寸交给 targetSizeCallback 算出宽高，新建渲染目标并用租约包裹，
        /// 同时把租约登记进 cache，以便归还或整池释放时处理。
        /// </summary>
        public RenderTargetLease Rent(GraphicsDevice device, GetTargetSize targetSizeCallback, RenderTargetDescriptor? descriptor = null)
        {
            ArgumentNullException.ThrowIfNull(device);
            ArgumentNullException.ThrowIfNull(targetSizeCallback);
            ObjectDisposedException.ThrowIf(disposed, this);
            descriptor ??= RenderTargetDescriptor.Default;
            GetTargetSizes(device, out var backbufferWidth, out var backbufferHeight, out var offscreenTargetWidth, out var offscreenTargetHeight);
            var (width, height) = targetSizeCallback(backbufferWidth, backbufferHeight, offscreenTargetWidth, offscreenTargetHeight);
            var target = descriptor.Value.Create(device, width, height);
            var lease = new RenderTargetLease(target, this);
            cache[lease] = targetSizeCallback;
            return lease;
        }
        /// <summary>
        /// 归还租约：从登记表移除该租约并立即 Dispose 其渲染目标（本池不复用缓存）。
        /// 若租约不在本池登记表中（例如已归还过）则直接返回，不做任何处理。
        /// </summary>
        public override void Return(RenderTargetLease lease)
        {
            ArgumentNullException.ThrowIfNull(lease);
            ObjectDisposedException.ThrowIf(disposed, this);
            if (!cache.Remove(lease))
                return;
            lease.Target.Dispose();
        }
        /// <summary>
        /// 释放整个池：先释放所有尚未归还的目标，再置 disposed 标记（幂等，重复调用直接返回）。
        /// 置位后任何 Rent/Return 都会抛 ObjectDisposedException。
        /// </summary>
        public override void Dispose()
        {
            if (disposed)
                return;
            Trim();
            disposed = true;
        }
        // ── 私有工具 ──
        /// <summary>
        /// 读取当前后缓冲尺寸（PresentationParameters）与离屏 tileTarget 尺寸，供尺寸回调计算使用
        /// </summary>
        private static void GetTargetSizes(GraphicsDevice device, out int backbufferWidth, out int backbufferHeight, out int offscreenTargetWidth, out int offscreenTargetHeight)
        {
            backbufferWidth = device.PresentationParameters.BackBufferWidth;
            backbufferHeight = device.PresentationParameters.BackBufferHeight;
            offscreenTargetWidth = Main.instance.tileTarget.Width;
            offscreenTargetHeight = Main.instance.tileTarget.Height;
        }
        /// <summary>
        /// 释放并清空登记表中所有尚未归还的渲染目标（仅在整池 Dispose 时调用）
        /// </summary>
        private void Trim()
        {
            foreach (var lease in cache.Keys)
                lease.Target.Dispose();
            cache.Clear();
        }
    }
}
