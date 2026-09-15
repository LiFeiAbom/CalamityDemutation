using CalamityDemutation.Content.Buffs.PositiveBuffs;
using CalamityDemutation.Content.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Potions.Agentia
{
    /// <summary>
    /// 无尽饥渴药水（Ceaseless Hunger Potion）：饮用后获得 10 秒（600 帧）无尽饥渴增益，
    /// 将大范围内的掉落物吸附向玩家。效果结算见 CeaselessHunger 与 CalamityDemutationPlayer。
    /// </summary>
    internal class CeaselessHungerPotion:ModItem
    {
        /// <summary>
        /// 药水类消耗品：可堆叠、可食用，使用后附加 600 帧（10 秒）无尽饥渴增益
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 18;
            Item.useTurn = true;
            Item.maxStack = 999;
            Item.rare = ItemRarityID.Orange;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.UseSound = SoundID.Item3;
            Item.consumable = true;
            Item.buffType = ModContent.BuffType<CeaselessHunger>();   // 附加无尽饥渴增益
            Item.buffTime = 600;                                      // 增益持续 600 帧（10 秒）
            Item.value = Item.buyPrice(0, 2, 0, 0);
        }
        /// <summary>
        /// 合成表：每次产出 4 瓶（炼金台）。现代版用本模组自有的银河奇点（GalacticaSingularity），
        /// 经典版分支改取经典版灾厄的 GalacticaSingularity，两侧均搭配暗物质（DarkPlasma）
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：水×4 + DarkPlasma + 本模组银河奇点（每次 4 瓶）
                if (calamity.TryFind<ModItem>("DarkPlasma", out ModItem darkPlasma1))
                {
                    Recipe recipe = CreateRecipe(4);
                    recipe.AddIngredient(ItemID.BottledWater, 4);
                    recipe.AddIngredient(darkPlasma1.Type);
                    recipe.AddIngredient<GalacticaSingularity>();
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
                // 现代版灾厄：水×4 + 血珠×20 + DarkPlasma（每次 4 瓶）
                if (calamity.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb1) && calamity.TryFind<ModItem>("DarkPlasma", out ModItem darkPlasma2))
                {
                    Recipe recipe = CreateRecipe(4);
                    recipe.AddIngredient(ItemID.BottledWater, 4);
                    recipe.AddIngredient(bloodOrb1.Type, 20);
                    recipe.AddIngredient(darkPlasma2.Type);
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄：水×4 + DarkPlasma + 经典版 GalacticaSingularity（每次 4 瓶）
                if (calamity1.TryFind<ModItem>("DarkPlasma", out ModItem darkPlasma3) && calamity1.TryFind<ModItem>("GalacticaSingularity", out ModItem galacticaSingularity1))
                {
                    Recipe recipe1 = CreateRecipe(4);
                    recipe1.AddIngredient(ItemID.BottledWater, 4);
                    recipe1.AddIngredient(darkPlasma3.Type);
                    recipe1.AddIngredient(galacticaSingularity1.Type);
                    recipe1.AddTile(TileID.AlchemyTable);
                    recipe1.Register();
                }
                // 经典版灾厄：水×4 + 血珠×20 + DarkPlasma（每次 4 瓶）
                if (calamity1.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb2) && calamity1.TryFind<ModItem>("DarkPlasma", out ModItem darkPlasma4))
                {
                    Recipe recipe1 = CreateRecipe(4);
                    recipe1.AddIngredient(ItemID.BottledWater, 4);
                    recipe1.AddIngredient(bloodOrb2.Type, 20);
                    recipe1.AddIngredient(darkPlasma4.Type);
                    recipe1.AddTile(TileID.AlchemyTable);
                    recipe1.Register();
                }
            }
        }
    }
}
