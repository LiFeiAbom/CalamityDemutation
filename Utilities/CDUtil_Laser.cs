using CalamityDemutation.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
namespace CalamityDemutation.Utilities
{
    /// <summary>
    /// 通用工具类（激光 / 手持弹幕部分）：为元素王者之剑系列的移植补上灾厄侧依赖的等价物——
    /// 残影绘制、激光的精确撞墙距离、全局弹幕查询与"能否继续持握"判据。
    /// </summary>
    internal static partial class CDUtil
    {
        /// <summary>
        /// 居中残影绘制（移植自灾厄 CalamityUtils.DrawAfterimagesCentered）。
        /// 按 TrailCacheLength 记录的 oldPos 逐帧重绘，三种模式：
        /// 0 = 普通（不透明度 100%→0）、1 = 圣骑士锤式（66%→0，可用 typeOneIncrement 抽稀）、
        /// 2 = 带每帧朝向的普通残影；模式非法时退化为直接画本体。
        /// 灾厄此处用 CalamityClientConfig.Afterimages 总开关，本工程改用 ConfigSystem.PerformanceMode（关闭残影的性能开关）。
        /// </summary>
        public static void DrawAfterimagesCentered(Projectile proj, int mode, Color lightColor, int typeOneIncrement = 1, Texture2D texture = null, bool drawCentered = true, bool shrink = false, int armorShaderToUse = 0)
        {
            if (texture is null)
                texture = TextureAssets.Projectile[proj.type].Value;
            int frameHeight = texture.Height / Main.projFrames[proj.type];
            int frameY = frameHeight * proj.frame;
            float scale = proj.scale;
            float rotation = proj.rotation;
            Rectangle rectangle = new Rectangle(0, frameY, texture.Width, frameHeight);
            Vector2 origin = rectangle.Size() / 2f;
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (proj.spriteDirection == -1)
                spriteEffects = SpriteEffects.FlipHorizontally;
            bool afterimagesEnabled = ConfigSystem.Instance?.PerformanceMode != true;
            // 模式非法导致一张残影都没画时，退化为画本体，避免弹幕整个消失
            bool failedToDrawAfterimages = false;
            if (afterimagesEnabled)
            {
                Vector2 centerOffset = drawCentered ? proj.Size / 2f : Vector2.Zero;
                Color alphaColor = proj.GetAlpha(lightColor);
                switch (mode)
                {
                    case 0:
                        for (int i = 0; i < proj.oldPos.Length; ++i)
                        {
                            Vector2 drawPos = proj.oldPos[i] + centerOffset - Main.screenPosition + new Vector2(0f, proj.gfxOffY);
                            // 这几处 float 强制转换不能省，去掉会破坏残影渐变
                            float interpolant = ((float)(proj.oldPos.Length - i) / (float)proj.oldPos.Length);
                            Color color = alphaColor * interpolant;
                            var drawData = new DrawData(texture, drawPos, rectangle, color)
                            {
                                rotation = rotation,
                                origin = origin,
                                effect = spriteEffects
                            };
                            GameShaders.Armor.Apply(armorShaderToUse, proj, drawData);
                            Main.spriteBatch.Draw(texture, drawPos, new Rectangle?(rectangle), color, rotation, origin, shrink ? scale * interpolant : scale, spriteEffects, 0f);
                        }
                        break;
                    case 1:
                        // 循环必须前进：抽稀间隔至少为 1
                        int increment = Math.Max(1, typeOneIncrement);
                        Color drawColor = alphaColor;
                        int afterimageCount = ProjectileID.Sets.TrailCacheLength[proj.type];
                        float afterimageColorCount = (float)afterimageCount * 1.5f;
                        int k = 0;
                        while (k < afterimageCount)
                        {
                            Vector2 drawPos = proj.oldPos[k] + centerOffset - Main.screenPosition + new Vector2(0f, proj.gfxOffY);
                            float interpolant = ((float)(proj.oldPos.Length - k) / (float)proj.oldPos.Length);
                            if (k > 0)
                            {
                                float colorMult = (float)(afterimageCount - k);
                                drawColor *= colorMult / afterimageColorCount;
                            }
                            var drawData = new DrawData(texture, drawPos, rectangle, drawColor)
                            {
                                rotation = rotation,
                                origin = origin,
                                effect = spriteEffects
                            };
                            GameShaders.Armor.Apply(armorShaderToUse, proj, drawData);
                            Main.spriteBatch.Draw(texture, drawPos, new Rectangle?(rectangle), drawColor, rotation, origin, shrink ? scale * interpolant : scale, spriteEffects, 0f);
                            k += increment;
                        }
                        break;
                    case 2:
                        for (int i = 0; i < proj.oldPos.Length; ++i)
                        {
                            float afterimageRot = proj.oldRot[i];
                            SpriteEffects sfxForThisAfterimage = proj.oldSpriteDirection[i] == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                            Vector2 drawPos = proj.oldPos[i] + centerOffset - Main.screenPosition + new Vector2(0f, proj.gfxOffY);
                            // 同上：float 强制转换不能省
                            float interpolant = ((float)(proj.oldPos.Length - i) / (float)proj.oldPos.Length);
                            Color color = alphaColor * interpolant;
                            var drawData = new DrawData(texture, drawPos, rectangle, color)
                            {
                                rotation = rotation,
                                origin = origin,
                                effect = spriteEffects
                            };
                            GameShaders.Armor.Apply(armorShaderToUse, proj, drawData);
                            Main.spriteBatch.Draw(texture, drawPos, new Rectangle?(rectangle), color, afterimageRot, origin, shrink ? scale * interpolant : scale, sfxForThisAfterimage, 0f);
                        }
                        break;
                    default:
                        failedToDrawAfterimages = true;
                        break;
                }
            }
            // 画本体：残影第 0 张就是本体，所以只在"没画残影"的几种情况下补画
            if (!afterimagesEnabled || ProjectileID.Sets.TrailCacheLength[proj.type] <= 0 || failedToDrawAfterimages)
            {
                Vector2 startPos = drawCentered ? proj.Center : proj.position;
                Vector2 drawPos = startPos - Main.screenPosition + new Vector2(0f, proj.gfxOffY);
                var drawData = new DrawData(texture, drawPos, rectangle, proj.GetAlpha(lightColor));
                GameShaders.Armor.Apply(armorShaderToUse, proj, drawData);
                Main.spriteBatch.Draw(texture, drawPos, rectangle, proj.GetAlpha(lightColor), rotation, origin, scale, spriteEffects, 0f);
            }
        }
        /// <summary>
        /// 从 start 沿 rotation 方向前进，返回首次撞到实心块的精确距离（移植自灾厄 CollisionUtils.PreciseDistanceToTileCollisionHit）。
        /// 按 step 逐点推进，命中实心块后按该块的斜坡/半砖形态判断真实接触点；整段无阻挡时返回 length。
        /// </summary>
        public static float PreciseDistanceToTileCollisionHit(Vector2 start, float rotation, float length, float step = 1)
        {
            Vector2 unitVect = rotation.ToRotationVector2();
            Vector2 end = unitVect * length;
            if (length < 1f)
            {
                Point endWorldPos = end.ToTileCoordinates();
                return IsSolidTileAt(endWorldPos.X, endWorldPos.Y) ? 0 : length;
            }
            Vector2 currentPos = start;
            Point lastAirPos = new Point(-1, -1);
            for (float i = 0; i < length; i += step)
            {
                currentPos += unitVect * step;
                Point tilePos = currentPos.ToTileCoordinates();
                if (tilePos == lastAirPos)
                    continue;
                if (!WorldGen.InWorld(tilePos.X, tilePos.Y))
                    continue;
                Tile tile = Main.tile[tilePos.X, tilePos.Y];
                if (!IsSolidTileAt(tilePos.X, tilePos.Y))
                {
                    lastAirPos = tilePos;
                    continue;
                }
                if (tile.Slope == SlopeType.Solid && !tile.IsHalfBlock)
                    return (currentPos - start).Length();
                Vector2 tileWorldPos = new Vector2(tilePos.X * 16, tilePos.Y * 16);
                Vector2 currentPosInTile = currentPos - tileWorldPos;
                if (tile.IsHalfBlock)
                {
                    if (currentPosInTile.Y >= 8f)
                        return (currentPos - start).Length();
                }
                else if (tile.Slope == SlopeType.SlopeDownLeft)
                {
                    if (currentPosInTile.X <= currentPosInTile.Y)
                        return (currentPos - start).Length();
                }
                else if (tile.Slope == SlopeType.SlopeDownRight)
                {
                    if ((16 - currentPosInTile.X) <= currentPosInTile.Y)
                        return (currentPos - start).Length();
                }
                else if (tile.Slope == SlopeType.SlopeUpLeft)
                {
                    if (currentPosInTile.X <= (16 - currentPosInTile.Y))
                        return (currentPos - start).Length();
                }
                else if (tile.Slope == SlopeType.SlopeUpRight)
                {
                    if (currentPosInTile.X >= currentPosInTile.Y)
                        return (currentPos - start).Length();
                }
            }
            return length;
        }
        /// <summary>
        /// 指定格是否为"实心块"（灾厄 Tile.IsTileSolid 的等价内联）：越界返回 false，
        /// 否则要求该格有未被致动器关闭的方块且对应 tileSolid 为实心。
        /// </summary>
        private static bool IsSolidTileAt(int x, int y)
        {
            if (!WorldGen.InWorld(x, y))
                return false;
            Tile tile = Main.tile[x, y];
            return tile.HasUnactuatedTile && Main.tileSolid[tile.TileType];
        }
        /// <summary>
        /// 场上是否已存在指定类型的弹幕（移植自灾厄 CalamityUtils.AnyProjectiles）
        /// </summary>
        public static bool AnyProjectiles(int projectileType)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].type == projectileType)
                    return true;
            }
            return false;
        }
        /// <summary>
        /// 玩家是否无法继续使用手持弹幕（移植自灾厄 PlayerUtils.CantUseHoldout）：
        /// 玩家无效/死亡/被控/物品栏被锁，或 needsToHold 且已松开使用键时返回 true
        /// </summary>
        /// <summary>
        /// 专供"左键挥砍 + 右键蓄力"这类**左右键共用一件物品**的武器使用的松开判据。
        /// 不能沿用 <see cref="CantUseHoldout"/>：那依赖 player.channel，而左右键共用一个物品时
        /// vanilla 的 channel 会被两键互相干扰，导致右键刚生出的手持弹幕当帧就被判为"已松开"而自杀。
        /// 这里只由弹幕主人为本机玩家时的鼠标右键状态（加上玩家可用性）决定；
        /// 远端玩家的链条不在本地销毁，等主人侧同步。
        /// </summary>
        public static bool HoldoutReleased(this Player player) => player.whoAmI == Main.myPlayer
            && (!Main.mouseRight || player == null || !player.active || player.dead || player.CCed || player.noItems);
        /// <summary>
        /// 玩家是否无法继续使用手持弹幕（移植自灾厄 PlayerUtils.CantUseHoldout）：
        /// 玩家无效/死亡/被控/物品栏被锁，或 needsToHold 且已松开使用键时返回 true
        /// </summary>
        public static bool CantUseHoldout(this Player player, bool needsToHold = true) => player == null || !player.active || player.dead || (!player.channel && needsToHold) || player.CCed || player.noItems;
    }
}
