using CalamityDemutation.Content.Items.Accessories.JobAcc.Ranged;
using CalamityDemutation.Content.Items.Armors.Bloodflare;
using CalamityDemutation.Content.Items.Armors.GodSlayer;
using CalamityDemutation.Content.Items.Armors.Silva;
using CalamityDemutation.Content.Items.Armors.Tarragon;
using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.AuricTesla
{
    /// <summary>
    /// 金羽十杀盔（AuricTeslaPlumedHelm，英文名 Auric Tesla Plumed Helm） - 古圣金源套装的**盗贼**向头部部件：
    /// 一身**复合**承载龙蒿 / 血炎 / 弑神者 / 始源林海四套的**盗贼**效果
    ///（按经典版 CalamityModClassicPreTrailer 同名件 1:1 移植；CI 对应件 AuricTeslaHeadRogue，防御同为 34）。
    /// 与近战头（AuricTeslaHelm）/ 射手头（AuricTeslaHoodedFacemask）/ 召唤头（AuricTeslaSpaceHelmet）/
    /// 法师头（AuricTeslaWireHemmedVisage）对称：置位四套的 *Throwing 标记。
    /// 按经典版原样**不置** godSlayerDamage（≤80 低伤压制，近战头专属）也**不加** aggro；
    /// 套装也**不给额外盗贼伤害**（数值全在单件的 +20%，法伤头那套「跟 CI」的口径不适用于本件）。
    /// 四条盗贼效果全部由下位各自的既有实现承接，本件只置位标记：
    /// 龙蒿（25 次暴击免伤 / 带减益加伤）、血炎（生命阈值加成 / 暴击 50% 回血）、
    /// 弑神者（满血盗贼属性 +10% / 受伤 >80 额外无敌帧）、始源林海（>50% 攻速 / 无敌后 +10% /
    /// 以及与 auricSet 联动的「>50% 生命时盗贼暴击 1.25 倍伤害」——正是本件套装文案里那一行）。
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class AuricTeslaPlumedHelm:ModItem
    {
        /// <summary>
        /// 物品基础属性：18x18、防御 34（比近战头的 54、射手头的 40 低，经典/CI 同值，
        /// 源码同行另留 //132 注释，系开发期遗留数字）、价值 1 铂金 80 金、月后稀有度 20（彩虹闪烁名）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(1, 80, 0, 0);  // 价值 1 铂金 80 金（buyPrice 口径，与其余金源部件一致）
            Item.defense = 34;                        // 单件防御 34
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
        /// 套装光环：开启残影拖尾（经典版同其余金源头；现代版此件改用轮廓线）
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }
        /// <summary>
        /// 套装效果：把下位四套的**盗贼**侧标记全部置位（龙蒿 tarraThrowing / 血炎 bloodflareThrowing /
        /// 弑神者 godSlayerThrowing / 林海 silvaThrowing）＋四套的通用套装标记与金源 auricSet，
        /// 使这一颗头同时吃到四套的盗贼专属效果。附加荆棘、岩浆延时、水下呼吸、血腥再生与岩浆中的防御/回血，
        /// 并把潜行上限抬到 160（源 rogueStealthMax = 1.6f）。
        /// 按经典版原样：不加 aggro、不置 godSlayerDamage、套装也不给额外盗贼伤害。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = this.GetLocalization("SetBonus").Format(KeybindsSystem.GodslayerDashKeyDisplay);   // 套装说明里的 [KEY] 换成当前冲刺绑定键
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.tarraSet = true;             // 龙蒿套装效果
            modPlayer.tarraThrowing = true;        // 龙蒿（盗贼向：25 次暴击免伤 / 带减益加伤）
            modPlayer.bloodflareSet = true;        // 血炎套装效果
            modPlayer.bloodflareThrowing = true;   // 血炎（盗贼向：生命阈值加成 / 暴击 50% 回血）
            modPlayer.godSlayer = true;            // 弑神者套装效果（弑神者冲刺的开启条件）
            modPlayer.godSlayerThrowing = true;    // 弑神者（盗贼向：满血 +10% 属性 / >80 额外无敌帧）
            modPlayer.silvaSet = true;             // 始源林海套装效果
            modPlayer.silvaThrowing = true;        // 始源林海（盗贼向：>50% 攻速 / 无敌后 +10% / auricSet 联动的暴击 1.25 倍）
            modPlayer.auricSet = true;             // 本模组的金之特斯拉标记
            player.thorns += 3f;                   // 荆棘反伤
            player.lavaMax += 240;                 // 岩浆免疫时间延长 4 秒
            player.ignoreWater = true;             // 水下不减速
            player.crimsonRegen = true;            // 血腥再生（原版猩红装备的那套）
            if (player.lavaWet)
            {
                player.statDefense += 30;          // 泡在岩浆里额外 +30 防御
                player.lifeRegen += 10;            // 以及 +10 生命回复
            }
            CDUtil.GrantRogueStealth(player, 1.6f);   // 潜行上限 160（源 rogueStealthMax = 1.6f；内部值 1f = 显示 100 点）
        }
        /// <summary>
        /// 单件效果：置位 auricBoost 标记（静止时增伤/增暴击与击退加成），
        /// 并提升盗贼伤害 20%、盗贼暴击 20%（经典版 UpdateEquip 原样；
        /// 源里写的是 <c>CalamityCustomThrowingDamagePlayer.throwingDamage += 0.2f / throwingCrit += 20</c>）。
        /// 另加**移速 +25%**——这一条经典版没有、是 **CI 对应件 AuricTeslaHeadRogue** 的设计，
        /// 用户 2026-10-06 对 CI 照后点名补上（CI 五颗金源头里只有盗贼这颗带移速）。
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.auricBoost = true;
            DamageClass rogue = CDUtil.GetRogueDamageClass();   // 现代版 = CalamityMod/RogueDamageClass；拿不到才退到 tML 的 Throwing
            player.GetDamage(rogue) += 0.2f;                    // 盗贼伤害 +20%
            player.GetCritChance(rogue) += 20;                  // 盗贼暴击率 +20%
            CDUtil.AddClassicThrowingStats(player, 0.2f, 20);   // 经典版：写进它的自定义投掷字段（反射）
            player.moveSpeed += 0.25f;                          // 移速 +25%（CI 的 AuricTeslaHeadRogue 原样）
        }
        /// <summary>
        /// 配方：由龙蒿头盔 + 血炎魔盔 + 弑神者面具 + 始源林海面具（四件下位**盗贼**头）升阶。
        /// 现代版灾厄用 10 个金之锭 + 妄想护符、宇宙砧（AuricBar 数量取 1.4.4 公开源码的 10，与另四颗金源头同）；
        /// 经典版用一长串后期材料、德雷顿熔炉——两分支的坯料清单与既有四颗金源头逐字一致，只把下位头换成盗贼件。
        /// </summary>
        public override void AddRecipes()
        {
            // ── 现代版灾厄：四件下位盗贼头 + 金之锭 ×10 + 妄想护符，宇宙砧 ──
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("AuricBar", out ModItem auricBar) && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<TarragonHelmet>();   // 龙蒿头盔（本模组移植物）
                    recipe.AddIngredient<BloodflareHelm>();   // 血饮魔精盔
                    recipe.AddIngredient<GodSlayerMask>();    // 弑神者面具
                    recipe.AddIngredient<SilvaMask>();        // 始源林海面具
                    recipe.AddIngredient(auricBar.Type, 10);  // 灾厄材料：金之锭 ×10
                    recipe.AddIngredient<PsychoticAmulet>();  // 妄想护符（本模组移植物）
                    recipe.AddTile(cosmicAnvil.Type);         // 宇宙砧
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
                    recipeClassic.AddIngredient<TarragonHelmet>();
                    recipeClassic.AddIngredient<BloodflareHelm>();
                    recipeClassic.AddIngredient<GodSlayerMask>();
                    recipeClassic.AddIngredient<SilvaMask>();
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
