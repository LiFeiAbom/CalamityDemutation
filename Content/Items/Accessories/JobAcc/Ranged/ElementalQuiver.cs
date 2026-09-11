using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Ranged
{
    /// <summary>
    /// 元素箭袋 - 顶级远程职业饰品
    /// +20% 远程伤害、+20% 远程暴击、+4 生命回复、+1 远程击退、+30% 挖掘速度、+5 防御，
    /// 40% 概率不消耗弹药，且箭矢有小概率分裂为镜像箭矢
    /// （分裂逻辑见 CalamityDemutationGlobalProjectile.AI）。
    /// </summary>
    internal class ElementalQuiver:ModItem
    {
        /// <summary>
        /// 物品基础属性：贴图尺寸、价值、防御、饰品标记，并指定模组自定义的月后稀有度等级
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 28;                         // 贴图宽 28 像素
            Item.height = 32;                        // 贴图高 32 像素
            Item.value = Item.buyPrice(0, 90, 0, 0); // 价值 90 金
            Item.defense = 5;                        // 提供 5 点防御
            Item.accessory = true;                   // 标记为饰品，可装备于饰品栏
            // 月后物品：稀有度颜色交由全局物品统一渲染
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 20;
        }
        /// <summary>
        /// 装备时置位 elementalQuiver 标记，属性数值、不耗弹与箭矢分裂判定均在 CalamityDemutationPlayer / GlobalProjectile 中完成
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 装备时仅置位标记，数值结算统一在 CalamityDemutationPlayer 中完成
            player.GetModPlayer<CalamityDemutationPlayer>().elementalQuiver = true;
        }
        /// <summary>
        /// 配方：兼容灾厄现代版与经典版，两版灾厄材料名/合成站不同，需分别注册配方
        /// </summary>
        public override void AddRecipes()
        {
            // 兼容灾厄现代版与经典版：两者材料名/合成站不同，需分别注册配方
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.MagicQuiver);
                recipe.AddIngredient<DaedalusEmblem>();
                recipe.AddIngredient(calamity.Find<ModItem>("Necroplasm").Type, 20);
                recipe.AddIngredient(calamity.Find<ModItem>("NightmareFuel").Type, 20);
                recipe.AddIngredient(calamity.Find<ModItem>("EndothermicEnergy").Type, 20);
                recipe.AddTile(calamity.Find<ModTile>("CosmicAnvil").Type);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(ItemID.MagicQuiver);
                recipe1.AddIngredient<DaedalusEmblem>();
                recipe1.AddIngredient(calamity1.Find<ModItem>("Phantoplasm").Type, 20);
                recipe1.AddIngredient(calamity1.Find<ModItem>("NightmareFuel").Type, 20);
                recipe1.AddIngredient(calamity1.Find<ModItem>("EndothermicEnergy").Type, 20);
                recipe1.AddTile(calamity1.Find<ModTile>("DraedonsForge").Type);
                recipe1.Register();
            }
        }
    }
}
