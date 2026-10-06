using CalamityDemutation.Content.Items.Accessories.Function;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
using SilvaCrystalBuff = CalamityDemutation.Content.Buffs.SummonBuffs.SilvaCrystal;
using SilvaCrystalProj = CalamityDemutation.Content.Projectiles.Summon.SilvaCrystal;
namespace CalamityDemutation.Content.Items.Armors.Silva
{
    /// <summary>
    /// 始源林海头盔（SilvaHelmet） - 始源林海套装的召唤向头部部件
    ///（按经典版灾厄 CalamityModClassicPreTrailer 同名件 1:1 移植；现代版对应 SilvaHeadSummon，
    /// 本工程沿用经典版的类名/防御/单件与套装数值）。
    /// 单件：仆从上限 +5；另有用户 2026-10-05 指定的三条召唤头统一属性
    ///（召唤伤害 +11%、鞭子攻击范围 +11%、鞭子攻击速度 +11%）。
    /// 套装效果（逐条对应 player.setBonus 的说明文字，实现位置见括号）：
    /// 1. 召唤伤害 +75%（本类 UpdateArmorSet 内直接加，经典版原样）
    /// 2. 免疫几乎所有减益（silvaSet → CalamityDemutationPlayer.PostUpdateMiscEffects）
    /// 3. 所有弹幕命中敌人时生成治疗叶球（silvaSet → CalamityDemutationGlobalProjectile）
    /// 4. 最大奔跑速度与加速度 +5%（silvaSet → CalamityDemutationPlayer.PostUpdateRunSpeeds）
    /// 5. 生命被压到 1 点时 10 秒内不会因任何后续伤害死亡（silvaSet → CalamityDemutationPlayer.PreKill）
    /// 6. 该效果每命只触发一次、最大生命降至 400 时停止（silvaHitCounter → CalamityDemutationPlayer）
    /// 7. 召唤远古叶棱晶为你轰击敌人（本类 UpdateArmorSet：SilvaCrystal 增益 + 弹幕）
    /// 8. 无敌窗口结束后仆从伤害 +10%、仆从上限 +2
    ///（silvaSummon → PostUpdateMiscEffects / ModifyHitNPCWithProj）
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class SilvaHelmet:ModItem
    {
        /// <summary>远古叶棱晶（SilvaCrystal）的基础伤害：经典版 1500，实际伤害由玩家召唤伤害缩放</summary>
        private const int CrystalBaseDamage = 1500;
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度（防御取经典版召唤头的 13，比近战头 52 / 射手头 36 低）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素，照经典版源码）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 90, 0, 0);  // 价值 90 金（与其余始源林海部件一致）
            Item.defense = 13;                        // 防御 13（经典版值，源码同行另留 //110 注释，系开发期遗留数字）
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15;  // 月后自定义稀有度 15 级
        }
        /// <summary>
        /// 判定套装：头部 + 始源林海盔甲 + 始源林海护胫
        /// </summary>
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<SilvaArmor>() && legs.type == ModContent.ItemType<SilvaLeggings>();
        }
        /// <summary>
        /// 套装激活时的角色拖影特效（与近战/射手头一致）
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }
        /// <summary>
        /// 套装效果：置位 silvaSet（通用套装效果）与 silvaSummon（召唤侧标记），给 +75% 召唤伤害，
        /// 并把本地化套装描述写入显示文本；随后在主人端补 SilvaCrystal 增益并保证叶棱晶在场。
        /// 生成与补增益都只在主人端做——本钩子每名玩家 × 每一端都会跑（见工程记忆第 5 节）。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.silvaSet = true;     // 套装标记（免疫 debuff/免死无敌/吸血叶球/移速）
            modPlayer.silvaSummon = true;  // 召唤侧标记（叶棱晶 + 无敌结束后的召唤强化）
            player.GetDamage<SummonDamageClass>() += 0.75f;  // 召唤伤害 +75%（经典版 UpdateArmorSet 原样）
            player.setBonus = this.GetLocalizedValue("SetBonus");
            // 远古叶棱晶：补增益 → 不在场时补一只（经典版把这段写在物品里，此处 1:1 照搬）
            if (player.whoAmI != Main.myPlayer)
                return;
            if (player.FindBuffIndex(ModContent.BuffType<SilvaCrystalBuff>()) == -1)
            {
                player.AddBuff(ModContent.BuffType<SilvaCrystalBuff>(), 3600, true);
            }
            if (player.ownedProjectileCounts[ModContent.ProjectileType<SilvaCrystalProj>()] < 1)
            {
                int damage = (int)player.GetTotalDamage<SummonDamageClass>().ApplyTo(CrystalBaseDamage);
                int crystal = Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center.X, player.Center.Y, 0f, -1f,
                    ModContent.ProjectileType<SilvaCrystalProj>(), damage, 0f, Main.myPlayer);
                if (Main.projectile.IndexInRange(crystal))
                {
                    // originalDamage 存基础值：tML 每帧按玩家当前召唤伤害重算 Projectile.damage，
                    // 配装变化即时生效（取代源里依赖 CalamityGlobalProjectile 的那段手工重算）
                    Main.projectile[crystal].originalDamage = CrystalBaseDamage;
                }
            }
        }
        /// <summary>
        /// 穿戴时的属性加成：仆从上限（经典版原样），外加用户指定的三条召唤头统一属性
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.maxMinions += 5;                           // 仆从上限 +5（经典版原样）
            player.GetDamage<SummonDamageClass>() += 0.11f;   // 召唤伤害 +11%（用户指定的额外单件属性）
            player.whipRangeMultiplier += 0.11f;              // 鞭子攻击范围 +11%（同上）
            player.GetAttackSpeed<SummonMeleeSpeedDamageClass>() += 0.11f;  // 鞭子攻击速度 +11%（同上）
        }
        /// <summary>
        /// 配方：与其余始源林海头部逐字一致（经典分支照经典版源码；现代分支沿用工程既有写法），均需本模组材料 LeadCore
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：在 CosmicAnvil 处用 PlantyMush/EffulgentFeather/AscendantSpiritEssence 合成
                if (calamity.TryFind<ModItem>("PlantyMush", out ModItem plantyMush)
                    && calamity.TryFind<ModItem>("EffulgentFeather", out ModItem effulgentFeather)
                    && calamity.TryFind<ModItem>("AscendantSpiritEssence", out ModItem ascendantSpiritEssence)
                    && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(plantyMush.Type, 30);
                    recipe.AddIngredient(effulgentFeather.Type, 8);
                    recipe.AddIngredient(ascendantSpiritEssence.Type, 2);
                    recipe.AddIngredient<LeadCore>();
                    recipe.AddTile(cosmicAnvil.Type);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                // 经典版灾厄：在 DraedonsForge 处用 DarksunFragment/EffulgentFeather/CosmiliteBar 等合成
                if (classic.TryFind<ModItem>("DarksunFragment", out ModItem darksunFragment)
                    && classic.TryFind<ModItem>("EffulgentFeather", out ModItem classicEffulgentFeather)
                    && classic.TryFind<ModItem>("CosmiliteBar", out ModItem cosmiliteBar)
                    && classic.TryFind<ModItem>("Tenebris", out ModItem tenebris)
                    && classic.TryFind<ModItem>("NightmareFuel", out ModItem nightmareFuel)
                    && classic.TryFind<ModItem>("EndothermicEnergy", out ModItem endothermicEnergy)
                    && classic.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient(darksunFragment.Type, 5);
                    recipeClassic.AddIngredient(classicEffulgentFeather.Type, 5);
                    recipeClassic.AddIngredient(cosmiliteBar.Type, 5);
                    recipeClassic.AddIngredient(tenebris.Type, 6);
                    recipeClassic.AddIngredient(nightmareFuel.Type, 14);
                    recipeClassic.AddIngredient(endothermicEnergy.Type, 14);
                    recipeClassic.AddIngredient<LeadCore>();
                    recipeClassic.AddTile(draedonsForge.Type);
                    recipeClassic.Register();
                }
            }
        }
    }
}
