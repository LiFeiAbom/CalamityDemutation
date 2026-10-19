using CalamityDemutation.Content.Buffs.Alcohol;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Potions.Alcohol
{
    /// <summary>
    /// 摩闪酒（Moonshine）—— 酒类药水第六件。
    /// <para>
    /// 面板与效果取 2.0.4：28×18、堆叠 9999、黄档、8 分钟、价值 1 金 30 银、研究解锁 5；
    /// 防御 +10、伤害减免 +3%、生命再生 −0.5 HP/s、酒醉 +1。
    /// </para>
    /// <para>
    /// 配方取 2.0.7.2：麦芽酒 + 熔渣线材料×3 @ 酒桶，或
    /// 瓶装水 + 血珠×5 + 熔渣线材料×1 @ 炼药桌，各产 1。
    /// 现代用 ScoriaOre；经典用 CruptixBar；都缺时回退 ChaoticOre。
    /// </para>
    /// </summary>
    internal class Moonshine : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 5;
        }
        public override void SetDefaults()
        {
            Item.DefaultToFood(28, 18, ModContent.BuffType<MoonshineBuff>(), 8 * 60 * 60, true);
            Item.useTurn = true;
            Item.maxStack = 9999;
            Item.rare = ItemRarityID.Yellow;
            Item.value = Item.buyPrice(0, 1, 30, 0);
        }
        public override void AddRecipes()
        {
            foreach (string modName in new[] { "CalamityMod", "CalamityModClassicPreTrailer" })
            {
                if (!ModLoader.TryGetMod(modName, out Mod calamity))
                    continue;
                ModItem scoriaMaterial = null;
                if (calamity.TryFind<ModItem>("ScoriaOre", out ModItem scoriaOre))
                    scoriaMaterial = scoriaOre;
                else if (calamity.TryFind<ModItem>("CruptixBar", out ModItem cruptixBar))
                    scoriaMaterial = cruptixBar;
                else if (calamity.TryFind<ModItem>("ChaoticOre", out ModItem chaoticOre))
                    scoriaMaterial = chaoticOre;
                if (scoriaMaterial == null)
                    continue;
                CreateRecipe()
                    .AddIngredient(ItemID.Ale)
                    .AddIngredient(scoriaMaterial.Type, 3)
                    .AddTile(TileID.Kegs)
                    .Register();
                if (calamity.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb))
                {
                    CreateRecipe()
                        .AddIngredient(ItemID.BottledWater)
                        .AddIngredient(bloodOrb.Type, 5)
                        .AddIngredient(scoriaMaterial.Type)
                        .AddTile(TileID.AlchemyTable)
                        .Register()
                        .DisableDecraft();
                }
            }
        }
    }
}
