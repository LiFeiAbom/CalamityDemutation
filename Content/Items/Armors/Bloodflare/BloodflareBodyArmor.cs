using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Bloodflare
{
    /// <summary>
    /// 炎血胸甲（BloodflareBodyArmor） - 炎血套（Bloodflare）胸部防具
    /// 提供生命/魔力上限与通用伤害、暴击加成；浸在岩浆中时额外获得防御与生命回复。
    /// </summary>
    [AutoloadEquip(EquipType.Body)]
    internal class BloodflareBodyArmor:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;          // 贴图宽（像素）
            Item.height = 18;         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 48, 0, 0);  // 价值 48 金
            Item.defense = 35;        // 防御 35
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 13;  // 月后稀有度 13 级，名称颜色为荧光绿
        }
        /// <summary>
        /// 单件装备加成：生命/魔力上限、通用伤害与暴击，浸岩浆时额外防御与回复
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.statLifeMax2 += 100;                              // 最大生命 +100
            player.statManaMax2 += 100;                              // 最大魔力 +100
            player.GetDamage<GenericDamageClass>() += 0.14f;         // 全类型伤害 +14%
            player.GetCritChance<GenericDamageClass>() += 14;        // 全类型暴击率 +14%
            if (player.lavaWet)
            {
                player.statDefense += 30;                            // 处于岩浆中：防御 +30
                player.lifeRegen += 10;                              // 处于岩浆中：生命回复 +10
            }
        }
        /// <summary>
        /// 注册配方：现代版灾厄（CalamityMod）与经典预发布版灾厄（CalamityModClassicPreTrailer）
        /// 材料不同，故分别注册两套配方，均在远古操纵机（LunarCraftingStation）处合成。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄材料：Bloodstone×50、BloodOrb×20、RuinousSoul×4
                if (calamity.TryFind<ModItem>("Bloodstone", out ModItem bloodstone)
                    && calamity.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb)
                    && calamity.TryFind<ModItem>("RuinousSoul", out ModItem ruinousSoul))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(bloodstone.Type, 50);
                    recipe.AddIngredient(bloodOrb.Type, 20);
                    recipe.AddIngredient(ruinousSoul.Type, 4);
                    recipe.AddTile(TileID.LunarCraftingStation);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄材料：BloodstoneCore×13、RuinousSoul×3
                if (calamity1.TryFind<ModItem>("BloodstoneCore", out ModItem bloodstoneCore)
                    && calamity1.TryFind<ModItem>("RuinousSoul", out ModItem classicRuinousSoul))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(bloodstoneCore.Type, 13);
                    recipe1.AddIngredient(classicRuinousSoul.Type, 3);
                    recipe1.AddTile(TileID.LunarCraftingStation);
                    recipe1.Register();
                }
            }
        }
    }
}
