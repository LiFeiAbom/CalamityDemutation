using CalamityDemutation.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Comprehensive
{
    /// <summary>
    /// 冰之石 - 综合饰品
    /// 提供 +5% 减伤、+3% 通用伤害与 +6 基础防御，并发出蓝色光芒。
    /// </summary>
    internal class CryoStone:ModItem
    {
        /// <summary>
        /// 注册贴图动画：竖直逐帧滚动，并让其像元素之魂一样被绘制
        /// </summary>
        public override void SetStaticDefaults()
        {
            // 逐帧竖直滚动贴图，营造元素之魂般的动画效果
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(6, 5));
            ItemID.Sets.AnimatesAsSoul[Type] = true;
        }
        /// <summary>
        /// 物品基础属性：尺寸、价值、稀有度、防御与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 20;                          // 贴图宽（像素）
            Item.height = 20;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 15, 0, 0);  // 价值 15 金
            Item.rare = ItemRarityID.Pink;            // 稀有度：粉（Pink）
            Item.defense = 6;                         // 装备时 +6 防御
            Item.accessory = true;                    // 标记为饰品，可装备于饰品栏
        }
        /// <summary>
        /// 装备时置位标记，具体数值在 CalamityDemutationPlayer 中统一结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 仅置位装备标记，实际数值在 CalamityDemutationPlayer.PostUpdateMiscEffects 中统一结算
            player.GetModPlayer<CalamityDemutationPlayer>().cryoStone = true;
        }
    }
}
