using System;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Utilities
{
    /// <summary>
    /// 通用工具类（灾厄反射桥部分）：用反射读写灾厄侧 ModPlayer 的字段。
    /// <para>
    /// 目前只有一件事——把本模组金源套的 <c>auricSet</c> 镜像到**现代版灾厄**的
    /// <c>CalamityPlayer.auricSet</c>（1.4.4-release 的 <c>CalPlayer/CalamityPlayer.cs:1299</c>，
    /// 是个 public 字段；2.0.4 版本字段同名）。这样玩家戴本模组的金源套时灾厄侧也会"认账"，
    /// 于是它自己那几条 auricSet 分支生效：
    /// ① 金源矿石不再触发 Auric Rejection（<c>CalamityPlayerMiscEffects.cs:917</c>——触发时是 300 伤害 + 大幅击退）；
    /// ② 飞毯换成金源贴图（同文件 :1140）；③ 金源拖影与纳米粒子（<c>CalamityPlayerDrawEffects.cs:453</c>）。
    /// **现代版的 auricSet 没有任何数值加成**，所以镜像过去不产生平衡副作用。
    /// </para>
    /// <para>
    /// 为什么**不**对经典版（CalamityModClassicPreTrailer）做同样的镜像：经典版的 auricSet 除了外观，
    /// 还在 <c>PostUpdateRunSpeeds</c> 里无条件给 <c>+10% 跑速/加速度</c>
    /// （<c>CalamityPlayerPreTrailer.cs:5477/5492</c>），而本工程自己也实现了一份金源套 +10% 跑速——
    /// 两边都算就变成双份。故只镜像现代版；经典版其余 auricSet 分支都还要求它自己的 *Set 标记，
    /// 我们不会去置位，本就不会触发。
    /// </para>
    /// <para>
    /// 时机：灾厄在 <c>CalamityPlayer.ResetEffects()</c> 里每帧把 auricSet 清零，直到
    /// <c>PostUpdateMiscEffects()</c> 与绘制期才读取。所以调用点必须落在两者之间——
    /// 本工程的 <c>CalamityDemutationPlayer.PostUpdateEquips()</c> 正好：PostUpdateEquips 相位
    /// 全部跑完之后才会进入 PostUpdateMiscEffects 相位；写早了会被灾厄的 ResetEffects 抹掉。
    /// 每名玩家 × 每一端都会调用本方法，联机下服务端也能拿到正确状态
    /// （灾厄的矿石排斥判定在服务端同样会跑）。
    /// </para>
    /// <para>
    /// 性能：类型/字段/方法的反射句柄只在首次调用时探测一次并缓存，失败也记作已探测、之后直接短路
    /// （灾厄未加载时不会每帧扫类型）；写回用缓存的装箱 true，逐帧零分配。
    /// </para>
    /// </summary>
    internal static partial class CDUtil
    {
        /// <summary>是否已探测过灾厄侧反射句柄（无论成败只探测一次）</summary>
        private static bool calamityAuricProbed;
        /// <summary>CalamityPlayer.auricSet 的字段句柄</summary>
        private static FieldInfo calamityAuricSetField;
        /// <summary>Player.GetModPlayer&lt;CalamityPlayer&gt;() 的方法句柄</summary>
        private static MethodInfo calamityGetModPlayer;
        /// <summary>缓存装箱的 true，避免逐帧装箱分配</summary>
        private static readonly object BoxedTrue = true;

        /// <summary>
        /// 把本模组金源套的置位镜像到现代版灾厄的 CalamityPlayer.auricSet。
        /// 由调用方判断玩家确实穿着本模组金源套（auricSet 为真）后再调用；
        /// 不需要"置回 false"——灾厄每帧自己清零，我们只在需要时写 true。
        /// </summary>
        public static void MirrorAuricSetToCalamity(Player player)
        {
            ProbeCalamityAuricBridge();
            if (calamityAuricSetField == null || calamityGetModPlayer == null)
                return;
            if (calamityGetModPlayer.Invoke(player, null) is ModPlayer calamityPlayer)
            {
                calamityAuricSetField.SetValue(calamityPlayer, BoxedTrue);
            }
        }

        /// <summary>
        /// 一次性探测灾厄侧的 Type / FieldInfo / MethodInfo 并缓存；任一步失败都放弃并记为已探测（不重试）。
        /// </summary>
        private static void ProbeCalamityAuricBridge()
        {
            if (calamityAuricProbed)
                return;
            calamityAuricProbed = true;
            if (!ModLoader.TryGetMod("CalamityMod", out Mod calamity))
                return;   // 未安装现代版灾厄：直接短路，之后不再探测
            Type calamityPlayerType = calamity.Code.GetTypes()
                .FirstOrDefault(t => t.Name == "CalamityPlayer" && t.IsSubclassOf(typeof(ModPlayer)));
            if (calamityPlayerType == null)
                return;   // 灾厄改了类名：放弃
            FieldInfo auricField = calamityPlayerType.GetField("auricSet",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (auricField == null)
                return;   // 该版本没有 auricSet 字段：放弃
            // Player.GetModPlayer<T>() 是无参泛型（另一个 GetModPlayer<T>(T) 带参，不会被 GetMethod 命中）
            MethodInfo getModPlayer = typeof(Player).GetMethod("GetModPlayer", Type.EmptyTypes)
                ?.MakeGenericMethod(calamityPlayerType);
            if (getModPlayer == null)
                return;
            calamityAuricSetField = auricField;
            calamityGetModPlayer = getModPlayer;
        }

        // ── 盗贼潜行桥（魔影面罩 DemonshadeHelmRogue 用）──

        /// <summary>是否已探测过经典版的潜行字段（无论成败只探测一次）</summary>
        private static bool classicStealthProbed;
        /// <summary>CalamityPlayerPreTrailer.rogueStealthMax 的字段句柄</summary>
        private static FieldInfo classicRogueStealthMaxField;
        /// <summary>Player.GetModPlayer&lt;CalamityPlayerPreTrailer&gt;() 的方法句柄</summary>
        private static MethodInfo classicGetModPlayerForStealth;

        /// <summary>
        /// 给玩家补盗贼潜行：把潜行上限抬到 <paramref name="maxStealth"/>（内部值，1f = 显示 100 点），
        /// 并把他标成「算作盗贼甲」。**必须每帧调用**——灾厄在 ResetEffects 里把上限清零，
        /// 到它 PostUpdateMiscEffects 才积攒潜行；调用点同样是本工程的 PostUpdateEquips。
        /// <para>
        /// 两条路：现代版灾厄走**官方 Mod.Call**（<c>AddMaxStealth</c> + <c>SetWearingRogueArmor</c>——
        /// 这两个键在 2.0.4 与 1.4.4-release 的 ModCalls 里都有；现代版的闸门是独立的 wearingRogueArmor 布尔，
        /// 所以两个调用缺一不可）；经典版没有对应 Call（它的 Call 只有 zone/boss 查询）→ 反射写 rogueStealthMax，
        /// 经典版没有 wearingRogueArmor 字段，它的闸门就是「上限 > 0」，故只写上限即可。
        /// </para>
        /// <para>
        /// 两边都按**加算**处理：灾厄自己的盗贼饰品/套装也往这个字段上加，直接赋值会盖掉它们
        /// （经典版走反射时读现值再加；现代版的 AddMaxStealth 本身就是加算）。
        /// </para>
        /// </summary>
        public static void GrantRogueStealth(Player player, float maxStealth)
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                calamity.Call("AddMaxStealth", player, maxStealth);    // rogueStealthMax += maxStealth
                calamity.Call("SetWearingRogueArmor", player, true);   // 置位"算作盗贼甲"的闸门
            }
            GrantClassicRogueStealth(player, maxStealth);
        }

        /// <summary>
        /// 经典版的潜行回退：反射把 <c>CalamityPlayerPreTrailer.rogueStealthMax</c> 加上给定值。
        /// 句柄只在首次调用时探测并缓存（失败也记为已探测，之后直接短路）。
        /// </summary>
        private static void GrantClassicRogueStealth(Player player, float maxStealth)
        {
            if (!classicStealthProbed)
            {
                classicStealthProbed = true;
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
                {
                    Type classicPlayerType = classic.Code.GetTypes()
                        .FirstOrDefault(t => t.Name == "CalamityPlayerPreTrailer" && t.IsSubclassOf(typeof(ModPlayer)));
                    FieldInfo stealthField = classicPlayerType?.GetField("rogueStealthMax",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo getModPlayer = classicPlayerType == null
                        ? null
                        : typeof(Player).GetMethod("GetModPlayer", Type.EmptyTypes)?.MakeGenericMethod(classicPlayerType);
                    if (stealthField != null && getModPlayer != null)
                    {
                        classicRogueStealthMaxField = stealthField;
                        classicGetModPlayerForStealth = getModPlayer;
                    }
                }
            }
            if (classicRogueStealthMaxField == null || classicGetModPlayerForStealth == null)
                return;
            if (classicGetModPlayerForStealth.Invoke(player, null) is ModPlayer classicPlayer)
            {
                // 读现值再加：经典版自己的盗贼饰品/套装也会往这个字段上加，直接赋值会盖掉它们
                float current = classicRogueStealthMaxField.GetValue(classicPlayer) is float f ? f : 0f;
                classicRogueStealthMaxField.SetValue(classicPlayer, current + maxStealth);
            }
        }

        // ── 盗贼伤害 / 暴击桥（魔影面罩用）──

        /// <summary>是否已探测过盗贼伤害相关的句柄（无论成败只探测一次）</summary>
        private static bool rogueStatProbed;
        /// <summary>现代版灾厄的真·盗贼伤害类（CalamityMod/RogueDamageClass）；拿不到时回退 tML 的 Throwing</summary>
        private static DamageClass cachedRogueDamageClass;
        /// <summary>经典版 CalamityCustomThrowingDamagePlayer.throwingDamage 的字段句柄</summary>
        private static FieldInfo classicThrowingDamageField;
        /// <summary>经典版 CalamityCustomThrowingDamagePlayer.throwingCrit 的字段句柄</summary>
        private static FieldInfo classicThrowingCritField;
        /// <summary>经典版 CalamityCustomThrowingDamagePlayer.throwingVelocity 的字段句柄</summary>
        private static FieldInfo classicThrowingVelocityField;
        /// <summary>Player.GetModPlayer&lt;CalamityCustomThrowingDamagePlayer&gt;() 的方法句柄</summary>
        private static MethodInfo classicGetThrowingModPlayer;

        /// <summary>
        /// 取「盗贼」伤害类。**现代版灾厄的盗贼类是真正的 <c>DamageClass</c>**
        ///（<c>CalamityMod/RogueDamageClass</c>，1.4.4-release 与 2.0.4 同名），而 <c>DamageClass</c> 在 tML 里是
        /// <c>ModType</c>，所以能直接按「模组名 + 内容名」取到，**不需要反射**；取不到时（例如只装了经典版）
        /// 才回退 tML 的 <see cref="DamageClass.Throwing"/>。
        /// <para>
        /// 关于「用 Throwing 可不可以」：灾厄的 <c>RogueDamageClass</c> 对 tML 的 Throwing 是**完全继承**
        ///（<c>DamageClasses/RogueDamageClasses.cs</c> 的 <c>GetModifierInheritance → StatInheritanceData.Full</c>），
        /// 所以给 Throwing 加的加成**确实**会作用到盗贼武器——CWR 那颗头就是图省事这么写的。
        /// 但方向性只成立一半：继承是「子类吃父类」，反过来不成立，
        /// 于是 <c>DamageType = Throwing</c> 的弹幕**拿不到玩家"盗贼专属"的加成**（只吃投掷/通用桶）。
        /// 本工程要让红魔三叉戟按盗贼结算，所以优先用真·盗贼类，只在确实拿不到时才退到 Throwing。
        /// </para>
        /// </summary>
        public static DamageClass GetRogueDamageClass(Player player = null)
        {
            if (!rogueStatProbed)
            {
                rogueStatProbed = true;
                cachedRogueDamageClass = ModContent.TryFind("CalamityMod", "RogueDamageClass", out DamageClass rogue)
                    ? rogue
                    : DamageClass.Throwing;
            }
            return cachedRogueDamageClass;
        }

        /// <summary>
        /// 经典版的盗贼（投掷）伤害/暴击桥：经典版**没有盗贼 DamageClass**，它的盗贼数值是一对自定义 ModPlayer 字段
        /// （<c>CalamityCustomThrowingDamagePlayer.throwingDamage</c> / <c>.throwingCrit</c>，基准 1f / 4，
        /// 每帧在它自己的 ResetEffects 里复位），盗贼武器按
        /// <c>基准 × (throwingDamage + 玩家召唤伤害 additive)</c> 结算（见经典版 <c>Items/Weapons/LunicEye.cs:47</c>）。
        /// 所以这里反射把加成**加**进去——必须每帧调用（它每帧复位），且调用点要在它 ResetEffects 之后。
        /// </summary>
        public static void AddClassicThrowingStats(Player player, float damageAdd, int critAdd)
        {
            ProbeClassicThrowingBridge();
            if (classicThrowingDamageField == null || classicThrowingCritField == null || classicGetThrowingModPlayer == null)
                return;
            if (classicGetThrowingModPlayer.Invoke(player, null) is ModPlayer throwingPlayer)
            {
                // 读现值再加：经典版自己的盗贼装备同样往这两个字段上加，直接赋值会盖掉它们
                float damage = classicThrowingDamageField.GetValue(throwingPlayer) is float d ? d + damageAdd : 1f + damageAdd;
                int crit = classicThrowingCritField.GetValue(throwingPlayer) is int c ? c + critAdd : 4 + critAdd;
                classicThrowingDamageField.SetValue(throwingPlayer, damage);
                classicThrowingCritField.SetValue(throwingPlayer, crit);
            }
        }

        /// <summary>
        /// 把「基准伤害」换算成当前盗贼面板值（红魔三叉戟用）：
        /// 现代版走真·盗贼类的 <c>GetDamage(...).ApplyTo(base)</c>；经典版照它自己的公式
        /// <c>base × (throwingDamage + 玩家召唤伤害 additive)</c>；两者都失败时退回现代版写法。
        /// </summary>
        public static int GetRogueScaledDamage(Player player, float baseDamage)
        {
            if (ModLoader.HasMod("CalamityMod"))
                return (int)player.GetDamage(GetRogueDamageClass()).ApplyTo(baseDamage);
            ProbeClassicThrowingBridge();
            if (classicThrowingDamageField != null && classicGetThrowingModPlayer != null
                && classicGetThrowingModPlayer.Invoke(player, null) is ModPlayer throwingPlayer
                && classicThrowingDamageField.GetValue(throwingPlayer) is float throwingDamage)
            {
                return (int)(baseDamage * (throwingDamage + player.GetDamage(DamageClass.Summon).Additive));
            }
            return (int)player.GetDamage(GetRogueDamageClass()).ApplyTo(baseDamage);
        }

        /// <summary>
        /// 一次性探测经典版 <c>CalamityCustomThrowingDamagePlayer</c> 的两个字段与取 ModPlayer 的方法，并缓存。
        /// </summary>
        private static void ProbeClassicThrowingBridge()
        {
            if (classicThrowingDamageProbed)
                return;
            classicThrowingDamageProbed = true;
            if (!ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
                return;
            Type throwingPlayerType = classic.Code.GetTypes()
                .FirstOrDefault(t => t.Name == "CalamityCustomThrowingDamagePlayer" && t.IsSubclassOf(typeof(ModPlayer)));
            if (throwingPlayerType == null)
                return;
            FieldInfo damageField = throwingPlayerType.GetField("throwingDamage", BindingFlags.Public | BindingFlags.Instance);
            FieldInfo critField = throwingPlayerType.GetField("throwingCrit", BindingFlags.Public | BindingFlags.Instance);
            FieldInfo velocityField = throwingPlayerType.GetField("throwingVelocity", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo getter = typeof(Player).GetMethod("GetModPlayer", Type.EmptyTypes)?.MakeGenericMethod(throwingPlayerType);
            if (damageField == null || critField == null || getter == null)
                return;
            classicThrowingDamageField = damageField;
            classicThrowingCritField = critField;
            classicThrowingVelocityField = velocityField;
            classicGetThrowingModPlayer = getter;
        }

        /// <summary>是否已探测过经典版投掷字段（无论成败只探测一次）</summary>
        private static bool classicThrowingDamageProbed;

        /// <summary>
        /// 盗贼**弹速**加成：现代版走灾厄官方 ModCall（<c>AddRogueVelocity</c>，2.0.4 与 1.4.4-release 都有）；
        /// 经典版没有对应 Call → 反射给 <c>CalamityCustomThrowingDamagePlayer.throwingVelocity</c> 加算
        /// （基准 1f，每帧复位，所以同样必须每帧调用）。
        /// 目前只有弑神者盗贼头的「满血时盗贼全属性 +10%」用得到这条。
        /// </summary>
        public static void AddRogueVelocity(Player player, float add)
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
                calamity.Call("AddRogueVelocity", player, add);
            ProbeClassicThrowingBridge();
            if (classicThrowingVelocityField == null || classicGetThrowingModPlayer == null)
                return;
            if (classicGetThrowingModPlayer.Invoke(player, null) is ModPlayer throwingPlayer)
            {
                float velocity = classicThrowingVelocityField.GetValue(throwingPlayer) is float v ? v + add : 1f + add;
                classicThrowingVelocityField.SetValue(throwingPlayer, velocity);
            }
        }

        /// <summary>是否已探测过经典版的盗贼弹幕标记（无论成败只探测一次）</summary>
        private static bool classicRogueProjProbed;
        /// <summary>经典版 CalamityGlobalProjectile.rogue 的字段句柄</summary>
        private static FieldInfo classicRogueFlagField;
        /// <summary>Projectile.GetGlobalProjectile&lt;CalamityGlobalProjectile&gt;() 的方法句柄</summary>
        private static MethodInfo classicGetGlobalProjectile;

        /// <summary>
        /// 判定一个弹幕算不算「盗贼」弹幕（龙蒿/魔影盗贼套的「每 25 次盗贼暴击」计数用）。
        /// <para>
        /// 现代版：灾厄盗贼武器在 SetDefaults 里写 <c>Item.DamageType = RogueDamageClass.Instance</c>，
        /// 弹幕继承该类型 → <c>CountsAsClass(真·盗贼类)</c> 即可命中。
        /// </para>
        /// <para>
        /// 经典版：它的盗贼弹幕**没有 DamageType**（武器不带类型，伤害靠
        /// <c>CalamityCustomThrowingDamagePlayer.throwingDamage</c> 那个自定义倍率结算），
        /// 盗贼身份记在它自己的 <c>CalamityGlobalProjectile.rogue</c> 布尔上（由各盗贼弹幕自己置位）
        /// → 只能反射读该全局弹幕实例的字段。
        /// </para>
        /// <para>两版各查一次（允许同时装两个模组），任一命中即算盗贼。只在暴击命中路径上调用，开销可接受。</para>
        /// </summary>
        public static bool IsRogueProjectile(Projectile projectile)
        {
            if (ModLoader.HasMod("CalamityMod") && projectile.CountsAsClass(GetRogueDamageClass()))
                return true;
            return IsClassicRogueProjectile(projectile);
        }

        /// <summary>
        /// 经典版判定：反射读 <c>CalamityGlobalProjectile.rogue</c>。句柄只在首次调用时探测并缓存。
        /// </summary>
        private static bool IsClassicRogueProjectile(Projectile projectile)
        {
            if (!classicRogueProjProbed)
            {
                classicRogueProjProbed = true;
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
                {
                    Type classicGlobalProjType = classic.Code.GetTypes()
                        .FirstOrDefault(t => t.Name == "CalamityGlobalProjectile" && t.IsSubclassOf(typeof(GlobalProjectile)));
                    FieldInfo rogueField = classicGlobalProjType?.GetField("rogue", BindingFlags.Public | BindingFlags.Instance);
                    MethodInfo getter = classicGlobalProjType == null
                        ? null
                        : typeof(Projectile).GetMethod("GetGlobalProjectile", Type.EmptyTypes)?.MakeGenericMethod(classicGlobalProjType);
                    if (rogueField != null && getter != null)
                    {
                        classicRogueFlagField = rogueField;
                        classicGetGlobalProjectile = getter;
                    }
                }
            }
            if (classicRogueFlagField == null || classicGetGlobalProjectile == null)
                return false;
            return classicGetGlobalProjectile.Invoke(projectile, null) is GlobalProjectile classicGlobal
                && classicRogueFlagField.GetValue(classicGlobal) is bool isRogue && isRogue;
        }
    }
}
