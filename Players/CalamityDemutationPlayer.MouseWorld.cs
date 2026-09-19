using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Players
{
    /// <summary>
    /// 跨端鼠标世界坐标（移植自 CalamityEntropy 自研的 mouseWorld 体系，用于替代只在本地有效的
    /// <c>Main.MouseWorld</c>）：手持弹幕在别的客户端上跑时读 Main.MouseWorld 只会拿到那个客户端自己的
    /// 鼠标位置，朝向会错乱，所以需要一套跨端可读的坐标。
    /// <para>
    /// 做法与 CE 一致：本地玩家每帧把自己的鼠标坐标写进 <see cref="mouseWorldDelta"/>
    /// （只存相对 <c>Player.MountedCenter</c> 的偏移，这是网络同步的最小载荷），联机时按
    /// 「间隔 2 帧 + 位移超过 5 像素」节流发一条短包；服务端收到后代播给其余客户端，
    /// 各端把偏移写进对应玩家的 ModPlayer 副本——于是每个端都能用**自己**当前的 MountedCenter
    /// 加上这个偏移还原出发送者的鼠标位置，玩家移动时也不会漂。
    /// </para>
    /// <para>
    /// 用法：读取方调 <see cref="GetMouseWorld"/>（它顺带置位本帧的监听标记，本地玩家据此决定要不要发包），
    /// 每帧的写入与标记复位由 <c>ResetEffects</c> 末尾统一调用 <see cref="UpdateMouseWorldSync"/> 完成。
    /// </para>
    /// </summary>
    internal partial class CalamityDemutationPlayer:ModPlayer
    {
        /// <summary>
        /// 自定义网络消息码：鼠标世界坐标同步（见本文件头部说明），由主类 CalamityDemutation.HandlePacket 处理。
        /// 载荷 = 玩家索引（byte）+ 相对 MountedCenter 的偏移（两个 short）。
        /// </summary>
        public const byte MsgSyncMouseWorld = 5;
        /// <summary>鼠标位移超过这个距离才值得发包（像素，CE 原阈值）</summary>
        private const float MouseSyncDistanceThreshold = 5f;
        /// <summary>两次发包之间至少间隔的帧数（CE 原节流值）</summary>
        private const int MouseSyncInterval = 2;
        /// <summary>本帧是否有武器/弹幕要读鼠标坐标：由读取方置位，每帧在 UpdateMouseWorldSync 末尾复位</summary>
        public bool MouseWorldListener;
        /// <summary>鼠标世界坐标相对 MountedCenter 的偏移（网络同步载荷；本端玩家由本地写入，其他玩家由网络包写入）</summary>
        public Vector2 mouseWorldDelta;
        /// <summary>上次发包时的偏移，用于判断位移是否超过阈值</summary>
        private Vector2 lastSyncedMouseDelta = new(float.MinValue, float.MinValue);
        /// <summary>距上次发包的帧数</summary>
        private int mouseSyncTimer;
        /// <summary>
        /// 各端可见的玩家鼠标世界坐标：以本端此刻的 MountedCenter 为基准加上同步来的偏移。
        /// 写入时把绝对坐标换算回相对偏移，读出来才是可跨端重建的值。
        /// </summary>
        public Vector2 MouseWorld
        {
            get => Player.MountedCenter + mouseWorldDelta;
            set => mouseWorldDelta = value - Player.MountedCenter;
        }
        /// <summary>
        /// 取该玩家各端可见的鼠标世界坐标（等价 CE 的 <c>CEUtils.MouseWorld</c>）：调用即置位本帧监听标记，
        /// 本地玩家据此在 <see cref="UpdateMouseWorldSync"/> 里决定要不要把坐标同步出去。
        /// </summary>
        public Vector2 GetMouseWorld()
        {
            MouseWorldListener = true;
            return MouseWorld;
        }
        /// <summary>
        /// 每帧的鼠标坐标维护（由 ResetEffects 末尾调用）：本地玩家写入当前鼠标位置，并在联机客户端上
        /// 按节流规则发包；末尾复位监听标记，等下一帧读取方重新置位。
        /// </summary>
        private void UpdateMouseWorldSync()
        {
            if (Player.whoAmI == Main.myPlayer && !Main.dedServ)
            {
                MouseWorld = Main.MouseWorld;
                mouseSyncTimer++;
                if (Main.netMode == NetmodeID.MultiplayerClient && MouseWorldListener
                    && mouseSyncTimer >= MouseSyncInterval && (mouseWorldDelta - lastSyncedMouseDelta).Length() > MouseSyncDistanceThreshold)
                {
                    mouseSyncTimer = 0;
                    lastSyncedMouseDelta = mouseWorldDelta;
                    ModPacket packet = Mod.GetPacket();
                    packet.Write(MsgSyncMouseWorld);
                    packet.Write((byte)Player.whoAmI);
                    packet.Write((short)mouseWorldDelta.X);
                    packet.Write((short)mouseWorldDelta.Y);
                    packet.Send();
                }
            }
            MouseWorldListener = false;
        }
        /// <summary>
        /// 写入别的玩家同步来的鼠标偏移（由主类 HandlePacket 分流过来；服务端只负责代播，不调这里）。
        /// </summary>
        internal void ReceiveMouseWorld(float deltaX, float deltaY)
        {
            mouseWorldDelta = new Vector2(deltaX, deltaY);
        }
    }
}
