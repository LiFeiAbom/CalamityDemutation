using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.GodSlayer
{
    /// <summary>
    /// 弑神者面具（GodSlayerMask） - 弑神者套装的**盗贼**向头部部件
    ///（按经典版灾厄 CalamityModClassicPreTrailer 同名件 1:1 移植；现代版对应 GodSlayerHeadRogue，
    /// CI 对应件 GodSlayerHeadRogueold）。
    /// 注意与同目录另外三颗区分：<see cref="GodSlayerHelm"/> 近战 / <see cref="GodSlayerHelmet"/> 射手 /
    /// <see cref="GodSlayerVisage"/> 法师 / <see cref="GodSlayerHornedHelm"/> 召唤，
    /// 经典版里这颗 <c>GodSlayerMask</c> 才是盗贼头。
    /// 单件：盗贼伤害 +14%、盗贼暴击 +14%。
    /// 套装效果（逐条对应 player.setBonus 的说明文字，实现位置见括号）：
    /// 1. 致命伤保命并回复生命 / 45 秒冷却 / 冷却期 +10% 全伤害
    ///（godSlayer → CalamityDemutationPlayer.PreKill / PostUpdateMiscEffects）
    /// 2. 满生命时所有盗贼属性 +10%（伤害 / 暴击 / 弹速）
    ///（godSlayerThrowing → PostUpdateMiscEffects）
    /// 3. 单次受到超过 80 点伤害时获得额外无敌帧（godSlayerThrowing → PostHurt，源 +30 帧）
    /// 4. 潜行上限 140（套装方法里经 <see cref="CDUtil.GrantRogueStealth"/> 补给灾厄侧）
    /// 冲刺那两行属"让 tooltip 成真"的补充说明：godSlayer 已置位，弑神者冲刺确实可用。
    /// 注意：**不置** godSlayerDamage（≤80 压制）——那一条是近战头专属。
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class GodSlayerMask:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 75, 0, 0);  // 价值 75 金（与其余弑神者部件一致）
            Item.defense = 29;                        // 防御 29（经典版值，源码同行另留 //96 注释，系开发期遗留数字）
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
        /// 套装激活时的视觉表现：绘制盔甲残影（与经典版 GodSlayerMask 一致）
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }
        /// <summary>
        /// 套装激活：置位 godSlayer（致命保护 + 冲刺闸门）与 godSlayerThrowing（盗贼两条），
        /// 把潜行上限 140 补给灾厄侧，并把本地化套装描述写入显示文本。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.godSlayer = true;
            modPlayer.godSlayerThrowing = true;
            // SetBonus 里的 {0} = 冲刺键显示名，{1} = 保命回复量（随数值膨胀开关切换，见 PreKill）
            player.setBonus = this.GetLocalization("SetBonus").Format(KeybindsSystem.GodslayerDashKeyDisplay, ConfigSystem.StatInflationEnabled ? 300 : 100);
            CDUtil.GrantRogueStealth(player, 1.4f);   // 潜行上限 140（源 rogueStealthMax = 1.4f；内部值 1f = 显示 100 点）
        }
        /// <summary>
        /// 单件装备加成：盗贼伤害 / 盗贼暴击各 +14（经典版 UpdateEquip 原样；
        /// 源里写的是 <c>CalamityCustomThrowingDamagePlayer.throwingDamage += 0.14f / throwingCrit += 14</c>）。
        /// 现代版加在真·盗贼伤害类上；经典版没有盗贼 DamageClass，另走反射桥。
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            DamageClass rogue = CDUtil.GetRogueDamageClass();  // 现代版 = CalamityMod/RogueDamageClass；拿不到才退到 tML 的 Throwing
            player.GetDamage(rogue) += 0.14f;                  // 盗贼伤害 +14%
            player.GetCritChance(rogue) += 14;                 // 盗贼暴击率 +14%
            CDUtil.AddClassicThrowingStats(player, 0.14f, 14); // 经典版：写进它的自定义投掷字段（反射）
        }
        /// <summary>
        /// 注册配方：现代版与经典版灾厄材料不同，各注册一条（与既有弑神者头同规矩）
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
