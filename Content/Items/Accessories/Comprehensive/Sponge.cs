using CalamityDemutation.Content.Items.Accessories.Defense;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Comprehensive
{
    /// <summary>
    /// 海绵（Sponge） - 综合型饰品
    /// 提升生命与法力上限，并继承吸收者、甘露安瓿等吸收类饰品效果。
    /// </summary>
    internal class Sponge:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、基础防御与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.defense = 20;                        // 常驻 +20 防御
            Item.width = 20;                          // 贴图宽（像素）
            Item.height = 20;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 90, 0, 0);  // 价值 90 金
            Item.accessory = true;                    // 标记为饰品，可装备于饰品栏
            // 使用模组自定义的月后稀有度等级
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;
        }
        /// <summary>
        /// 装备时置位标记，具体数值在 CalamityDemutationPlayer 中统一结算。
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.beeResist = true;   // 继承甘露安瓿/蜜露的蜜蜂抗性：被蜜蜂类敌人与蜜蜂弹幕命中时伤害减至 75%
            modPlayer.sponge = true;      // 海绵本体：生命/法力上限、减伤、荆棘与浸水增益等，见 PostUpdateMiscEffects
        }
        /// <summary>
        /// 配方（分版本）：吸收者 + 甘露安瓿 + 星辉矿锭 + Necroplasm（经典版改用 Phantoplasm），在宇宙砧/德雷顿熔炉合成
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if(calamity.TryFind<ModItem>("CosmiliteBar", out ModItem cosmiliteBar1) && calamity.TryFind<ModItem>("Necroplasm", out ModItem necroplasm1) && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<TheAbsorber>();
                    recipe.AddIngredient<AmbrosialAmpoule>();
                    recipe.AddIngredient(cosmiliteBar1.Type, 15);
                    recipe.AddIngredient(necroplasm1.Type, 15);
                    recipe.AddTile(cosmicAnvil1.Type);
                    recipe.Register();
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if(calamity1.TryFind<ModItem>("CosmiliteBar", out ModItem cosmiliteBar2) && calamity1.TryFind<ModItem>("Phantoplasm", out ModItem phantoplasm1) && calamity1.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge1))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient<TheAbsorber>();
                    recipe1.AddIngredient<AmbrosialAmpoule>();
                    recipe1.AddIngredient(cosmiliteBar2.Type, 15);
                    recipe1.AddIngredient(phantoplasm1.Type, 15);
                    recipe1.AddTile(draedonsForge1.Type);
                    recipe1.Register();
                }
            }
        }
    }
}
