using CalamityDemutation.Content.Buffs.PositiveBuffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Potions.Agentia
{
    /// <summary>
    /// 光合作用药水（Photosynthesis Potion）：饮用后获得 5 分钟（18000 帧）光合作用增益，
    /// 静止不动时大幅提升生命回复（白天全额、夜晚 1/5）。效果结算见 Photosynthesis 与 CalamityDemutationPlayer。
    /// </summary>
    internal class PhotosynthesisPotion:ModItem
    {
        /// <summary>
        /// 药水类消耗品：可堆叠、可食用，使用后附加 18000 帧（5 分钟）光合作用增益
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 28;                                         // 贴图宽（像素）
            Item.height = 18;                                        // 贴图高（像素）
            Item.useTurn = true;                                     // 使用时允许转向
            Item.maxStack = 999;                                     // 最大堆叠
            Item.rare = ItemRarityID.Orange;                         // 稀有度：橙色
            Item.useAnimation = 17;                                  // 动画时长 17 帧
            Item.useTime = 17;                                       // 使用间隔 17 帧
            Item.useStyle = ItemUseStyleID.EatFood;                  // 使用样式：进食
            Item.UseSound = SoundID.Item3;                           // 饮用音效
            Item.consumable = true;                                  // 一次性消耗品
            Item.buffType = ModContent.BuffType<Photosynthesis>();   // 附加光合作用增益
            Item.buffTime = 18000;                                   // 增益持续 18000 帧（5 分钟）
            Item.value = Item.buyPrice(0, 2, 0, 0);                  // 价值 2 金
        }
        /// <summary>
        /// 合成表：两分支配方材料差异较大——现代版为发光药水 + 再生药水 + 日光精华，
        /// 经典版为药剂瓶 + BeetleJuice×2 + ManeaterBulb + TrapperBulb + EssenceofCinder；
        /// 各自另有药剂瓶 + 血珠×40 的备选配方（均在炼金台）
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：发光药水 + 再生药水 + EssenceofSunlight
                if(calamity.TryFind<ModItem>("EssenceofSunlight", out ModItem essenceofSunlight1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.ShinePotion);
                    recipe.AddIngredient(ItemID.RegenerationPotion);
                    recipe.AddIngredient(essenceofSunlight1.Type);
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
                // 经典版灾厄：药剂瓶 + BeetleJuice×2 + ManeaterBulb + TrapperBulb + EssenceofCinder
                if(calamity1.TryFind<ModItem>("BeetleJuice", out ModItem beetleJuice1) && calamity1.TryFind<ModItem>("ManeaterBulb", out ModItem maneaterBulb1) && calamity1.TryFind<ModItem>("TrapperBulb", out ModItem trapperBulb1) && calamity1.TryFind<ModItem>("EssenceofCinder", out ModItem essenceofCinder1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.BottledWater);
                    recipe.AddIngredient(beetleJuice1.Type, 2);
                    recipe.AddIngredient(maneaterBulb1.Type);
                    recipe.AddIngredient(trapperBulb1.Type);
                    recipe.AddIngredient(essenceofCinder1.Type);
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
                // 经典版灾厄：药剂瓶 + 血珠×40
                if(calamity1.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb2))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.BottledWater);
                    recipe.AddIngredient(bloodOrb2.Type, 40);
                    recipe.AddTile(TileID.AlchemyTable);
                    recipe.Register();
                }
            }
        }
    }
}
