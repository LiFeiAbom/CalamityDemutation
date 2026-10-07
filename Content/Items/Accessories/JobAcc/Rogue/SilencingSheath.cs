using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Rogue
{
    /// <summary>
    /// 静默剑鞘（Silencing Sheath）—— "暗物质剑鞘"那条链的最下端（静默剑鞘 → 毁灭徽章 → 暗物质剑鞘 → 日蚀魔镜），
    /// 前期工匠作坊档。口径按用户 2026-10-07 拍板取灾厄 **2.0** 版：32×34、橙档、4 金；
    /// 潜行上限 **+20 点**、站定与移动潜行恢复**各 +15%**
    /// （2.0.3.9 起被本体削到 +10 点 / 各 +4%，档位还降到绿档 2 金——本件刻意保留旧数值）。
    /// 另按用户点名追加基础属性：**盗贼伤害 +2%、盗贼暴击 +2**。
    /// <para>
    /// 效果分两处落地：潜行相关的两条（恢复速度 + 上限）写在 <c>CalamityDemutationPlayer.PostUpdateEquips</c>
    /// （灾厄在 ResetEffects 里复位这些字段、PostUpdateMiscEffects 之后才读取），伤害/暴击写在
    /// <c>PostUpdateMiscEffects</c>。
    /// </para>
    /// <para>
    /// 配方照 2.0（四版完全一致，且**全是原版材料**）：任意魔金锭×8 + 丝绸×10 + 任意二阶 Boss 材料×3 @ 工匠作坊。
    /// 源配方用的是灾厄自建的 <c>AnyEvilBar</c> / <c>Boss2Material</c> 两个配方组（内容 = 魔金锭/血金锭、
    /// 暗影鳞片/组织样本，全是原版物品），本件刻意不依赖灾厄，改用本模组自建的同内容组
    /// <c>CalamityDemutation:AnyEvilBar</c> / <c>CalamityDemutation:Boss2Material</c>（见 RecipeSystem）。
    /// </para>
    /// <para>
    /// 与灾厄本体重名：本体 2.2.2 仍有同名件（削弱后的 +10/+4% 版），本件是"旧版回归"的同名不同物；
    /// 按用户既有的口径，**不做互斥**。
    /// </para>
    /// </summary>
    internal class SilencingSheath : ModItem
    {
        /// <summary>
        /// 基础属性：32×34、价值 4 金（= 灾厄 Rarity3/Orange 档价）、稀有度橙、饰品
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 34;
            Item.value = Item.buyPrice(0, 4, 0, 0);
            Item.rare = ItemRarityID.Orange;
            Item.accessory = true;
        }
        /// <summary>
        /// 装备时置位 <c>silencingSheath</c> 标记；数值在 CalamityDemutationPlayer 里按标记触发
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().silencingSheath = true;
        }
        /// <summary>
        /// 配方：照 2.0 —— 任意魔金锭×8 + 丝绸×10 + 任意二阶 Boss 材料×3 @ 工匠作坊（全原版材料、不依赖灾厄）
        /// </summary>
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddRecipeGroup("CalamityDemutation:AnyEvilBar", 8);
            recipe.AddIngredient(ItemID.Silk, 10);
            recipe.AddRecipeGroup("CalamityDemutation:Boss2Material", 3);
            recipe.AddTile(TileID.TinkerersWorkbench);
            recipe.Register();
        }
    }
}
