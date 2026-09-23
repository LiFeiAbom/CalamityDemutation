using System;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Systems
{
    /// <summary>
    /// 解除灾厄「复仇 ↔ 专家、死亡 ↔ 大师」的难度绑定（挂 <see cref="ConfigSystem.UnbindCalamityDifficulty"/> 开关，默认关）。
    ///
    /// 灾厄 2.2.x 起把原版 Expert / Master 也做成了难度表里的两档，于是「复仇=专家档、死亡=大师档」成了设计本身，
    /// 由两层一起保证：
    /// ① setter 层：<c>RevengeanceDifficulty.BackBoneGameModeID =&gt; Expert</c>、<c>DeathDifficulty.BackBoneGameModeID =&gt; Master</c>，
    ///    在难度界面点选它们时会顺手把 <see cref="Main.GameMode"/> 改成对应的骨架难度；
    /// ② 纠偏层：<c>DifficultyModeSystem.PostUpdateWorld</c> 每帧检查——专家档下发现死亡就降回复仇、大师档下发现复仇就升成死亡，
    ///    且走 <c>broadcast: true</c>（聊天栏提示 + 音效），所以"每帧顶回去"会刷屏，不能那么做。
    ///
    /// ② 只能运行时接管灾厄那个方法来绕过，本类即做这件事：在调用原方法前后把两个难度标志"藏起来"
    /// （<c>CalamityWorld.death / revenge</c> 临时置 false），灾厄的纠偏看不到违规组合，因而不降级、不发声、不发提示；
    /// 调用结束原样还原，并把被 ① 改掉的 <see cref="Main.GameMode"/> 顶回世界真实难度。
    /// 只有在「世界真实难度 + 当前想要的那一档」构成跨档组合时才介入，其余情况（含 FTW、旅程模式）一律原样放行。
    ///
    /// 灾厄缺席、版本改名或反射失配时本类整体静默失效（只打一条警告），不会影响游戏启动。
    /// </summary>
    internal sealed class CalamityDifficultyUnbind : ModSystem
    {
        /// <summary>灾厄 <c>CalamityWorld.death</c> 静态字段</summary>
        private static FieldInfo deathField;
        /// <summary>灾厄 <c>CalamityWorld.revenge</c> 静态字段</summary>
        private static FieldInfo revengeField;
        /// <summary>世界加载时的真实难度快照；灾厄 setter 会改写 Main.GameMode，故不能用它当判据</summary>
        private static int worldGameMode = GameModeID.Normal;
        /// <summary>钩子是否已挂上；未挂上时全程不介入</summary>
        private static bool hookInstalled;
        /// <summary>本模组实例；静态钩子里打日志用（Mod 是实例属性）</summary>
        private static Mod mod;
        /// <summary>本次世界是否已记录过"开始介入"的日志（每次世界加载重置，避免刷屏）</summary>
        private static bool loggedIntercept;
        /// <summary>灾厄加载后挂钩：反射拿到难度系统与两个世界标志，用 MonoModHooks 接管 PostUpdateWorld</summary>
        public override void PostSetupContent()
        {
            mod = Mod;
            if (!ModLoader.TryGetMod("CalamityMod", out Mod calamity))
                return;
            Type systemType = calamity.Code?.GetType("CalamityMod.Systems.DifficultyModeSystem");
            Type worldType = calamity.Code?.GetType("CalamityMod.World.CalamityWorld");
            MethodInfo target = systemType?.GetMethod("PostUpdateWorld", BindingFlags.Public | BindingFlags.Instance);
            deathField = worldType?.GetField("death", BindingFlags.Public | BindingFlags.Static);
            revengeField = worldType?.GetField("revenge", BindingFlags.Public | BindingFlags.Static);
            if (target == null || deathField == null || revengeField == null)
            {
                mod.Logger.Warn("[难度解绑] 灾厄难度系统反射失配（可能是灾厄版本改名），本功能保持关闭");
                return;
            }
            try
            {
                // 钩子委托的构造要按运行时的灾厄类型来（软依赖编译期拿不到 DifficultyModeSystem 这个名字），
                // 且必须是一次干净的静态绑定：Expression 现编译出来的 lambda 带闭包，委托 Target 非空，
                // MonoMod 会把它当成"绑了实例对象的委托"而拒绝（Target method is static, but a target object was provided）。
                // 用泛型静态方法 MakeGenericMethod 后 CreateDelegate，签名天生精确匹配、无闭包，orig 还能直接强类型调用
                Type origType = typeof(Action<>).MakeGenericType(systemType);
                Type hookType = typeof(Action<,>).MakeGenericType(origType, systemType);
                MethodInfo hookMethod = typeof(CalamityDifficultyUnbind).GetMethod(nameof(PostUpdateWorldHook), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(systemType);
                MonoModHooks.Add(target, hookMethod.CreateDelegate(hookType));
                hookInstalled = true;
            }
            catch (Exception e)
            {
                mod.Logger.Warn("[难度解绑] 钩子挂载失败，本功能保持关闭：" + e.Message);
            }
        }
        /// <summary>世界加载时记录真实难度（此刻灾厄还没机会改写 Main.GameMode）</summary>
        public override void PostWorldLoad()
        {
            worldGameMode = Main.GameMode;
            loggedIntercept = false;
        }
        /// <summary>世界卸载后回到默认值，避免菜单状态残留影响下一个世界</summary>
        public override void OnWorldUnload()
        {
            worldGameMode = GameModeID.Normal;
        }
        /// <summary>
        /// 接管灾厄的 <c>DifficultyModeSystem.PostUpdateWorld</c>：跨档组合时藏起难度标志再调原方法，其余情况直接放行。
        /// 泛型参数由 <c>MakeGenericMethod</c> 在运行时绑成灾厄的难度系统类型；<paramref name="orig"/> 是原始方法委托。
        /// </summary>
        private static void PostUpdateWorldHook<T>(Action<T> orig, T self) where T : ModSystem
        {
            if (!hookInstalled || ConfigSystem.Instance?.UnbindCalamityDifficulty != true || Main.gameMenu)
            {
                orig(self);
                return;
            }
            try
            {
                bool wantDeath = (bool)deathField.GetValue(null);
                bool wantRevenge = (bool)revengeField.GetValue(null);
                // 只在"反档"组合时介入：专家世界要死亡、大师世界只要复仇。FTW 的档位映射是另一套，交回灾厄原样处理
                bool expertWantsDeath = !Main.getGoodWorld && worldGameMode == GameModeID.Expert && wantDeath;
                bool masterWantsRevenge = !Main.getGoodWorld && worldGameMode == GameModeID.Master && wantRevenge && !wantDeath;
                if (!expertWantsDeath && !masterWantsRevenge)
                {
                    orig(self);
                    return;
                }
                try
                {
                    // 每次世界加载只记一条，用来区分"钩子没生效"与"灾厄的纠偏已经不在这个方法里"
                    if (!loggedIntercept)
                    {
                        loggedIntercept = true;
                        mod.Logger.Info("[难度解绑] 已介入跨档组合（世界难度 " + worldGameMode + "，死亡 " + wantDeath + "，复仇 " + wantRevenge + "）");
                    }
                    // 藏起来：灾厄纠偏读到的全是 false，既不会 SwitchToDifficulty（无提示无音效），也不会改难度标志
                    deathField.SetValue(null, false);
                    revengeField.SetValue(null, false);
                    orig(self);
                }
                finally
                {
                    deathField.SetValue(null, wantDeath);
                    revengeField.SetValue(null, wantRevenge);
                    // setter 层：灾厄"启用难度"时会顺手把世界改成该难度的骨架难度，这里顶回世界真实难度
                    if (Main.GameMode != worldGameMode)
                        Main.GameMode = worldGameMode;
                }
            }
            catch (Exception e)
            {
                // 实验性功能：任何意外都不该让游戏卡进每帧崩溃，直接自废并回落到灾厄原行为
                hookInstalled = false;
                mod.Logger.Warn("[难度解绑] 运行时异常，已自动关闭本功能：" + e.Message);
            }
        }
    }
}
