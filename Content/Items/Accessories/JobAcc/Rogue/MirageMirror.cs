using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Rogue
{
    /// <summary>
    /// 幻影魔镜（Mirage Mirror）—— 盗贼潜行流链条的第一件（幻影 → 深渊 → 日蚀），前期工匠作坊档。
    /// 口径按用户 2026-10-07 拍板取灾厄 **2.0** 版：30×30、橙档、4 金；站定潜行恢复 **+30%**、
    /// 移动 **+20%**、仇恨 **−200**（2.0.3.9 起被本体削到 +25%/+12%，本件刻意保留旧数值）。
    /// 另按用户点名追加基础属性：**最大潜行值 +15%（相对当前上限，走 CDUtil.GrantRogueStealthRatio，
    /// 不是平铺 +15 点）**、盗贼伤害 +2%、盗贼暴击 +2。
    /// <para>
    /// 效果分两处落地（相位要求不同，不要合并）：潜行相关的两条（恢复速度 + 上限）必须写在
    /// <c>CalamityDemutationPlayer.PostUpdateEquips</c> —— 灾厄在它自己的 ResetEffects 里每帧复位这些字段、
    /// 在 PostUpdateMiscEffects 之后才读取；伤害/暴击/仇恨写在 <c>PostUpdateMiscEffects</c> 的 mirageMirror 块
    /// （与本工程其它盗贼饰品同处）。
    /// </para>
    /// <para>
    /// **允许与灾厄本体的同名件同时佩戴**（用户明确要求，两件的潜行恢复会叠加，是有意为之），故本件不做
    /// <c>CanEquipAccessory</c> 互斥。潜行恢复那两个字段是灾厄 <c>CalamityPlayer</c> 的字段、Mod.Call 里没有
    /// 对应项，故走 CDUtil 的反射桥；经典版灾厄没有这两个字段，潜行恢复加成只对现代版生效。
    /// </para>
    /// </summary>
    internal class MirageMirror : ModItem
    {
        /// <summary>
        /// 基础属性：30×30、价值 4 金（= 灾厄 Rarity3/Orange 档价）、稀有度橙、饰品
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.value = Item.buyPrice(0, 4, 0, 0);
            Item.rare = ItemRarityID.Orange;
            Item.accessory = true;
        }
        /// <summary>
        /// 装备时置位 <c>mirageMirror</c> 标记；数值与反射桥调用分别在
        /// CalamityDemutationPlayer 的 PostUpdateMiscEffects / PostUpdateEquips 里按标记触发
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().mirageMirror = true;
        }
        /// <summary>
        /// 配方：照 2.0（四版完全一致，且全是原版材料）——魔法镜 **或** 冰雪镜 + 黑透镜 + 骨×50 @ 工匠作坊
        /// </summary>
        public override void AddRecipes()
        {
            Recipe recipeMagic = CreateRecipe();
            recipeMagic.AddIngredient(ItemID.MagicMirror);
            recipeMagic.AddIngredient(ItemID.BlackLens);
            recipeMagic.AddIngredient(ItemID.Bone, 50);
            recipeMagic.AddTile(TileID.TinkerersWorkbench);
            recipeMagic.Register();

            Recipe recipeIce = CreateRecipe();
            recipeIce.AddIngredient(ItemID.IceMirror);
            recipeIce.AddIngredient(ItemID.BlackLens);
            recipeIce.AddIngredient(ItemID.Bone, 50);
            recipeIce.AddTile(TileID.TinkerersWorkbench);
            recipeIce.Register();
        }
    }
}
