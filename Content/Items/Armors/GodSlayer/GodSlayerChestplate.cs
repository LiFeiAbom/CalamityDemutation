using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.GodSlayer
{
    /// <summary>
    /// 神裁者胸甲（GodSlayerChestplate） - 弑神者套胸部
    /// 单件：+15% 全伤害、+15% 暴击、+15% 移速、+250 生命上限、+150 法力上限、反伤 +0.9。
    /// 同时置位两个标记，均在 CalamityDemutationPlayer 中结算：
    /// godSlayerReflect 使 80 及以下伤害被压到 1，并有 1/20 概率完全免伤；
    /// godSlayerDamageProtect 使不超过当前保护上限（最高 80）的伤害被完全闪避，触发后上限重置为 20。
    /// </summary>
    [AutoloadEquip(EquipType.Body)]
    internal class GodSlayerChestplate:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、售价、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 60, 0, 0);  // 售价 60 金
            Item.defense = 41;                        // 防御 41
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;  // 月后稀有度 14（蓝色名）
        }
        /// <summary>
        /// 单件属性与标记置位；godSlayerReflect / godSlayerDamageProtect 由 CalamityDemutationPlayer 消费
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.godSlayerReflect = true;
            modPlayer.godSlayerDamageProtect = true;
            player.thorns += 0.9f;                          // 反伤倍率 +0.9（与头盔的 +2.5 叠加）
            player.statLifeMax2 += 250;                     // 生命上限 +250
            player.statManaMax2 += 150;                     // 法力上限 +150
            player.moveSpeed += 0.15f;                      // 移速 +15%
            player.GetDamage<GenericDamageClass>() += 0.15f;   // 全伤害 +15%
            player.GetCritChance<GenericDamageClass>() += 15;  // 全暴击 +15%
        }
        /// <summary>
        /// 注册配方：现代版与经典版灾厄材料不同，分别注册。
        /// 现代版用 CosmiliteBar(20) + AscendantSpiritEssence(4)，于 CosmicAnvil（宇宙砧）合成；
        /// 经典版用 CosmiliteBar(23) + NightmareFuel(11) + EndothermicEnergy(11)，于 DraedonsForge（德雷顿熔炉）合成。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(calamity.Find<ModItem>("CosmiliteBar").Type, 20);
                recipe.AddIngredient(calamity.Find<ModItem>("AscendantSpiritEssence").Type, 4);
                recipe.AddTile(calamity.Find<ModTile>("CosmicAnvil").Type);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(calamity1.Find<ModItem>("CosmiliteBar").Type, 23);
                recipe1.AddIngredient(calamity1.Find<ModItem>("NightmareFuel").Type, 11);
                recipe1.AddIngredient(calamity1.Find<ModItem>("EndothermicEnergy").Type, 11);
                recipe1.AddTile(calamity1.Find<ModTile>("DraedonsForge").Type);
                recipe1.Register();
            }
        }
    }
}
