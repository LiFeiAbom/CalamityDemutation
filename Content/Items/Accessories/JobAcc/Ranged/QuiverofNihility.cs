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
    /// ③ 配方按「材料 / 站台以各自版本实际有的为准」注册（细节见 <see cref="AddRecipes"/>）：任意箭袋
    ///    + 暗黑等离子 ×3 + 月亮材料 ×5，现代版 / 经典版各一条——经典版灾厄没有 <c>AnyQuiver</c> 配方组，
    ///    故回退到本模组自建的 <c>CalamityDemutation:AnyQuiver</c> 组（内容与灾厄一致：
    ///    魔法箭袋 / 熔火箭袋 / 潜猎者箭袋）；
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
        /// 配方：现代版 / 经典版灾厄各注册一条，材料与站台一律**按各自版本实际存在的取**。
        /// <para>
        /// 现代版灾厄 2.0.x 的源配方是「任意箭袋 + 暗黑等离子 ×3 + 银河奇点（GalacticaSingularity）×5
        /// @ 远古操纵机」；但 2.2.x 起本体把银河奇点整个删掉了，自己的同名配方改成了
        /// 「任意箭袋 + 涡流碎片（<see cref="ItemID.FragmentVortex"/>）×5 + 暗黑等离子 ×3 @ 秘银砧」。
        /// 若照 2.0.4 硬写银河奇点，在 2.2.x 上 <c>TryFind</c> 会返回 false 而把整条配方**静默跳过**
        /// （2026-10-07 玩家实测的"配方不见了"就是这个：日志里一条报错都不会有），故这里按版本自适应：
        /// 有银河奇点就照 2.0.4 的老口径，没有就照本体现用的涡流碎片 + 秘银砧。
        /// 经典版灾厄（CalamityModClassicPreTrailer）两样材料都还在，继续照 2.0.4 的原始口径。
        /// </para>
        /// </summary>
        public override void AddRecipes()
        {
            // 现代版灾厄：暗黑等离子是两版都有的必备材料，"月亮材料 + 站台"按版本取
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("DarkPlasma", out ModItem darkPlasmaModern))
                {
                    // 2.0.x：银河奇点 ×5 @ 远古操纵机（源配方）；2.2.x：银河奇点已被删除，改用本体现用的涡流碎片 ×5 @ 秘银砧
                    bool hasGalactica = calamity.TryFind<ModItem>("GalacticaSingularity", out ModItem galacticaModern);
                    AddNihilityRecipe(
                        quiverGroup: "AnyQuiver",
                        moonMaterialType: hasGalactica ? galacticaModern.Type : ItemID.FragmentVortex,
                        darkPlasmaType: darkPlasmaModern.Type,
                        craftingStation: hasGalactica ? TileID.LunarCraftingStation : TileID.MythrilAnvil);
                }
                else
                {
                    Mod.Logger.Warn("虚无箭袋：现代版灾厄里找不到 DarkPlasma，本条配方未注册。");
                }
            }

            // 经典版灾厄：银河奇点 / 暗黑等离子都在，照 2.0.4 源配方；它没有 AnyQuiver 配方组，用本模组自建的同内容组
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if (classic.TryFind<ModItem>("DarkPlasma", out ModItem darkPlasmaClassic) &&
                    classic.TryFind<ModItem>("GalacticaSingularity", out ModItem galacticaClassic))
                {
                    AddNihilityRecipe(
                        quiverGroup: "CalamityDemutation:AnyQuiver",
                        moonMaterialType: galacticaClassic.Type,
                        darkPlasmaType: darkPlasmaClassic.Type,
                        craftingStation: TileID.LunarCraftingStation);
                }
                else
                {
                    Mod.Logger.Warn("虚无箭袋：经典版灾厄里找不到 GalacticaSingularity / DarkPlasma，本条配方未注册。");
                }
            }
        }

        /// <summary>
        /// 注册一条虚无箭袋配方（两个版本分支共用）：
        /// 「<paramref name="quiverGroup"/> 任意箭袋 + 月亮材料 ×5 + 暗黑等离子 ×3 @ 指定站台」。
        /// 现代版灾厄自带 <c>AnyQuiver</c> 配方组（照源直接用）；经典版没有，故传本模组自建的
        /// <c>CalamityDemutation:AnyQuiver</c>（两组的有效物品一致：魔法箭袋 / 熔火箭袋 / 潜猎者箭袋）。
        /// 配方组在本模组的 ModSystem 里登记，而 tML 的加载顺序是「所有模组的 AddRecipeGroups → 所有
        /// 模组的 AddRecipes」（<c>Recipe.SetupRecipeGroups</c> 早于 <c>RecipeLoader.AddRecipes</c>），
        /// 所以这里两个名字都能按名查到；万一组名不存在时才退到本模组自己的组。
        /// </summary>
        private void AddNihilityRecipe(string quiverGroup, int moonMaterialType, int darkPlasmaType, int craftingStation)
        {
            string groupName = RecipeGroup.recipeGroupIDs.ContainsKey(quiverGroup) ? quiverGroup : "CalamityDemutation:AnyQuiver";
            Recipe recipe = CreateRecipe();
            recipe.AddRecipeGroup(groupName);
            recipe.AddIngredient(moonMaterialType, 5);
            recipe.AddIngredient(darkPlasmaType, 3);
            recipe.AddTile(craftingStation);
            recipe.Register();
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
