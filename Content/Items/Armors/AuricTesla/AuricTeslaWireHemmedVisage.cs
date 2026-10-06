using CalamityDemutation.Content.Items.Accessories.JobAcc.Ranged;
using CalamityDemutation.Content.Items.Armors.Bloodflare;
using CalamityDemutation.Content.Items.Armors.GodSlayer;
using CalamityDemutation.Content.Items.Armors.Silva;
using CalamityDemutation.Content.Items.Armors.Tarragon;
using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.AuricTesla
{
    /// <summary>
    /// 金虚万象盔（AuricTeslaWireHemmedVisage，英文名 Auric Tesla Wire-Hemmed Visage） - 古圣金源套装的法师向头部部件：
    /// 一身**复合**承载龙蒿 / 血炎 / 弑神者 / 始源林海四套的**法师**效果
    ///（按经典版 CalamityModClassicPreTrailer 同名件 1:1 移植；CI 对应件 AuricTeslaHeadMagic 与
    /// 现代版同名件的防御（24）一致，仅套装实现细节有别）。
    /// 与近战头（AuricTeslaHelm）/ 射手头（AuricTeslaHoodedFacemask）/ 召唤头（AuricTeslaSpaceHelmet）对称：
    /// 置位四套的 *Mage 标记，按经典版原样**不置** godSlayerDamage（≤80 低伤压制，近战头专属）也**不加** aggro。
    /// 弑神者冲刺同样可用（依赖套装置位的 godSlayer 标记）。
    /// 四条法师效果全部由下位各自的实现承接（叶暴风与法弹回血 / 幽灵魔弹与暴击火焰爆炸 /
    /// 弑神者烈焰与治疗烈焰 / 法弹巨型爆炸），本件只负责置位标记。
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class AuricTeslaWireHemmedVisage:ModItem
    {
        /// <summary>
        /// 物品基础属性：18x18、防御 24（比近战头的 54、射手头的 40 低，经典/CI/现代三源同值，
        /// 源码同行另留 //132 注释，系开发期遗留数字）、价值 1 铂金 80 金、月后稀有度 20（彩虹闪烁名）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(1, 80, 0, 0);  // 价值 1 铂金 80 金（buyPrice 口径，与其余金源部件一致）
            Item.defense = 24;                        // 单件防御 24
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
        /// 套装光环：开启残影拖尾（经典版同近战/射手/召唤头；现代版此件改用轮廓线）
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }
        /// <summary>
        /// 套装效果：把下位四套的**法师**侧标记全部置位（龙蒿 tarraMage / 血炎 bloodflareMage /
        /// 弑神者 godSlayerMage / 林海 silvaMage）＋四套的通用套装标记与金源 auricSet，
        /// 使这一颗头同时吃到四套的法师专属效果。附加荆棘、岩浆延时、水下呼吸、血腥再生与岩浆中的防御/回血。
        /// 按经典版原样：不加 aggro、不置 godSlayerDamage；套装也**不给额外法伤**
        ///（法伤全在单件的 +20%，这点与召唤头的「套装 +120%」不同）。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = this.GetLocalization("SetBonus").Format(KeybindsSystem.GodslayerDashKeyDisplay);   // 套装说明里的 [KEY] 换成当前冲刺绑定键
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.tarraSet = true;          // 龙蒿套装效果
            modPlayer.tarraMage = true;         // 龙蒿（法师向：叶暴风 + 法弹回血）
            modPlayer.bloodflareSet = true;     // 血炎套装效果
            modPlayer.bloodflareMage = true;    // 血炎（法师向：幽灵魔弹 + 暴击火焰爆炸）
            modPlayer.godSlayer = true;         // 弑神者套装效果（弑神者冲刺的开启条件）
            modPlayer.godSlayerMage = true;     // 弑神者（法师向：弑神者烈焰/治疗烈焰 + 受击魔法爆炸）
            modPlayer.silvaSet = true;          // 始源林海套装效果
            modPlayer.silvaMage = true;         // 始源林海（法师向：法弹巨型爆炸 + 无敌后法伤加成）
            modPlayer.auricSet = true;          // 本模组的金之特斯拉标记
            player.thorns += 3f;                // 荆棘反伤
            player.lavaMax += 240;              // 岩浆免疫时间延长 4 秒
            player.ignoreWater = true;          // 水下不减速
            player.crimsonRegen = true;         // 血腥再生（原版猩红装备的那套）
            if (player.lavaWet)
            {
                player.statDefense += 30;       // 泡在岩浆里额外 +30 防御
                player.lifeRegen += 10;         // 以及 +10 生命回复
            }
        }
        /// <summary>
        /// 单件效果：置位 auricBoost 标记（静止时增伤/增暴击与击退加成），
        /// 并提升魔法伤害 20%、魔法暴击 20%、最大法力 100（经典版 UpdateEquip 原样）
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.auricBoost = true;
            player.GetDamage<MagicDamageClass>() += 0.2f;   // 魔法伤害 +20%
            player.GetCritChance<MagicDamageClass>() += 20; // 魔法暴击率 +20%
            player.statManaMax2 += 100;                     // 最大法力 +100
        }
        /// <summary>
        /// 配方：由龙蒿面具 + 血炎角面 + 弑神者面甲 + 始源林海罩帽（四件下位**法师**头）升阶。
        /// 现代版灾厄用 10 个金之锭 + 妄想护符、宇宙砧（AuricBar 数量取 1.4.4 公开源码的 10，与另三颗金源头同）；
        /// 经典版用一长串后期材料、德雷顿熔炉——两分支的坯料清单与既有三颗金源头逐字一致，只把下位头换成法师件。
        /// </summary>
        public override void AddRecipes()
        {
            // ── 现代版灾厄：四件下位法师头 + 金之锭 ×10 + 妄想护符，宇宙砧 ──
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("AuricBar", out ModItem auricBar) && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<TarragonMask>();          // 龙蒿面具（本模组移植物）
                    recipe.AddIngredient<BloodflareHornedMask>();  // 血魇九头盔
                    recipe.AddIngredient<GodSlayerVisage>();       // 弑神者面甲
                    recipe.AddIngredient<SilvaMaskedCap>();        // 始源林海罩帽
                    recipe.AddIngredient(auricBar.Type, 10);       // 灾厄材料：金之锭 ×10
                    recipe.AddIngredient<PsychoticAmulet>();       // 妄想护符（本模组移植物）
                    recipe.AddTile(cosmicAnvil.Type);              // 宇宙砧
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
                    recipeClassic.AddIngredient<TarragonMask>();
                    recipeClassic.AddIngredient<BloodflareHornedMask>();
                    recipeClassic.AddIngredient<GodSlayerVisage>();
                    recipeClassic.AddIngredient<SilvaMaskedCap>();
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
