using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Rogue
{
    /// <summary>
    /// 欺诈硬币（Coin of Deceit）—— "暗物质剑鞘"那条链的下位材料
    /// （欺诈硬币 → 毁灭徽章 → 暗物质剑鞘 → 日蚀魔镜），前期铁砧档。
    /// <para>
    /// ① 效果取 **2.0**：20×22、蓝档、1 金；潜行打击只消耗潜行上限的 **75%**
    /// （2.0.3.9 / 2.0.4 改成 85%、1.4.4-release 改成 90%——本件刻意保留 2.0 的 75%）；
    /// 盗贼基础属性按用户 2026-10-07 指定：**+3% 盗贼伤害 / +3 盗贼暴击**
    /// （源为 +6 盗贼暴击，拆一半到伤害上）。
    /// </para>
    /// <para>
    /// ② 配方取 **1.4.4 / 2.0.3.9+ 那一侧**（四版里唯一**全原版材料**的一条）：
    /// 任意铜锭×12 + 任意魔金锭×8 @ 铁砧。灾厄 2.0 的配方要 金锭×4 + 铜锭×8 + 酸木×5
    /// （酸木是灾厄材料，会把这件锁死在现代分支），而本件刻意要**让整条链在经典分支也能合成**，
    /// 故不用 2.0 配方、改用全原版的那条。源配方用的是灾厄自建的 <c>AnyCopperBar</c> / <c>AnyEvilBar</c>
    /// 两个组（内容 = 铜锭/锡锭、魔金锭/血金锭，全是原版物品），本件不依赖灾厄，改用本模组自建的同内容组
    /// <c>CalamityDemutation:AnyCopperBar</c> / <c>CalamityDemutation:AnyEvilBar</c>（见 RecipeSystem）。
    /// </para>
    /// <para>
    /// 效果分两处落地：盗贼伤害/暴击写在 <c>CalamityDemutationPlayer.PostUpdateMiscEffects</c>
    /// （现代版加成加在灾厄的盗贼类上、经典版反射写它自己的自定义投掷字段）；"潜行打击消耗 75%"
    /// 是灾厄 CalamityPlayer 上的开关，必须写在 <c>PostUpdateEquips</c>（灾厄在 ResetEffects 里每帧复位、
    /// 攻击结算时才读取），由 CDUtil 反射写入。
    /// </para>
    /// <para>
    /// 与灾厄本体重名：本体（2.2.2）仍有同名件（现在给的是 90% 档），本件是"旧版回归"的同名不同物；
    /// 按用户既有口径，**不做互斥**。经典版灾厄没有这件，本件是它在经典分支的唯一来源。
    /// </para>
    /// </summary>
    internal class CoinofDeceit : ModItem
    {
        /// <summary>
        /// 基础属性：20×22、价值 1 金（灾厄蓝档价）、稀有度蓝、饰品
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 22;
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.rare = ItemRarityID.Blue;
            Item.accessory = true;
        }
        /// <summary>
        /// 装备时置位 <c>coinofDeceit</c> 标记；数值在 CalamityDemutationPlayer 里按标记触发
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().coinofDeceit = true;
        }
        /// <summary>
        /// 配方：任意铜锭×12 + 任意魔金锭×8 @ 铁砧（全原版材料、不依赖灾厄；四版里取全原版的那条）
        /// </summary>
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddRecipeGroup("CalamityDemutation:AnyCopperBar", 12);
            recipe.AddRecipeGroup("CalamityDemutation:AnyEvilBar", 8);
            recipe.AddTile(TileID.Anvils);
            recipe.Register();
        }
    }
}
