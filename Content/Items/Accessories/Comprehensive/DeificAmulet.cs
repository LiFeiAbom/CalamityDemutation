using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Comprehensive
{
    /// <summary>
    /// 神之护符（Deific Amulet） - 综合型饰品
    /// 融合天体手铐、水母项链、恐慌项链、鲨牙项链与星光面纱的效果：
    /// 提供恐慌（低血加速）、魔力磁铁与魔法手铐，+25 通用护甲穿透，浸水时发出粉紫色光；
    /// 受击后延长无敌帧并降下神圣之星反击。
    /// </summary>
    internal class DeificAmulet:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、稀有度、价值与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 26;                          // 贴图宽（像素）
            Item.height = 26;                         // 贴图高（像素）
            Item.rare = ItemRarityID.Yellow;          // 稀有度：黄色
            Item.value = Item.buyPrice(0, 45, 0, 0);  // 价值 45 金
            Item.accessory = true;                    // 作为饰品装备
        }
        /// <summary>
        /// 装备时置位 deificAmulet 标记；真正的数值（恐慌/魔力磁铁/魔法手铐、+25 通用护甲穿透、
        /// 浸水发光，以及受击无敌帧与神圣之星）在 CalamityDemutationPlayer 的
        /// PostUpdateMiscEffects / PostHurt 中统一结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.deificAmulet = true;
        }
        /// <summary>
        /// 配方：五件原版饰品 + 陨石锭，现代版（CalamityMod）与经典版（CalamityModClassicPreTrailer）
        /// 的灾厄材料不同，故分两分支注册，均在秘银砧合成
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：灾厄材料用 StarblightSoot
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.CelestialCuffs);
                recipe.AddIngredient(ItemID.JellyfishNecklace);
                recipe.AddIngredient(ItemID.PanicNecklace);
                recipe.AddIngredient(ItemID.SharkToothNecklace);
                recipe.AddIngredient(ItemID.StarVeil);
                recipe.AddIngredient(calamity.Find<ModItem>("StarblightSoot").Type, 25);
                recipe.AddIngredient(ItemID.MeteoriteBar, 25);
                recipe.AddTile(TileID.MythrilAnvil);
                recipe.Register();
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄：对应材料为 Stardust
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(ItemID.CelestialCuffs);
                recipe1.AddIngredient(ItemID.JellyfishNecklace);
                recipe1.AddIngredient(ItemID.PanicNecklace);
                recipe1.AddIngredient(ItemID.SharkToothNecklace);
                recipe1.AddIngredient(ItemID.StarVeil);
                recipe1.AddIngredient(calamity.Find<ModItem>("Stardust").Type, 25);
                recipe1.AddIngredient(ItemID.MeteoriteBar, 25);
                recipe1.AddTile(TileID.MythrilAnvil);
                recipe1.Register();
            }
        }
    }
}
