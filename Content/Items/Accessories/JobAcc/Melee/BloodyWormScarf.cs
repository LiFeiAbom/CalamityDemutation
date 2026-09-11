using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Melee
{
    /// <summary>
    /// 血蠕虫围巾 - 近战职业专家饰品（颈部栏位）
    /// +10% 近战伤害、+10% 近战攻速、+15% 伤害减免；由血蠕虫牙进阶而来。
    /// </summary>
    [AutoloadEquip(EquipType.Neck)]
    internal class BloodyWormScarf:ModItem
    {
        /// <summary>
        /// 物品基础属性：贴图尺寸、售价、专家专属与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 26;                         // 贴图宽 26 像素
            Item.height = 42;                        // 贴图高 42 像素
            Item.value = Item.buyPrice(0, 15, 0, 0); // 售价 15 金
            Item.expert = true;                      // 专家模式专属物品
            Item.accessory = true;                   // 标记为饰品，可装备于饰品栏
        }
        /// <summary>
        /// 装备时置位 bloodyWormScarf 标记，具体数值在 CalamityDemutationPlayer 中统一结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 装备时仅置位标记，数值结算统一在 CalamityDemutationPlayer 中完成
            player.GetModPlayer<CalamityDemutationPlayer>().bloodyWormScarf = true;
        }
        /// <summary>
        /// 配方：兼容灾厄现代版与经典版，两版灾厄材料构成不同（经典版少一组暗影之魂），需分别注册配方
        /// </summary>
        public override void AddRecipes()
        {
            // 兼容灾厄现代版与经典版：两者材料名不同，需分别注册配方
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient<BloodyWormTooth>();
                recipe.AddIngredient(ItemID.WormScarf);
                recipe.AddIngredient(ItemID.SoulofNight, 3);
                recipe.AddTile(TileID.MythrilAnvil);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient<BloodyWormTooth>();
                recipe1.AddIngredient(ItemID.WormScarf);
                recipe1.AddTile(TileID.MythrilAnvil);
                recipe1.Register();
            }
        }
    }
}
