using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Defense
{
    /// <summary>
    /// 蜜露（古）—— 灾厄 <b>2.0.3.9</b> 口径的蜜露（照搬 2.0.3.9 的 <c>HoneyDew</c>）。
    /// 本工程已有一件同名的 <see cref="HoneyDew"/>，那是另一套口径的移植（丛林增益 + 蜜蜂减伤 + 免疫毒液），
    /// 两者**独立并存、互不影响**：本件走源版那条「蜂蜜系协同链」。
    /// 与旧件的差异：源版给 +30 最大生命，并置位三个新标记——
    /// 永远蜂蜜式回血、蜂蜜站桩加速回血、病症/中毒类减益时长减半（旧件一个都没有）；
    /// 也不设 <c>beeResist</c>（那是旧件口径）。
    /// 配方照源：蜂蜜瓶×10 + 蜂蜡×3 + MurkyPaste×3 @ 铁砧。
    /// </summary>
    internal class HoneyDew2:ModItem
    {
        /// <summary>物品基础属性：20×20、无防御、稀有度绿（源为 Rarity2 档，对应 2 金）</summary>
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.value = Item.buyPrice(0, 2, 0, 0);
            Item.rare = ItemRarityID.Green;
            Item.accessory = true;
        }
        /// <summary>
        /// 装备时：+30 最大生命，并置位蜂蜜系三个标记（具体数值一律在 CalamityDemutationPlayer 中结算）
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.statLifeMax2 += 30;
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.alwaysHoneyRegen = true;
            modPlayer.honeyTurboRegen = true;
            modPlayer.honeyDewHalveDebuffs = true;
        }
        /// <summary>
        /// 配方（照源）：蜂蜜瓶×10 + 蜂蜡×3 + 污浊糊×3 @ 铁砧。
        /// 污浊糊用的是**本工程自有**的 <see cref="MurkyPaste"/>（不再软依赖灾厄那件——
        /// 灾厄 1.4.4 世系里没有它；自持之后本配方与世系无关、必然注册）
        /// </summary>
        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.BottledHoney, 10).
                AddIngredient(ItemID.BeeWax, 3).
                AddIngredient<MurkyPaste>(3).
                AddTile(TileID.Anvils).
                Register();
        }
    }
}
