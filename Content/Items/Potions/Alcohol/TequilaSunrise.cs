using CalamityDemutation.Content.Buffs.Alcohol;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Potions.Alcohol
{
    /// <summary>
    /// 龙舌兰日出（Tequila Sunrise）—— 酒类药水第三件。
    /// <para>
    /// 面板取经典 1.4.2：28×18、堆叠 30、浅红档、5 分钟、价值 20 金、研究解锁 5；
    /// 贴图取经典版 18×22，增益图标 32×32。
    /// </para>
    /// <para>
    /// 效果按用户 2026-10-19 修正：通用（昼夜均生效）+3% 伤害、+2% 暴击、+3% 伤害减免、+3% 击退；
    /// 白天再叠加 1.4.2 原版的 +7% 伤害、+3% 暴击、+15 防御、+7% 伤害减免、+7% 击退。
    /// 生命再生 −5 HP/s（10 点 lifeRegen），酒醉 +1。
    /// </para>
    /// <para>
    /// 配方取 2.0.7.2：麦芽酒 + 熔渣线材料×3 @ 酒桶，或
    /// 瓶装水 + 血珠×5 + 熔渣线材料×1 @ 炼药桌，各产 1。
    /// 现代用 ScoriaOre；经典用 CruptixBar，缺件时回退 ChaoticOre。
    /// </para>
    /// </summary>
    internal class TequilaSunrise : ModItem
    {
        /// <summary>通用伤害加成（昼夜均生效）</summary>
        public const float GeneralDamageBoost = 0.03f;
        /// <summary>通用暴击加成（昼夜均生效）</summary>
        public const int GeneralCritBoost = 2;
        /// <summary>通用伤害减免（昼夜均生效）</summary>
        public const float GeneralDamageReduction = 0.03f;
        /// <summary>通用击退加成（昼夜均生效）</summary>
        public const float GeneralKnockbackBoost = 0.03f;
        /// <summary>白天额外伤害加成（1.4.2 原版）</summary>
        public const float DayDamageBoost = 0.07f;
        /// <summary>白天额外暴击加成（1.4.2 原版）</summary>
        public const int DayCritBoost = 3;
        /// <summary>白天额外防御加成（1.4.2 原版）</summary>
        public const int DayDefenseBoost = 15;
        /// <summary>白天额外伤害减免（1.4.2 原版）</summary>
        public const float DayDamageReduction = 0.07f;
        /// <summary>白天额外击退加成（1.4.2 原版）</summary>
        public const float DayKnockbackBoost = 0.07f;
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 5;
        }
        /// <summary>
        /// 1.4.2 的经典面板：28×18、堆叠 30、浅红档、5 分钟、价值 20 金。
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 18;
            Item.useTurn = true;
            Item.maxStack = 30;
            Item.rare = ItemRarityID.LightRed;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.UseSound = SoundID.Item3;
            Item.consumable = true;
            Item.buffType = ModContent.BuffType<TequilaSunriseBuff>();
            Item.buffTime = 5 * 60 * 60;
            Item.value = Item.buyPrice(0, 20, 0, 0);
        }
        /// <summary>
        /// 2.0.7.2 双配方（各产 1），与莫斯科骡子同款：
        /// 麦芽酒 + 熔渣线材料×3 @ 酒桶；
        /// 瓶装水 + 血珠×5 + 熔渣线材料×1 @ 炼药桌。
        /// 现代取 ScoriaOre；经典取 CruptixBar；两者都没有时回退 ChaoticOre。
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
