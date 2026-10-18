using CalamityDemutation.Content.Buffs.Alcohol;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Potions.Alcohol
{
    /// <summary>
    /// 血腥玛丽（Bloody Mary）—— 酒类药水门类首件。
    /// <para>
    /// 配方按用户 2026-10-19 修改为工程药剂口径的双配方：
    /// 瓶装水×1 + 血泪（血月召唤物）×1，或瓶装水×1 + 血珠×50，均 @ 炼药桌，各产 1 瓶；
    /// 物品形态（20×32、蓝档、6 分钟、饮用音/动画）也取 2.1，
    /// 价值按用户 2026-10-18 指定取 2.0.4 的 2 金 60 银。
    /// </para>
    /// <para>
    /// 效果为用户 2026-10-18 修正后的数值：+15% 通用伤害、+15% 近战攻速、
    /// +15% 移速、-5 防御、酒醉等级 +1、-4 HP/s；血月时刷怪率与最大刷怪量 ×10。
    /// </para>
    /// </summary>
    internal class BloodyMary : ModItem
    {
        /// <summary>2.0.4 口径的研究解锁数</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 5;
            ItemID.Sets.DrinkParticleColors[Type] = new Color[3]
            {
                new Color(204, 90, 73),
                new Color(168, 37, 37),
                new Color(120, 28, 28)
            };
        }
        /// <summary>
        /// 2.1 的食物/饮品默认值：20×32、堆叠上限、蓝档、饮用音、6 分钟增益；
        /// 随后把价值覆盖为 2.0.4 的 2 金 60 银。
        /// </summary>
        public override void SetDefaults()
        {
            Item.DefaultToFood(20, 32, ModContent.BuffType<BloodyMaryBuff>(), 6 * 60 * 60, true);
            Item.value = Item.buyPrice(0, 2, 60, 0);
        }
        /// <summary>
        /// 双配方（各产 1）：瓶装水 + 血泪；瓶装水 + 血珠×50。
        /// 血珠按现代 / 经典灾厄分别软依赖，站台按工程其它药剂统一用炼药桌。
        /// </summary>
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.BottledWater)
                .AddIngredient(ItemID.BloodMoonStarter)
                .AddTile(TileID.AlchemyTable)
                .Register();

            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity)
                && calamity.TryFind<ModItem>("BloodOrb", out ModItem modernBloodOrb))
            {
                CreateRecipe()
                    .AddIngredient(ItemID.BottledWater)
                    .AddIngredient(modernBloodOrb.Type, 50)
                    .AddTile(TileID.AlchemyTable)
                    .Register();
            }

            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic)
                && classic.TryFind<ModItem>("BloodOrb", out ModItem classicBloodOrb))
            {
                CreateRecipe()
                    .AddIngredient(ItemID.BottledWater)
                    .AddIngredient(classicBloodOrb.Type, 50)
                    .AddTile(TileID.AlchemyTable)
                    .Register();
            }
        }
    }
}
