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
    }
}
