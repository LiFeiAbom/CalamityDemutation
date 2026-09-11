using CalamityDemutation.Content.Items.Accessories.JobAcc.Ranged;
using CalamityDemutation.Content.Items.Armors.Bloodflare;
using CalamityDemutation.Content.Items.Armors.GodSlayer;
using CalamityDemutation.Content.Items.Armors.Silva;
using CalamityDemutation.Content.Items.Armors.Tarragon;
using CalamityDemutation.Players;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace CalamityDemutation.Content.Items.Armors.AuricTesla
{
    [AutoloadEquip(EquipType.Head)]
    internal class AuricTeslaHelm:ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = Item.buyPrice(1, 80, 0, 0);
            Item.defense = 54; //132
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 20;
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<AuricTeslaBodyArmor>() && legs.type == ModContent.ItemType<AuricTeslaCuisses>();
        }
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
        }
        public override void UpdateEquip(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.auricBoost = true;
            player.GetDamage<MeleeDamageClass>() += 0.2f;
            player.GetCritChance<MeleeDamageClass>() += 20;
            player.GetAttackSpeed<MeleeDamageClass>() += 0.28f;
        }
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = "\n" +
                "Melee Tarragon, Bloodflare, God Slayer, and Silva armor effects\n" +
                "All projectiles spawn healing auric orbs on enemy hits\n" +
                "Max run speed and acceleration boosted by 10%\n" +
                "Your melee damage is multiplied based on how high your HP is; at full HP this effect is at max";
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
            TryTriggerGodslayerDash(player);
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
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient<TarragonHelm>();
                recipe.AddIngredient<BloodflareMask>();
                recipe.AddIngredient<SilvaHelm>();
                recipe.AddIngredient<GodSlayerHelm>();
                recipe.AddIngredient(calamity.Find<ModItem>("AuricBar").Type, 10);
                recipe.AddIngredient<PsychoticAmulet>();
                recipe.AddTile(calamity.Find<ModTile>("CosmicAnvil").Type);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient<TarragonHelm>();
                recipe1.AddIngredient<BloodflareMask>();
                recipe1.AddIngredient<SilvaHelm>();
                recipe1.AddIngredient<GodSlayerHelm>();
                recipe1.AddIngredient(calamity1.Find<ModItem>("AuricOre").Type, 60);
                recipe1.AddIngredient(calamity1.Find<ModItem>("EndothermicEnergy").Type, 10);
                recipe1.AddIngredient(calamity1.Find<ModItem>("NightmareFuel").Type, 10);
                recipe1.AddIngredient(calamity1.Find<ModItem>("Phantoplasm").Type, 8);
                recipe1.AddIngredient(calamity1.Find<ModItem>("DarksunFragment").Type, 10);
                recipe1.AddIngredient(calamity1.Find<ModItem>("BarofLife").Type, 6);
                recipe1.AddIngredient(calamity1.Find<ModItem>("HellcasterFragment").Type, 5);
                recipe1.AddIngredient(calamity1.Find<ModItem>("CoreofCalamity").Type, 2);
                recipe1.AddIngredient(calamity1.Find<ModItem>("GalacticaSingularity").Type);
                recipe1.AddIngredient<PsychoticAmulet>();
                recipe1.AddTile(calamity1.Find<ModTile>("DraedonsForge").Type);
                recipe1.Register();
            }
        }
        // 以下静态字段缓存反射结果：灾厄 CalamityPlayer 的类型、字段、方法以及一次性的解析/日志标志。
        // 只解析一次，避免每帧反射的开销；未装灾厄或解析失败时全为 null，冲刺静默失效。
        private static Type calamityPlayerType;
        private static FieldInfo godSlayerField;
        private static FieldInfo godSlayerDashHotKeyPressedField;
        private static FieldInfo lastUsedDashIDField;
        private static FieldInfo deferredDashIDField;
        private static FieldInfo cooldownsField;
        private static string godslayerDashID;
        private static MethodInfo getModPlayerMethod;
        private static bool calamityResolveDone;
        private static bool loggedSetBonus;
        private static bool loggedDashWrite;
        /// <summary>
        /// 反射解析灾厄(现代版) CalamityPlayer 上冲刺所需的成员，只解析一次后缓存。
        /// 灾厄经典版没有这套 dash 框架，未安装灾厄时返回 false，冲刺静默失效。
        /// </summary>
        private static bool ResolveCalamityDash()
        {
            if (calamityResolveDone)
                return deferredDashIDField != null;
            calamityResolveDone = true;
            if (!ModLoader.TryGetMod("CalamityMod", out Mod calamity))
                return false;
            Type[] calamityTypes = calamity.Code.GetTypes();
            calamityPlayerType = calamityTypes.FirstOrDefault(t => t.Name == "CalamityPlayer" && t.IsSubclassOf(typeof(ModPlayer)));
            if (calamityPlayerType == null)
                return false;
            const BindingFlags instance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            godSlayerField = calamityPlayerType.GetField("godSlayer", instance);
            godSlayerDashHotKeyPressedField = calamityPlayerType.GetField("godSlayerDashHotKeyPressed", instance);
            lastUsedDashIDField = calamityPlayerType.GetField("LastUsedDashID", instance);
            deferredDashIDField = calamityPlayerType.GetField("DeferredDashID", instance);
            cooldownsField = calamityPlayerType.GetField("cooldowns", instance);
            godslayerDashID = FindGodslayerDashID(calamityTypes);
            getModPlayerMethod = typeof(Player).GetMethod("GetModPlayer", System.Type.EmptyTypes)?.MakeGenericMethod(calamityPlayerType);
            bool ok = godSlayerField != null && godSlayerDashHotKeyPressedField != null && lastUsedDashIDField != null
                && deferredDashIDField != null && cooldownsField != null && godslayerDashID != null && getModPlayerMethod != null;
            if (!ok)
                CalamityDemutation.Instance?.Logger.Warn("弑神者冲刺：灾厄 dash 反射解析失败"
                    + $"(godSlayer={godSlayerField != null}, godSlayerDashHotKeyPressed={godSlayerDashHotKeyPressedField != null}"
                    + $", LastUsedDashID={lastUsedDashIDField != null}, DeferredDashID={deferredDashIDField != null}"
                    + $", cooldowns={cooldownsField != null}, GodslayerArmorDashID={godslayerDashID}"
                    + $", GetModPlayer={getModPlayerMethod != null})，冲刺已禁用。");
            return ok;
        }
        /// <summary>
        /// 取灾厄 GodslayerArmorDash.ID：先找静态字段，找不到再退回静态属性。
        /// </summary>
        private static string FindGodslayerDashID(Type[] calamityTypes)
        {
            Type dashType = calamityTypes.FirstOrDefault(t => t.Name == "GodslayerArmorDash");
            if (dashType == null)
                return null;
            const BindingFlags stat = BindingFlags.Public | BindingFlags.Static;
            if (dashType.GetField("ID", stat) is FieldInfo idField)
                return idField.GetValue(null) as string;
            if (dashType.GetProperty("ID", stat) is PropertyInfo idProperty)
                return idProperty.GetValue(null) as string;
            return null;
        }
        /// <summary>
        /// 取出玩家对应的灾厄 CalamityPlayer 实例（通过反射缓存的方法调用）。
        /// 反射未就绪或取不到时返回 false，调用方应直接跳过冲刺逻辑。
        /// </summary>
        private static bool TryGetCalamityPlayer(Player player, out ModPlayer calamityPlayer)
        {
            calamityPlayer = null;
            if (!ResolveCalamityDash())
                return false;
            calamityPlayer = getModPlayerMethod.Invoke(player, null) as ModPlayer;
            return calamityPlayer != null;
        }
        /// <summary>
        /// 弑神者冲刺：把灾厄的 godSlayer 标志置真，让灾厄以为玩家穿着它的弑神者套。
        /// 这样灾厄自己的按键处理(CalamityPlayer.ProcessTriggers)就会在按下「God Slayer Dash」键时
        /// 置真 godSlayerDashHotKeyPressed，且闸门(滑轮/钩爪/被舌卷/坐骑/冷却/dashDelay)全部由灾厄负责。
        /// 这里只做和灾厄 GodSlayerHeadMelee 头盔里同样的消费：把「下次冲刺用弑神者冲刺」写进 DeferredDashID。
        /// 注意不要写 auricSet —— 那会额外给玩家金之排斥免疫、改飞毯贴图、改席尔瓦水晶伤害，与冲刺无关。
        /// </summary>
        private static void TryTriggerGodslayerDash(Player player)
        {
            if (player.whoAmI != Main.myPlayer || !TryGetCalamityPlayer(player, out ModPlayer calamityPlayer))
                return;
            if (!loggedSetBonus)
            {
                loggedSetBonus = true;
                Main.NewText("[CD诊断] 弑神者 UpdateArmorSet 已进入"); // TODO 确认可触发后删除
            }
            godSlayerField.SetValue(calamityPlayer, true);
            bool hotKeyPressed = (bool)godSlayerDashHotKeyPressedField.GetValue(calamityPlayer);
            object lastUsedDashID = lastUsedDashIDField.GetValue(calamityPlayer);
            if (!hotKeyPressed && !(player.dashDelay != 0 && lastUsedDashID != null && lastUsedDashID.Equals(godslayerDashID)))
                return;
            deferredDashIDField.SetValue(calamityPlayer, godslayerDashID);
            player.dash = 0;
            if (!loggedDashWrite)
            {
                loggedDashWrite = true;
                Main.NewText("[CD诊断] 已写入 DeferredDashID = " + godslayerDashID); // TODO 确认可触发后删除
            }
        }
        /// <summary>
        /// 本模组自己的冲刺键入口：直接把灾厄的 godSlayerDashHotKeyPressed 置真，
        /// 之后的流程与灾厄自己的按键完全一致(由 TryTriggerGodslayerDash 每帧消费)。
        /// 闸门照抄灾厄 CalamityPlayer 的按键判定，冷却检查用于防止绕过 45 秒冷却。
        /// </summary>
        internal static void RequestGodslayerDash(Player player)
        {
            if (player.whoAmI != Main.myPlayer || !TryGetCalamityPlayer(player, out ModPlayer calamityPlayer))
                return;
            if (player.pulley || player.grappling[0] != -1 || player.tongued || player.mount.Active || player.dashDelay != 0)
                return;
            if (HasCalamityCooldown(calamityPlayer, "GodSlayerDash"))
                return;
            godSlayerDashHotKeyPressedField.SetValue(calamityPlayer, true);
        }
        /// <summary>
        /// 读灾厄 CalamityPlayer.cooldowns 字典判断冷却是否在走，防止绕过 45 秒冷却。
        /// </summary>
        private static bool HasCalamityCooldown(ModPlayer calamityPlayer, string id)
        {
            return cooldownsField.GetValue(calamityPlayer) is IDictionary cooldowns && cooldowns.Contains(id);
        }
    }
}
