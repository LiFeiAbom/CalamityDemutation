using CalamityDemutation.Content.Buffs.PositiveBuffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Potions.Agentia
{
    /// <summary>
    /// 渊洋秘药（Abyssal Elixir）：饮用后获得 3 分钟（10800 帧）渊洋之愿增益，
    /// 提供水下无限呼吸。效果结算见 AbyssalWish。
    /// </summary>
    internal class AbyssalElixir:ModItem
    {
        /// <summary>
        /// 药水类消耗品：可堆叠、可食用，使用后附加 10800 帧（3 分钟）渊洋之愿增益
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 18;
            Item.useTurn = true;
            Item.maxStack = 999;
            Item.rare = ItemRarityID.Orange;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.UseSound = SoundID.Item3;
            Item.consumable = true;
            Item.buffType = ModContent.BuffType<AbyssalWish>();   // 附加渊洋之愿增益
            Item.buffTime = 10800;                                // 增益持续 10800 帧（3 分钟）
            Item.value = Item.buyPrice(0, 2, 0, 0);
        }
        /// <summary>
        /// 合成表：两版灾厄各有一条专属材料配方，外加共通的药剂瓶 + 血珠×40 退路，均在炼金台。
        /// 专属材料取两版各自能拿到的海洋物：沉海旗鱼只有现代版灾厄才有（经典版没有沉海捕获物系列），
        /// 故经典版退回原版珊瑚 / 贝壳
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：药剂瓶 + 沉海旗鱼 + 珊瑚
                if(calamity.TryFind<ModItem>("SunkenSailfish", out ModItem sunkenSailfish1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.BottledWater);
                    recipe.AddIngredient(sunkenSailfish1.Type);
                    recipe.AddIngredient(ItemID.Coral);
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
                // 现代版灾厄：药剂瓶 + 血珠×40
                if(calamity.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.BottledWater);
                    recipe.AddIngredient(bloodOrb1.Type, 40);
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄：药剂瓶 + 珊瑚 + 贝壳
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(ItemID.BottledWater);
                recipe1.AddIngredient(ItemID.Coral);
                recipe1.AddIngredient(ItemID.Seashell);
                recipe1.AddTile(TileID.AlchemyTable);
                recipe1.Register();
                // 经典版灾厄：药剂瓶 + 血珠×40
                if(calamity1.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb2))
                {
                    recipe1 = CreateRecipe();
                    recipe1.AddIngredient(ItemID.BottledWater);
                    recipe1.AddIngredient(bloodOrb2.Type, 40);
                    recipe1.AddTile(TileID.AlchemyTable);
                    recipe1.Register();
                }
            }
        }
    }
}
