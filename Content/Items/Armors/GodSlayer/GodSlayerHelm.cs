using CalamityDemutation.Players;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.GodSlayer
{
    /// <summary>
    /// 神裁者头盔（GodSlayerHelm） - 弑神者套头部，近战特化
    /// 单件：+14% 近战伤害、+14% 近战暴击、+20% 近战攻击速度。
    /// 套装效果（player.setBonus 官方描述逐条）：
    /// 致命伤时不会死亡并回复 300 生命，该效果每 45 秒只能触发一次；
    /// 该效果冷却期间获得 +10% 全伤害；
    /// 单次受到超过 80 伤害时会释放一群高伤害的弑神飞镖；
    /// 敌人攻击你时会受到大量反伤；
    /// 原本伤害不超过 80 的攻击会被削减到 1。
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class GodSlayerHelm:ModItem
    {
        // ── 静态字段 ──
        /// <summary>
        /// 反射到的灾厄 CalamityPlayer 类型（现代版），仅解析一次；未装灾厄或失败时为 null
        /// </summary>
        private static Type calamityPlayerType;
        /// <summary>
        /// 是否已尝试过灾厄 dash 反射解析，避免每帧重复解析
        /// </summary>
        private static bool calamityResolveDone;
        /// <summary>
        /// 灾厄 CalamityPlayer.cooldowns 字段（冲刺冷却表）
        /// </summary>
        private static FieldInfo cooldownsField;
        /// <summary>
        /// 灾厄 CalamityPlayer.DeferredDashID 字段（下次冲刺使用的 dash ID）
        /// </summary>
        private static FieldInfo deferredDashIDField;
        /// <summary>
        /// 缓存的 Player.GetModPlayer 泛型方法（已绑定灾厄 CalamityPlayer）
        /// </summary>
        private static MethodInfo getModPlayerMethod;
        /// <summary>
        /// 灾厄 CalamityPlayer.godSlayerDashHotKeyPressed 字段
        /// </summary>
        private static FieldInfo godSlayerDashHotKeyPressedField;
        /// <summary>
        /// 灾厄 GodslayerArmorDash.ID 的值（弑神者冲刺的 dash 标识）
        /// </summary>
        private static string godslayerDashID;
        /// <summary>
        /// 灾厄 CalamityPlayer.godSlayer 字段（置真后灾厄会按弑神者套处理冲刺）
        /// </summary>
        private static FieldInfo godSlayerField;
        /// <summary>
        /// 灾厄 CalamityPlayer.LastUsedDashID 字段
        /// </summary>
        private static FieldInfo lastUsedDashIDField;
        /// <summary>
        /// 是否已打印过「写入 DeferredDashID」的诊断日志（仅打印一次）
        /// </summary>
        private static bool loggedDashWrite;
        /// <summary>
        /// 是否已打印过「进入 UpdateArmorSet」的诊断日志（仅打印一次）
        /// </summary>
        private static bool loggedSetBonus;
        // ── 生命周期方法 ──
        /// <summary>
        /// 物品基础属性：尺寸、售价、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;                          // 贴图宽（像素）
            Item.height = 18;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 75, 0, 0);  // 售价 75 金
            Item.defense = 48; //96                  // 48 为当前防御值，//96 为原值记录（保留原义）
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;  // 月后稀有度 14（蓝色名）
        }
        /// <summary>
        /// 判定套装：头部 + 神裁者胸甲 + 神裁者护腿
        /// </summary>
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<GodSlayerChestplate>() && legs.type == ModContent.ItemType<GodSlayerLeggings>();
        }
        /// <summary>
        /// 套装激活时的拖影特效
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;  // 开启盔甲拖影
        }
        /// <summary>
        /// 套装激活：置位 godSlayer（致命保护 + 45 秒冷却增伤）与 godSlayerMelee（受击超 80 释放弑神飞镖），
        /// 两者均在 CalamityDemutationPlayer 中结算；同时提升反伤并尝试挂上弑神者冲刺。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.godSlayer = true;
            modPlayer.godSlayerMelee = true;
            player.setBonus = "\n" +
                "You will survive fatal damage and will be healed 300 HP if an attack would have killed you\n" +
                "This effect can only occur once every 45 seconds\n" +
                "While the cooldown for this effect is active you gain a 10% increase to all damage\n" +
                "Taking over 80 damage in one hit will cause you to release a swarm of high-damage god killer darts\n" +
                "Enemies take a lot of damage when they hit you\n" +
                "An attack that would deal 80 damage or less will have its damage reduced to 1\n" +
                "Pressing Y key allows the player to dash towards the position of their cursor for 1.25 seconds\n" +
                "Dealing great damage and inflicting the God Slayer Inferno debuff on contact\n" +
                "This effect has a 45-second cooldown\n";
            player.thorns += 2.5f;  // 反伤倍率 +2.5（与胸甲的 +0.9 叠加）
            TryTriggerGodslayerDash(player);
        }
        /// <summary>
        /// 单件属性：近战伤害 / 近战暴击 / 近战攻击速度
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.GetDamage<MeleeDamageClass>() += 0.14f;      // +14% 近战伤害
            player.GetCritChance<MeleeDamageClass>() += 14;      // +14% 近战暴击
            player.GetAttackSpeed<MeleeDamageClass>() += 0.2f;   // +20% 近战攻速
        }
        /// <summary>
        /// 注册配方：现代版与经典版灾厄材料不同，分别注册。
        /// 现代版用 CosmiliteBar(10) + AscendantSpiritEssence(2)，于 CosmicAnvil（宇宙砧）合成；
        /// 经典版用 CosmiliteBar(14) + NightmareFuel(8) + EndothermicEnergy(8)，于 DraedonsForge（德雷顿熔炉）合成。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(calamity.Find<ModItem>("CosmiliteBar").Type, 10);
                recipe.AddIngredient(calamity.Find<ModItem>("AscendantSpiritEssence").Type, 2);
                recipe.AddTile(calamity.Find<ModTile>("CosmicAnvil").Type);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(calamity1.Find<ModItem>("CosmiliteBar").Type, 14);
                recipe1.AddIngredient(calamity1.Find<ModItem>("NightmareFuel").Type, 8);
                recipe1.AddIngredient(calamity1.Find<ModItem>("EndothermicEnergy").Type, 8);
                recipe1.AddTile(calamity1.Find<ModTile>("DraedonsForge").Type);
                recipe1.Register();
            }
        }
        // ── 公开方法 ──
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
        // ── 私有工具 ──
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
        /// 读灾厄 CalamityPlayer.cooldowns 字典判断冷却是否在走，防止绕过 45 秒冷却。
        /// </summary>
        private static bool HasCalamityCooldown(ModPlayer calamityPlayer, string id)
        {
            return cooldownsField.GetValue(calamityPlayer) is IDictionary cooldowns && cooldowns.Contains(id);
        }
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
    }
}
