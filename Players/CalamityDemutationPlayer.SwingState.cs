using Terraria.ModLoader;
namespace CalamityDemutation.Players
{
    /// <summary>
    /// 各近战武器「本轮第几式 / 第几次」的玩家侧计数汇总。
    /// <para>
    /// 为什么统一放这里：这些计数器原本是 <c>ModItem</c> 上的实例字段，而 ModItem 是全类型共用的单例——
    /// 单机看不出问题，联机时两名玩家拿同一把武器会互相翻转对方的招式与段位（虚空薄锋的左右键连招、
    /// 巨龙之怒的段数、最终分形的十段循环都是典型受害者）。挪进 ModPlayer 后每名玩家各持一份。
    /// </para>
    /// <para>
    /// 不需要额外发包：这些值只在出手那一刻决定弹幕的 <c>ai</c> 与伤害，两者随弹幕的生成包一起同步给其他端；
    /// 跨世界与重生都不清空，与它们原本那份共享计数器的行为一致（招式循环一直延续）。
    /// 首个按此口径改造的是泓渊亡铭（见 <c>CalamityDemutationPlayer.Erebodrepanon.cs</c>）。
    /// </para>
    /// </summary>
    internal partial class CalamityDemutationPlayer:ModPlayer
    {
        /// <summary>深渊分形：本次挥砍的朝向，1 与 -1 交替（传给弹幕时 0 记作 -1）</summary>
        public int abyssFractalAtkType = 1;
        /// <summary>璀璨分形：本次挥砍的朝向，1 与 -1 交替（传给弹幕时 0 记作 -1）</summary>
        public int brilliantFractalAtkType = 1;
        /// <summary>星熠分形：本次挥砍的朝向，1 与 -1 交替（传给弹幕时 0 记作 -1）</summary>
        public int starlitFractalAtkType = 1;
        /// <summary>星熠分形：使用次数计数，每两次（第 2、4、6…次）额外出射一颗分形之星</summary>
        public int starlitFractalUseCount = 0;
        /// <summary>破碎剑柄：本次挥砍的方向标记（1 / -1 交替），作为 ai[0] 传给手持弹幕</summary>
        public int brokenHiltAtkType = 1;
        /// <summary>元素分形：本次挥砍的招式，0 = 环绕旋挥、1 = 朝鼠标刺出（两者交替）</summary>
        public int elementalFractalAtkType = 0;
        /// <summary>破碎分形：本次挥砍的招式下标，0→1→2 循环（2 为刺出式）</summary>
        public int shatteredFractalAtkType = 0;
        /// <summary>苍穹分形：本次挥砍的招式下标，0→1→2 循环（2 为刺出式）</summary>
        public int welkinFractalAtkType = 0;
        /// <summary>灵魂分形：本次挥砍的招式，0~4 循环（0/2 左挥、1/3 右挥、4 掷剑）</summary>
        public int spiritFractalAtkType = 0;
        /// <summary>虚空分形：本次挥砍的招式，0~7 循环（偶数左挥、奇数右挥、6 掷剑、7 全屏斩）</summary>
        public int voidFractalAtkType = 0;
        /// <summary>最终分形：本次挥砍的招式，0~9 循环（偶数左挥、奇数右挥、8 掷剑、9 锁链全屏斩）</summary>
        public int finalFractalAtkType = 0;
        /// <summary>无政府之刃：左键挥砍计数，每满 3 次才发射一道光束</summary>
        public int anarchyBladeShootCount = 0;
        /// <summary>虚影薄锋：本次左键的连段，0/1 交替决定挥砍方向与旋向（右键时临时置 3 走突刺式）</summary>
        public int voidshadeAttackType = 0;
        /// <summary>虚影薄锋：连段闲置计时，超过 120 帧没出手就把连段拨回 0</summary>
        public int voidshadeComboExpireTimer = 0;
        /// <summary>巨龙之怒：左键四段挥砍的段数（0~3，伤害依次 ×1.0/×1.15/×1.25/×1.55）</summary>
        public int dragonRageLevel = 0;
        /// <summary>巨龙之怒：右键两段重击的段数（0/1 → ai 4/5），第三次右键放蓄力重击后归零</summary>
        public int dragonRageLevelAlt = 0;
        /// <summary>星流之刃：累计命中次数，满 5 次触发爆炸分支（彗星分支见下一个字段）</summary>
        public int exoBladeHitCount = 0;
        /// <summary>星流之刃：累计命中次数，满 2 次触发彗星分支</summary>
        public int exoBladeHitCount2 = 0;
        /// <summary>天罚：左键计数，0~6 循环，满 6 后下一次挥砍触发天罚</summary>
        public int nemesisFireIndex = 0;
        /// <summary>元素圣剑：左键彩虹光束的颜色编号，每次发射递增、0~11 循环（同时决定挥砍粉尘颜色）</summary>
        public int elementalExcaliburBeamType = 0;
        /// <summary>禅心剑：本次挥砍是否还没响过命中音（出手置真、命中置假）</summary>
        public bool ataraxiaHitSound = true;
    }
}
