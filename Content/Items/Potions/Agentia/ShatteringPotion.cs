using CalamityDemutation.Content.Buffs.PositiveBuffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Potions.Agentia
{
    /// <summary>
    /// 碎甲药水（Shattering Potion）：破甲药水的强化版，饮用后获得 5 分钟（18000 帧）碎甲增益，
    /// 通用暴击率 +8%、通用伤害 +8%，并在命中时追加灾厄破甲减益。
    /// 效果结算见 ArmorShattering 与 CalamityDemutationPlayer。
    /// </summary>
    internal class ShatteringPotion:ModItem
    {
        /// <summary>
        /// 药水类消耗品：可食用，使用后附加 18000 帧（5 分钟）碎甲增益；堆叠上限为 30（低于普通药水的 999）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 18;
            Item.useTurn = true;
            Item.maxStack = 30;                                     // 堆叠上限 30（强化药水，低于常规 999）
            Item.rare = ItemRarityID.Orange;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.UseSound = SoundID.Item3;
            Item.consumable = true;
            Item.buffType = ModContent.BuffType<ArmorShattering>();   // 附加碎甲增益
            Item.buffTime = 18000;                                    // 增益持续 18000 帧（5 分钟）
            Item.value = Item.buyPrice(0, 2, 0, 0);
        }
        /// <summary>
        /// 合成表：用碎块药水（现代版需 2 瓶 / 经典版 1 瓶）升级，或药剂瓶 + 血珠×30 + 甲虫壳，均在炼金台
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：碎块药水×2 + 药剂瓶 + 甲虫壳
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient<CrumblingPotion>(2);
                recipe.AddIngredient(ItemID.BottledWater);
                recipe.AddIngredient(ItemID.BeetleHusk);
                recipe.AddTile(TileID.AlchemyTable);
                recipe.Register();
                // 现代版灾厄：药剂瓶 + 血珠×30 + 甲虫壳
                if(calamity.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb1))
                {
                    recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.BottledWater);
                    recipe.AddIngredient(bloodOrb1.Type, 30);
                    recipe.AddIngredient(ItemID.BeetleHusk);
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄：碎块药水×1 + 药剂瓶 + 甲虫壳
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient<CrumblingPotion>();
                recipe1.AddIngredient(ItemID.BottledWater);
                recipe1.AddIngredient(ItemID.BeetleHusk);
                recipe1.AddTile(TileID.AlchemyTable);
                recipe1.Register();
                // 经典版灾厄：药剂瓶 + 血珠×30 + 甲虫壳
                if(calamity1.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb2))
                {
                    recipe1 = CreateRecipe();
                    recipe1.AddIngredient(ItemID.BottledWater);
                    recipe1.AddIngredient(bloodOrb2.Type, 30);
                    recipe1.AddIngredient(ItemID.BeetleHusk);
                    recipe1.AddTile(TileID.AlchemyTable);
                    recipe1.Register();
                }
            }
        }
    }
}
