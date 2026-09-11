using CalamityDemutation.Content.Items.Accessories.Function;
using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Magic
{
    /// <summary>
    /// 灾厄符印 - 魔法职业饰品
    /// 提供 +15% 魔法伤害、+10% 魔法暴击、+100 最大魔力、-15% 魔力消耗，
    /// 附带探宝（findTreasure）与减少药水疾病时间（pStone）效果。
    /// </summary>
    internal class SigilofCalamitas:ModItem
    {
        /// <summary>
        /// 静态默认值：注册逐帧竖直滚动贴图动画（每 6 帧一帧、共 8 帧），
        /// 并标记为"以灵魂形式呈现动画"，使饰品在背包/世界中如元素之魂般漂浮闪烁。
        /// </summary>
        public override void SetStaticDefaults()
        {
            // 逐帧竖直滚动贴图，营造元素之魂般的动画效果
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(6, 8));
            ItemID.Sets.AnimatesAsSoul[Type] = true;
        }
        /// <summary>
        /// 物品基础属性：贴图尺寸、价值、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 28;                        // 贴图宽 28 像素
            Item.height = 32;                       // 贴图高 32 像素
            Item.value = Item.buyPrice(0, 30, 0, 0); // 价值 30 金
            Item.rare = ItemRarityID.Cyan;          // 稀有度：青（Cyan）
            Item.accessory = true;                  // 标记为饰品，可装备于饰品栏
        }
        /// <summary>
        /// 装备时置位 sigilofCalamitas 标记，具体数值在 CalamityDemutationPlayer 中统一结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 仅置位装备标记，实际数值在 CalamityDemutationPlayer.PostUpdateMiscEffects 中统一结算
            player.GetModPlayer<CalamityDemutationPlayer>().sigilofCalamitas = true;
        }
        /// <summary>
        /// 配方：兼容灾厄现代版与经典版，两版灾厄材料名不同，需分别注册配方；
        /// 每版内再给出腐化世界（邪水 UnholyWater）与猩红世界（血水 BloodWater）两种选择。
        /// </summary>
        public override void AddRecipes()
        {
            // 腐化与猩红两种世界分别提供不同合成配方（邪水/血水）
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.CharmofMyths);
                recipe.AddIngredient(ItemID.SorcererEmblem);
                recipe.AddIngredient(ItemID.CrystalShard, 20);
                recipe.AddIngredient(calamity.Find<ModItem>("AshesofCalamity").Type, 5);
                recipe.AddIngredient<CoreofChaos>(5);
                recipe.AddIngredient(ItemID.SpellTome);
                recipe.AddIngredient<ChaosAmulet>();
                recipe.AddIngredient(ItemID.UnholyWater, 10);
                recipe.AddTile(TileID.MythrilAnvil);
                recipe.Register();
                recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.CharmofMyths);
                recipe.AddIngredient(ItemID.SorcererEmblem);
                recipe.AddIngredient(ItemID.CrystalShard, 20);
                recipe.AddIngredient(calamity.Find<ModItem>("AshesofCalamity").Type, 5);
                recipe.AddIngredient<CoreofChaos>(5);
                recipe.AddIngredient(ItemID.SpellTome);
                recipe.AddIngredient<ChaosAmulet>();
                recipe.AddIngredient(ItemID.BloodWater, 10);
                recipe.AddTile(TileID.MythrilAnvil);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(ItemID.CharmofMyths);
                recipe1.AddIngredient(ItemID.SorcererEmblem);
                recipe1.AddIngredient(ItemID.CrystalShard, 20);
                recipe1.AddIngredient(calamity1.Find<ModItem>("CalamityDust").Type, 5);
                recipe1.AddIngredient(calamity1.Find<ModItem>("CoreofChaos").Type, 5);
                recipe1.AddIngredient(ItemID.SpellTome);
                recipe1.AddIngredient(calamity1.Find<ModItem>("ChaosAmulet").Type);
                recipe1.AddIngredient(ItemID.UnholyWater, 10);
                recipe1.AddTile(TileID.MythrilAnvil);
                recipe1.Register();
                recipe1 = CreateRecipe();
                recipe1.AddIngredient(ItemID.CharmofMyths);
                recipe1.AddIngredient(ItemID.SorcererEmblem);
                recipe1.AddIngredient(ItemID.CrystalShard, 20);
                recipe1.AddIngredient(calamity1.Find<ModItem>("CalamityDust").Type, 5);
                recipe1.AddIngredient(calamity1.Find<ModItem>("CoreofChaos").Type, 5);
                recipe1.AddIngredient(ItemID.SpellTome);
                recipe1.AddIngredient(calamity1.Find<ModItem>("ChaosAmulet").Type);
                recipe1.AddIngredient(ItemID.BloodWater, 10);
                recipe1.AddTile(TileID.MythrilAnvil);
                recipe1.Register();
            }
        }
    }
}
