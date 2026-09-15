using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria.ModLoader;
namespace CalamityDemutation.Systems.Cooldowns
{
    /// <summary>
    /// 冷却注册表（移植自灾厄 2.2.2）：
    /// 在 ResizeArrays 阶段收拢本模组所有已自动注册的 CooldownHandler 子类，
    /// 逐个分配 netID 并建立"字符串 ID → netID"映射；之后才能用字符串 ID 创建冷却实例。
    /// </summary>
    internal sealed class CooldownRegistry:ModSystem
    {
        /// <summary>
        /// 按 ushort netID 索引的全部已注册冷却（注册时分配 netID，未注册的冷却不可使用）
        /// </summary>
        public static Cooldown[] registry = new Cooldown[defaultSize];
        private const ushort defaultSize = 256;
        private static ushort nextCDNetID = 0;
        private static Dictionary<string, ushort> nameToNetID = new Dictionary<string, ushort>(defaultSize);
        /// <summary>
        /// 内容加载完成后为每个 CooldownHandler 子类分配 netID 并登记 ID 映射；
        /// 处理器 ID 取子类静态属性 ID（缺失时回退为类型全名）
        /// </summary>
        public override void ResizeArrays()
        {
            var cooldowns = ModContent.GetContent<CooldownHandler>();
            var count = cooldowns.Count();
            Array.Resize(ref registry, count);
            MethodInfo registerBaseMethod = typeof(CooldownRegistry).GetMethod(nameof(Register), BindingFlags.Public | BindingFlags.Static);
            foreach (var cooldown in cooldowns)
            {
                var type = cooldown.GetType();
                // 取处理器的静态 ID 属性；取不到时用类型全名兜底
                string handlerID = (string)type.GetProperty("ID").GetValue(null);
                handlerID ??= cooldown.FullName;
                // 反射构造泛型 Register 调用（此处无法直接把 type 作为泛型实参传入）
                MethodInfo genericRegister = registerBaseMethod.MakeGenericMethod(typeArguments: [type]);
                genericRegister.Invoke(null, [handlerID]);
            }
        }
        public override void Unload()
        {
            registry = null;
            nameToNetID?.Clear();
            nameToNetID = null;
        }
        /// <summary>
        /// 按字符串 ID 取出已注册的冷却，未注册时返回 null
        /// </summary>
        public static Cooldown Get(string id)
        {
            bool hasValue = nameToNetID.TryGetValue(id, out ushort netID);
            return hasValue ? registry[netID] : null;
        }
        /// <summary>
        /// 为某个 CooldownHandler 注册一条冷却并分配 netID；注册前该冷却无法使用
        /// </summary>
        /// <returns>注册得到的冷却标识</returns>
        public static Cooldown<HandlerT> Register<HandlerT>(string id) where HandlerT:CooldownHandler
        {
            int currentMaxID = registry.Length;
            // 仅当注册数达到 65,536 上限（理论上不可能）时拒绝继续注册
            if (nextCDNetID == currentMaxID)
                return null;
            Cooldown<HandlerT> cd = new Cooldown<HandlerT>(id, nextCDNetID);
            nameToNetID[cd.ID] = cd.netID;
            registry[cd.netID] = cd;
            ++nextCDNetID;
            // 数组用到末尾时容量翻倍（原灾厄注释：目前基本不需要，留作保险）
            if (nextCDNetID == currentMaxID && currentMaxID < ushort.MaxValue)
            {
                Cooldown[] largerArray = new Cooldown[currentMaxID * 2];
                for (int i = 0; i < currentMaxID; ++i)
                    largerArray[i] = registry[i];
                registry = largerArray;
            }
            return cd;
        }
    }
}
