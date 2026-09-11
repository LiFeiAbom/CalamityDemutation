using System;
namespace CalamityDemutation.Enums
{
    /// <summary>
    /// 泰拉整体绘制顺序中的一组锚点，供粒子等图形系统挂接绘制层。
    /// 移植自灾厄的 GeneralDrawLayer（完整保留全部层级，当前仅 AfterDusts 实际接线）。
    /// </summary>
    [Flags]
    public enum GeneralDrawLayer
    {
        BeforeAllTiles = 1 << 0,
        BeforeSolidTiles = 1 << 1,
        BeforeNPCs = 1 << 2,
        AfterNPCs = 1 << 3,
        BeforeProjectiles = 1 << 4,
        AfterProjectiles = 1 << 5,
        AfterPlayers = 1 << 6,
        AfterDusts = 1 << 7,
        AfterEverything = 1 << 8,
    }
}
