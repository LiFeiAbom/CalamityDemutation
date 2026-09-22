using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Systems.Graphic
{
    /// <summary>
    /// 帽子附加层（移植自灾厄 HatExtensionLayer） - 挂在原版头部绘制层之后，
    /// 由实现了 <see cref="IExtendedHat"/> 的头部装备触发：用与头部完全相同的帧号
    /// 取附加层贴图的对应行，并按头部的颜色、旋转与染色一并绘制。
    /// 位置对齐沿用原版头部那套算法，最后叠加实现方给出的自定义偏移。
    /// </summary>
    internal class HatExtensionLayer:PlayerDrawLayer
    {
        /// <summary>
        /// 绘制位置：原版头部层之后
        /// </summary>
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
        /// <summary>
        /// 可见性：非残影，或玩家尚未死亡时绘制
        /// </summary>
        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => drawInfo.shadow == 0f || !drawInfo.drawPlayer.dead;
        /// <summary>
        /// 取当前头部装备（换装槽有内容时优先取换装槽），判定是否实现 IExtendedHat；
        /// 再校验该物品的头部装备槽与玩家当前头部槽一致，符合则追加附加层绘制数据。
        /// <see cref="IExtendedHat.EquipSlotName"/> 留空时**直接取装备自身的 <c>Item.headSlot</c>**
        /// （而不是拿物品名去 <c>EquipLoader.GetEquipSlot</c> 查表）：两者在正常情况是同一个值，
        /// 但查表失败会返回 -1 而静默不画——2026-09-22 排查"附加层不显示"时改成这个更直白的写法
        /// </summary>
        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player drawPlayer = drawInfo.drawPlayer;
            Item headItem = drawPlayer.armor[0];
            if (drawPlayer.armor[10].type > ItemID.None)
                headItem = drawPlayer.armor[10];
            if (ModContent.GetModItem(headItem.type) is IExtendedHat extendedHat)
            {
                string equipSlotName = extendedHat.EquipSlotName(drawPlayer);
                int equipSlot = equipSlotName != "" ? EquipLoader.GetEquipSlot(Mod, equipSlotName, EquipType.Head) : headItem.headSlot;
                if (extendedHat.PreDrawExtension(drawInfo) && !drawInfo.drawPlayer.dead && equipSlot == drawPlayer.head)
                {
                    // 必须用 drawInfo.Position 而非 Player.Position，否则人物选择界面与地图上的头部层会错位
                    Vector2 headDrawPosition = drawInfo.Position - Main.screenPosition;
                    // 宽高取自 drawPlayer 没问题，只做居中
                    headDrawPosition += new Vector2((drawPlayer.width - drawPlayer.bodyFrame.Width) / 2f, drawPlayer.height - drawPlayer.bodyFrame.Height + 4f);
                    headDrawPosition = new Vector2((int)headDrawPosition.X, (int)headDrawPosition.Y);  // 取整消除逐帧抖动
                    headDrawPosition += drawPlayer.headPosition + drawInfo.headVect;
                    headDrawPosition += extendedHat.ExtensionSpriteOffset(drawInfo);
                    Texture2D extraPieceTexture = ModContent.Request<Texture2D>(extendedHat.ExtensionTexture).Value;
                    // 按玩家身体帧号取附加层对应行，与头部保持同一帧
                    Rectangle frame = extraPieceTexture.Frame(1, 20, 0, drawPlayer.bodyFrame.Y / drawPlayer.bodyFrame.Height);
                    DrawData pieceDrawData = new DrawData(extraPieceTexture, headDrawPosition, frame, drawInfo.colorArmorHead, drawPlayer.headRotation, drawInfo.headVect, 1f, drawInfo.playerEffect, 0)
                    {
                        shader = drawPlayer.dye?[0].dye ?? 0
                    };
                    drawInfo.DrawDataCache.Add(pieceDrawData);
                }
            }
        }
    }
}
