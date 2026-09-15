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
            Item.width = 18;
            Item.height = 18;
            Item.value = Item.buyPrice(1, 80, 0, 0);
            Item.defense = 54; //132
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
            player.setBonus = "\n" +
                "Melee Tarragon, Bloodflare, God Slayer, and Silva armor effects\n" +
                "All projectiles spawn healing auric orbs on enemy hits\n" +
                "Max run speed and acceleration boosted by 10%\n" +
                "Your melee damage is multiplied based on how high your HP is; at full HP this effect is at max\n" +
                $"Press [{KeybindsSystem.GodslayerDashKeyDisplay}] to dash in any of eight directions\n" +
                "Dealing great damage and inflicting the God Slayer Inferno debuff on contact\n" +
                "This effect has a 45-second cooldown";
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.tarraSet = true;
            modPlayer.tarraMelee = true;
            modPlayer.bloodflareSet = true;
            modPlayer.bloodflareMelee = true;
            modPlayer.godSlayer = true;
            modPlayer.godSlayerMelee = true;
            modPlayer.silvaSet = true;
            modPlayer.silvaMelee = true;
            modPlayer.auricSet = true;
            player.thorns += 3f;
            player.lavaMax += 240;
            player.ignoreWater = true;
            player.crimsonRegen = true;
            player.aggro += 1200;
            if (player.lavaWet)
            {
                player.statDefense += 30;
                player.lifeRegen += 10;
            }
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
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("AuricBar", out ModItem auricBar) && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<TarragonHelm>();
                    recipe.AddIngredient<BloodflareMask>();
                    recipe.AddIngredient<SilvaHelm>();
                    recipe.AddIngredient<GodSlayerHelm>();
                    recipe.AddIngredient(auricBar.Type, 10);
                    recipe.AddIngredient<PsychoticAmulet>();
                    recipe.AddTile(cosmicAnvil.Type);
                    recipe.Register();
                }
            }
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
                    recipe1.AddIngredient(auricOre.Type, 60);
                    recipe1.AddIngredient(endothermicEnergy.Type, 10);
                    recipe1.AddIngredient(nightmareFuel.Type, 10);
                    recipe1.AddIngredient(phantoplasm.Type, 8);
                    recipe1.AddIngredient(darksunFragment.Type, 6);
                    recipe1.AddIngredient(barofLife.Type, 5);
                    recipe1.AddIngredient(hellcasterFragment.Type, 5);
                    recipe1.AddIngredient(coreofCalamity.Type, 2);
                    recipe1.AddIngredient(galacticaSingularity.Type);
                    recipe1.AddIngredient<PsychoticAmulet>();
                    recipe1.AddTile(draedonsForge.Type);
                    recipe1.Register();
                }
            }
        }
    }
}
