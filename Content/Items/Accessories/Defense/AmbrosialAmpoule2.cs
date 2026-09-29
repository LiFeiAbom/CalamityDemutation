using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Defense
{
    /// <summary>
    /// 甘露安瓿（古）—— 灾厄 <b>2.0.3.9</b> 口径的甘露安瓿（照搬 2.0.3.9 的 <c>AmbrosialAmpoule</c>）。
    /// 与旧件 <see cref="AmbrosialAmpoule"/> 独立并存。⚠️ 与旧件的两处关键差异：
    /// ① 源版**没有** <c>Item.defense</c>（旧件多给了 6 点防御）；
    /// ② 源版给 +70 最大生命并置位整条蜂蜜系标记（旧件只置 <c>beeResist</c> + <c>ambrosialAmpoule</c>）。
    /// 是这条（古）链里蜂蜜系的顶件：蜜露（古）→ 生命露（古）→ 甘露安瓿（古）。
    /// </summary>
    internal class AmbrosialAmpoule2:ModItem
    {
        /// <summary>物品基础属性：20×20、**无防御**、稀有度青（源为 Rarity9 档，对应 80 金）</summary>
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.value = Item.buyPrice(0, 80, 0, 0);
            Item.rare = ItemRarityID.Cyan;
            Item.accessory = true;
        }
        /// <summary>
        /// 装备时：+70 最大生命，置位 <c>aAmpoule</c> 与蜂蜜系三个标记（继承蜜露与生命露的全部效果），
        /// 并按源加光（光辉软泥（古）/ 无暇粹魂晶都没装备时才加）
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.statLifeMax2 += 70;
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.aAmpoule = true;
            modPlayer.alwaysHoneyRegen = true;
            modPlayer.honeyDewHalveDebuffs = true;
            modPlayer.livingDewHalveDebuffs = true;
            if (!(modPlayer.rOoze || modPlayer.purity) && !hideVisual)
            {
                Lighting.AddLight(player.Center, new Vector3(1.2f, 1.2f, 0.72f));
            }
        }
        /// <summary>
        /// 配方（照源）：生命露（古）+ 光辉软泥（古）+ LifeAlloy×3 @ 远古操纵台。
        /// 经典版没有 LifeAlloy，按用户 2026-09-28 的指定改用 <b>BarofLife</b>×3；台子是原版月台，两版同名
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("LifeAlloy", out ModItem lifeAlloy))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<LivingDew2>();
                    recipe.AddIngredient<RadiantOoze2>();
                    recipe.AddIngredient(lifeAlloy.Type, 3);
                    recipe.AddTile(TileID.LunarCraftingStation);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if (classic.TryFind<ModItem>("BarofLife", out ModItem barofLife))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient<LivingDew2>();
                    recipeClassic.AddIngredient<RadiantOoze2>();
                    recipeClassic.AddIngredient(barofLife.Type, 3);
                    recipeClassic.AddTile(TileID.LunarCraftingStation);
                    recipeClassic.Register();
                }
            }
        }
    }
}
