using CalamityDemutation.Content.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Movement
{
    /// <summary>
    /// 斯塔提斯忍者腰带（Statis' Ninja Belt） - 移动型饰品
    /// 提升跳跃速度并允许连续跳跃，可爬墙、冲刺并闪避攻击；
    /// 另提供 +5% 通用伤害与 +5% 通用暴击（源为盗贼加成，本工程无盗贼职业，改写为通用加成）。
    /// 是合成斯塔提斯诅咒腰带的前置材料。
    /// </summary>
    internal class StatisNinjaBelt:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 28;                         // 贴图宽 28 像素
            Item.height = 32;                        // 贴图高 32 像素
            Item.value = Item.buyPrice(0, 45, 0, 0); // 价值 45 金
            Item.rare = ItemRarityID.Cyan;           // 稀有度：青（Cyan）
            Item.accessory = true;                   // 标记为饰品，可装备于饰品栏
        }
        /// <summary>
        /// 装备时直接赋予机动性与增伤：自动跳跃与跳跃速度、额外坠落速度、闪避（黑腰带）、
        /// 冲刺、爬墙（尖刺靴），以及通用伤害 / 通用暴击各 +5%
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.autoJump = true;
            player.jumpSpeedBoost += 0.4f;
            player.extraFall += 35;
            player.blackBelt = true;
            player.dash = 1;          // 仅视觉字段，本身不授予冲刺
            player.dashType = 1;      // 1 = 忍者大师装备式冲刺，真正生效的是这一条
            player.spikedBoots = 2;
            player.GetDamage<GenericDamageClass>() += 0.05f;
            player.GetCritChance<GenericDamageClass>() += 5;
        }
        /// <summary>
        /// 配方：蛙腿 + 净化凝胶 ×50 + 极寒核心 + 大师忍者装备，在秘银砧合成。
        /// 净化凝胶取自灾厄（现代版与经典版同名），需分别注册
        /// </summary>
        public override void AddRecipes()
        {
            // 兼容灾厄现代版与经典版：净化凝胶同名，分别注册避免重复
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("PurifiedGel", out ModItem purifiedGel1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.FrogLeg);
                    recipe.AddIngredient(purifiedGel1.Type, 50);
                    recipe.AddIngredient<CoreofEleum>();
                    recipe.AddIngredient(ItemID.MasterNinjaGear);
                    recipe.AddTile(TileID.MythrilAnvil);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModItem>("PurifiedGel", out ModItem purifiedGel2))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(ItemID.FrogLeg);
                    recipe1.AddIngredient(purifiedGel2.Type, 50);
                    recipe1.AddIngredient<CoreofEleum>();
                    recipe1.AddIngredient(ItemID.MasterNinjaGear);
                    recipe1.AddTile(TileID.MythrilAnvil);
                    recipe1.Register();
                }
            }
        }
    }
}
