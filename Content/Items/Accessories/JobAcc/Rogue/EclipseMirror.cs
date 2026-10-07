using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Rogue
{
    /// <summary>
    /// 蚀日魔镜（Eclipse Mirror）—— 潜行链条的最后一环（幻影 → 深渊 → **蚀日**）。
    /// 口径按用户 2026-10-07 拍板：
    /// <para>
    /// ① 本体取 **2.0**：**38×38** 大方镜、价值 **1 铂金 40 金**（`Rarity14BuyPrice`）、
    /// 月后稀有度 **14（蓝，对应灾厄的 DarkBlue）**。
    /// </para>
    /// <para>
    /// ② 基础效果按用户点名：盗贼伤害 **+11%**、盗贼暴击 **+11**（源为 +6%/+6）、**最大潜行值 +25 点**（源为 +20）；
    /// 其余照 2.0 全量：站定潜行恢复 **+20%**、移动潜行**指数加速**（`eclipseMirror` 旗标）、仇恨 **−700**、
    /// 潜行打击**半价**、**盗贼潜行打击必定暴击**。
    /// </para>
    /// <para>
    /// ③ 闪避走 **2.0**：回满潜行 + 固定 90 秒冷却 + 无伤害门槛
    ///（2.0.4 才改成按伤害 15–90 秒并加"低于最大生命 5% 不触发"）；与深渊魔镜**共用同一个**冷却字段。
    /// </para>
    /// <para>
    /// ④ 配方走 **1.4.4 那一侧**：本工程自家链条的**深渊魔镜**（它自己又吃自写的幻影魔镜）+ **暗物质剑鞘**
    /// + 暗日碎片 `DarksunFragment`×20；站台按工程惯例分版本——现代版宇宙砧（`CosmicAnvil`）、
    /// 经典版嘉登熔炉（`DraedonsForge`，经典版没有宇宙砧）。
    /// </para>
    /// <para>
    /// 效果分四处落地：盗贼伤害/暴击/仇恨写 <c>PostUpdateMiscEffects</c>；潜行四项（恢复/上限/半价/加速旗标）
    /// 写 <c>PostUpdateEquips</c>（灾厄在 ResetEffects 里每帧复位这些字段、之后才读取）；
    /// 闪避写 <c>FreeDodge</c>；"必暴击"写 <c>ModifyHitNPCWithProj</c>（<c>HitModifiers.SetCrit()</c>，
    /// 2.0 是在灾厄 CalamityGlobalProjectile.ModifyHitNPC 里把 <c>ref bool crit</c> 置真）。
    /// </para>
    /// <para>
    /// 与灾厄本体重名：本体仍有同名件（无必暴击、+10 潜行、红档 30×46），本件是"旧版回归 + 用户再平衡"的
    /// 同名不同物；按既有口径**不做互斥**。
    /// </para>
    /// </summary>
    internal class EclipseMirror : ModItem
    {
        /// <summary>
        /// 基础属性：38×38、价值 1 铂金 40 金（灾厄 2.0 的 `Rarity14BuyPrice`）、
        /// 基础稀有度红 + 月后稀有度 14（蓝）、饰品
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 38;
            Item.height = 38;
            Item.value = Item.buyPrice(1, 40, 0, 0);
            Item.rare = ItemRarityID.Red;   // 基础稀有度红色，名称颜色由 postMoonLordRarity 覆盖
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;   // 月后稀有度 14：蓝（＝灾厄 DarkBlue）
            Item.accessory = true;
        }
        /// <summary>
        /// 装备时置位 <c>eclipseMirror</c> 标记；数值 / 潜行 / 闪避 / 必暴击都在 CalamityDemutationPlayer 里按标记触发
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().eclipseMirror = true;
        }
        /// <summary>
        /// 配方：深渊魔镜（本模组）+ 暗物质剑鞘（本模组）+ 暗日碎片×20，现代/经典两分支各注册一条
        /// （现代宇宙砧、经典嘉登熔炉）；材料对不上时写日志，不静默消失。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("DarksunFragment", out ModItem darksunFragment) &&
                    calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<AbyssalMirror>();
                    recipe.AddIngredient<DarkMatterSheath>();
                    recipe.AddIngredient(darksunFragment.Type, 20);
                    recipe.AddTile(cosmicAnvil.Type);
                    recipe.Register();
                }
                else
                {
                    Mod.Logger.Warn("蚀日魔镜：现代版灾厄里找不到 DarksunFragment / CosmicAnvil，本条配方未注册。");
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if (classic.TryFind<ModItem>("DarksunFragment", out ModItem darksunFragmentClassic) &&
                    classic.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient<AbyssalMirror>();
                    recipeClassic.AddIngredient<DarkMatterSheath>();
                    recipeClassic.AddIngredient(darksunFragmentClassic.Type, 20);
                    recipeClassic.AddTile(draedonsForge.Type);
                    recipeClassic.Register();
                }
                else
                {
                    Mod.Logger.Warn("蚀日魔镜：经典版灾厄里找不到 DarksunFragment / DraedonsForge，本条配方未注册。");
                }
            }
        }
    }
}
