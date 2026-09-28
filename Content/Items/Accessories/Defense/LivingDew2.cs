using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Defense
{
    /// <summary>
    /// 生命露（古）—— 灾厄 <b>2.0.3.9</b> 口径的生命露（照搬 2.0.3.9 的 <c>LivingDew</c>）。
    /// 与旧件 <see cref="LivingDew"/> 独立并存：旧件只给丛林增益，本件走源版那条「蜂蜜系协同链」。
    /// 效果 = 蜜露（古）的全部标记 + 多一条「火系/燃烧类减益时长也减半」，外加 +50 最大生命。
    /// </summary>
    internal class LivingDew2:ModItem
    {
        /// <summary>物品基础属性：34×22、无防御、稀有度黄绿（源为 Rarity7 档，对应 48 金）</summary>
        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 22;
            Item.value = Item.buyPrice(0, 48, 0, 0);
            Item.rare = ItemRarityID.Lime;
            Item.accessory = true;
        }
        /// <summary>
        /// 装备时：+50 最大生命，并置位蜂蜜系四个标记（比蜜露多一个 livingDewHalveDebuffs）
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.statLifeMax2 += 50;
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.alwaysHoneyRegen = true;
            modPlayer.honeyTurboRegen = true;
            modPlayer.honeyDewHalveDebuffs = true;
            modPlayer.livingDewHalveDebuffs = true;
        }
        /// <summary>
        /// 配方（照源）：蜜露（古）×1 + 捕蝇草球茎×3 + LivingShard×6 + EssenceofSunlight×5 @ 秘银砧。
        /// **捕蝇草球茎用本工程自有**的 <see cref="TrapperBulb"/>（灾厄 1.4.4 世系里没有那件，自持后与世系无关）；
        /// 另两样仍是灾厄材料，故整条挂在灾厄下。
        /// 经典版没有 EssenceofSunlight，改用 <b>EssenceofCinder</b>（用户 2026-09-28 指定的映射），LivingShard 两版同名
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod modern)
                && modern.TryFind<ModItem>("LivingShard", out ModItem livingShard)
                && modern.TryFind<ModItem>("EssenceofSunlight", out ModItem essenceofSunlight))
            {
                CreateRecipe().
                    AddIngredient<HoneyDew2>().
                    AddIngredient<TrapperBulb>(3).
                    AddIngredient(livingShard.Type, 6).
                    AddIngredient(essenceofSunlight.Type, 5).
                    AddTile(TileID.MythrilAnvil).
                    Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic)
                && classic.TryFind<ModItem>("LivingShard", out ModItem classicLivingShard)
                && classic.TryFind<ModItem>("EssenceofCinder", out ModItem essenceofCinder))
            {
                CreateRecipe().
                    AddIngredient<HoneyDew2>().
                    AddIngredient<TrapperBulb>(3).
                    AddIngredient(classicLivingShard.Type, 6).
                    AddIngredient(essenceofCinder.Type, 5).
                    AddTile(TileID.MythrilAnvil).
                    Register();
            }
        }
    }
}
