using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Comprehensive
{
    /// <summary>
    /// 合体大脑 - 专家饰品，由腐化脑 + 混乱大脑（BrainOfConfusion）合成进阶
    /// 提供 +10% 通用伤害、+5% 通用暴击、1/8 概率闪避攻击；
    /// 受击无敌帧期间概率召唤灵气雨反击，受击后使附近敌人混乱并发出紫雾。
    /// </summary>
    internal class AmalgamatedBrain:ModItem
    {
        /// <summary>
        /// 基础属性：中尺寸贴图、专家限定、价值 15 金、作为饰品装备
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 34;
            Item.value = Item.buyPrice(0, 15, 0, 0);
            Item.expert = true;
            Item.accessory = true;
        }
        /// <summary>
        /// 装备时仅置位标记，增伤/暴击/闪避/反击/混乱光环统一在 CalamityDemutationPlayer 中结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().amalgamatedBrain = true;
        }
        /// <summary>
        /// 互斥：大杂烩已包含合体大脑效果，禁止反向与大杂烩同装（与大杂烩侧互为双向）
        /// </summary>
        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player) => incomingItem.type != ModContent.ItemType<TheAmalgam>();
        /// <summary>
        /// 配方：腐化脑 + 混乱大脑在秘银砧合成（兼容现代版与经典版灾厄，材料判定不区分灾厄版本）
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity) || ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient<RottenBrain>();
                recipe.AddIngredient(ItemID.BrainOfConfusion);
                recipe.AddTile(TileID.MythrilAnvil);
                recipe.Register();
            }
        }
    }
}
