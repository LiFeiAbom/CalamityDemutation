using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using System;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Tiles
{
    /// <summary>
    /// 蜡烛/塑像增益共存（挂"还原灾厄内容削弱"开关，仅现代版灾厄生效）。
    /// <para>
    /// 灾厄把向导出售的 4 根蜡烛（恢复 ResilientCandle / 失重 WeightlessCandle / 活力 VigorousCandle /
    /// 恶念 SpitefulCandle）与 2 座塑像（腐化 CorruptionEffigy / 血腥 CrimsonEffigy）做成"右键开启、同类互斥"：
    /// 方块自己的 RightClick 会先 ClearBuff 掉本组全部同类增益，再给自己 AddBuff 108000 帧（Ammo Box 时长）。
    /// 蜡烛一组 4 个互相排斥、塑像一组 2 个互相排斥，两组之间互不影响；灾厄侧除此之外没有任何其他排斥，
    /// 两个塑像的负面（腐化 -10% 减伤 / 血腥 -10% 最大生命）随之一起叠加，所以补回来就是真正的共存。
    /// </para>
    /// <para>
    /// 拦不住、只能补：tModLoader 的 TileLoader.RightClick 先调 ModTile.RightClick、再遍历 GlobalTile 钩子
    /// （且丢弃钩子返回值），故本钩子跑在灾厄的清除之后，只能按"点击前"的快照把同类增益补回。
    /// 快照见 <see cref="CalamityDemutationPlayer.candleEffigyBuffMask"/>，每帧在 PostUpdateMiscEffects 末尾刷新。
    /// 音效无需重放——灾厄自己的 RightClick 已经播过了。
    /// </para>
    /// <para>
    /// 经典版灾厄没有这套机制（那边蜡烛/塑像是 NearbyEffects 靠近就给 20 帧，没有右键、没有互斥），
    /// 因此只按现代版灾厄的方块类名 + 增益类名查找，经典版一个字节都不动。
    /// </para>
    /// </summary>
    internal class CandleEffigyCoexistence:GlobalTile
    {
        /// <summary>增益时长：照抄灾厄方块里用的 Ammo Box 时长</summary>
        private const int buffDuration = 108000;
        /// <summary>灾厄蜡烛方块类名（顺序对应下方增益缓存的 0..3）</summary>
        private static readonly string[] candleTileNames = ["PurpleCandle", "BlueCandle", "PinkCandle", "YellowCandle"];
        /// <summary>灾厄塑像方块类名（顺序对应下方增益缓存的 4..5）</summary>
        private static readonly string[] effigyTileNames = ["CorruptionEffigy", "CrimsonEffigy"];
        /// <summary>对应的灾厄增益类名：0..3 蜡烛、4..5 塑像（与 ModPlayer 快照位掩码同序）</summary>
        private static readonly string[] buffNames = ["PurpleCandleBuff", "BlueCandleBuff", "PinkCandleBuff", "YellowCandleBuff", "CorruptionEffigyBuff", "CrimsonEffigyBuff"];
        /// <summary>6 个增益类型缓存：-1 未初始化，0 表示灾厄未安装或未找到</summary>
        private static readonly int[] buffTypes = [-1, -1, -1, -1, -1, -1];
        /// <summary>功能是否生效：开关打开且加载的是现代版灾厄</summary>
        private static bool Active => ConfigSystem.Instance?.RevertCalamityContentNerfs == true && ModLoader.HasMod("CalamityMod");
        /// <summary>懒加载 6 个增益类型（首次调用时按类名 TryFind 一次，之后复用）</summary>
        private static void EnsureBuffTypes()
        {
            if (buffTypes[0] != -1)
                return;
            for (int k = 0; k < buffNames.Length; k++)
                buffTypes[k] = ModContent.TryFind("CalamityMod", buffNames[k], out ModBuff buff) ? buff.Type : 0;
        }
        /// <summary>
        /// 生成本帧的快照位掩码（0..3 蜡烛、4..5 塑像），供方块右键后据此补回被清掉的同类增益。
        /// 由 CalamityDemutationPlayer.PostUpdateMiscEffects 每帧调用；未启用开关或无灾厄时恒为 0。
        /// </summary>
        public static int BuildMask(Player player)
        {
            if (!Active)
                return 0;
            EnsureBuffTypes();
            int mask = 0;
            for (int k = 0; k < buffTypes.Length; k++)
            {
                if (buffTypes[k] > 0 && player.HasBuff(buffTypes[k]))
                    mask |= 1 << k;
            }
            return mask;
        }
        /// <summary>灾厄蜡烛/塑像方块 → 增益组（0 蜡烛 / 1 塑像）；其他模组与原版方块返回 -1</summary>
        private static int GetBuffGroup(int tileType)
        {
            ModTile modTile = TileLoader.GetTile(tileType);
            if (modTile == null || modTile.Mod?.Name != "CalamityMod")
                return -1;
            if (Array.IndexOf(candleTileNames, modTile.Name) > -1)
                return 0;
            if (Array.IndexOf(effigyTileNames, modTile.Name) > -1)
                return 1;
            return -1;
        }
        /// <summary>
        /// 方块右键钩子：跑在灾厄 ModTile.RightClick（清除本组同类增益 + 给自己上增益）之后，
        /// 把快照里本组、被它清掉的那几个增益按原时长补回来，从而让同类增益共存。
        /// </summary>
        public override void RightClick(int i, int j, int type)
        {
            if (!Active)
                return;
            int group = GetBuffGroup(type);
            if (group < 0)
                return;
            Player player = Main.LocalPlayer;
            int mask = player.GetModPlayer<CalamityDemutationPlayer>().candleEffigyBuffMask;
            if (mask == 0)
                return;
            EnsureBuffTypes();
            // 蜡烛是 0..3、塑像是 4..5：灾厄只清本组，所以这里也只补本组
            int start = group == 0 ? 0 : candleTileNames.Length;
            int end = group == 0 ? candleTileNames.Length : buffTypes.Length;
            for (int k = start; k < end; k++)
            {
                if ((mask & (1 << k)) != 0 && buffTypes[k] > 0)
                    player.AddBuff(buffTypes[k], buffDuration);
            }
        }
    }
}
