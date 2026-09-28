using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.StatLife
{
    /// <summary>
    /// 光辉软泥（古）—— 灾厄 <b>2.0.3.9</b> 口径的光辉软泥（照搬 2.0.3.9 的 <c>RadiantOoze</c>）。
    /// 与旧件 <see cref="RadiantOoze"/> 独立并存：旧件置 <c>radiantOoze</c>（夜间暖黄光 + 回血），
    /// 本件置源版的 <c>rOoze</c> 标记——按「缺失生命比例」给再生（满血 4 点 → 空血 12 点，即 2~6 HP/s）。
    /// 装备时另加一层暖黄光；源里要求另外两件（甘露安瓿（古）/ 无暇粹魂晶）都没装备、且不隐藏时才加。
    /// </summary>
    internal class RadiantOoze2:ModItem
    {
        /// <summary>物品基础属性：20×20、无防御、稀有度浅红（源为 Rarity4 档，对应 12 金）</summary>
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.value = Item.buyPrice(0, 12, 0, 0);
            Item.rare = ItemRarityID.LightRed;
            Item.accessory = true;
        }
        /// <summary>
        /// 装备时置位 <c>rOoze</c>（再生在 CalamityDemutationPlayer 中结算），
        /// 并按源的判据加暖黄光：另两件（古）配件都没装备时才加，避免叠加
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.rOoze = true;
            if (!(modPlayer.aAmpoule || modPlayer.purity) && !hideVisual)
            {
                Lighting.AddLight(player.Center, new Vector3(1f, 1f, 0.6f));
            }
        }
        /// <summary>
        /// 配方（照源）：现代版 污化凝胶×45 + 纯凝胶×15 @ 铁砧；
        /// 经典版没有 BlightedGel（污化凝胶），按用户 2026-09-28 的指定改用纯凝胶，并把两份合成一条 ×60
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod modern)
                && modern.TryFind<ModItem>("BlightedGel", out ModItem blightedGel)
                && modern.TryFind<ModItem>("PurifiedGel", out ModItem purifiedGel))
            {
                CreateRecipe().
                    AddIngredient(blightedGel.Type, 45).
                    AddIngredient(purifiedGel.Type, 15).
                    AddTile(TileID.Anvils).
                    Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic)
                && classic.TryFind<ModItem>("PurifiedGel", out ModItem classicPurifiedGel))
            {
                CreateRecipe().
                    AddIngredient(classicPurifiedGel.Type, 60).
                    AddTile(TileID.Anvils).
                    Register();
            }
        }
    }
}
