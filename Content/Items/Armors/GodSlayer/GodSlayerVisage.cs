using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.GodSlayer
{
    /// <summary>
    /// 弑神者面甲（GodSlayerVisage） - 弑神者套装的法师向头部部件
    ///（按经典版灾厄 CalamityModClassicPreTrailer 同名件 1:1 移植；现代版已无此件，CI 对应件为
    /// GodSlayerHeadMagicold）。注意与同目录的 GodSlayerMask 区分——经典版里那颗是**盗贼**头，本工程无盗贼职业故不移植。
    /// 单件：魔法伤害 +14%、魔法暴击 +14%、最大法力 +100。
    /// 套装效果（逐条对应 player.setBonus 的说明文字，实现位置见括号）：
    /// 1. 致命伤保命并回复生命 / 45 秒冷却 / 冷却期 +10% 全伤害
    ///（godSlayer → CalamityDemutationPlayer.PreKill / PostUpdateMiscEffects）
    /// 2. 魔法攻击命中敌人时释放弑神者烈焰与治疗烈焰（godSlayerMage → CalamityDemutationGlobalProjectile.OnHitNPC：
    ///    GodSlayerOrb + GodSlayerHealOrb，节流预算与召唤侧的弑神幻影共用 godSlayerDmg）
    /// 3. 受到伤害时释放魔法弑神爆炸（godSlayerMage → CalamityDemutationPlayer.PostHurt：GodSlayerBlaze）
    /// 冲刺那两行属"让 tooltip 成真"的补充说明：godSlayer 已置位，弑神者冲刺确实可用。
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class GodSlayerVisage:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 75, 0, 0);  // 价值 75 金（与其余弑神者部件一致）
            Item.defense = 21;                        // 防御 21（经典版值，源码同行另留 //96 注释，系开发期遗留数字）
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
        /// 套装激活时的视觉表现：绘制盔甲残影（与经典版 GodSlayerVisage 一致）
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }
        /// <summary>
        /// 套装激活：置位 godSlayer（致命保护 + 冲刺闸门）与 godSlayerMage（烈焰/治疗球 + 受击爆炸），
        /// 并把本地化套装描述写入显示文本。
        /// 注意：**不置** godSlayerDamage（≤80 压制）与 godSlayerMelee——那两条是近战头专属；
        /// 也不置 godSlayerSummon（噬神蠕虫/幻影是召唤头专属）。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.godSlayer = true;
            modPlayer.godSlayerMage = true;
            // SetBonus 里的 {0} = 冲刺键显示名，{1} = 保命回复量（随数值膨胀开关切换，见 PreKill）
            player.setBonus = this.GetLocalization("SetBonus").Format(KeybindsSystem.GodslayerDashKeyDisplay, ConfigSystem.StatInflationEnabled ? 300 : 100);
        }
        /// <summary>
        /// 单件装备加成：魔法三连（伤害 / 暴击 / 法力），经典版 UpdateEquip 原样
        /// 另加「法力消耗 ×0.83」——这一条经典版没有、是 **CI 对应件 GodSlayerHeadMagicold** 的设计，
        /// 用户 2026-10-06 对 CI 照后点名补上。
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.GetDamage<MagicDamageClass>() += 0.14f;     // 魔法伤害 +14%
            player.GetCritChance<MagicDamageClass>() += 14;    // 魔法暴击率 +14%
            player.statManaMax2 += 100;                        // 最大法力 +100
            player.manaCost *= 0.83f;                          // 法力消耗 ×0.83（CI 的 GodSlayerHeadMagicold 原样）
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
