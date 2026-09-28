using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Defense
{
    /// <summary>
    /// 感染宝石（InfectedJewel）—— 灾厄 <b>2.0.3.9</b> 口径（照搬 2.0.3.9 的 <c>InfectedJewel</c>，
    /// 源里带 <c>[LegacyName("CelestialJewel")]</c>，本工程不涉及跨模组改名故略去）。
    /// 是宝石系三件的中位：王冠宝石 → 感染宝石 → 无暇粹魂晶，三者**互斥**（源为 else-if 链），效果逐级升级。
    /// 装备只置 <c>infectedJewel</c> 标记，实际数值在 CalamityDemutationPlayer 中结算；
    /// 本件是这条链里第一个用到**动态防御** <c>jewelBonusDefense</c> 的（随减益数上涨、每秒回落）。
    /// </summary>
    internal class InfectedJewel:ModItem
    {
        /// <summary>物品基础属性：26×26、防御 +6、稀有度黄绿（源为 Rarity7 档，对应 48 金）</summary>
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.defense = 6;
            Item.value = Item.buyPrice(0, 48, 0, 0);
            Item.rare = ItemRarityID.Lime;
            Item.accessory = true;
        }
        /// <summary>装备时置位 <c>infectedJewel</c>（数值在 CalamityDemutationPlayer 中结算）</summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().infectedJewel = true;
        }
        /// <summary>
        /// 配方（照源）：王冠宝石 + AureusCell×10 + Stardust×25 @ 秘银砧。
        /// 现代版灾厄里 <c>Stardust</c> 已改名 <b>StarblightSoot</b>（用户 2026-09-28 指定的映射）；
        /// 经典版保留 <c>Stardust</c>，但 <b>AureusCell</b> 不存在，按用户指定改用 <b>AstralJelly</b>
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod modern)
                && modern.TryFind<ModItem>("AureusCell", out ModItem aureusCell)
                && modern.TryFind<ModItem>("StarblightSoot", out ModItem starblightSoot))
            {
                CreateRecipe().
                    AddIngredient<CrownJewel>().
                    AddIngredient(aureusCell.Type, 10).
                    AddIngredient(starblightSoot.Type, 25).
                    AddTile(TileID.MythrilAnvil).
                    Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic)
                && classic.TryFind<ModItem>("AstralJelly", out ModItem astralJelly)
                && classic.TryFind<ModItem>("Stardust", out ModItem stardust))
            {
                CreateRecipe().
                    AddIngredient<CrownJewel>().
                    AddIngredient(astralJelly.Type, 10).
                    AddIngredient(stardust.Type, 25).
                    AddTile(TileID.MythrilAnvil).
                    Register();
            }
        }
    }
}
