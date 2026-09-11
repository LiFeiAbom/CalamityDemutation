using CalamityDemutation.Content.Projectiles;
using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
namespace CalamityDemutation.Utilities
{
    /// <summary>
    /// 通用工具类（挥砍武器部分）：提供 BaseSwingCO 所需的纹理/帧动画/向量扩展，
    /// 以及 CWR()/Calamity() 的软依赖等价扩展（返回本模组自身的 ModPlayer/GlobalProjectile）。
    /// </summary>
    internal static partial class CDUtil
    {
        /// <summary>
        /// 获取当前手持物品（对应 CWR 的 Player.ActiveItem()）
        /// </summary>
        public static Item ActiveItem(this Player player) => player.HeldItem;
        /// <summary>
        /// 检测玩家是否存活活跃（对应 CWR 的 Alives）
        /// </summary>
        public static bool Alives(this Player player) => player != null && player.active && !player.dead;
        /// <summary>
        /// 灾厄原生 Calamity() 的软依赖等价物：返回本模组弹幕数据（承载 timesPierced）
        /// </summary>
        public static CalamityDemutationGlobalProjectile Calamity(this Projectile projectile)
        {
            return projectile.GetGlobalProjectile<CalamityDemutationGlobalProjectile>();
        }
        /// <summary>
        /// 整帧间隔切换动画帧，到达最大帧后回绕
        /// </summary>
        public static void ClockFrame(ref int frameCounter, int intervalFrame, int maxFrame)
        {
            if (Main.GameUpdateCount % intervalFrame == 0)
            {
                frameCounter++;
            }
            if (frameCounter > maxFrame)
            {
                frameCounter = 0;
            }
        }
        /// <summary>
        /// 获取本模组玩家数据（对应 CWR 的 Player.CWR()，承载 SwingIndex 等挥砍状态）
        /// </summary>
        public static CalamityDemutationPlayer CWR(this Player player)
        {
            return player.GetModPlayer<CalamityDemutationPlayer>();
        }
        /// <summary>
        /// 获取本模组弹幕数据（对应 CWR 的 Projectile.CWR()）
        /// </summary>
        public static CalamityDemutationGlobalProjectile CWR(this Projectile projectile)
        {
            return projectile.GetGlobalProjectile<CalamityDemutationGlobalProjectile>();
        }
        /// <summary>
        /// 返回向量的法线（垂直）向量
        /// </summary>
        public static Vector2 GetNormalVector(this Vector2 vr)
        {
            Vector2 nVr = new(vr.Y, -vr.X);
            return Vector2.Normalize(nVr);
        }
        /// <summary>
        /// 按总帧数计算单帧缩放中心（对应 CWR 的 GetOrig）
        /// </summary>
        public static Vector2 GetOrig(Texture2D value, int frameCounterMax = 1)
        {
            float singleFrameY = value.Height / frameCounterMax;
            return new Vector2(value.Width * 0.5f, singleFrameY / 2);
        }
        /// <summary>
        /// 获取玩家稳定中心（对应 CWR 的 GetPlayerStabilityCenter）
        /// </summary>
        public static Vector2 GetPlayerStabilityCenter(this Player player) => player.MountedCenter.Floor() + new Vector2(0, player.gfxOffY);
        /// <summary>
        /// 按帧数与当前帧计算竖直切片矩形（用于多帧贴图取帧）
        /// </summary>
        public static Rectangle GetRec(Texture2D value, int frame, int frameCounterMax = 1)
        {
            int singleFrameY = value.Height / frameCounterMax;
            return new Rectangle(0, singleFrameY * frame, value.Width, singleFrameY);
        }
        /// <summary>
        /// 按资源路径立即加载并返回一张 Texture2D
        /// </summary>
        public static Texture2D GetT2DValue(string texture, bool immediateLoad = false)
        {
            return ModContent.Request<Texture2D>(texture, immediateLoad ? AssetRequestMode.AsyncLoad : AssetRequestMode.ImmediateLoad).Value;
        }
        /// <summary>
        /// 检测玩家是否按下左/右键（对应 CWR 的 PressKey，netCed 为 true 时仅在本地玩家生效）
        /// </summary>
        public static bool PressKey(this Player player, bool leftCed = true, bool netCed = true) => (!netCed || Main.myPlayer == player.whoAmI) && (leftCed ? PlayerInput.Triggers.Current.MouseLeft : PlayerInput.Triggers.Current.MouseRight);
        /// <summary>
        /// 随机方向单位向量乘以 0~max 随机长度（对应 CWR 的 randVr）
        /// </summary>
        public static Vector2 randVr(int max)
        {
            return Main.rand.NextVector2Unit() * Main.rand.Next(0, max);
        }
        /// <summary>
        /// 把二维向量提升为 Z 分量为 0 的三维向量
        /// </summary>
        public static Vector3 Vec3(this Vector2 vector) => new Vector3(vector.X, vector.Y, 0);
    }
}
