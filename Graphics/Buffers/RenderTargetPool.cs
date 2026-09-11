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
        public static RenderTargetDescriptor Default { get; } = new(SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents, false);   // 默认：彩色、无深度、内容每次丢弃
        public static RenderTargetDescriptor DefaultPreserveContents { get; } = new(SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents, false);   // 默认但在切换目标时保留已有内容
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
        public RenderTarget2D Target { get; set; }
        private readonly RenderTargetPool pool;
        /// <summary>
        /// 记录被租借的目标与所属池，使 Dispose 能把目标归还到正确的池
        /// </summary>
        public RenderTargetLease(RenderTarget2D target, RenderTargetPool pool)
        {
            Target = target;
            this.pool = pool;
        }
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
        private static readonly SharedRenderTargetPool shared = new();
        public static RenderTargetPool Shared => shared;
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

    /// <summary>
    /// 共享渲染目标池实现（移植自灾厄 Daybreak 的 SharedRenderTargetPool）
    /// </summary>
    internal sealed class SharedRenderTargetPool : RenderTargetPool
    {
        private readonly record struct Key(int Width, int Height, RenderTargetDescriptor Descriptor)
        {
            /// <summary>
            /// 由渲染目标反推缓存键（尺寸 + 格式描述符），用于归还时归类
            /// </summary>
            public static Key From(RenderTarget2D target)
            {
                return new Key(target.Width, target.Height, RenderTargetDescriptor.From(target));
            }
        }

        private sealed class Entry
        {
            public Stack<RenderTarget2D> Targets { get; } = new Stack<RenderTarget2D>();   // 同规格目标的空闲栈
            public DateTime LastUsed { get; set; } = DateTime.UtcNow;   // 最近一次使用时间，用于超时清理
        }

        private const int max_per_key = 4;   // 每种规格最多缓存 4 个目标
        private const int max_total_targets = 128;   // 全池缓存总量上限
        private static readonly TimeSpan max_idle_time = TimeSpan.FromSeconds(5);   // 空闲超过 5 秒的缓存会被回收
        private static readonly TimeSpan minimum_trim_time = TimeSpan.FromSeconds(1);   // 两次回收尝试的最小间隔，避免每帧遍历

        private readonly Dictionary<Key, Entry> cache = new Dictionary<Key, Entry>();
        private DateTime lastTrimmed = DateTime.UtcNow;
        private int totalCached;
        private bool disposed;

        /// <summary>
        /// 借出（Rent）：校验参数与池状态后按规格查缓存，命中则弹出一个空闲目标，未命中则新建；
        /// 返回值用 RenderTargetLease 包裹（其 Dispose 即归还）。无论成功与否，最后都会尝试做一次超时回收。
        /// </summary>
        public override RenderTargetLease Rent(GraphicsDevice device, int width, int height, RenderTargetDescriptor descriptor)
        {
            ArgumentNullException.ThrowIfNull(device);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(width, 0);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(height, 0);
            ObjectDisposedException.ThrowIf(disposed, this);
            try
            {
                var key = new Key(width, height, descriptor);
                if (!cache.TryGetValue(key, out var entry))
                {
                    cache[key] = entry = new Entry();
                }
                else
                {
                    entry.LastUsed = DateTime.UtcNow;
                }
                RenderTarget2D target;
                if (entry.Targets.Count > 0)
                {
                    target = entry.Targets.Pop();   // 复用缓存目标
                    totalCached--;
                }
                else
                {
                    target = descriptor.Create(device, width, height);   // 缓存为空则新建
                }
                return new RenderTargetLease(target, this);
            }
            finally
            {
                TrimAged();
            }
        }

        /// <summary>
        /// 归还（Return）：目标按规格压回空闲栈复用；若该规格已缓存满（max_per_key）
        /// 或全池缓存量达上限（max_total_targets），则直接 Dispose 真正释放。
        /// </summary>
        public override void Return(RenderTargetLease lease)
        {
            ArgumentNullException.ThrowIfNull(lease);
            ObjectDisposedException.ThrowIf(disposed, this);
            var key = Key.From(lease.Target);
            if (!cache.TryGetValue(key, out var entry))
            {
                cache[key] = entry = new Entry();
            }
            else
            {
                entry.LastUsed = DateTime.UtcNow;
            }
            if (entry.Targets.Count < max_per_key)
            {
                if (totalCached >= max_total_targets)
                {
                    lease.Target.Dispose();   // 池总量已满，不再缓存
                    return;
                }
                entry.Targets.Push(lease.Target);
                totalCached++;
            }
            else
            {
                lease.Target.Dispose();   // 该规格缓存已满，直接释放
            }
        }

        /// <summary>
        /// 释放整个池：清空并 Dispose 所有缓存目标，置 disposed 标记避免重复释放
        /// </summary>
        public override void Dispose()
        {
            if (disposed)
                return;
            Trim();
            disposed = true;
        }

        /// <summary>
        /// 释放并清空全部缓存目标（池销毁时调用）
        /// </summary>
        private void Trim()
        {
            foreach (var entry in cache.Values)
            {
                while (entry.Targets.Count > 0)
                {
                    entry.Targets.Pop().Dispose();
                    totalCached--;
                }
            }
            cache.Clear();
        }

        /// <summary>
        /// 回收长时间未使用的缓存：受 minimum_trim_time 限流（至少间隔 1 秒才真正遍历），
        /// 清理掉超过 max_idle_time（5 秒）没被使用的规格。在每次 Rent 的 finally 中调用。
        /// </summary>
        private void TrimAged()
        {
            var now = DateTime.UtcNow;
            if (now - lastTrimmed < minimum_trim_time)
                return;
            lastTrimmed = now;
            foreach (var pair in cache)
            {
                if (now - pair.Value.LastUsed <= max_idle_time)
                    continue;
                while (pair.Value.Targets.Count > 0)
                {
                    pair.Value.Targets.Pop().Dispose();
                    totalCached--;
                }
                cache.Remove(pair.Key);
            }
        }
    }
}
