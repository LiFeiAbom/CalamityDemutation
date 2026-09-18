using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Demonshade
{
    /// <summary>
    /// 恶魔之影胸甲（DemonshadeBreastplate） - 恶魔之影套（Demonshade）胸部防具
    /// 提供巨额生命/魔力上限、通用伤害、暴击与近战攻速，并附带高额荆棘反伤；
    /// 装备时置位 shadeRegen，最终在 CalamityDemutationPlayer.UpdateLifeRegen 中结算生命回复。
    /// </summary>
    [AutoloadEquip(EquipType.Body)]
    internal class DemonshadeBreastplate:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;          // 贴图宽（像素）
            Item.height = 18;         // 贴图高（像素）
            Item.value = Item.buyPrice(4, 0, 0, 0);  // 价值 4 铂金
            Item.defense = 62;        // 防御 62
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 16;  // 月后稀有度 16 级，名称颜色为品红
        }
        /// <summary>
        /// 单件装备加成：置位 shadeRegen 标记并提供伤害/暴击/攻速/上限与荆棘
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.shadeRegen = true;                            // 置位生命回复标记，最终在 CalamityDemutationPlayer.UpdateLifeRegen 中结算
            player.GetDamage<GenericDamageClass>() += 0.4f;         // 全类型伤害 +40%
            player.GetCritChance<GenericDamageClass>() += 20;       // 全类型暴击率 +20%
            player.GetAttackSpeed<MeleeDamageClass>() += 0.25f;     // 近战攻速 +25%
            player.statLifeMax2 += 1000;                            // 最大生命 +1000
            player.statManaMax2 += 1000;                            // 最大魔力 +1000
            player.thorns = 200f;//100=>200
        }
        /// <summary>
        /// 注册配方：现代版灾厄（CalamityMod）与经典预发布版灾厄（CalamityModClassicPreTrailer）
        /// 材料相同（ShadowspecBar×50），但对应各自的暗影合金锭与德雷顿熔炉，故分别注册。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("ShadowspecBar", out ModItem shadowspecBar) && calamity.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(shadowspecBar.Type, 50);  // 现代版灾厄：ShadowspecBar×50
                    recipe.AddTile(draedonsForge.Type);            // 现代版灾厄：德雷顿熔炉
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModItem>("ShadowspecBar", out ModItem classicShadowspecBar) && calamity1.TryFind<ModTile>("DraedonsForge", out ModTile classicDraedonsForge))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(classicShadowspecBar.Type, 50);  // 经典版灾厄：ShadowspecBar×50
                    recipe1.AddTile(classicDraedonsForge.Type);            // 经典版灾厄：德雷顿熔炉
                    recipe1.Register();
                }
            }
        }
    }
}
