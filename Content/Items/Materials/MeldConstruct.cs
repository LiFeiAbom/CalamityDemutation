using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Materials
{
    /// <summary>
    /// 熵构体（Meld Construct）—— 「超新星」下位链的第四件，也是**毁灭之星的前件**。
    /// <para>
    /// 为什么要自持：它在灾厄 **1.4.4-release 起被删除**（本机实装 2.2.2 的 `.tmod` 文件表里一条都没有），
    /// 而 2.0 / 2.0.3.9 / 2.0.4 / 2.0.7.2 都还在；毁灭之星 2.0 的配方要它 ×10。
    /// 用户 2026-10-08 授权按"方案 B"处理：**连它一起搬进来**，于是毁灭之星可以照 2.0 原配方一字不改。
    /// </para>
    /// <para>
    /// 口径取 **2.0**：15×12 判定（贴图 38×40）、堆叠 999、售出价 1 金 20 银、青档、研究 25
    /// （2.0.3.9 起堆叠抬到 9999、2.0.4 改成红档并把配方里的星尘换成星疫煤烟——本件都不取）。
    /// </para>
    /// <para>
    /// 配方照 2.0：**熔凝团 <c>MeldBlob</c>×6 + 星尘 <c>Stardust</c>×3 → 熵构体×3 @ 远古操纵机**
    /// （原版站台）。这两味材料**现代版与经典版灾厄都有**，所以只注册一条配方、两个分支通用
    /// （先现代后经典；都取不到就写警告，不静默消失）。
    /// </para>
    /// </summary>
    internal class MeldConstruct : ModItem
    {
        /// <summary>研究解锁 25 个（照源 2.0 的 SacrificeTotal）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 25;
        }
        /// <summary>基础属性：15×12、堆叠 999、售出价 1 金 20 银、青档（照源 2.0）</summary>
        public override void SetDefaults()
        {
            Item.width = 15;
            Item.height = 12;
            Item.maxStack = 999;
            Item.value = Item.sellPrice(gold: 1, silver: 20);
            Item.rare = ItemRarityID.Cyan;
        }
        /// <summary>配方照 2.0：熔凝团×6 + 星尘×3 → ×3 @ 远古操纵机（两版同名 → 只注册一条）</summary>
        public override void AddRecipes()
        {
            if (TryAddRecipeFrom("CalamityMod") || TryAddRecipeFrom("CalamityModClassicPreTrailer"))
                return;
            Mod.Logger.Warn("熵构体：两版灾厄都找不到 MeldBlob / Stardust，配方未注册。");
        }
        /// <summary>从指定灾厄版本取两味材料注册一条配方；缺任何一件即返回 false（不落半条配方）</summary>
        private bool TryAddRecipeFrom(string calamityModName)
        {
            if (!ModLoader.TryGetMod(calamityModName, out Mod calamity))
                return false;
            if (!calamity.TryFind<ModItem>("MeldBlob", out ModItem meldBlob) ||
                !calamity.TryFind<ModItem>("Stardust", out ModItem stardust))
                return false;
            Recipe recipe = CreateRecipe(3);
            recipe.AddIngredient(meldBlob.Type, 6);
            recipe.AddIngredient(stardust.Type, 3);
            recipe.AddTile(TileID.LunarCraftingStation);
            recipe.Register();
            return true;
        }
    }
}
