using Terraria.ModLoader;
namespace CalamityDemutation.Players
{
    /// <summary>
    /// 各近战武器「本轮第几式 / 第几次」的玩家侧计数汇总（只保留**未被删除的武器**用的那几个：
    /// CWR 重制的无政府之刃 / 巨龙之怒 / 星流之刃，以及灾厄本体的元素圣剑 / 禅心剑）。
    /// <para>
    /// 为什么统一放这里：这些计数器原本是 <c>ModItem</c> 上的实例字段，而 ModItem 是全类型共用的单例——
    /// 单机看不出问题，联机时两名玩家拿同一把武器会互相翻转对方的招式与段位（虚空薄锋的左右键连招、
    /// 巨龙之怒的段数、最终分形的十段循环都是典型受害者）。挪进 ModPlayer 后每名玩家各持一份。
    /// </para>
    /// <para>
    /// 不需要额外发包：这些值只在出手那一刻决定弹幕的 <c>ai</c> 与伤害，两者随弹幕的生成包一起同步给其他端；
    /// 跨世界与重生都不清空，与它们原本那份共享计数器的行为一致（招式循环一直延续）。
    /// 这套写法的来历见 git 历史（原先按此口径逐个改造）。
    /// </para>
    /// </summary>
    internal partial class CalamityDemutationPlayer:ModPlayer
    {
        /// <summary>无政府之刃：左键挥砍计数，每满 3 次才发射一道光束</summary>
        public int anarchyBladeShootCount = 0;
        /// <summary>巨龙之怒：左键四段挥砍的段数（0~3，伤害依次 ×1.0/×1.15/×1.25/×1.55）</summary>
        public int dragonRageLevel = 0;
        /// <summary>巨龙之怒：右键两段重击的段数（0/1 → ai 4/5），第三次右键放蓄力重击后归零</summary>
        public int dragonRageLevelAlt = 0;
        /// <summary>星流之刃：累计命中次数，满 5 次触发爆炸分支（彗星分支见下一个字段）</summary>
        public int exoBladeHitCount = 0;
        /// <summary>星流之刃：累计命中次数，满 2 次触发彗星分支</summary>
        public int exoBladeHitCount2 = 0;
        /// <summary>元素圣剑：左键彩虹光束的颜色编号，每次发射递增、0~11 循环（同时决定挥砍粉尘颜色）</summary>
        public int elementalExcaliburBeamType = 0;
        /// <summary>禅心剑：本次挥砍是否还没响过命中音（出手置真、命中置假）</summary>
        public bool ataraxiaHitSound = true;
    }
}
