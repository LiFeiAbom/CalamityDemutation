using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Demonshade
{
    /// <summary>
    /// 恶魔之影护腿（DemonshadeGreaves） - 恶魔之影套（Demonshade）腿部防具
    /// 提供通用伤害与暴击加成；装备时置位 shadowSpeed，最终在
    /// CalamityDemutationPlayer 中结算为奔跑速度与加速度提升。
    /// </summary>
    [AutoloadEquip(EquipType.Legs)]
    internal class DemonshadeGreaves:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;          // 贴图宽（像素）
            Item.height = 18;         // 贴图高（像素）
            Item.value = Item.buyPrice(3, 0, 0, 0);  // 价值 3 铂金
            Item.defense = 57;  // 防御 57（经典版为 50，同行 //15 系经典版开发期遗留注释；本模组上调至 57）
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 16;  // 月后稀有度 16 级，名称颜色为品红
        }
        /// <summary>
        /// 单件装备加成：置位 shadowSpeed 标记并提供通用伤害与暴击
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.shadowSpeed = true;                         // 置位奔跑加速标记，最终结算为 +50% 奔跑速度与加速度
            player.GetDamage<GenericDamageClass>() += 0.3f;      // 全类型伤害 +30%
            player.GetCritChance<GenericDamageClass>() += 15;    // 全类型暴击率 +15%
        }
        /// <summary>
        /// 注册配方：现代版灾厄（CalamityMod）与经典预发布版灾厄（CalamityModClassicPreTrailer）
        /// 材料相同（ShadowspecBar×45），但对应各自的暗影合金锭与德雷顿熔炉，故分别注册。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("ShadowspecBar", out ModItem shadowspecBar) && calamity.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(shadowspecBar.Type, 45);  // 现代版灾厄：ShadowspecBar×45
                    recipe.AddTile(draedonsForge.Type);            // 现代版灾厄：德雷顿熔炉
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModItem>("ShadowspecBar", out ModItem classicShadowspecBar) && calamity1.TryFind<ModTile>("DraedonsForge", out ModTile classicDraedonsForge))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(classicShadowspecBar.Type, 45);  // 经典版灾厄：ShadowspecBar×45
                    recipe1.AddTile(classicDraedonsForge.Type);            // 经典版灾厄：德雷顿熔炉
                    recipe1.Register();
                }
            }
        }
    }
}
