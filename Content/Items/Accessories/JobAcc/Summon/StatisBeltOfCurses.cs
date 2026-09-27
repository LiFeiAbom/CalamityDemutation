using CalamityDemutation.Content.Items.Accessories.Movement;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Summon
{
    /// <summary>
    /// 诅咒凝滞腰带 - 顶级召唤职业饰品
    /// +20% 召唤伤害、+4 仆从上限、+20% 鞭范围与攻速，
    /// 附带自动跳跃、二段跳加速、额外坠落速度、闪避、冲刺与尖刺靴等机动性效果。
    /// 凝滞系列最终形态：凝滞祝福 → 凝滞诅咒 → 本件，由凝滞诅咒进阶而来。
    /// </summary>
    internal class StatisBeltOfCurses:ModItem
    {
        /// <summary>
        /// 物品基础属性：贴图尺寸、价值、饰品标记，并指定模组自定义的月后稀有度等级
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 28;                         // 贴图宽 28 像素
            Item.height = 32;                        // 贴图高 32 像素
            Item.value = Item.buyPrice(0, 90, 0, 0); // 价值 90 金
            Item.accessory = true;                   // 标记为饰品，可装备于饰品栏
            // 月后物品：稀有度颜色交由全局物品统一渲染
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;
        }
        /// <summary>
        /// 装备时置位 statisBeltOfCurses 标记，数值结算、debuff 施加与机动性效果在 CalamityDemutationPlayer 中统一完成
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 装备时仅置位标记，数值结算统一在 CalamityDemutationPlayer 中完成
            player.GetModPlayer<CalamityDemutationPlayer>().statisBeltOfCurses = true;
        }
        /// <summary>
        /// 配方：以凝滞诅咒为核心材料，兼容灾厄现代版与经典版（两版材料名/合成站不同），分别注册配方
        /// </summary>
        public override void AddRecipes()
        {
            // 兼容灾厄现代版与经典版：两者材料名/合成站不同，需分别注册配方
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("Necroplasm", out ModItem necroplasm1) && calamity.TryFind<ModItem>("NightmareFuel", out ModItem nightmareFuel1) && calamity.TryFind<ModItem>("EndothermicEnergy", out ModItem endothermicEnergy1) && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<StatisCurse>();
                    recipe.AddIngredient<StatisNinjaBelt>();
                    recipe.AddIngredient(necroplasm1.Type, 20);
                    recipe.AddIngredient(nightmareFuel1.Type, 20);
                    recipe.AddIngredient(endothermicEnergy1.Type, 20);
                    recipe.AddTile(cosmicAnvil1.Type);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModItem>("Phantoplasm", out ModItem phantoplasm1) && calamity1.TryFind<ModItem>("NightmareFuel", out ModItem nightmareFuel2) && calamity1.TryFind<ModItem>("EndothermicEnergy", out ModItem endothermicEnergy2) && calamity1.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge1))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient<StatisCurse>();
                    recipe1.AddIngredient<StatisNinjaBelt>();
                    recipe1.AddIngredient(phantoplasm1.Type, 20);
                    recipe1.AddIngredient(nightmareFuel2.Type, 20);
                    recipe1.AddIngredient(endothermicEnergy2.Type, 20);
                    recipe1.AddTile(draedonsForge1.Type);
                    recipe1.Register();
                }
            }
        }
    }
}
