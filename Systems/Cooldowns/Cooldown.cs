namespace CalamityDemutation.Systems.Cooldowns
{
    /// <summary>
    /// 冷却标识基类（移植自灾厄 2.2.2）：冷却本身只是一个字符串 ID + 一个注册期分配的 netID，
    /// 是网络同步用的最小识别接口；真正的行为由 CooldownHandler 子类实现，运行状态由 CooldownInstance 承载。
    /// 本基类永远不会被直接实例化，实际使用的是下面的泛型版本 Cooldown&lt;T&gt;。
    /// </summary>
    internal class Cooldown
    {
        /// <summary>
        /// 注册时分配的 netID，一经注册不可再改动
        /// </summary>
        internal readonly ushort netID;
        /// <summary>
        /// 冷却的唯一字符串 ID，建议在 CooldownHandler 实现里以静态属性形式定义
        /// </summary>
        public readonly string ID = "";
        internal Cooldown(string id, ushort nid)
        {
            ID = id;
            netID = nid;
        }
    }
    /// <summary>
    /// 泛型冷却标识：把 Handler 类型编码进泛型参数，
    /// CooldownInstance 通过反射取出该类型来创建对应的行为与绘制处理器。
    /// </summary>
    internal class Cooldown<T>:Cooldown where T:CooldownHandler
    {
        internal Cooldown(string id, ushort nid):base(id, nid) { }
    }
}
