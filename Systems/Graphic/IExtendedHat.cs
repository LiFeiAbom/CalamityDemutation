using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
namespace CalamityDemutation.Systems.Graphic
{
    /// <summary>
    /// 可扩展帽子接口（移植自灾厄 IExtendedHat） - 由装备在头部槽位的物品实现，
    /// 让该头盔在原版头部绘制完成后，再额外叠加一张附加层贴图。
    /// 用于标准 _Head.png（固定 20 帧 × 40×56）塞不下的造型，
    /// 例如恶魔之影头盔上半的兜帽与角就单独画在 _Extension 贴图里。
    /// 实际绘制见 <see cref="HatExtensionLayer"/>。
    /// </summary>
    internal interface IExtendedHat
    {
        /// <summary>
        /// 附加层贴图路径
        /// </summary>
        string ExtensionTexture { get; }
        /// <summary>
        /// 附加层相对头部的像素偏移。自动绘制时头部挂载点的偏移已由绘制层处理，
        /// 这里只需给附加层自身的额外位移
        /// </summary>
        Vector2 ExtensionSpriteOffset(PlayerDrawSet drawInfo);
        /// <summary>
        /// 返回 true 表示由绘制层按 <see cref="ExtensionTexture"/> 与 <see cref="ExtensionSpriteOffset"/> 自动绘制；
        /// 返回 false 表示实现方自行绘制附加层
        /// </summary>
        bool PreDrawExtension(PlayerDrawSet drawInfo) => true;
        /// <summary>
        /// 物品对应的头部装备槽名；留空则取物品名。
        /// 同一物品拥有多张头部贴图时用于区分具体画哪一张
        /// </summary>
        string EquipSlotName(Player drawPlayer) => "";
    }
}
