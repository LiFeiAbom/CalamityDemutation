using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Rogue
{
    /// <summary>
    /// 毁灭徽章（Ruin Medallion）—— "暗物质剑鞘"链的第二件（欺诈硬币 → 毁灭徽章 → 暗物质剑鞘 → 日蚀魔镜），
    /// 秘银砧档。口径按用户 2026-10-07 拍板：
    /// <para>
    /// ① 潜行打击取 **2.0 的"半价"**（<c>stealthStrikeHalfCost</c>；2.0.3.9+ 改成"只需消耗 75% 上限"）。
    /// </para>
    /// <para>
    /// ② 基础属性按用户指定：**最大潜行值 +10 点**、**盗贼伤害 +4%**、**盗贼暴击 +4**
    /// （源为 盗贼伤害 +6% / 盗贼暴击 +6，用户点名替换成上面这组）。
    /// </para>
    /// <para>
    /// ③ 配方沿用本链的"经典分支也能合成"口径：**欺诈硬币（本模组自写的那件）×1 + 不洁核心×4 +
    /// 灾祸精华×2 @ 秘银砧**。灾祸精华在 2.0 写作 <c>EssenceofChaos</c>、2.0.3.9+ 写作 <c>EssenceofHavoc</c>，
    /// 故现代分支用 <c>EssenceofHavoc</c>、经典分支用 <c>EssenceofChaos</c>，各注册一条（两版的 <c>UnholyCore</c> 都取）。
    /// </para>
    /// <para>
    /// 效果分两处落地：盗贼伤害/暴击写在 <c>CalamityDemutationPlayer.PostUpdateMiscEffects</c>
    /// （现代版加在灾厄盗贼类上、经典版反射写它自己的自定义投掷字段）；潜行上限 +10 点与"潜行打击半价"
    /// 必须写在 <c>PostUpdateEquips</c>（灾厄在 ResetEffects 里每帧复位这些字段、之后才读取），走 CDUtil 的反射桥。
    /// </para>
    /// <para>
    /// 与灾厄本体重名：本体仍有同名件，本件是"旧版回归 + 用户再平衡"的同名不同物；按既有口径**不做互斥**。
    /// </para>
    /// </summary>
    internal class RuinMedallion : ModItem
    {
        /// <summary>
        /// 基础属性：20×28、价值 24 金（灾厄 2.0.x 粉档 `Rarity5BuyPrice`；1.4.4-release 才是 20 金）、
        /// 稀有度粉、饰品
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 28;
            Item.value = Item.buyPrice(0, 24, 0, 0);
            Item.rare = ItemRarityID.Pink;
            Item.accessory = true;
        }
        /// <summary>
        /// 装备时置位 <c>ruinMedallion</c> 标记；数值在 CalamityDemutationPlayer 里按标记触发
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().ruinMedallion = true;
        }
        /// <summary>
        /// 配方：欺诈硬币（本模组自写）×1 + 不洁核心×4 + 灾祸精华×2 @ 秘银砧，现代/经典两分支各注册一条
        /// </summary>
        public override void AddRecipes()
        {
            // 现代版：灾祸精华 = EssenceofHavoc
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("UnholyCore", out ModItem unholyCore) &&
                    calamity.TryFind<ModItem>("EssenceofHavoc", out ModItem essenceofHavoc))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<CoinofDeceit>();
                    recipe.AddIngredient(unholyCore.Type, 4);
                    recipe.AddIngredient(essenceofHavoc.Type, 2);
                    recipe.AddTile(TileID.MythrilAnvil);
                    recipe.Register();
                }
                else
                {
                    Mod.Logger.Warn("毁灭徽章：现代版灾厄里找不到 UnholyCore / EssenceofHavoc，本条配方未注册。");
                }
            }
            // 经典版：没有 EssenceofHavoc，改用同族的 EssenceofChaos
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if (classic.TryFind<ModItem>("UnholyCore", out ModItem unholyCore) &&
                    classic.TryFind<ModItem>("EssenceofChaos", out ModItem essenceofChaos))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient<CoinofDeceit>();
                    recipeClassic.AddIngredient(unholyCore.Type, 4);
                    recipeClassic.AddIngredient(essenceofChaos.Type, 2);
                    recipeClassic.AddTile(TileID.MythrilAnvil);
                    recipeClassic.Register();
                }
                else
                {
                    Mod.Logger.Warn("毁灭徽章：经典版灾厄里找不到 UnholyCore / EssenceofChaos，本条配方未注册。");
                }
            }
        }
    }
}
