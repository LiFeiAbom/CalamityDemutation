using CalamityDemutation.Content.Items.Accessories.JobAcc.Ranged;
using CalamityDemutation.Content.Items.Armors.Bloodflare;
using CalamityDemutation.Content.Items.Armors.GodSlayer;
using CalamityDemutation.Content.Items.Armors.Silva;
using CalamityDemutation.Content.Items.Armors.Tarragon;
using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using System;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.AuricTesla
{
    /// <summary>
    /// 金之特斯拉头盔 - 月后终极套装头部：一身承载塔拉贡/血焰/弑神者/席尔瓦四套效果。
    /// 弑神者冲刺同样可用（依赖套装置位的 godSlayer 标记），冲刺本体由本模组自持实现，
    /// 见 CalamityDemutationPlayer.GodSlayerDash.cs。
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class AuricTeslaHelm:ModItem
    {
        // ── 生命周期方法 ──
        /// <summary>
        /// 物品基础属性：18x18、防御 54、价值 1 铂金 80 金（buyPrice 口径；游戏内价值为其 1/5），月后稀有度 20（彩虹闪烁名）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(1, 80, 0, 0);  // 价值 1 铂金 80 金（buyPrice 口径）
            Item.defense = 54;                        // 单件防御 54
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
        /// 套装光环：开启残影拖尾
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }
        /// <summary>
        /// 套装效果：一身承载塔拉贡/血焰/弑神者/席尔瓦四套，附加荆棘、岩浆延时、水下呼吸、
        /// 血腥再生、仇恨提升与岩浆中额外防御/回血。
        /// 末尾两段反射只把灾厄（现代版/经典版）CalamityPlayer.auricSet 读进局部变量便丢弃，
        /// 并未写回，属无效残留代码（本文件下方注释亦明确不应写 auricSet）；如需灾厄侧生效须另行处理。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = this.GetLocalization("SetBonus").Format(KeybindsSystem.GodslayerDashKeyDisplay);   // 套装说明里的 [KEY] 换成当前冲刺绑定键
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.tarraSet = true;          // 塔拉贡套装效果
            modPlayer.tarraMelee = true;        // 塔拉贡（近战向）
            modPlayer.bloodflareSet = true;     // 血焰套装效果
            modPlayer.bloodflareMelee = true;
            modPlayer.godSlayer = true;         // 弑神者套装效果（弑神者冲刺的开启条件）
            modPlayer.godSlayerMelee = true;
            modPlayer.silvaSet = true;          // 席尔瓦套装效果
            modPlayer.silvaMelee = true;
            modPlayer.auricSet = true;          // 本模组的金之特斯拉标记
            player.thorns += 3f;                // 荆棘反伤
            player.lavaMax += 240;              // 岩浆免疫时间延长 4 秒
            player.ignoreWater = true;          // 水下不减速
            player.crimsonRegen = true;         // 血腥再生（原版猩红装备的那套）
            player.aggro += 1200;               // 仇恨大幅提升
            if (player.lavaWet)
            {
                player.statDefense += 30;       // 泡在岩浆里额外 +30 防御
                player.lifeRegen += 10;         // 以及 +10 生命回复
            }
            // ── 以下两段反射只是把灾厄 CalamityPlayer.auricSet 读进局部变量后丢弃，未写回，属无效残留（见类注释） ──
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                var calamityPlayerType = calamity.Code.GetTypes()
                    .FirstOrDefault(t => t.Name == "CalamityPlayer" && t.IsSubclassOf(typeof(ModPlayer)));
                if (calamityPlayerType != null)
                {
                    var getModPlayerMethod = typeof(Player).GetMethod("GetModPlayer", [])
                        ?.MakeGenericMethod(calamityPlayerType);
                    if (getModPlayerMethod != null)
                    {
                        if (getModPlayerMethod.Invoke(player, null) is ModPlayer calPlayer)
                        {
                            var field = calamityPlayerType.GetField("auricSet",
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);
                            if (field != null)
                            {
                                bool auricSet = (bool)field.GetValue(calPlayer);
                                auricSet = true;
                            }
                        }
                    }
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                var calamityPlayerType = calamity1.Code.GetTypes()
                    .FirstOrDefault(t => t.Name == "CalamityPlayerPreTrailer" && t.IsSubclassOf(typeof(ModPlayer)));
                if (calamityPlayerType != null)
                {
                    var getModPlayerMethod = typeof(Player).GetMethod("GetModPlayer", [])
                        ?.MakeGenericMethod(calamityPlayerType);
                    if (getModPlayerMethod != null)
                    {
                        if (getModPlayerMethod.Invoke(player, null) is ModPlayer calPlayer)
                        {
                            var field = calamityPlayerType.GetField("auricSet",
                                 System.Reflection.BindingFlags.Public |
                                 System.Reflection.BindingFlags.NonPublic |
                                 System.Reflection.BindingFlags.Instance);
                            if (field != null)
                            {
                                bool auricSet = (bool)field.GetValue(calPlayer);
                                auricSet = true;
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 单件效果：置位 auricBoost 标记，并提升近战伤害 20%、近战暴击 20%、近战攻速 28%
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.auricBoost = true;
            player.GetDamage<MeleeDamageClass>() += 0.2f;
            player.GetCritChance<MeleeDamageClass>() += 20;
            player.GetAttackSpeed<MeleeDamageClass>() += 0.28f;
        }
        /// <summary>
        /// 配方：由塔拉贡头 + 血焰面具 + 席尔瓦头 + 弑神者头升阶，
        /// 现代版灾厄用 10 个金之锭 + 宇宙砧，经典版用若干后期材料 + 德雷顿熔炉。
        /// </summary>
        public override void AddRecipes()
        {
            // ── 现代版灾厄：四件下位头 + 金之锭 ×10 + 妄想护符，宇宙砧 ──
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("AuricBar", out ModItem auricBar) && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<TarragonHelm>();          // 塔拉贡头（本模组移植物）
                    recipe.AddIngredient<BloodflareMask>();        // 血焰面具
                    recipe.AddIngredient<SilvaHelm>();             // 席尔瓦头
                    recipe.AddIngredient<GodSlayerHelm>();         // 弑神者头
                    recipe.AddIngredient(auricBar.Type, 10);       // 灾厄材料：金之锭 ×10
                    recipe.AddIngredient<PsychoticAmulet>();       // 妄想护符（本模组移植物）
                    recipe.AddTile(cosmicAnvil.Type);              // 宇宙砧
                    recipe.Register();
                }
            }
            // ── 经典版灾厄：四件下位头 + 一长串后期材料，德雷顿熔炉 ──
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModItem>("AuricOre", out ModItem auricOre)
                    && calamity1.TryFind<ModItem>("EndothermicEnergy", out ModItem endothermicEnergy)
                    && calamity1.TryFind<ModItem>("NightmareFuel", out ModItem nightmareFuel)
                    && calamity1.TryFind<ModItem>("Phantoplasm", out ModItem phantoplasm)
                    && calamity1.TryFind<ModItem>("DarksunFragment", out ModItem darksunFragment)
                    && calamity1.TryFind<ModItem>("BarofLife", out ModItem barofLife)
                    && calamity1.TryFind<ModItem>("HellcasterFragment", out ModItem hellcasterFragment)
                    && calamity1.TryFind<ModItem>("CoreofCalamity", out ModItem coreofCalamity)
                    && calamity1.TryFind<ModItem>("GalacticaSingularity", out ModItem galacticaSingularity)
                    && calamity1.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient<TarragonHelm>();
                    recipe1.AddIngredient<BloodflareMask>();
                    recipe1.AddIngredient<SilvaHelm>();
                    recipe1.AddIngredient<GodSlayerHelm>();
                    recipe1.AddIngredient(auricOre.Type, 60);              // 经典版材料：金之矿石 ×60
                    recipe1.AddIngredient(endothermicEnergy.Type, 10);     // 吸热能量 ×10
                    recipe1.AddIngredient(nightmareFuel.Type, 10);         // 噩梦燃料 ×10
                    recipe1.AddIngredient(phantoplasm.Type, 8);            // 幻影质 ×8
                    recipe1.AddIngredient(darksunFragment.Type, 6);        // 暗黑碎片 ×6
                    recipe1.AddIngredient(barofLife.Type, 5);              // 生命锭 ×5
                    recipe1.AddIngredient(hellcasterFragment.Type, 5);     // 地狱施法者碎片 ×5
                    recipe1.AddIngredient(coreofCalamity.Type, 2);         // 灾厄核心 ×2
                    recipe1.AddIngredient(galacticaSingularity.Type);      // 银河奇点 ×1
                    recipe1.AddIngredient<PsychoticAmulet>();
                    recipe1.AddTile(draedonsForge.Type);
                    recipe1.Register();
                }
            }
        }
    }
}
