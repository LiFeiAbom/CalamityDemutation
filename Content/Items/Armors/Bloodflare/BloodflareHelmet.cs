using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Bloodflare
{
    /// <summary>
    /// 血炎狂龙盔（BloodflareHelmet） - 血炎套装的召唤向头部部件
    ///（按经典版灾厄同名件 1:1 移植，对应现代版 BloodflareHeadSummon）。
    /// 单件：仆从上限 +3、岩浆免疫时长 +240、水中自由移动；
    /// 另有用户 2026-10-05 指定的三条额外单件属性（召唤头的统一口径）：
    /// 召唤伤害 +11%、鞭子攻击范围 +11%、鞭子攻击速度 +11%。
    /// 套装效果（逐条对应 player.setBonus 的说明文字，实现位置见括号）：
    /// 1. 召唤伤害 +55%（本类 UpdateArmorSet 内直接加，经典版原样）
    /// 2. 极大幅提升生命再生（player.crimsonRegen）
    /// 3. 生命低于 50% 的敌人被击中时有几率掉红心、高于 50% 时掉魔力星（bloodflareSet → CalamityDemutationGlobalNPC）
    /// 4. 血月期间被击杀的敌人更易掉血珠（bloodflareSet → CalamityDemutationGlobalNPC.OnKill）
    /// 5. 召唤波尔特加斯特地雷环绕自身（bloodflareSummon → PostUpdateMiscEffects，每 900 帧 3 枚 GhostlyMine）
    /// 6. 生命 ≥90% 时 +10% 召唤伤害；≤50% 时 +20 防御与 +2 生命再生（bloodflareSummon → PostUpdateMiscEffects）
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class BloodflareHelmet:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 60, 0, 0);  // 价值 60 金（与其余血炎部件一致）
            Item.defense = 16;                        // 防御 16（经典版值，源码同行另留 //85 注释，系开发期遗留数字）
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 13;  // 月后自定义稀有度 13 级
        }
        /// <summary>
        /// 判定是否集齐血炎套三件（胸甲 + 护腿）
        /// </summary>
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<BloodflareBodyArmor>() && legs.type == ModContent.ItemType<BloodflareCuisses>();
        }
        /// <summary>
        /// 套装激活时的视觉表现：绘制细微残影（与经典版 BloodflareHelmet 一致；职业头都不画轮廓线）
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadowSubtle = true;
        }
        /// <summary>
        /// 套装激活：置位 bloodflareSet 与 bloodflareSummon 标记，给 +55% 召唤伤害与猩红回血，
        /// 并把本地化套装描述（hjson 的 SetBonus 键）写入显示文本。
        /// bloodflareSet 与近战/射手头共用（红心与魔力星掉落、血月血珠）；bloodflareSummon 单独驱动
        /// 生命阈值加成与环绕地雷。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.bloodflareSet = true;
            modPlayer.bloodflareSummon = true;
            player.GetDamage<SummonDamageClass>() += 0.55f;  // 召唤伤害 +55%（经典版 UpdateArmorSet 原样）
            player.crimsonRegen = true;                      // 猩红回血：极大幅提升生命再生
            player.setBonus = this.GetLocalizedValue("SetBonus");
        }
        /// <summary>
        /// 单件装备加成：仆从上限、岩浆与水下生存（经典版 UpdateEquip 原样），
        /// 外加用户指定的三条召唤头统一属性（召唤伤害 / 鞭子范围 / 鞭子攻速）
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.maxMinions += 3;                           // 仆从上限 +3
            player.GetDamage<SummonDamageClass>() += 0.11f;   // 召唤伤害 +11%（用户指定的额外单件属性）
            player.whipRangeMultiplier += 0.11f;              // 鞭子攻击范围 +11%（同上）
            player.GetAttackSpeed<SummonMeleeSpeedDamageClass>() += 0.11f;  // 鞭子攻击速度 +11%（同上；鞭子走 SummonMeleeSpeed 攻速类）
            player.lavaMax += 240;                            // 岩浆免疫时长 +240 帧（4 秒）
            player.ignoreWater = true;                        // 水中不受移动减速
        }
        /// <summary>
        /// 注册配方：现代版与经典版灾厄材料不同，各注册一条（均在远古操纵机合成，与其余血炎头同规矩）
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄材料：Bloodstone×25、BloodOrb×10、RuinousSoul×2
                if (calamity.TryFind<ModItem>("Bloodstone", out ModItem bloodstone)
                    && calamity.TryFind<ModItem>("BloodOrb", out ModItem bloodOrb)
                    && calamity.TryFind<ModItem>("RuinousSoul", out ModItem ruinousSoul))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(bloodstone.Type, 25);
                    recipe.AddIngredient(bloodOrb.Type, 10);
                    recipe.AddIngredient(ruinousSoul.Type, 2);
                    recipe.AddTile(TileID.LunarCraftingStation);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                // 经典版灾厄材料：BloodstoneCore×11、RuinousSoul×2
                if (classic.TryFind<ModItem>("BloodstoneCore", out ModItem bloodstoneCore)
                    && classic.TryFind<ModItem>("RuinousSoul", out ModItem classicRuinousSoul))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient(bloodstoneCore.Type, 11);
                    recipeClassic.AddIngredient(classicRuinousSoul.Type, 2);
                    recipeClassic.AddTile(TileID.LunarCraftingStation);
                    recipeClassic.Register();
                }
            }
        }
    }
}
