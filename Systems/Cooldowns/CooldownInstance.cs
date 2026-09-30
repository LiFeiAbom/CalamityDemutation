using System;
using CalamityDemutation.Systems.UI;
using Terraria;
namespace CalamityDemutation.Systems.Cooldowns
{
    /// <summary>
    /// 冷却实例（移植自灾厄 2.2.2）：某个玩家身上"正在走"的一条冷却，
    /// 记录 netID / 所属玩家 / 总时长 / 剩余时长，并持有实现行为与绘制的 CooldownHandler 实例。
    /// </summary>
    internal class CooldownInstance
    {
        public CooldownInstance(Player p, Cooldown cd, int dur)
        {
            netID = cd.netID;
            player = p;
            duration = dur;
            timeLeft = dur;
            handler = null;
            AssignHandler(cd);
        }
        public CooldownInstance(Player p, Cooldown cd, int dur, params object[] args)
        {
            netID = cd.netID;
            player = p;
            duration = dur;
            timeLeft = dur;
            handler = null;
            AssignHandler(cd, args);
        }
        // 下面两个方法假定传入的 Cooldown 一定是 Cooldown<T>:CooldownHandler>（事实也确实如此，
        // 因为没有任何代码会直接实例化非泛型的 Cooldown）
        internal void AssignHandler(Cooldown cd)
        {
            Type handlerT = cd.GetType().GenericTypeArguments[0];
            handler = Activator.CreateInstance(handlerT) as CooldownHandler;
            handler.instance = this;
        }
        internal void AssignHandler(Cooldown cd, params object[] args)
        {
            Type handlerT = cd.GetType().GenericTypeArguments[0];
            handler = Activator.CreateInstance(handlerT, args) as CooldownHandler;
            handler.instance = this;
        }
        /// <summary>
        /// 本实例所代表冷却的 netID；游戏引擎用它在运行期反查行为与绘制实现
        /// </summary>
        internal ushort netID;
        /// <summary>
        /// 该冷却所属的玩家
        /// </summary>
        public Player player;
        /// <summary>
        /// 冷却的原始总时长（帧，60 帧 = 1 秒）
        /// </summary>
        public int duration;
        /// <summary>
        /// 冷却的剩余时长（帧，60 帧 = 1 秒）
        /// </summary>
        public int timeLeft;
        /// <summary>
        /// 冷却的"完成度"比例
        /// </summary>
        public float Completion => CooldownRackUI.DebugFullDisplay ? CooldownRackUI.DebugForceCompletion : (duration != 0 ? timeLeft / (float)duration : 0);
        /// <summary>
        /// 实现该冷却行为与绘制方式的处理器
        /// </summary>
        public CooldownHandler handler;
    }
}
