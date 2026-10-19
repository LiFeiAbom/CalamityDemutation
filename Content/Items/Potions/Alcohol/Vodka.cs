using CalamityDemutation.Content.Buffs.Alcohol;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Potions.Alcohol
{
    /// <summary>
    /// 伏特加（Vodka）—— 酒类药水第七件。
    /// <para>
    /// 面板与效果取 2.0.4：28×18、堆叠 9999、浅紫档、8 分钟、价值 1 金 30 银、研究解锁 5；
    /// 伤害 +6%、暴击 +2%、防御 −5（固定值）、生命再生 −0.5 HP/s、酒醉 +1。
    /// </para>
    /// <para>
    /// 配方取 2.0.7.2：麦芽酒 + HallowedOre×3 @ 酒桶，或
    /// 瓶装水 + 血珠×5 + HallowedOre×1 @ 炼药桌，各产 1。
    /// 经典版没有 HallowedOre，用原版 HallowedBar×1 等价兜底。
    /// </para>
    /// </summary>
    internal class Vodka : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 5;
        }
        public override void SetDefaults()
        {
            Item.DefaultToFood(28, 18, ModContent.BuffType<VodkaBuff>(), 8 * 60 * 60, true);
            Item.useTurn = true;
            Item.maxStack = 9999;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = Item.buyPrice(0, 1, 30, 0);
        }
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity)
                && calamity.TryFind<ModItem>("HallowedOre", out ModItem hallowedOre)
                && calamity.TryFind<ModItem>("BloodOrb", out ModItem modernBloodOrb))
            {
                CreateRecipe()
                    .AddIngredient(ItemID.Ale)
                    .AddIngredient(hallowedOre.Type, 3)
                    .AddTile(TileID.Kegs)
                    .Register();
                CreateRecipe()
                    .AddIngredient(ItemID.BottledWater)
                    .AddIngredient(modernBloodOrb.Type, 5)
                    .AddIngredient(hallowedOre.Type)
                    .AddTile(TileID.AlchemyTable)
                    .Register()
                    .DisableDecraft();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic)
                && classic.TryFind<ModItem>("BloodOrb", out ModItem classicBloodOrb))
            {
                CreateRecipe()
                    .AddIngredient(ItemID.Ale)
                    .AddIngredient(ItemID.HallowedBar)
                    .AddTile(TileID.Kegs)
                    .Register();
                CreateRecipe()
                    .AddIngredient(ItemID.BottledWater)
                    .AddIngredient(classicBloodOrb.Type, 5)
                    .AddIngredient(ItemID.HallowedBar)
                    .AddTile(TileID.AlchemyTable)
                    .Register()
                    .DisableDecraft();
            }
        }
    }
}
