using Terraria.ModLoader;
namespace CalamityDemutation.Players
{
    /// <summary>
    /// SHPC（传奇法师枪械）的玩家侧状态，按第 5 节口径单独开一个 partial：
    /// 武器状态一律放 ModPlayer（不放 ModItem 字段），每名玩家各持一份，联机不会互相串号。
    /// 递减点在 <c>CalamityDemutationPlayer.ResetEffects</c>（每帧一次）。
    /// </summary>
    internal partial class CalamityDemutationPlayer : ModPlayer
    {
        /// <summary>
        /// SHPC 右键能量束附带的"追踪相位球"冷却（帧）：读到 0 才补发一颗、随即置 60
        /// （源：CI `DestroyerLegendary` 用的玩家侧 <c>fireCD</c>；那边是通用开火冷却，本工程只服务这一处）。
        /// </summary>
        public int shpcPhaseOrbCooldown = 0;
    }
}
