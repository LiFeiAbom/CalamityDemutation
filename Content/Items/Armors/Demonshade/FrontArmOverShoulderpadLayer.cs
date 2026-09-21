using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Demonshade
{
    /// <summary>
    /// 前臂盖肩甲层（移植自灾厄 FrontArmOverShoulderpadLayer） - 挂在原版"手臂盖住手持物"层之后，
    /// 由实现了 <see cref="IDrawArmOverShoulderpad"/> 的胸甲触发，用与身体完全相同的帧号
    /// 取前臂贴图的对应行，重绘到肩甲之上。位置与染色均跟随身体。
    /// </summary>
    internal class FrontArmOverShoulderpadLayer:PlayerDrawLayer
    {
        /// <summary>
        /// 绘制位置：原版手臂盖住手持物层之后
        /// </summary>
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.ArmOverItem);
        /// <summary>
        /// 可见性：非残影，或玩家尚未死亡时绘制
        /// </summary>
        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => drawInfo.shadow == 0f || !drawInfo.drawPlayer.dead;
        /// <summary>
        /// 取当前胸甲装备（换装槽有内容时优先取换装槽），判定是否实现 IDrawArmOverShoulderpad；
        /// 再校验该物品的身体装备槽与玩家当前身体槽一致，符合则追加前臂绘制数据
        /// </summary>
        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player drawPlayer = drawInfo.drawPlayer;
            Item bodyItem = drawPlayer.armor[1];
            if (drawPlayer.armor[11].type > ItemID.None)
                bodyItem = drawPlayer.armor[11];
            if (ModContent.GetModItem(bodyItem.type) is IDrawArmOverShoulderpad frontArm)
            {
                string equipSlotName = frontArm.EquipSlotName(drawPlayer) != "" ? frontArm.EquipSlotName(drawPlayer) : bodyItem.ModItem.Name;
                int equipSlot = EquipLoader.GetEquipSlot(Mod, equipSlotName, EquipType.Body);
                if (drawPlayer.body != equipSlot)
                    return;
                int dyeShader = drawPlayer.dye?[1].dye ?? 0;
                // 必须用 drawInfo.Position 而非 Player.Position，否则人物选择界面与地图上会错位
                Vector2 drawPosition = drawInfo.Position - Main.screenPosition;
                // 宽高取自 drawPlayer 没问题，只做居中
                drawPosition += new Vector2((drawPlayer.width - drawPlayer.bodyFrame.Width) / 2f, drawPlayer.height - drawPlayer.bodyFrame.Height + 4f);
                drawPosition = new Vector2((int)drawPosition.X, (int)drawPosition.Y);  // 取整消除逐帧抖动
                drawPosition += drawPlayer.bodyPosition + drawInfo.bodyVect;
                Texture2D extraPieceTexture = ModContent.Request<Texture2D>(frontArm.FrontArmTexture).Value;
                // 按玩家身体帧号取前臂贴图对应行，与身体保持同一帧
                Rectangle frame = extraPieceTexture.Frame(1, 20, 0, drawPlayer.bodyFrame.Y / drawPlayer.bodyFrame.Height);
                DrawData pieceDrawData = new DrawData(extraPieceTexture, drawPosition, frame, drawInfo.colorArmorBody, drawPlayer.fullRotation, drawInfo.bodyVect, 1f, drawInfo.playerEffect, 0)
                {
                    shader = dyeShader
                };
                drawInfo.DrawDataCache.Add(pieceDrawData);
            }
        }
    }
}
