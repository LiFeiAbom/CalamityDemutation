using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Rogue
{
    /// <summary>
    /// 深渊魔镜（Abyssal Mirror）—— 盗贼潜行链条的第二件（幻影 → **深渊** → 日蚀），秘银砧档。
    /// 口径按用户 2026-10-07 拍板取灾厄 **2.0** 版：30×38、青柠档（7）、48 金；
    /// 站定潜行恢复 **+30%**、移动 **+20%**（2.0.3.9 起被本体削到 +25%/+12%）、仇恨 **−450**，
    /// 并给一次**闪避**（详见 <c>CalamityDemutationPlayer.FreeDodge</c> 的 abyssalMirror 分支）。
    /// 另按用户 2026-10-07 追加基础属性：**最大潜行值 +20%（相对当前上限，走 GrantRogueStealthRatio）**、
    /// 盗贼伤害 +5%、盗贼暴击 +5。
    /// <para>
    /// 与幻影魔镜一样**允许与灾厄本体那件同名件同时佩戴**（用户明确要求，故不做互斥）。
    /// 潜行恢复走 <see cref="CalamityDemutation.Utilities.CDUtil.AddStealthGen"/> 的反射桥
    /// （灾厄没有对应 Mod.Call，经典版也没有这两个字段 → 该加成现代版独占）；
    /// 闪避的"回潜行"走 <c>CDUtil.AddRogueStealthValue</c>。
    /// </para>
    /// <para>
    /// 配方照 2.0：幻影魔镜 + 墨炸弹 + 幽灵锭×8 + 海棱镜×10 + 深层细胞×5 + 流明素×5 @ 秘银砧。
    /// **两处按版本调整**（工程双版本惯例）：
    /// ① 幻影魔镜吃**本工程自写**的那件（链条在自己家里闭环，与斯塔提斯腰带吃自写件同理）；
    /// ② 经典版灾厄**没有墨炸弹**、且它的流明素叫 `Lumenite`，故经典分支去掉墨炸弹、流明素换 Lumenite
    ///（代价略低，已在 AGENTS 记录；若日后想补齐，可把墨炸弹也移植进来）。
    /// </para>
    /// </summary>
    internal class AbyssalMirror : ModItem
    {
        /// <summary>
        /// 基础属性：30×38、价值 48 金（= 灾厄 Rarity7/Lime 档价）、稀有度青柠、饰品
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 38;
            Item.value = Item.buyPrice(0, 48, 0, 0);
            Item.rare = ItemRarityID.Lime;
            Item.accessory = true;
        }
        /// <summary>
        /// 装备时置位 <c>abyssalMirror</c> 标记；数值、潜行恢复与闪避都在 CalamityDemutationPlayer 里按标记触发
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().abyssalMirror = true;
        }
        /// <summary>
        /// 配方：现代版照 2.0（含墨炸弹与流明素）；经典版去掉墨炸弹、流明素换经典版的 Lumenite。
        /// 材料对不上时写日志，不静默消失。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("InkBomb", out ModItem inkBomb) &&
                    calamity.TryFind<ModItem>("SeaPrism", out ModItem seaPrism) &&
                    calamity.TryFind<ModItem>("DepthCells", out ModItem depthCells) &&
                    calamity.TryFind<ModItem>("Lumenyl", out ModItem lumenyl))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<MirageMirror>();
                    recipe.AddIngredient(inkBomb.Type);
                    recipe.AddIngredient(ItemID.SpectreBar, 8);
                    recipe.AddIngredient(seaPrism.Type, 10);
                    recipe.AddIngredient(depthCells.Type, 5);
                    recipe.AddIngredient(lumenyl.Type, 5);
                    recipe.AddTile(TileID.MythrilAnvil);
                    recipe.Register();
                }
                else
                {
                    Mod.Logger.Warn("深渊魔镜：现代版灾厄里找不到 InkBomb / SeaPrism / DepthCells / Lumenyl，本条配方未注册。");
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if (classic.TryFind<ModItem>("SeaPrism", out ModItem seaPrismClassic) &&
                    classic.TryFind<ModItem>("DepthCells", out ModItem depthCellsClassic) &&
                    classic.TryFind<ModItem>("Lumenite", out ModItem lumenite))
                {
                    // 经典版没有墨炸弹（InkBomb 是现代版才有的饰品），故这一味去掉；
                    // 它的流明素叫 Lumenite（同源改名，见 TrueBiomeBlade 的经典分支同款口径）
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient<MirageMirror>();
                    recipeClassic.AddIngredient(ItemID.SpectreBar, 8);
                    recipeClassic.AddIngredient(seaPrismClassic.Type, 10);
                    recipeClassic.AddIngredient(depthCellsClassic.Type, 5);
                    recipeClassic.AddIngredient(lumenite.Type, 5);
                    recipeClassic.AddTile(TileID.MythrilAnvil);
                    recipeClassic.Register();
                }
                else
                {
                    Mod.Logger.Warn("深渊魔镜：经典版灾厄里找不到 SeaPrism / DepthCells / Lumenite，本条配方未注册。");
                }
            }
        }
    }
}
