using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Summon
{
    /// <summary>
    /// 凝滞诅咒 - 召唤职业饰品
    /// 在凝滞祝福的基础上，额外提供 +10% 鞭范围、+10% 鞭攻速，
    /// 召唤弹幕命中时施加"时间之殇"与暗影焰（ShadowFlame）debuff。
    /// 凝滞系列中间件：由凝滞祝福进阶而来，又是合成诅咒凝滞腰带的材料。
    /// </summary>
    internal class StatisCurse:ModItem
    {
        /// <summary>
        /// 物品基础属性：贴图尺寸、价值、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 28;                         // 贴图宽 28 像素
            Item.height = 32;                        // 贴图高 32 像素
            Item.value = Item.buyPrice(0, 60, 0, 0); // 价值 60 金
            Item.rare = ItemRarityID.Red;            // 稀有度：红（Red）
            Item.accessory = true;                   // 标记为饰品，可装备于饰品栏
        }
        /// <summary>
        /// 装备时置位 statisCurse 标记，数值结算与"时间之殇"+暗影焰 debuff 施加在 CalamityDemutationPlayer 中统一完成
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 装备时仅置位标记，数值结算与 debuff 施加统一在 CalamityDemutationPlayer 中完成
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.statisCurse = true;
        }
        /// <summary>
        /// 配方：以凝滞祝福与第一暗影焰为核心材料（不涉及灾厄材料），任一版本灾厄加载时注册一次
        /// </summary>
        public override void AddRecipes()
        {
            // 配方不涉及灾厄材料，两版写法完全一致；用 || 合并，避免同时装两版时重复注册
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) || ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient<StatisBlessing>();
                recipe.AddIngredient<TheFirstShadowflame>();
                recipe.AddIngredient(ItemID.FragmentStardust, 10);
                recipe.AddTile(TileID.LunarCraftingStation);
                recipe.Register();
            }
        }
    }
}
