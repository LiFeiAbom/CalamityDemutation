using CalamityDemutation.Content.Projectiles;
using CalamityDemutation.Content.Projectiles.Typeless;
using CalamityDemutation.Content.Items.Materials;
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
    /// ③ 配方照 2.0.4：任意箭袋 + 银河奇点 ×5 + 暗黑等离子 ×3 @ 远古操纵机，现代版 / 经典版各一条。
    ///    **银河奇点按本工程既有口径分版本取**（同 <c>CeaselessHungerPotion</c> / <c>ArkoftheElements</c> 的写法）：
    ///    现代版用本模组自写的 <see cref="GalacticaSingularity"/>（灾厄 2.2.x 已把自己的银河奇点删掉），
    ///    经典版取经典版灾厄的；经典版灾厄没有 <c>AnyQuiver</c> 配方组，故退回本模组自建的
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
        /// 配方：照 2.0.4 的「任意箭袋 + 银河奇点 ×5 + 暗黑等离子 ×3 @ 远古操纵机」，现代 / 经典版各一条。
        /// <para>
        /// **银河奇点按本工程既有口径取用**（同 <c>CeaselessHungerPotion</c> / <c>ArkoftheElements</c>）：
        /// 现代版灾厄 2.2.x 已把自己那件银河奇点整个删除，故现代分支直接用**本模组自写的**
        /// <see cref="GalacticaSingularity"/>（四种月亮碎片 @ 远古操纵机，同源同口径）；经典版灾厄两样
        /// 材料都还在，继续取经典版自己的。
        /// </para>
        /// <para>
        /// **别再照 2.0.4 硬写 <c>calamity.TryFind&lt;ModItem&gt;("GalacticaSingularity")</c>**：
        /// 2026-10-07 玩家实测的"配方不见了"就是这么来的——2.2.x 上该名字查不到，<c>TryFind</c> 返回
        /// false 后整个 <c>if</c> 块被跳过，**一条配方都没注册且日志零报错**。软依赖材料要么有"取不到"
        /// 的退路，要么直接改用本模组自持件。
        /// </para>
        /// </summary>
        public override void AddRecipes()
        {
            // 现代版灾厄：银河奇点用本模组自写件补位；暗黑等离子仍是灾厄材料（两版都还在）
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("DarkPlasma", out ModItem darkPlasmaModern))
                {
                    AddNihilityRecipe("AnyQuiver", ModContent.ItemType<GalacticaSingularity>(), darkPlasmaModern.Type);
                }
                else
                {
                    Mod.Logger.Warn("虚无箭袋：现代版灾厄里找不到 DarkPlasma，本条配方未注册。");
                }
            }

            // 经典版灾厄：银河奇点 / 暗黑等离子都还在，照 2.0.4 源配方；它没有 AnyQuiver 配方组，用本模组自建的同内容组
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if (classic.TryFind<ModItem>("DarkPlasma", out ModItem darkPlasmaClassic) &&
                    classic.TryFind<ModItem>("GalacticaSingularity", out ModItem galacticaClassic))
                {
                    AddNihilityRecipe("CalamityDemutation:AnyQuiver", galacticaClassic.Type, darkPlasmaClassic.Type);
                }
                else
                {
                    Mod.Logger.Warn("虚无箭袋：经典版灾厄里找不到 GalacticaSingularity / DarkPlasma，本条配方未注册。");
                }
            }
        }

        /// <summary>
        /// 注册一条虚无箭袋配方（两个版本分支共用）：
        /// 「<paramref name="quiverGroup"/> 任意箭袋 + 银河奇点 ×5 + 暗黑等离子 ×3 @ 远古操纵机」。
        /// 现代版灾厄自带 <c>AnyQuiver</c> 配方组（照源直接用）；经典版没有，故传本模组自建的
        /// <c>CalamityDemutation:AnyQuiver</c>（两组的有效物品一致：魔法箭袋 / 熔火箭袋 / 潜猎者箭袋）。
        /// 配方组在本模组的 ModSystem 里登记，而 tML 的加载顺序是「所有模组的 AddRecipeGroups → 所有
        /// 模组的 AddRecipes」（<c>Recipe.SetupRecipeGroups</c> 早于 <c>RecipeLoader.AddRecipes</c>），
        /// 所以两个名字都能按名查到；万一组名不存在时才退到本模组自己的组。
        /// </summary>
        private void AddNihilityRecipe(string quiverGroup, int galacticaType, int darkPlasmaType)
        {
            string groupName = RecipeGroup.recipeGroupIDs.ContainsKey(quiverGroup) ? quiverGroup : "CalamityDemutation:AnyQuiver";
            Recipe recipe = CreateRecipe();
            recipe.AddRecipeGroup(groupName);
            recipe.AddIngredient(galacticaType, 5);
            recipe.AddIngredient(darkPlasmaType, 3);
            recipe.AddTile(TileID.LunarCraftingStation);
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
