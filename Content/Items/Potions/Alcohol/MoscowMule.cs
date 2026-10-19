using CalamityDemutation.Content.Buffs.Alcohol;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Potions.Alcohol
{
    /// <summary>
    /// 莫斯科骡子（Moscow Mule）—— 酒类药水第二件。
    /// <para>
    /// 物品形态取 2.0.4：32×34、堆叠 9999、黄档、8 分钟、价值 5 金 30 银、研究解锁 5；
    /// 配方取 2.0.7.2：麦芽酒×1 + 熔渣线材料×3 @ 酒桶，或
    /// 瓶装水×1 + 血珠×5 + 熔渣线材料×1 @ 炼药桌，各产 1 瓶。
    /// 现代用 ScoriaOre；经典没有该矿石，用 CruptixBar（Chaotic Bar）作为对应材料。
    /// 经典版以锭代替矿，成本高于现代版。
    /// </para>
    /// <para>
    /// 效果取 2.0.3.9+ 口径：通用伤害 +9%、通用暴击 +3%、武器击退 +50%、
    /// 生命再生 −2 HP/s、酒醉等级 +1。
    /// </para>
    /// </summary>
    internal class MoscowMule : ModItem
    {
        /// <summary>2.0.3.9 的通用暴击加成</summary>
        public const int CritBoost = 3;
        /// <summary>2.0.3.9 的通用伤害加成</summary>
        public const float DamageBoost = 0.09f;
        /// <summary>2.0.3.9 的武器击退加成（StatModifier 的加法乘数）</summary>
        public const float KnockbackBoost = 0.5f;
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 5;
        }
        /// <summary>
        /// 2.0.4 的饮品默认值，随后覆盖为黄档与 2.0.4 的价值。
        /// </summary>
        public override void SetDefaults()
        {
            Item.DefaultToFood(32, 34, ModContent.BuffType<MoscowMuleBuff>(), 8 * 60 * 60, true);
            Item.useTurn = true;
            Item.maxStack = 9999;
            Item.rare = ItemRarityID.Yellow;
            Item.value = Item.buyPrice(0, 5, 30, 0);
        }
        /// <summary>
        /// 2.0.7.2 双配方（各产 1）：
        /// 麦芽酒 + 熔渣线材料×3 @ 酒桶；
        /// 瓶装水 + 血珠×5 + 熔渣线材料×1 @ 炼药桌。
        /// 现代取 ScoriaOre；经典取 CruptixBar（Chaotic Bar）；
        /// 两者都没有时回退 ChaoticOre。血珠同样按版本 TryFind，缺件时跳过。
        /// </summary>
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
