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
            ProbeClassicStealthBridge();
            if (classicRogueStealthMaxField == null || classicGetModPlayerForStealth == null)
                return;
            if (classicGetModPlayerForStealth.Invoke(player, null) is ModPlayer classicPlayer)
            {
                // 读现值再加：经典版自己的盗贼饰品/套装也会往这个字段上加，直接赋值会盖掉它们
                float current = classicRogueStealthMaxField.GetValue(classicPlayer) is float f ? f : 0f;
                classicRogueStealthMaxField.SetValue(classicPlayer, current + maxStealth);
            }
        }

        /// <summary>
        /// 一次性探测经典版的 <c>CalamityPlayerPreTrailer.rogueStealthMax</c> 与其取 ModPlayer 的方法并缓存。
        /// </summary>
        private static void ProbeClassicStealthBridge()
        {
            if (classicStealthProbed)
                return;
            classicStealthProbed = true;
            if (!ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
                return;
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

        /// <summary>
        /// 读当前盗贼潜行上限：现代版走官方 ModCall <c>GetMaxStealth</c>，经典版反射读 <c>rogueStealthMax</c>；
        /// 两边都拿不到就返回 0。
        /// </summary>
        public static float GetRogueStealthMax(Player player)
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) && calamity.Call("GetMaxStealth", player) is float modernMax)
                return modernMax;
            ProbeClassicStealthBridge();
            if (classicRogueStealthMaxField != null && classicGetModPlayerForStealth != null
                && classicGetModPlayerForStealth.Invoke(player, null) is ModPlayer classicPlayer
                && classicRogueStealthMaxField.GetValue(classicPlayer) is float classicMax)
            {
                return classicMax;
            }
            return 0f;
        }

        /// <summary>
        /// 按**当前潜行上限的比例**再追加一档（CI 的弑神者盗贼头写法：<c>rogueStealthMax += 当前上限 / 7</c>，
        /// 即 ratio = 1/7）。必须在 <see cref="GrantRogueStealth"/> 之后调用——它读的是"加上基础档之后"的实时上限，
        /// 所以会连带把玩家其它盗贼装备给的上限一起按比例放大，与 CI 的行为一致。
        /// </summary>
        public static void GrantRogueStealthRatio(Player player, float ratio)
        {
            float current = GetRogueStealthMax(player);
            if (current > 0f)
                GrantRogueStealth(player, current * ratio);
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

        // ── 潜行打击桥（纳米技术用）──

        /// <summary>是否已探测过灾厄 GlobalProjectile 的潜行打击字段（无论成败只探测一次）</summary>
        private static bool calamityStealthStrikeProbed;
        /// <summary>CalamityMod.Projectiles.CalamityGlobalProjectile.stealthStrike 的字段句柄</summary>
        private static FieldInfo calamityStealthStrikeField;
        /// <summary>CalamityMod.Projectiles.CalamityGlobalProjectile.stealthStrikeHitCount 的字段句柄</summary>
        private static FieldInfo calamityStealthStrikeHitCountField;
        /// <summary>Projectile.GetGlobalProjectile&lt;CalamityGlobalProjectile&gt;() 的方法句柄</summary>
        private static MethodInfo calamityGetGlobalProjectileForStealth;

        /// <summary>
        /// 判断这枚弹幕是不是「盗贼潜行打击」打出来的，并给出它已经命中过几次
        /// （<paramref name="hitCount"/>；拿不到计数字段时给 0）。纳米技术用它决定要不要砸下纳米闪光。
        /// <para>
        /// 现代版灾厄把这两个状态放在 <c>CalamityMod.Projectiles.CalamityGlobalProjectile</c> 上
        /// （<c>stealthStrike</c> / <c>stealthStrikeHitCount</c>，2.2.2 实测字段名与类型一致），
        /// 本工程是软依赖，只能反射读——句柄在首次调用时探测并缓存，失败也记为已探测。
        /// </para>
        /// <para>
        /// 经典版灾厄没有潜行打击这套东西（它的盗贼走自定义投掷倍率，全局弹幕上没有 stealthStrike），
        /// 所以本方法对经典版恒为 false —— 纳米技术的潜行打击部分是现代版独占的。
        /// </para>
        /// </summary>
        public static bool IsStealthStrike(Projectile projectile, out int hitCount)
        {
            hitCount = 0;
            ProbeCalamityStealthStrikeBridge();
            if (calamityStealthStrikeField == null || calamityGetGlobalProjectileForStealth == null)
                return false;
            if (!(calamityGetGlobalProjectileForStealth.Invoke(projectile, null) is GlobalProjectile globalProj))
                return false;
            if (!(calamityStealthStrikeField.GetValue(globalProj) is bool strike) || !strike)
                return false;
            if ((calamityStealthStrikeHitCountField?.GetValue(globalProj)) is int count)
                hitCount = count;
            return true;
        }

        /// <summary>
        /// 一次性探测灾厄侧的潜行打击字段与取全局弹幕实例的方法并缓存；任一步失败都放弃并记为已探测。
        /// </summary>
        private static void ProbeCalamityStealthStrikeBridge()
        {
            if (calamityStealthStrikeProbed)
                return;
            calamityStealthStrikeProbed = true;
            if (!ModLoader.TryGetMod("CalamityMod", out Mod calamity))
                return;
            Type globalProjType = calamity.Code.GetTypes()
                .FirstOrDefault(t => t.Name == "CalamityGlobalProjectile" && t.IsSubclassOf(typeof(GlobalProjectile)));
            if (globalProjType == null)
                return;   // 灾厄改了类名：放弃
            FieldInfo strikeField = globalProjType.GetField("stealthStrike",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (strikeField == null)
                return;   // 该版本没有潜行打击字段：放弃
            MethodInfo getter = typeof(Projectile).GetMethod("GetGlobalProjectile", Type.EmptyTypes)?.MakeGenericMethod(globalProjType);
            if (getter == null)
                return;
            calamityStealthStrikeField = strikeField;
            calamityStealthStrikeHitCountField = globalProjType.GetField("stealthStrikeHitCount",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            calamityGetGlobalProjectileForStealth = getter;
        }

        // ── 潜行打击消耗档桥（欺诈硬币 / 毁灭徽章用）──

        /// <summary>是否已探测过现代版 CalamityPlayer 的潜行打击消耗字段（无论成败只探测一次）</summary>
        private static bool calamityStealthCostProbed;
        /// <summary>CalamityPlayer.stealthStrikeHalfCost 的字段句柄</summary>
        private static FieldInfo calamityStealthHalfCostField;
        /// <summary>CalamityPlayer.stealthStrike75Cost 的字段句柄</summary>
        private static FieldInfo calamityStealth75CostField;
        /// <summary>CalamityPlayer.stealthStrike90Cost 的字段句柄</summary>
        private static FieldInfo calamityStealth90CostField;
        /// <summary>Player.GetModPlayer&lt;CalamityPlayer&gt;() 的方法句柄</summary>
        private static MethodInfo calamityGetModPlayerForStealthCost;

        /// <summary>
        /// 把盗贼「潜行打击」的消耗降到潜行上限的某个比例：<paramref name="costRatio"/> 取
        /// 0.5（半价 = <c>stealthStrikeHalfCost</c>）/ 0.75（<c>stealthStrike75Cost</c>）/
        /// 0.9（<c>stealthStrike90Cost</c>），只把对应的那一个布尔置真。
        /// <para>
        /// 灾厄在它自己的 ResetEffects 里每帧把这三个字段复位、在攻击结算（<c>StealthStrikeAvailable</c> /
        /// <c>ConsumeStealthByAttacking</c>）时按 half → 75 → 90 的优先级读取，所以本方法必须每帧调用、
        /// 且调用点要晚于它的 ResetEffects——本工程挂在 <c>PostUpdateEquips</c>。
        /// 三个档位字段在 2.0 / 2.0.3.9 / 2.0.4 / 已装 2.2.2 里都存在，走反射即可
        ///（2.0.3.9+ 还短暂有过 85% 档，已被删除，这里不涉及）。
        /// </para>
        /// <para>
        /// 经典版灾厄没有潜行打击这套机制（它的盗贼走自定义投掷倍率），本方法直接短路。
        /// </para>
        /// </summary>
        public static void SetStealthStrikeCost(Player player, float costRatio)
        {
            ProbeCalamityStealthCostBridge();
            if (calamityGetModPlayerForStealthCost == null)
                return;
            FieldInfo target = costRatio <= 0.5f ? calamityStealthHalfCostField
                : costRatio >= 0.9f ? calamityStealth90CostField
                : calamityStealth75CostField;
            if (target == null)
                return;
            if (calamityGetModPlayerForStealthCost.Invoke(player, null) is ModPlayer calamityPlayer)
                target.SetValue(calamityPlayer, true);
        }

        /// <summary>
        /// 一次性探测现代版 <c>CalamityPlayer</c> 的三个潜行打击消耗字段与取 ModPlayer 的方法并缓存；
        /// 任一步失败都放弃并记为已探测（<c>halfCost</c>/<c>75Cost</c> 至少要有，缺 <c>90Cost</c> 无妨）。
        /// </summary>
        private static void ProbeCalamityStealthCostBridge()
        {
            if (calamityStealthCostProbed)
                return;
            calamityStealthCostProbed = true;
            if (!ModLoader.TryGetMod("CalamityMod", out Mod calamity))
                return;
            Type calamityPlayerType = calamity.Code.GetTypes()
                .FirstOrDefault(t => t.Name == "CalamityPlayer" && t.IsSubclassOf(typeof(ModPlayer)));
            if (calamityPlayerType == null)
                return;   // 灾厄改了类名：放弃
            FieldInfo halfField = calamityPlayerType.GetField("stealthStrikeHalfCost",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo cost75Field = calamityPlayerType.GetField("stealthStrike75Cost",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo getter = typeof(Player).GetMethod("GetModPlayer", Type.EmptyTypes)?.MakeGenericMethod(calamityPlayerType);
            if (halfField == null || cost75Field == null || getter == null)
                return;   // 该版本没有这套字段：放弃
            calamityStealthHalfCostField = halfField;
            calamityStealth75CostField = cost75Field;
            calamityStealth90CostField = calamityPlayerType.GetField("stealthStrike90Cost",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            calamityGetModPlayerForStealthCost = getter;
        }

        // ── 潜行加速旗标桥（暗物质剑鞘 / 蚀日魔镜用）──

        /// <summary>是否已探测过现代版 CalamityPlayer 的两个潜行加速旗标（无论成败只探测一次）</summary>
        private static bool calamityStealthFlagProbed;
        /// <summary>CalamityPlayer.darkGodSheath 的字段句柄（暗物质剑鞘的移动加速）</summary>
        private static FieldInfo calamityDarkGodSheathField;
        /// <summary>CalamityPlayer.eclipseMirror 的字段句柄（蚀日魔镜的移动加速 + 深渊仇恨压制）</summary>
        private static FieldInfo calamityEclipseMirrorField;
        /// <summary>Player.GetModPlayer&lt;CalamityPlayer&gt;() 的方法句柄</summary>
        private static MethodInfo calamityGetModPlayerForStealthFlag;

        /// <summary>
        /// 置位灾厄 <c>CalamityPlayer.darkGodSheath</c>：让盗贼在**移动时**的潜行恢复来一点加速
        ///（灾厄 <c>UpdateStealthGenStats</c> 里的 <c>darkGodSheath → stealthAcceleration += 0.01f</c>）。
        /// 与其它潜行字段同理：灾厄在 ResetEffects 里每帧复位、之后才读取，故必须每帧调用且挂在 PostUpdateEquips。
        /// 经典版灾厄没有这套字段，会直接短路（暗物质剑鞘的"移动加速"是现代独占）。
        /// </summary>
        public static void SetDarkGodSheath(Player player) => SetCalamityStealthFlag(player, isEclipseMirror: false);

        /// <summary>
        /// 置位灾厄 <c>CalamityPlayer.eclipseMirror</c>：蚀日魔镜的移动潜行**指数加速**
        ///（<c>stealthAcceleration += 0.01f; *= 1.0084f</c>，与 darkGodSheath 同时挂时更猛）与
        /// 「深渊里也压制仇恨」（<c>range *= 0.3f</c>）。同样每帧、PostUpdateEquips。
        /// <para>
        /// 注意：1.4.4 世系里这个字段**只**管上面两条——闪避是由物品自己在
        /// <c>DodgeEffects</c> 里注册的，所以置位它**不会**让灾厄自己去闪避，不会与本工程自写的闪避打架
        ///（已按 1.4.4-release 源码逐处核对：eclipseMirror 的引用只有潜行加速 / 深渊压制 / 墨炸弹联动三处）。
        /// </para>
        /// </summary>
        public static void SetEclipseMirror(Player player) => SetCalamityStealthFlag(player, isEclipseMirror: true);

        /// <summary>置位两个潜行加速旗标中的一个（共用同一套探测与取 ModPlayer 句柄）。</summary>
        private static void SetCalamityStealthFlag(Player player, bool isEclipseMirror)
        {
            ProbeCalamityStealthFlagBridge();
            FieldInfo target = isEclipseMirror ? calamityEclipseMirrorField : calamityDarkGodSheathField;
            if (target == null || calamityGetModPlayerForStealthFlag == null)
                return;
            if (calamityGetModPlayerForStealthFlag.Invoke(player, null) is ModPlayer calamityPlayer)
                target.SetValue(calamityPlayer, true);
        }

        /// <summary>
        /// 一次性探测现代版 <c>CalamityPlayer</c> 的 <c>darkGodSheath</c> / <c>eclipseMirror</c> 两个字段
        /// 与取 ModPlayer 的方法并缓存（任一字段缺失只让对应那条短路，不影响另一条）。
        /// </summary>
        private static void ProbeCalamityStealthFlagBridge()
        {
            if (calamityStealthFlagProbed)
                return;
            calamityStealthFlagProbed = true;
            if (!ModLoader.TryGetMod("CalamityMod", out Mod calamity))
                return;
            Type calamityPlayerType = calamity.Code.GetTypes()
                .FirstOrDefault(t => t.Name == "CalamityPlayer" && t.IsSubclassOf(typeof(ModPlayer)));
            if (calamityPlayerType == null)
                return;   // 灾厄改了类名：放弃
            MethodInfo getter = typeof(Player).GetMethod("GetModPlayer", Type.EmptyTypes)?.MakeGenericMethod(calamityPlayerType);
            if (getter == null)
                return;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            calamityDarkGodSheathField = calamityPlayerType.GetField("darkGodSheath", flags);
            calamityEclipseMirrorField = calamityPlayerType.GetField("eclipseMirror", flags);
            calamityGetModPlayerForStealthFlag = getter;
        }

        // ── 潜行恢复速度桥（幻影魔镜用）──

        /// <summary>是否已探测过灾厄 CalamityPlayer 的潜行恢复字段（无论成败只探测一次）</summary>
        private static bool calamityStealthGenProbed;
        /// <summary>CalamityPlayer.stealthGenStandstill 的字段句柄</summary>
        private static FieldInfo calamityStealthGenStandstillField;
        /// <summary>CalamityPlayer.stealthGenMoving 的字段句柄</summary>
        private static FieldInfo calamityStealthGenMovingField;
        /// <summary>Player.GetModPlayer&lt;CalamityPlayer&gt;() 的方法句柄</summary>
        private static MethodInfo calamityGetModPlayerForStealthGen;

        /// <summary>
        /// 提高盗贼潜行恢复速度：给灾厄的 <c>stealthGenStandstill</c> / <c>stealthGenMoving</c> 加算
        /// （两字段基准 1f，灾厄在它自己的 ResetEffects 里每帧复位、在 PostUpdateMiscEffects 之后才读取，
        /// 所以必须每帧调用且调用点要晚于它的 ResetEffects——本工程挂在 <c>PostUpdateEquips</c>）。
        /// <para>
        /// 灾厄的 Mod.Call 列表里**没有**潜行恢复这一项（只有 AddMaxStealth / AddRogueVelocity / 各类潜行查询），
        /// 故这里走反射；经典版灾厄**根本没有这两个字段**（它的盗贼是旧的自定义投掷倍率 + modStealth），
        /// 所以本方法对经典版直接短路——潜行恢复加成是现代版独占的，与纳米技术的潜行打击同理。
        /// </para>
        /// <para>
        /// 换算口径（1.4.4 世系）：站定每帧恢复 = (潜行上限 / 4 秒) × stealthGenStandstill；
        /// 移动 = 上式 × 0.5 × stealthGenMoving × stealthAcceleration。即 +0.25f = 站定回满从 4 秒缩到 3.2 秒。
        /// </para>
        /// </summary>
        public static void AddStealthGen(Player player, float standstillAdd, float movingAdd)
        {
            ProbeCalamityStealthGenBridge();
            if (calamityStealthGenStandstillField == null || calamityGetModPlayerForStealthGen == null)
                return;
            if (calamityGetModPlayerForStealthGen.Invoke(player, null) is ModPlayer calamityPlayer)
            {
                // 读现值再加：灾厄自己的潜行饰品/药水也往这两个字段上加，直接赋值会盖掉它们
                float standstill = calamityStealthGenStandstillField.GetValue(calamityPlayer) is float s ? s + standstillAdd : 1f + standstillAdd;
                calamityStealthGenStandstillField.SetValue(calamityPlayer, standstill);
                if (calamityStealthGenMovingField != null)
                {
                    float moving = calamityStealthGenMovingField.GetValue(calamityPlayer) is float m ? m + movingAdd : 1f + movingAdd;
                    calamityStealthGenMovingField.SetValue(calamityPlayer, moving);
                }
            }
        }

        /// <summary>
        /// 一次性探测灾厄 CalamityPlayer 的两个潜行恢复字段与取 ModPlayer 的方法并缓存；
        /// 任一步失败都放弃并记为已探测。
        /// </summary>
        private static void ProbeCalamityStealthGenBridge()
        {
            if (calamityStealthGenProbed)
                return;
            calamityStealthGenProbed = true;
            if (!ModLoader.TryGetMod("CalamityMod", out Mod calamity))
                return;
            Type calamityPlayerType = calamity.Code.GetTypes()
                .FirstOrDefault(t => t.Name == "CalamityPlayer" && t.IsSubclassOf(typeof(ModPlayer)));
            if (calamityPlayerType == null)
                return;   // 灾厄改了类名：放弃
            FieldInfo standstillField = calamityPlayerType.GetField("stealthGenStandstill",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (standstillField == null)
                return;   // 该版本没有潜行恢复字段：放弃
            MethodInfo getter = typeof(Player).GetMethod("GetModPlayer", Type.EmptyTypes)?.MakeGenericMethod(calamityPlayerType);
            if (getter == null)
                return;
            calamityStealthGenStandstillField = standstillField;
            calamityStealthGenMovingField = calamityPlayerType.GetField("stealthGenMoving",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            calamityGetModPlayerForStealthGen = getter;
        }

        // ── 当前潜行值桥（深渊 / 日蚀魔镜的闪避回潜行用）──

        /// <summary>是否已探测过现代版 CalamityPlayer.rogueStealth（无论成败只探测一次）</summary>
        private static bool calamityStealthValueProbed;
        /// <summary>CalamityPlayer.rogueStealth 的字段句柄</summary>
        private static FieldInfo calamityStealthValueField;
        /// <summary>Player.GetModPlayer&lt;CalamityPlayer&gt;() 的方法句柄</summary>
        private static MethodInfo calamityGetModPlayerForStealthValue;
        /// <summary>是否已探测过经典版 CalamityPlayerPreTrailer.rogueStealth（无论成败只探测一次）</summary>
        private static bool classicStealthValueProbed;
        /// <summary>CalamityPlayerPreTrailer.rogueStealth 的字段句柄</summary>
        private static FieldInfo classicStealthValueField;
        /// <summary>Player.GetModPlayer&lt;CalamityPlayerPreTrailer&gt;() 的方法句柄</summary>
        private static MethodInfo classicGetModPlayerForStealthValue;

        /// <summary>
        /// 给玩家的**当前潜行值**（不是上限）加一笔，用于镜子闪避的"回潜行"
        /// （源写法：<c>rogueStealth += 0.5f</c>，内部 1f = 显示 100 点）。
        /// <para>
        /// 灾厄的 Mod.Call 只提供读取（<c>GetStealth</c> / <c>GetCurrentStealth</c>），**没有写入项**，
        /// 所以两版都走反射：现代版写 <c>CalamityPlayer.rogueStealth</c>，经典版写
        /// <c>CalamityPlayerPreTrailer.rogueStealth</c>（两版同名字段，都已实测存在）。
        /// 与 <see cref="GrantRogueStealth"/> 一样，两边在位就两边都加（工程既有的双版本惯例）。
        /// </para>
        /// </summary>
        public static void AddRogueStealthValue(Player player, float add)
        {
            ProbeStealthValueBridge();
            if (calamityStealthValueField != null && calamityGetModPlayerForStealthValue != null
                && calamityGetModPlayerForStealthValue.Invoke(player, null) is ModPlayer calamityPlayer)
            {
                float current = calamityStealthValueField.GetValue(calamityPlayer) is float c ? c + add : add;
                calamityStealthValueField.SetValue(calamityPlayer, current);
            }
            if (classicStealthValueField != null && classicGetModPlayerForStealthValue != null
                && classicGetModPlayerForStealthValue.Invoke(player, null) is ModPlayer classicPlayer)
            {
                float current = classicStealthValueField.GetValue(classicPlayer) is float c ? c + add : add;
                classicStealthValueField.SetValue(classicPlayer, current);
            }
        }

        /// <summary>
        /// 把玩家的**当前潜行值**直接顶到上限（蚀日魔镜闪避的"回满潜行"用；源写法
        /// <c>rogueStealth = rogueStealthMax</c>）。上限走 <see cref="GetRogueStealthMax"/>
        /// （现代版 Mod.Call / 经典版反射），写入复用与 <see cref="AddRogueStealthValue"/> 相同的字段句柄。
        /// </summary>
        public static void SetRogueStealthToMax(Player player)
        {
            ProbeStealthValueBridge();
            float max = GetRogueStealthMax(player);
            if (max <= 0f)
                return;
            if (calamityStealthValueField != null && calamityGetModPlayerForStealthValue != null
                && calamityGetModPlayerForStealthValue.Invoke(player, null) is ModPlayer calamityPlayer)
            {
                calamityStealthValueField.SetValue(calamityPlayer, max);
            }
            if (classicStealthValueField != null && classicGetModPlayerForStealthValue != null
                && classicGetModPlayerForStealthValue.Invoke(player, null) is ModPlayer classicPlayer)
            {
                classicStealthValueField.SetValue(classicPlayer, max);
            }
        }

        /// <summary>
        /// 一次性探测两版 CalamityPlayer 的 <c>rogueStealth</c> 字段与取 ModPlayer 的方法并缓存
        /// （现代版 <c>CalamityPlayer</c> / 经典版 <c>CalamityPlayerPreTrailer</c>，两版同名字段，都已实测存在）。
        /// </summary>
        private static void ProbeStealthValueBridge()
        {
            if (!calamityStealthValueProbed)
            {
                calamityStealthValueProbed = true;
                if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
                {
                    Type calamityPlayerType = calamity.Code.GetTypes()
                        .FirstOrDefault(t => t.Name == "CalamityPlayer" && t.IsSubclassOf(typeof(ModPlayer)));
                    FieldInfo stealthField = calamityPlayerType?.GetField("rogueStealth",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo getter = calamityPlayerType == null
                        ? null
                        : typeof(Player).GetMethod("GetModPlayer", Type.EmptyTypes)?.MakeGenericMethod(calamityPlayerType);
                    if (stealthField != null && getter != null)
                    {
                        calamityStealthValueField = stealthField;
                        calamityGetModPlayerForStealthValue = getter;
                    }
                }
            }
            if (!classicStealthValueProbed)
            {
                classicStealthValueProbed = true;
                if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
                {
                    Type classicPlayerType = classic.Code.GetTypes()
                        .FirstOrDefault(t => t.Name == "CalamityPlayerPreTrailer" && t.IsSubclassOf(typeof(ModPlayer)));
                    FieldInfo stealthField = classicPlayerType?.GetField("rogueStealth",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    MethodInfo getter = classicPlayerType == null
                        ? null
                        : typeof(Player).GetMethod("GetModPlayer", Type.EmptyTypes)?.MakeGenericMethod(classicPlayerType);
                    if (stealthField != null && getter != null)
                    {
                        classicStealthValueField = stealthField;
                        classicGetModPlayerForStealthValue = getter;
                    }
                }
            }
        }
    }
}
