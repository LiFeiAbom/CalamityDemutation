using CalamityDemutation.Content.Items.Accessories.Defense;
using CalamityDemutation.Content.Items.Accessories.JobAcc.Melee;
using CalamityDemutation.Content.Items.Accessories.StatLife;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Comprehensive
{
    /// <summary>
    /// 血神核心 - 综合型专家饰品
    /// 提供最大生命、暴击、增伤、减伤与吸血光环，并继承血肉图腾的接触伤害减半效果。
    /// 由血蠕虫围巾、血契、血肉图腾、血耀核心组合而成。
    /// </summary>
    internal class CoreOfTheBloodGod:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、专家物品与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 26;                          // 贴图宽（像素）
            Item.height = 26;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 90, 0, 0);  // 价值 90 金
            Item.expert = true;                       // 标记为专家物品（专家模式专属外观框）
            Item.accessory = true;                    // 标记为饰品，可装备于饰品栏
        }
        /// <summary>
        /// 装备时：置位血神核心标记，并顺带继承血肉图腾的接触伤害减半标记（详见 CalamityDemutationPlayer）
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.coreOfTheBloodGod = true;   // 血神核心本体效果
            modPlayer.fleshTotem = true;          // 继承血肉图腾的接触伤害减半效果
        }
        /// <summary>
        /// 配方（分版本）：四件本模组素材（血蠕虫围巾、血契、血肉图腾、血耀核心）相同，
        /// 现代版附加 CosmiliteBar + Necroplasm 在宇宙砧合成，经典版附加 CosmiliteBar + Phantoplasm 在德拉肯锻造台合成
        /// </summary>
        public override void AddRecipes()
        {
            // 分别适配现代版与经典版灾厄的合成配方
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient<BloodyWormScarf>();
                recipe.AddIngredient<BloodPact>();
                recipe.AddIngredient<FleshTotem>();
                recipe.AddIngredient<BloodflareCore>();
                recipe.AddIngredient(calamity.Find<ModItem>("CosmiliteBar").Type, 5);
                recipe.AddIngredient(calamity.Find<ModItem>("Necroplasm").Type, 5);
                recipe.AddTile(calamity.Find<ModTile>("CosmicAnvil").Type);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient<BloodyWormScarf>();
                recipe1.AddIngredient<BloodPact>();
                recipe1.AddIngredient<FleshTotem>();
                recipe1.AddIngredient<BloodflareCore>();
                recipe1.AddIngredient(calamity1.Find<ModItem>("CosmiliteBar").Type, 5);
                recipe1.AddIngredient(calamity1.Find<ModItem>("Phantoplasm").Type, 5);
                recipe1.AddTile(calamity1.Find<ModTile>("DraedonsForge").Type);
                recipe1.Register();
            }
        }
    }
}
