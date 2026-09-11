using CalamityDemutation.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Comprehensive
{
    /// <summary>
    /// 社群（The Community） - 综合型终极饰品
    /// 属性随 Boss 进度线性成长：从击败石巨人（初始档）到清完 18 档 Boss（满配），
    /// 覆盖通用增伤/暴击、近战攻速、减伤、移速、生命与魔力上限及回复、防御、跳跃、挖掘、
    /// 魔力消耗减免、荆棘与幸运；并持续缩减药水病、魔力病与一般 Debuff 的时长，
    /// 月后额外增加召唤栏与飞行时间。
    /// </summary>
    internal class TheCommunity:ModItem
    {
        /// <summary>
        /// 注册物品动画：贴图竖直滚动，并以"灵魂"方式渲染
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(5, 10));  // 每 5 帧切换一帧，共 10 帧
            ItemID.Sets.AnimatesAsSoul[Type] = true;                                  // 以灵魂样式漂浮渲染
        }
        /// <summary>
        /// 物品基础属性：尺寸、价值、饰品标记与月后稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 20;                          // 贴图宽（像素）
            Item.height = 20;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 60, 0, 0);  // 价值 60 金
            Item.accessory = true;                    // 作为饰品装备
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 20;  // 月后稀有度 20：名称彩虹闪烁
        }
        /// <summary>
        /// 装备时置位 theCommunity 标记；Boss 进度成长、Debuff 缩减与月后一次性加成
        /// 全部在 CalamityDemutationPlayer.PostUpdateMiscEffects 中统一结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.theCommunity= true;
        }
    }
}
