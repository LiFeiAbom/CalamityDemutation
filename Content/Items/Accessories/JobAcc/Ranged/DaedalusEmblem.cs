using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Ranged
{
    /// <summary>
    /// 代达罗斯徽章 - 远程职业饰品
    /// +15% 远程伤害、+10% 远程暴击、+2 生命回复、+0.5 远程击退、+15% 挖掘速度、+5 防御，
    /// 并有 20% 概率不消耗弹药（见 CalamityDemutationPlayer.CanConsumeAmmo）。
    /// </summary>
    internal class DaedalusEmblem:ModItem
    {
        /// <summary>
        /// 物品基础属性：贴图尺寸、价值、防御、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 28;                         // 贴图宽 28 像素
            Item.height = 32;                        // 贴图高 32 像素
            Item.value = Item.buyPrice(0, 30, 0, 0); // 价值 30 金
            Item.rare = ItemRarityID.Cyan;           // 稀有度：青（Cyan）
            Item.defense = 5;                        // 提供 5 点防御
            Item.accessory = true;                   // 标记为饰品，可装备于饰品栏
        }
        /// <summary>
        /// 装备时置位 daedalusEmblem 标记，属性数值与不耗弹判定均在 CalamityDemutationPlayer 中完成
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 装备时仅置位标记，数值结算统一在 CalamityDemutationPlayer 中完成
            player.GetModPlayer<CalamityDemutationPlayer>().daedalusEmblem = true;
        }
        /// <summary>
        /// 配方：兼容灾厄现代版与经典版，两版灾厄材料名不同，需分别注册配方
        /// </summary>
        public override void AddRecipes()
        {
            // 兼容灾厄现代版与经典版：两者材料名不同，需分别注册配方
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("CoreofCalamity", out ModItem coreofCalamity1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.CelestialStone);
                    recipe.AddIngredient(coreofCalamity1.Type);
                    recipe.AddIngredient(ItemID.RangerEmblem);
                    recipe.AddTile(TileID.MythrilAnvil);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModItem>("CoreofCalamity", out ModItem coreofCalamity2))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(ItemID.CelestialStone);
                    recipe1.AddIngredient(coreofCalamity2.Type);
                    recipe1.AddIngredient(ItemID.RangerEmblem);
                    recipe1.AddTile(TileID.MythrilAnvil);
                    recipe1.Register();
                }
            }
        }
    }
}
