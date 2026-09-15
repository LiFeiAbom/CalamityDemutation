using CalamityDemutation.Content.Items.Accessories.Comprehensive;
using CalamityDemutation.Content.Items.Accessories.Function;
using CalamityDemutation.Content.Items.Accessories.StatLife;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Defense
{
    /// <summary>
    /// 甘露安瓿 - 防御型饰品
    /// 提供减伤、挖掘速度与生命回复并免疫多种 debuff，同时减半蜜蜂类弹幕伤害。
    /// </summary>
    internal class AmbrosialAmpoule:ModItem
    {
        /// <summary>
        /// 物品基础属性：防御、尺寸、价值、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.defense = 6;                         // 装备时 +6 防御
            Item.width = 20;                          // 贴图宽（像素）
            Item.height = 20;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 45, 0, 0);  // 价值 45 金
            Item.rare = ItemRarityID.Red;             // 稀有度：红色
            Item.accessory = true;                    // 作为饰品装备
        }
        /// <summary>
        /// 装备时置位标记，具体数值在 CalamityDemutationPlayer 中统一结算。
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.beeResist = true;
            modPlayer.ambrosialAmpoule = true;
        }
        /// <summary>
        /// 配方（分版本）：腐化药剂或猩红药剂（按世界邪恶类型） + 古老粉末 + 光辉软泥 + 蜜露
        /// + 星辉尘 + 寒元锭，在秘银砧合成
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if(calamity.TryFind<ModItem>("StarblightSoot", out ModItem starblightSoot1) && calamity.TryFind<ModItem>("CryonicBar", out ModItem cryonicBar1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<CorruptFlask>();
                    recipe.AddIngredient<ArchaicPowder>();
                    recipe.AddIngredient<RadiantOoze>();
                    recipe.AddIngredient<HoneyDew>();
                    recipe.AddIngredient(starblightSoot1.Type, 15);
                    recipe.AddIngredient(cryonicBar1.Type, 5);
                    recipe.AddTile(TileID.MythrilAnvil);
                    recipe.Register();
                }
                if(calamity.TryFind<ModItem>("StarblightSoot", out ModItem starblightSoot2) && calamity.TryFind<ModItem>("CryonicBar", out ModItem cryonicBar2))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<CrimsonFlask>();
                    recipe.AddIngredient<ArchaicPowder>();
                    recipe.AddIngredient<RadiantOoze>();
                    recipe.AddIngredient<HoneyDew>();
                    recipe.AddIngredient(starblightSoot2.Type, 15);
                    recipe.AddIngredient(cryonicBar2.Type, 5);
                    recipe.AddTile(TileID.MythrilAnvil);
                    recipe.Register();
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if(calamity1.TryFind<ModItem>("Stardust", out ModItem stardust1) && calamity1.TryFind<ModItem>("CryoBar", out ModItem cryoBar1))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient<CorruptFlask>();
                    recipe1.AddIngredient<ArchaicPowder>();
                    recipe1.AddIngredient<RadiantOoze>();
                    recipe1.AddIngredient<HoneyDew>();
                    recipe1.AddIngredient(stardust1.Type, 15);
                    recipe1.AddIngredient(cryoBar1.Type, 5);
                    recipe1.AddTile(TileID.MythrilAnvil);
                    recipe1.Register();
                }
                if(calamity1.TryFind<ModItem>("Stardust", out ModItem stardust2) && calamity1.TryFind<ModItem>("CryoBar", out ModItem cryoBar2))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient<CrimsonFlask>();
                    recipe1.AddIngredient<ArchaicPowder>();
                    recipe1.AddIngredient<RadiantOoze>();
                    recipe1.AddIngredient<HoneyDew>();
                    recipe1.AddIngredient(stardust2.Type, 15);
                    recipe1.AddIngredient(cryoBar2.Type, 5);
                    recipe1.AddTile(TileID.MythrilAnvil);
                    recipe1.Register();
                }
            }
        }
    }
}
