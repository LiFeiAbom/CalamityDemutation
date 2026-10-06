using CalamityDemutation.Content.Projectiles;
using CalamityDemutation.Content.Projectiles.Typeless;
using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Ranged
{
    /// <summary>
    /// 虚无箭袋（Quiver of Nihility）—— 移植自灾厄 2.0.4 的 <c>Items/Accessories/QuiverofNihility</c>。
    /// 月后远程饰品：装备后在周身半径 300 处环绕四座虚空场（<see cref="VoidFieldGenerator"/>），
    /// 己方箭矢穿过虚空场时伤害 ×1.75 且弹速翻倍（判定见 <see cref="VoidFieldGenerator.AI"/>，
    /// 去重标记见 <see cref="CalamityDemutationGlobalProjectile.nihilicArrow"/>）。
    /// <para>
    /// 与源的差异（其余照搬）：
    /// ① 基础属性按用户 2026-10-06 点名修正为「远程伤害 +12%、远程暴击率 +12%」（源只有远程暴击 +5）；
    ///    数值结算按本工程口径统一写在 <c>CalamityDemutationPlayer</c> 里，本件只置位 <c>voidField</c> 标记；
    /// ② 稀有度走本工程的月后体系：源的 <c>Turquoise</c>（灾厄 Rarity12，青绿）→ <c>ItemRarityID.Red</c>
    ///    + <c>postMoonLordRarity = 12</c>（同为青绿），价值照 2.0.4 的 <c>RarityTurquoiseBuyPrice</c>
    ///    = 1 铂金 50 金；
    /// ③ 配方照 2.0.4：任意箭袋 + 暗黑等离子 ×3 + 银河奇点 ×5，在远古操纵机处合成；按本工程口径分
    ///    现代版 / 经典版各一条——经典版灾厄没有 <c>AnyQuiver</c> 配方组，故经典分支改用本模组自建的
    ///    <c>CalamityDemutation:AnyQuiver</c> 组（内容与灾厄一致：魔法箭袋 / 熔火箭袋 / 潜猎者箭袋）；
    /// ④ 源的 <c>donorItem</c> 标记属灾厄的"捐助者物品"体系，本工程没有对应机制，不保留。
    /// </para>
    /// </summary>
    [AutoloadEquip(EquipType.Back)]   // 装备时在玩家背后叠加箭袋外观（需 QuiverofNihility_Back.png）
    internal class QuiverofNihility : ModItem
    {
        /// <summary>
        /// 物品基础属性：贴图尺寸 42×36（实际贴图 46×84，故背包内另做 0.55× 缩放绘制）、
        /// 价值 1 铂金 50 金、饰品标记，并指定模组自定义的月后稀有度 12 级（青绿）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 42;                         // 贴图宽 42 像素
            Item.height = 36;                        // 贴图高 36 像素
            Item.value = Item.buyPrice(1, 50, 0, 0); // 价值 1 铂金 50 金（照 2.0.4 的 RarityTurquoiseBuyPrice）
            Item.accessory = true;                   // 标记为饰品，可装备于饰品栏
            // 月后物品：基础稀有度填红，名称颜色由 postMoonLordRarity 统一渲染
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 12;   // 月后自定义稀有度 12 级：青绿（对应灾厄 Turquoise）
        }
        /// <summary>
        /// 装备互斥判定：已经装了本件（<c>voidField</c> 已置位）时禁止再装第二件，避免场数叠加（照源）
        /// </summary>
        public override bool CanEquipAccessory(Player player, int slot, bool modded)
        {
            return !player.GetModPlayer<CalamityDemutationPlayer>().voidField;
        }
        /// <summary>
        /// 装备时置位 <c>voidField</c> 标记（虚空场弹幕据此续命、本件据此互斥），
        /// 并在本地玩家侧补挂四座虚空场弹幕；远程增伤/暴击的数值结算见 CalamityDemutationPlayer
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.voidField = true;
            // 仅在本地玩家侧生成虚空场，避免多人下重复
            if (player.whoAmI == Main.myPlayer)
            {
                var source = player.GetSource_Accessory(Item);
                // 数量不足 4 座时一次性补齐四座（第 i 座的序号经 ai[0] 传入，决定其在环上的 90° 间隔）
                if (player.ownedProjectileCounts[ModContent.ProjectileType<VoidFieldGenerator>()] < 4)
                {
                    for (int v = 0; v < 4; v++)
                    {
                        Projectile.NewProjectileDirect(source, player.Center, Vector2.Zero, ModContent.ProjectileType<VoidFieldGenerator>(), 0, 0f, Main.myPlayer, v);
                    }
                }
            }
        }
        /// <summary>
        /// 配方：照 2.0.4 的「任意箭袋 + 暗黑等离子 ×3 + 银河奇点 ×5 @ 远古操纵机」，
        /// 兼容灾厄现代版与经典版分别注册；经典版灾厄没有 AnyQuiver 配方组，改用本模组自建的
        /// CalamityDemutation:AnyQuiver（内容与灾厄一致）
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("DarkPlasma", out ModItem darkPlasma1) && calamity.TryFind<ModItem>("GalacticaSingularity", out ModItem galactica1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddRecipeGroup("AnyQuiver");
                    recipe.AddIngredient(darkPlasma1.Type, 3);
                    recipe.AddIngredient(galactica1.Type, 5);
                    recipe.AddTile(TileID.LunarCraftingStation);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if (classic.TryFind<ModItem>("DarkPlasma", out ModItem darkPlasma2) && classic.TryFind<ModItem>("GalacticaSingularity", out ModItem galactica2))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddRecipeGroup("CalamityDemutation:AnyQuiver");
                    recipeClassic.AddIngredient(darkPlasma2.Type, 3);
                    recipeClassic.AddIngredient(galactica2.Type, 5);
                    recipeClassic.AddTile(TileID.LunarCraftingStation);
                    recipeClassic.Register();
                }
            }
        }
        /// <summary>
        /// 背包内绘制：本件贴图（46×84）比常规饰品高得多，照源按 wantedScale 0.55 自定义缩放绘制，
        /// 免得在背包格子里被压扁（对应灾厄的 CalamityUtils.DrawInventoryCustomScale 内联版）
        /// </summary>
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            float wantedScale = Math.Max(scale, 0.55f * Main.inventoryScale);
            spriteBatch.Draw(TextureAssets.Item[Type].Value, position, frame, drawColor, 0f, origin, wantedScale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
