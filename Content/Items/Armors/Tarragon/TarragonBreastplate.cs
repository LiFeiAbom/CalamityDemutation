using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Tarragon
{
    /// <summary>
    /// 龙蒿胸甲（TarragonBreastplate） - 龙蒿套装的胸部部件
    /// 生存与全能向：物品自带 +2 生命回复（1 HP/s）；穿戴后 +40 生命上限，
    /// 全职业通用伤害 +10%、通用暴击率 +10%。
    /// 数值膨胀开关开启时恢复 2026-09-27 削弱前的旧值：生命上限 +150、魔力上限 +100、生命回复 +6。
    /// </summary>
    [AutoloadEquip(EquipType.Body)]
    internal class TarragonBreastplate:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、自带生命回复、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.lifeRegen = 2;                       // 物品自带生命回复 +2
            Item.value = Item.buyPrice(0, 40, 0, 0);  // 价值 40 金
            Item.defense = 37;                        // 防御力
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 12;  // 月后自定义稀有度 12 级（名称颜色覆盖见 CalamityDemutationGlobalItem）
        }
        /// <summary>
        /// 穿戴时的属性加成：生命上限、生命再生与通用伤害/暴击；
        /// 数值膨胀开关开启时改用 2026-09-27 削弱前的旧值（生命/魔力上限与生命回复三条一起恢复）。
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            bool legacy = ConfigSystem.StatInflationEnabled;
            player.statLifeMax2 += legacy ? LegacyStatLifeMax2 : 40;   // 生命上限：常态 +40 / 膨胀 +150（削弱前旧值）
            if (legacy)
            {
                player.statManaMax2 += LegacyStatManaMax2;             // 魔力上限 +100（削弱时整条删除，膨胀时恢复）
            }
            player.lifeRegen += legacy ? LegacyLifeRegen : 1;          // 生命回复：常态 +1 HP/s / 膨胀 +6（削弱前旧值）
            player.GetDamage<GenericDamageClass>() += 0.1f;   // 通用伤害 +10%（全职业增伤）
            player.GetCritChance<GenericDamageClass>() += 10; // 通用暴击率 +10%
        }
        /// <summary>数值膨胀开关开启时恢复的旧版生命上限（2026-09-27「150 → 40」那次削弱的原值）</summary>
        private const int LegacyStatLifeMax2 = 150;
        /// <summary>数值膨胀开关开启时恢复的旧版魔力上限（2026-09-27 削弱时整条删除的原值）</summary>
        private const int LegacyStatManaMax2 = 100;
        /// <summary>数值膨胀开关开启时恢复的旧版生命回复（2026-09-27 削弱时整条删除的原值 6；常态改成 1 HP/s）</summary>
        private const int LegacyLifeRegen = 6;
        /// <summary>
        /// 说明文字：数值膨胀开启时换成 TooltipInflated（最大生命 +150、最大魔力 +100、生命再生 +6），
        /// 关闭时用 Tooltip 的常态值（最大生命 +40、生命再生 +1）。
        /// 本件防御不随膨胀变化，故不传 defenseBonus。
        /// </summary>
        public override void ModifyTooltips(List<TooltipLine> tooltips) => tooltips.ApplyInflatedTooltip(this);
        /// <summary>
        /// 配方：现代版与经典版灾厄材料不同，分别注册两套配方
        /// （均使用原版合成站 TileID.LunarCraftingStation）
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄配方（UelibloomBar + DivineGeode）
                if (calamity.TryFind<ModItem>("UelibloomBar", out ModItem uelibloomBar)
                    && calamity.TryFind<ModItem>("DivineGeode", out ModItem divineGeode))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(uelibloomBar.Type, 24);
                    recipe.AddIngredient(divineGeode.Type, 18);
                    recipe.AddTile(TileID.LunarCraftingStation);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                // 经典版灾厄配方（UeliaceBar + DivineGeode）
                if (classic.TryFind<ModItem>("UeliaceBar", out ModItem ueliaceBar)
                    && classic.TryFind<ModItem>("DivineGeode", out ModItem classicDivineGeode))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient(ueliaceBar.Type, 15);
                    recipeClassic.AddIngredient(classicDivineGeode.Type, 18);
                    recipeClassic.AddTile(TileID.LunarCraftingStation);
                    recipeClassic.Register();
                }
            }
        }
    }
}
