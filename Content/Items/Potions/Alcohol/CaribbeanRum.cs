using CalamityDemutation.Content.Buffs.Alcohol;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Potions.Alcohol
{
    /// <summary>
    /// 加勒比朗姆酒（Caribbean Rum）—— 酒类药水第五件。
    /// <para>
    /// 面板与效果取 2.0.4：28×18、堆叠 9999、黄绿档、8 分钟、价值 5 金 30 银、研究解锁 5；
    /// 生命再生 +1 HP/s、移动速度 +10%、当前防御 −10%、浮空（下落速度 ×1.01）、酒醉 +1。
    /// </para>
    /// <para>
    /// 配方取 2.0.7.2：麦芽酒 + LivingShard @ 酒桶，或
    /// 瓶装水 + 血珠×5 + LivingShard @ 炼药桌，各产 1。
    /// LivingShard / 血珠按现代 / 经典灾厄分别 TryFind，缺件时跳过。
    /// </para>
    /// </summary>
    internal class CaribbeanRum : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 5;
        }
        public override void SetDefaults()
        {
            Item.DefaultToFood(28, 18, ModContent.BuffType<CaribbeanRumBuff>(), 8 * 60 * 60, true);
            Item.useTurn = true;
            Item.maxStack = 9999;
            Item.rare = ItemRarityID.Lime;
            Item.value = Item.buyPrice(0, 5, 30, 0);
        }
        public override void AddRecipes()
        {
            foreach (string modName in new[] { "CalamityMod", "CalamityModClassicPreTrailer" })
            {
                if (!ModLoader.TryGetMod(modName, out Mod calamity)
                    || !calamity.TryFind<ModItem>("LivingShard", out ModItem livingShard))
                    continue;
                CreateRecipe()
                    .AddIngredient(ItemID.Ale)
                    .AddIngredient(livingShard.Type)
                    .AddTile(TileID.Kegs)
                    .Register();
                if (calamity.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb))
                {
                    CreateRecipe()
                        .AddIngredient(ItemID.BottledWater)
                        .AddIngredient(bloodOrb.Type, 5)
                        .AddIngredient(livingShard.Type)
                        .AddTile(TileID.AlchemyTable)
                        .Register()
                        .DisableDecraft();
                }
            }
        }
    }
}
