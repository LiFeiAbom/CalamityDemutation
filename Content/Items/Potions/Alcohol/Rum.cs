using CalamityDemutation.Content.Buffs.Alcohol;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Potions.Alcohol
{
    /// <summary>
    /// 朗姆酒（Rum）—— 酒类药水第四件。
    /// <para>
    /// 面板取 2.0.4：28×18、堆叠 9999、浅红档、8 分钟、价值 1 金 30 银、研究解锁 5。
    /// </para>
    /// <para>
    /// 效果取 2.0.4：生命再生 +1 HP/s（2 点 lifeRegen）、移动速度 +10%、
    /// 防御 −8（固定值，用户 2026-10-19 修正）、酒醉等级 +1。
    /// </para>
    /// <para>
    /// 配方（用户 2026-10-19）：瓶装水 + 飞翔之魂 + 羽毛 + 蜘蛛牙 @ 酒桶，产 1。
    /// </para>
    /// </summary>
    internal class Rum : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 5;
        }
        public override void SetDefaults()
        {
            Item.DefaultToFood(28, 18, ModContent.BuffType<RumBuff>(), 8 * 60 * 60, true);
            Item.useTurn = true;
            Item.maxStack = 9999;
            Item.rare = ItemRarityID.LightRed;
            Item.value = Item.buyPrice(0, 1, 30, 0);
        }
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.BottledWater)
                .AddIngredient(ItemID.SoulofFlight)
                .AddIngredient(ItemID.Feather)
                .AddIngredient(ItemID.SpiderFang)
                .AddTile(TileID.Kegs)
                .Register();
        }
    }
}
