using Terraria;
namespace CalamityDemutation.Content.Items.Armors.Demonshade
{
    /// <summary>
    /// 前臂盖肩甲接口（移植自灾厄 IDrawArmOverShoulderpad） - 由胸甲实现，
    /// 用于把走路时露在身前的那条手臂重绘在肩甲之上，避免前臂被肩甲挡住。
    /// 注意：tModLoader 1.4 起 _Arms.png 不再随 AutoloadEquip(EquipType.Body) 自动加载
    /// （1.4 把 Body / Arms / FemaleBody 三张合并进单张 360×224 的 _Body.png），
    /// 因此这张前臂贴图只有靠本接口对应的绘制层才会被使用。
    /// 实际绘制见 <see cref="FrontArmOverShoulderpadLayer"/>。
    /// </summary>
    internal interface IDrawArmOverShoulderpad
    {
        /// <summary>
        /// 前臂贴图路径；尺寸需与常规装备贴图一致（40×1120，即 20 帧 × 40×56）
        /// </summary>
        string FrontArmTexture { get; }
        /// <summary>
        /// 物品对应的身体装备槽名；留空则取物品名
        /// </summary>
        string EquipSlotName(Player drawPlayer) => "";
    }
}
