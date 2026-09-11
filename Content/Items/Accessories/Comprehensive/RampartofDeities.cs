using CalamityDemutation.Content.Items.Accessories.Defense;
using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Comprehensive
{
    /// <summary>
    /// 众神的壁垒（Rampart of Deities） - 综合型饰品
    /// 由寒霜壁垒与神之护符合成，兼具二者的生存与回击能力：
    /// +18 防御、免疫击退，生命 >25% 时提供圣骑士护盾（可为附近队友加盾），
    /// ≤50% 生命获得冰屏障，≤15% 生命额外 +5% 减伤；
    /// 另附带恐慌/魔力磁铁/魔法手铐、+50 护甲穿透与受击降星反击。
    /// </summary>
    [AutoloadEquip(EquipType.Shield)]  // 装备时以盾牌外观渲染
    internal class RampartofDeities:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、售价、防御、饰品标记与月后稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 38;                          // 贴图宽（像素）
            Item.height = 44;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 90, 0, 0);  // 售价 90 金
            Item.defense = 18;                        // 装备时 +18 防御
            Item.accessory = true;                    // 作为饰品装备
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;  // 月后稀有度 14：名称染蓝
        }
        /// <summary>
        /// 装备时置位 rampartofDeities 标记；真正的数值（防御/击退免疫/圣骑士护盾/冰屏障/减伤、
        /// +50 护甲穿透与浸水发光，以及受击无敌帧与落星）在 CalamityDemutationPlayer 的
        /// PostUpdateMiscEffects / PostHurt 中统一结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.rampartofDeities = true;
        }
        /// <summary>
        /// 配方：寒霜壁垒 + 神之护符，现代版（CalamityMod）与经典版（CalamityModClassicPreTrailer）
        /// 的灾厄材料与合成站均不同，故分两分支注册
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：材料为 AuricBar 与 AscendantSpiritEssence，在 CosmicAnvil 合成
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient<FrigidBulwark>();
                recipe.AddIngredient<DeificAmulet>();
                recipe.AddIngredient(calamity.Find<ModItem>("AuricBar").Type, 5);
                recipe.AddIngredient(calamity.Find<ModItem>("AscendantSpiritEssence").Type, 4);
                recipe.AddTile(calamity.Find<ModTile>("CosmicAnvil").Type);
                recipe.Register();
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄：材料为 GalacticaSingularity、DivineGeode 与 CosmiliteBar，在 DraedonsForge 合成
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient<FrigidBulwark>();
                recipe1.AddIngredient<DeificAmulet>();
                recipe1.AddIngredient(calamity1.Find<ModItem>("GalacticaSingularity").Type, 5);
                recipe1.AddIngredient(calamity1.Find<ModItem>("DivineGeode").Type, 10);
                recipe1.AddIngredient(calamity1.Find<ModItem>("CosmiliteBar").Type, 20);
                recipe1.AddTile(calamity1.Find<ModTile>("DraedonsForge").Type);
                recipe1.Register();
            }
        }
    }
}
