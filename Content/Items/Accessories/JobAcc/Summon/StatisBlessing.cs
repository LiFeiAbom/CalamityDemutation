using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Summon
{
    /// <summary>
    /// 凝滞祝福 - 召唤职业饰品
    /// +2.5 召唤击退、+10% 召唤伤害、+3 仆从上限，
    /// 召唤弹幕命中时施加"时间之殇"（TemporalSadness）debuff。
    /// 凝滞系列基础件，可用凝滞诅咒进阶替代（后者额外获得暗影焰与鞭加成）。
    /// </summary>
    internal class StatisBlessing:ModItem
    {
        /// <summary>
        /// 物品基础属性：贴图尺寸、售价、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 28;                         // 贴图宽 28 像素
            Item.height = 32;                        // 贴图高 32 像素
            Item.value = Item.buyPrice(0, 45, 0, 0); // 售价 45 金
            Item.rare = ItemRarityID.Cyan;           // 稀有度：青（Cyan）
            Item.accessory = true;                   // 标记为饰品，可装备于饰品栏
        }
        /// <summary>
        /// 装备时置位 statisBlessing 标记，数值结算与"时间之殇" debuff 施加在 CalamityDemutationPlayer 中统一完成
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 装备时仅置位标记，数值结算与 debuff 施加统一在 CalamityDemutationPlayer 中完成
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.statisBlessing = true;
        }
        /// <summary>
        /// 配方：兼容灾厄现代版与经典版，两版灾厄材料名不同，需分别注册配方
        /// </summary>
        public override void AddRecipes()
        {
            // 兼容灾厄现代版与经典版：两者材料名不同，需分别注册配方
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.PapyrusScarab);
                recipe.AddIngredient(ItemID.PygmyNecklace);
                recipe.AddIngredient(ItemID.SummonerEmblem);
                recipe.AddIngredient(ItemID.BottledWater);
                recipe.AddIngredient<CoreofCinder>(5);
                recipe.AddIngredient(ItemID.HolyWater, 30);
                recipe.AddTile(TileID.MythrilAnvil);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(ItemID.PapyrusScarab);
                recipe1.AddIngredient(ItemID.PygmyNecklace);
                recipe1.AddIngredient(ItemID.SummonerEmblem);
                recipe1.AddIngredient(ItemID.BottledWater);
                recipe1.AddIngredient(calamity1.Find<ModItem>("CoreofCinder").Type, 5);
                recipe1.AddIngredient(ItemID.HolyWater, 30);
                recipe1.AddTile(TileID.MythrilAnvil);
                recipe1.Register();
            }
        }
    }
}
