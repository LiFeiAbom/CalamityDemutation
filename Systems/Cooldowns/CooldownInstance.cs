using System;
using System.IO;
using CalamityDemutation.Systems.UI;
using Terraria;
using Terraria.ModLoader.IO;
namespace CalamityDemutation.Systems.Cooldowns
{
    /// <summary>
    /// 冷却实例（移植自灾厄 2.2.2）：某个玩家身上"正在走"的一条冷却，
    /// 记录 netID / 所属玩家 / 总时长 / 剩余时长，并持有实现行为与绘制的 CooldownHandler 实例。
    /// </summary>
    internal class CooldownInstance
    {
        private const string NetIDSaveKey = "netID";
        private const string DurationSaveKey = "duration";
        private const string TimeLeftSaveKey = "timeLeft";
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
        internal CooldownInstance(Player p, string id, TagCompound tag)
        {
            netID = (ushort)tag.GetAsInt(NetIDSaveKey);
            Cooldown cd = CooldownRegistry.Get(id);
            // 存档里带有的冷却可能已经不存在（例如某个冷却被移除后读旧档）
            if (cd is null)
            {
                CalamityDemutation.Instance.Logger.Warn($"Cooldown \"{id}\" loaded from NBT, but was not found. This cooldown will not be applied to the player.");
                return;
            }
            // 若 netID 与注册值不一致则以注册值为准（字符串 ID 的优先级高于存档里的 netID）
            ushort registeredNetID = cd.netID;
            if (netID != registeredNetID)
            {
                CalamityDemutation.Instance.Logger.Warn($"Cooldown \"{id}\" loaded from NBT with discrepant netID {netID}. This cooldown was registered with netID {registeredNetID}");
                netID = registeredNetID;
            }
            player = p;
            duration = tag.GetAsInt(DurationSaveKey);
            timeLeft = tag.GetAsInt(TimeLeftSaveKey);
            AssignHandler(cd);
        }
        internal CooldownInstance(Player player, ushort netID, int duration, int timeLeft)
        {
            this.netID = netID;
            this.player = player;
            this.duration = duration;
            this.timeLeft = timeLeft;
            string id = CooldownRegistry.registry[netID].ID;
            AssignHandler(CooldownRegistry.Get(id));
        }
        /// <summary>
        /// 从二进制流（网络同步）反序列化构造一个冷却实例
        /// </summary>
        internal CooldownInstance(BinaryReader reader)
        {
            netID = reader.ReadUInt16();
            byte playerIDByte = reader.ReadByte();
            player = Main.player[playerIDByte];
            duration = reader.ReadInt32();
            timeLeft = reader.ReadInt32();
            string id = CooldownRegistry.registry[netID].ID;
            AssignHandler(CooldownRegistry.Get(id));
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
        /// <summary>
        /// 把本实例序列化成 TagCompound，供随玩家存档写入
        /// </summary>
        internal TagCompound Save()
        {
            return new TagCompound
            {
                { NetIDSaveKey, (int)netID },
                { DurationSaveKey, duration },
                { TimeLeftSaveKey, timeLeft }
            };
        }
        /// <summary>
        /// 把本实例序列化成二进制数据，供网络同步
        /// </summary>
        internal void Write(BinaryWriter writer)
        {
            writer.Write(netID);
            byte playerIDByte = (byte)player.whoAmI;
            writer.Write(playerIDByte);
            writer.Write(duration);
            writer.Write(timeLeft);
        }
    }
}
