using CalamityDemutation.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Comprehensive
{
    /// <summary>
    /// 暗日之戒（DarkSunRing） - 综合型饰品
    /// 提供 +2 仆从上限、+10 防御、+1 HP/s 生命再生、+12% 通用伤害、+12% 近战攻速、
    /// +5% 暴击、+15% 挖掘速度、仆从击退；白天额外 +3 HP/s 生命再生，夜晚额外 +30 防御。
    /// </summary>
    internal class DarkSunRing:ModItem
    {
        /// <summary>
        /// 注册贴图动画：竖直逐帧滚动，营造元素之魂般的动画效果
        /// </summary>
        public override void SetStaticDefaults()
        {
            // 逐帧竖直滚动贴图，营造元素之魂般的动画效果
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(6, 6));
            ItemID.Sets.AnimatesAsSoul[Type] = true;
        }
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御、生命回复与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 26;                          // 贴图宽（像素）
            Item.height = 26;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 90, 0, 0);  // 价值 90 金
            Item.defense = 10;                        // 装备时 +10 防御
            Item.lifeRegen = 2;                       // 装备时 +2 生命回复
            Item.accessory = true;                    // 标记为饰品，可装备于饰品栏
            // 使用模组自定义的月后稀有度等级
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15;
        }
        /// <summary>
        /// 装备时置位标记，具体数值在 CalamityDemutationPlayer 中统一结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 仅置位装备标记，实际数值在 CalamityDemutationPlayer.PostUpdateMiscEffects 中统一结算
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.darkSunRing = true;
        }
        /// <summary>
        /// 配方（分版本，均在秘银砧合成）：现代版用 UelibloomBar + DarksunFragment，
        /// 经典版用 UeliaceBar + DarksunFragment；两版灾厄材料不同，分别注册配方
        /// </summary>
        public override void AddRecipes()
        {
            // 分别适配现代版与经典版灾厄的合成配方
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("UelibloomBar", out ModItem uelibloomBar) && calamity.TryFind<ModItem>("DarksunFragment", out ModItem darksunFragment1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(uelibloomBar.Type, 10);
                    recipe.AddIngredient(darksunFragment1.Type, 100);
                    recipe.AddTile(TileID.MythrilAnvil);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModItem>("UeliaceBar", out ModItem ueliaceBar) && calamity1.TryFind<ModItem>("DarksunFragment", out ModItem darksunFragment2))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(ueliaceBar.Type, 10);
                    recipe1.AddIngredient(darksunFragment2.Type, 100);
                    recipe1.AddTile(TileID.MythrilAnvil);
                    recipe1.Register();
                }
            }
        }
    }
}
