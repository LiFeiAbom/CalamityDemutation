using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.GodSlayer
{
    /// <summary>
    /// 弑神者角盔（GodSlayerHornedHelm） - 弑神者套装的召唤向头部部件
    ///（按经典版灾厄同名件 1:1 移植；现代版对应 GodSlayerHornedHelm，CI 则把它作为"合并头"的一项）。
    /// 单件：仆从上限 +3，另有用户 2026-10-05 指定的召唤头统一三条属性
    ///（召唤伤害 +12%、鞭子攻击范围 +12%、鞭子攻击速度 +12%）。
    /// 套装效果（逐条对应 player.setBonus 的说明文字，实现位置见括号）：
    /// 1. 召唤伤害 +65%（本类 UpdateArmorSet 内直接加，经典版原样）
    /// 2. 致命伤保命并回复生命 / 45 秒冷却 / 冷却期 +10% 全伤害（godSlayer → CalamityDemutationPlayer.PreKill）
    /// 3. 命中敌人时召唤弑神幻影（godSlayerSummon → CalamityDemutationGlobalProjectile.OnHitNPC 的召唤分支）
    /// 4. 召唤一条噬神机械蠕虫（godSlayerSummon → CalamityDemutationPlayer.UpdateGodSlayerMechworm，每帧维护）
    /// 冲刺那两行属"让 tooltip 成真"的补充说明：godSlayer 已置位，弑神者冲刺确实可用。
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class GodSlayerHornedHelm:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 75, 0, 0);  // 价值 75 金（与其余弑神者部件一致）
            Item.defense = 12;                        // 防御 12（经典版值，源码同行另留 //96 注释，系开发期遗留数字）
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;  // 月后自定义稀有度 14 级
        }
        /// <summary>
        /// 判定是否集齐弑神者套三件（胸甲 + 护腿）
        /// </summary>
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<GodSlayerChestplate>() && legs.type == ModContent.ItemType<GodSlayerLeggings>();
        }
        /// <summary>
        /// 套装激活时的视觉表现：绘制盔甲残影（与经典版 GodSlayerHornedHelm 一致）
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }
        /// <summary>
        /// 套装激活：置位 godSlayer（致命保护 + 冲刺闸门）与 godSlayerSummon（幻影 + 机械蠕虫），
        /// 给 +65% 召唤伤害，并把本地化套装描述写入显示文本。
        /// 注意：**不置** godSlayerMelee（弑神飞镖）与 godSlayerDamage（≤80 压制）——经典版召唤头就没有这两项。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.godSlayer = true;
            modPlayer.godSlayerSummon = true;
            player.GetDamage<SummonDamageClass>() += 0.65f;  // 召唤伤害 +65%（经典版 UpdateArmorSet 原样）
            // SetBonus 里的 {0} = 冲刺键显示名，{1} = 保命回复量（随数值膨胀开关切换，见 PreKill）
            player.setBonus = this.GetLocalization("SetBonus").Format(KeybindsSystem.GodslayerDashKeyDisplay, ConfigSystem.StatInflationEnabled ? 300 : 100);
        }
        /// <summary>
        /// 单件装备加成：仆从上限（经典版 UpdateEquip 原样），外加用户指定的三条召唤头统一属性
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.maxMinions += 3;                           // 仆从上限 +3
            player.GetDamage<SummonDamageClass>() += 0.12f;   // 召唤伤害 +12%（用户指定的额外单件属性）
            player.whipRangeMultiplier += 0.12f;              // 鞭子攻击范围 +12%（同上）
            player.GetAttackSpeed<SummonMeleeSpeedDamageClass>() += 0.12f;  // 鞭子攻击速度 +12%（同上）
        }
        /// <summary>
        /// 注册配方：现代版与经典版灾厄材料不同，各注册一条（与既有近战/射手头同规矩）
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：CosmiliteBar×10 + AscendantSpiritEssence×2 @ 宇宙砧
                if (calamity.TryFind<ModItem>("CosmiliteBar", out ModItem cosmiliteBar)
                    && calamity.TryFind<ModItem>("AscendantSpiritEssence", out ModItem ascendantSpiritEssence)
                    && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(cosmiliteBar.Type, 10);
                    recipe.AddIngredient(ascendantSpiritEssence.Type, 2);
                    recipe.AddTile(cosmicAnvil.Type);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                // 经典版灾厄：CosmiliteBar×14 + NightmareFuel×8 + EndothermicEnergy×8 @ 德雷顿熔炉
                if (classic.TryFind<ModItem>("CosmiliteBar", out ModItem classicCosmiliteBar)
                    && classic.TryFind<ModItem>("NightmareFuel", out ModItem nightmareFuel)
                    && classic.TryFind<ModItem>("EndothermicEnergy", out ModItem endothermicEnergy)
                    && classic.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient(classicCosmiliteBar.Type, 14);
                    recipeClassic.AddIngredient(nightmareFuel.Type, 8);
                    recipeClassic.AddIngredient(endothermicEnergy.Type, 8);
                    recipeClassic.AddTile(draedonsForge.Type);
                    recipeClassic.Register();
                }
            }
        }
    }
}
