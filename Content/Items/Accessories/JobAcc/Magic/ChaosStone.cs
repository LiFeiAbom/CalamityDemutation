using CalamityDemutation.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Magic
{
    /// <summary>
    /// 混沌石 - 魔法职业饰品
    /// 提供 +50 最大魔力、-5% 魔力消耗与 +3% 通用伤害，并发出红色光芒。
    /// </summary>
    internal class ChaosStone:ModItem
    {
        /// <summary>
        /// 静态默认值：注册逐帧竖直滚动贴图动画（每 6 帧一帧、共 4 帧），
        /// 并标记为"以灵魂形式呈现动画"，使饰品在背包/世界中漂浮闪烁。
        /// </summary>
        public override void SetStaticDefaults()
        {
            // 逐帧竖直滚动贴图，营造元素之魂般的动画效果
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(6, 4));
            ItemID.Sets.AnimatesAsSoul[Type] = true;
        }
        /// <summary>
        /// 物品基础属性：贴图尺寸、价值、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 20;                        // 贴图宽 20 像素
            Item.height = 20;                       // 贴图高 20 像素
            Item.value = Item.buyPrice(0, 15, 0, 0); // 价值 15 金
            Item.rare = ItemRarityID.Pink;          // 稀有度：粉（Pink）
            Item.accessory = true;                  // 标记为饰品，可装备于饰品栏
        }
        /// <summary>
        /// 装备时置位 chaosStone 标记，具体数值在 CalamityDemutationPlayer 中统一结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 仅置位装备标记，实际数值在 CalamityDemutationPlayer.PostUpdateMiscEffects 中统一结算
            player.GetModPlayer<CalamityDemutationPlayer>().chaosStone = true;
        }
    }
}
