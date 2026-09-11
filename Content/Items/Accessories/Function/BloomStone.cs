using CalamityDemutation.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Function
{
    /// <summary>
    /// 花绽石 - 功能饰品
    /// 提供 +2% 通用伤害和暴击率并发出绿光，周期性对周围敌人施加持续伤害 debuff，
    /// 并在玩家脚下随机生长植物。
    /// </summary>
    internal class BloomStone:ModItem
    {
        /// <summary>
        /// 注册贴图动画：DrawAnimationVertical(4, 7) 表示每 4 帧切换一帧、共 7 帧的竖直滚动动画；
        /// AnimatesAsSoul 让物品在世界上像元素之魂一样带飘动光效
        /// </summary>
        public override void SetStaticDefaults()
        {
            // 逐帧竖直滚动贴图，营造元素之魂般的动画效果
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(4, 7));
            ItemID.Sets.AnimatesAsSoul[Type] = true;
        }
        /// <summary>
        /// 物品基础属性：尺寸、价值、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 20;                          // 贴图宽（像素）
            Item.height = 20;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 15, 0, 0);  // 价值 15 金
            Item.rare = ItemRarityID.Pink;            // 稀有度：粉色
            Item.accessory = true;                    // 作为饰品装备
        }
        /// <summary>
        /// 装备时置位 bloomStone 标记；通用伤害/暴击、绿色照明、周期性 debuff 光环
        /// 与脚下植物生长逻辑均在 CalamityDemutationPlayer.PostUpdateMiscEffects 中结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 仅置位装备标记，实际数值与植物生长逻辑在 CalamityDemutationPlayer 中结算
            player.GetModPlayer<CalamityDemutationPlayer>().bloomStone = true;
        }
    }
}
