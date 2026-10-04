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
    /// 金兜铁面盔（AuricTeslaHoodedFacemask） - 古圣金源套装的射手向头部部件：
    /// 一身**合并**承载龙蒿/血炎/弑神者/始源林海四套的**射手**效果（按经典版 1:1 移植）。
    /// 与近战头（AuricTeslaHelm）对称：置位四套的 *Ranged 标记而非 *Melee，并按经典版原样
    /// **不置** godSlayerDamage（≤80 低伤压制）与 **不加 aggro**（两者都只有近战头有）。
    /// 弑神者冲刺同样可用（依赖套装置位的 godSlayer 标记），冲刺本体见 CalamityDemutationPlayer.GodSlayerDash.cs。
    /// 与现代 2.0.4 同名件的差别（对齐经典版）：现代版已去掉 silvaSet/silvaRanged（林海并入金源）与
    /// lavaMax/lavaWet 相关项，且 ArmorSetShadows 改用 armorEffectDrawOutlines；本工程走经典。
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class AuricTeslaHoodedFacemask:ModItem
    {
        /// <summary>
        /// 物品基础属性：18x18、防御 40（比近战头的 54 低）、价值 1 铂金 80 金、月后稀有度 20（彩虹闪烁名）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素，照经典版源码；贴图实际为 26×26）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(1, 80, 0, 0);  // 价值 1 铂金 80 金（buyPrice 口径，与其余金源部件一致）
            Item.defense = 40;                        // 单件防御 40（经典版值，源码同行另留 //132 注释，系开发期遗留数字）
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
        /// 套装光环：开启残影拖尾（经典版同近战头；现代版此件改用轮廓线）
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }
        /// <summary>
        /// 套装效果：把下位四套的**射手**侧标记全部置位（龙蒿 tarraRanged / 血炎 bloodflareRanged /
        /// 弑神者 godSlayerRanged / 林海 silvaRanged）＋四套的通用套装标记与金源 auricSet，
        /// 使这一颗头同时吃到四套的射手专属效果（树叶爆炸、灵魂爆发与血液爆炸光球、破片弹与溢暴击、
        /// 远程攻速与林海无敌期增伤）。附加荆棘、岩浆延时、水下呼吸、血腥再生与岩浆中的防御/回血。
        /// 按经典版原样：不加 aggro、不置 godSlayerDamage。末尾的冲刺说明用到按键占位符。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = this.GetLocalization("SetBonus").Format(KeybindsSystem.GodslayerDashKeyDisplay);   // 套装说明里的 [KEY] 换成当前冲刺绑定键
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.tarraSet = true;            // 龙蒿套装效果
            modPlayer.tarraRanged = true;         // 龙蒿（射手向）
            modPlayer.bloodflareSet = true;       // 血炎套装效果
            modPlayer.bloodflareRanged = true;    // 血炎（射手向）
            modPlayer.godSlayer = true;           // 弑神者套装效果（弑神者冲刺的开启条件）
            modPlayer.godSlayerRanged = true;     // 弑神者（射手向；经典版射手头不置 godSlayerDamage）
            modPlayer.silvaSet = true;            // 始源林海套装效果
            modPlayer.silvaRanged = true;         // 始源林海（射手向）
            modPlayer.auricSet = true;            // 本模组的金之特斯拉标记
            player.thorns += 3f;                  // 荆棘反伤
            player.lavaMax += 240;                // 岩浆免疫时间延长 4 秒
            player.ignoreWater = true;            // 水下不减速
            player.crimsonRegen = true;           // 血腥再生（原版猩红装备的那套）
            if (player.lavaWet)
            {
                player.statDefense += 30;         // 泡在岩浆里额外 +30 防御
                player.lifeRegen += 10;           // 以及 +10 生命回复
            }
        }
        /// <summary>
        /// 单件效果：置位 auricBoost 标记（静止时增伤/增暴击与击退加成），并提升远程伤害 30%、远程暴击 30%。
        /// 经典版与现代版此件都**没有**远程攻速；射手侧攻速由置位的 silvaRanged 提供（+10%）。
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.auricBoost = true;
            player.GetDamage<RangedDamageClass>() += 0.3f;
            player.GetCritChance<RangedDamageClass>() += 30;
        }
        /// <summary>
        /// 配方：由龙蒿面甲 + 血炎角盔 + 始源林海角盔 + 弑神者战盔（四件下位**射手**头）升阶。
        /// 现代版灾厄用 10 个金之锭 + 妄想护符、宇宙砧（AuricBar 数量取 1.4.4 公开源码的 10，与近战头同）；
        /// 经典版用一长串后期材料、德雷顿熔炉——两分支的坯料清单与既有近战头逐字一致，只把下位头换成射手件。
        /// </summary>
        public override void AddRecipes()
        {
            // ── 现代版灾厄：四件下位射手头 + 金之锭 ×10 + 妄想护符，宇宙砧 ──
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("AuricBar", out ModItem auricBar) && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<TarragonVisage>();         // 龙蒿面甲（本模组移植物）
                    recipe.AddIngredient<BloodflareHornedHelm>();   // 血炎角盔
                    recipe.AddIngredient<SilvaHornedHelm>();        // 始源林海角盔
                    recipe.AddIngredient<GodSlayerHelmet>();        // 弑神者战盔
                    recipe.AddIngredient(auricBar.Type, 10);        // 灾厄材料：金之锭 ×10
                    recipe.AddIngredient<PsychoticAmulet>();        // 妄想护符（本模组移植物）
                    recipe.AddTile(cosmicAnvil.Type);               // 宇宙砧
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
                    recipeClassic.AddIngredient<TarragonVisage>();
                    recipeClassic.AddIngredient<BloodflareHornedHelm>();
                    recipeClassic.AddIngredient<SilvaHornedHelm>();
                    recipeClassic.AddIngredient<GodSlayerHelmet>();
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
