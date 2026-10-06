using CalamityDemutation.Content.Items.Accessories.JobAcc.Ranged;
using CalamityDemutation.Content.Items.Armors.Bloodflare;
using CalamityDemutation.Content.Items.Armors.GodSlayer;
using CalamityDemutation.Content.Items.Armors.Silva;
using CalamityDemutation.Content.Items.Armors.Tarragon;
using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using Terraria;
using Terraria.ModLoader;
using SilvaCrystalBuff = CalamityDemutation.Content.Buffs.SummonBuffs.SilvaCrystal;
using SilvaCrystalProj = CalamityDemutation.Content.Projectiles.Summon.SilvaCrystal;
namespace CalamityDemutation.Content.Items.Armors.AuricTesla
{
    /// <summary>
    /// 金宇星界盔（AuricTeslaSpaceHelmet，英文名 Auric Tesla Space Helmet） - 古圣金源套装的召唤向头部部件：
    /// 一身**复合**承载龙蒿 / 血炎 / 弑神者 / 始源林海四套的**召唤**效果
    ///（按经典版 CalamityModClassicPreTrailer 同名件 1:1 移植；CI 对应件 AuricTeslaHeadSummon
    /// 的防御、单件 +7 仆从、套装 +120% 召唤伤害、叶棱晶基础伤害 3000 与配方材料均一致，仅实现细节有别）。
    /// 与近战头（AuricTeslaHelm）/ 射手头（AuricTeslaHoodedFacemask）对称：置位四套的 *Summon 标记，
    /// 按经典版原样**不置** godSlayerDamage（单次 ≤80 → 1，近战头专属）也**不加** aggro（两者都只有近战头有）。
    /// 弑神者冲刺同样可用（依赖套装置位的 godSlayer 标记），冲刺本体见 CalamityDemutationPlayer.GodSlayerDash.cs。
    /// 与现代 2.0.4 同名件的差别（对齐经典版）：现代版改了职业头的形制与文案、ArmorSetShadows 改用
    /// armorEffectDrawOutlines，本工程走经典；**另按本工程召唤头统一单件口径**补了三条额外属性
    ///（召唤伤害 / 鞭子攻击范围 / 鞭子攻击速度各 +12%，源里没有——这是工程自调，与四件下位召唤头一致）。
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class AuricTeslaSpaceHelmet:ModItem
    {
        /// <summary>远古叶棱晶（SilvaCrystal）的基础伤害：经典版/CI 均为 3000（高于林海头的 1500）</summary>
        private const int CrystalBaseDamage = 3000;
        /// <summary>
        /// 物品基础属性：18x18、防御 12（比近战头的 54、射手头的 40 都低，经典版与 CI 同值，
        /// 源码同行另留 //132 注释，系开发期遗留数字）、价值 1 铂金 80 金、月后稀有度 20（彩虹闪烁名）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素，照经典版源码；贴图实际为 26×20）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(1, 80, 0, 0);  // 价值 1 铂金 80 金（buyPrice 口径，与其余金源部件一致）
            Item.defense = 12;                        // 单件防御 12
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 20;
        }
        /// <summary>
        /// 套装判定：头 + 金之特斯拉胸甲 + 金之特斯拉护腿
        /// </summary>
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<AuricTeslaBodyArmor>() && legs.type == ModContent.ItemType<AuricTeslaCuisses>();
        }
        /// <summary>
        /// 套装光环：开启残影拖尾（经典版同近战/射手头；现代版此件改用轮廓线）
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }
        /// <summary>
        /// 套装效果：把下位四套的**召唤**侧标记全部置位（龙蒿 tarraSummon / 血炎 bloodflareSummon /
        /// 弑神者 godSlayerSummon / 林海 silvaSummon）＋四套的通用套装标记与金源 auricSet，
        /// 使这一颗头同时吃到四套的召唤专属效果（生命光环与满血加成、环绕地雷与生命阈值加成、
        /// 噬神机械蠕虫与弑神幻影、远古叶棱晶与无敌结束后的召唤强化）。
        /// 附加荆棘、岩浆延时、水下呼吸、血腥再生、+120% 召唤伤害与岩浆中的防御/回血。
        /// 末尾在主人端补 SilvaCrystal 增益并保证叶棱晶在场；机械蠕虫不在这里生成——
        /// 由玩家侧 <see cref="CalamityDemutationPlayer.UpdateGodSlayerMechworm"/>（godSlayerSummon 触发）统一维护。
        /// 按经典版原样：不加 aggro、不置 godSlayerDamage。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = this.GetLocalization("SetBonus").Format(KeybindsSystem.GodslayerDashKeyDisplay);   // 套装说明里的 [KEY] 换成当前冲刺绑定键
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.tarraSet = true;          // 龙蒿套装效果
            modPlayer.tarraSummon = true;       // 龙蒿（召唤向）
            modPlayer.bloodflareSet = true;     // 血炎套装效果
            modPlayer.bloodflareSummon = true;  // 血炎（召唤向）
            modPlayer.godSlayer = true;         // 弑神者套装效果（弑神者冲刺的开启条件）
            modPlayer.godSlayerSummon = true;   // 弑神者（召唤向：蠕虫 + 幻影）
            modPlayer.silvaSet = true;          // 始源林海套装效果
            modPlayer.silvaSummon = true;       // 始源林海（召唤向：叶棱晶 + 无敌结束后的强化）
            modPlayer.auricSet = true;          // 本模组的金之特斯拉标记
            player.thorns += 3f;                // 荆棘反伤
            player.lavaMax += 240;              // 岩浆免疫时间延长 4 秒
            player.ignoreWater = true;          // 水下不减速
            player.crimsonRegen = true;         // 血腥再生（原版猩红装备的那套）
            player.GetDamage<SummonDamageClass>() += 1.2f;   // 召唤伤害 +120%（经典版 UpdateArmorSet 原样）
            player.maxMinions += 1;                          // 仆从上限 +1（CI 的 AuricTeslaHeadSummon 套装项，用户 2026-10-06 点名补上）
            if (player.lavaWet)
            {
                player.statDefense += 30;       // 泡在岩浆里额外 +30 防御
                player.lifeRegen += 10;         // 以及 +10 生命回复
            }
            // 远古叶棱晶：补增益 → 不在场时补一只（与 SilvaHelmet.UpdateArmorSet 同构，仅基础伤害 3000；
            // 生成只在主人端做——本钩子每名玩家 × 每一端都会跑，见工程记忆第 5 节）
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
                    // originalDamage 存基础值：tML 每帧按玩家当前召唤伤害重算 Projectile.damage
                    Main.projectile[crystal].originalDamage = CrystalBaseDamage;
                }
            }
        }
        /// <summary>
        /// 单件效果：置位 auricBoost 标记（静止时增伤/增暴击与击退加成），+7 仆从上限，
        /// 另按工程召唤头统一单件口径补召唤伤害 / 鞭子攻击范围 / 鞭子攻击速度各 +12%。
        /// 经典版与现代版此件单件只有 +7 仆从上限（无召唤伤害/鞭子项），+120% 召唤伤害在套装里。
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.auricBoost = true;
            player.maxMinions += 7;                           // 仆从上限 +7（经典版/CI 原样）
            player.GetDamage<SummonDamageClass>() += 0.12f;   // 召唤伤害 +12%（工程召唤头统一口径）
            player.whipRangeMultiplier += 0.12f;              // 鞭子攻击范围 +12%（同上）
            player.GetAttackSpeed<SummonMeleeSpeedDamageClass>() += 0.12f;  // 鞭子攻击速度 +12%（同上）
        }
        /// <summary>
        /// 配方：由龙蒿角盔 + 血炎狂龙盔 + 始源林海头盔 + 弑神者角盔（四件下位**召唤**头）升阶。
        /// 现代版灾厄用 10 个金之锭 + 妄想护符、宇宙砧（AuricBar 数量取 1.4.4 公开源码的 10，与另两颗金源头同）；
        /// 经典版用一长串后期材料、德雷顿熔炉——两分支的坯料清单与既有近战/射手头逐字一致，只把下位头换成召唤件。
        /// </summary>
        public override void AddRecipes()
        {
            // ── 现代版灾厄：四件下位召唤头 + 金之锭 ×10 + 妄想护符，宇宙砧 ──
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("AuricBar", out ModItem auricBar) && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<TarragonHornedHelm>();   // 龙蒿角盔（本模组移植物）
                    recipe.AddIngredient<BloodflareHelmet>();     // 血炎狂龙盔
                    recipe.AddIngredient<SilvaHelmet>();          // 始源林海头盔
                    recipe.AddIngredient<GodSlayerHornedHelm>();  // 弑神者角盔
                    recipe.AddIngredient(auricBar.Type, 10);      // 灾厄材料：金之锭 ×10
                    recipe.AddIngredient<PsychoticAmulet>();      // 妄想护符（本模组移植物）
                    recipe.AddTile(cosmicAnvil.Type);             // 宇宙砧
                    recipe.Register();
                }
            }
            // ── 经典版灾厄：同上四件 + 一长串后期材料，德雷顿熔炉 ──
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if (classic.TryFind<ModItem>("AuricOre", out ModItem auricOre)
                    && classic.TryFind<ModItem>("EndothermicEnergy", out ModItem endothermicEnergy)
                    && classic.TryFind<ModItem>("NightmareFuel", out ModItem nightmareFuel)
                    && classic.TryFind<ModItem>("Phantoplasm", out ModItem phantoplasm)
                    && classic.TryFind<ModItem>("DarksunFragment", out ModItem darksunFragment)
                    && classic.TryFind<ModItem>("BarofLife", out ModItem barofLife)
                    && classic.TryFind<ModItem>("HellcasterFragment", out ModItem hellcasterFragment)
                    && classic.TryFind<ModItem>("CoreofCalamity", out ModItem coreofCalamity)
                    && classic.TryFind<ModItem>("GalacticaSingularity", out ModItem galacticaSingularity)
                    && classic.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient<TarragonHornedHelm>();
                    recipeClassic.AddIngredient<BloodflareHelmet>();
                    recipeClassic.AddIngredient<SilvaHelmet>();
                    recipeClassic.AddIngredient<GodSlayerHornedHelm>();
                    recipeClassic.AddIngredient(auricOre.Type, 60);              // 经典版材料：金之矿石 ×60
                    recipeClassic.AddIngredient(endothermicEnergy.Type, 10);     // 吸热能量 ×10
                    recipeClassic.AddIngredient(nightmareFuel.Type, 10);         // 噩梦燃料 ×10
                    recipeClassic.AddIngredient(phantoplasm.Type, 8);            // 幻影质 ×8
                    recipeClassic.AddIngredient(darksunFragment.Type, 6);        // 暗黑碎片 ×6
                    recipeClassic.AddIngredient(barofLife.Type, 5);              // 生命锭 ×5
                    recipeClassic.AddIngredient(hellcasterFragment.Type, 5);     // 地狱施法者碎片 ×5
                    recipeClassic.AddIngredient(coreofCalamity.Type, 2);         // 灾厄核心 ×2
                    recipeClassic.AddIngredient(galacticaSingularity.Type);      // 银河奇点 ×1
                    recipeClassic.AddIngredient<PsychoticAmulet>();
                    recipeClassic.AddTile(draedonsForge.Type);
                    recipeClassic.Register();
                }
            }
        }
    }
}
