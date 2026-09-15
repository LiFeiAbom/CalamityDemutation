using System;
using System.Collections.Generic;
using CalamityDemutation.Systems.Cooldowns;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
namespace CalamityDemutation.Systems.UI
{
    /// <summary>
    /// 冷却机架 UI（移植自灾厄 2.2.2）：把本地玩家身上所有 ShouldDisplay 的冷却条
    /// 横向排在屏幕左上角（buff 栏下方），随距离鼠标远近调整亮度，悬停显示冷却名称。
    /// </summary>
    internal class CooldownRackUI
    {
        /// <summary>
        /// 展开模式下最多能画多少条冷却，超过后自动切换成紧凑模式
        /// </summary>
        public static int MaxLargeIcons = 10;
        /// <summary>
        /// 是否使用紧凑图标：
        /// 原为灾厄客户端配置项 CalamityClientConfig.CooldownDisplay（默认 Full），本工程无对应配置类，
        /// 此处硬编码默认值 Full —— 仅按冷却数量自动切换紧凑模式
        /// </summary>
        public static bool CompactIcons
        {
            get
            {
                return Main.LocalPlayer.GetDisplayedCooldowns().Count > MaxLargeIcons;
            }
        }
        /// <summary>
        /// 紧凑模式下图标横向间距
        /// </summary>
        public const float CompactXSpacing = 28f;
        /// <summary>
        /// 展开模式下图标横向间距
        /// </summary>
        public const float ExpandedXScaling = 46f;
        /// <summary>
        /// 当前模式下的图标横向间距
        /// </summary>
        public static Vector2 Spacing => CompactIcons ? Vector2.UnitX * CompactXSpacing : Vector2.UnitX * ExpandedXScaling;
        /// <summary>
        /// 冷却条的起始绘制位置（仿灾厄默认值）：左上角 (32, 100) 再加半个间距，
        /// 并随 buff 行数下移，避免和原版 buff 图标重叠
        /// </summary>
        public static Vector2 BaseDrawPosition => new Vector2(32, 100) + Spacing / 2f + Vector2.UnitY * 50 * MathF.Ceiling(Main.LocalPlayer.CountBuffs() / 11f);
        /// <summary>
        /// 调试开关：开启后所有冷却条以满完成度、满亮度绘制
        /// </summary>
        public static bool DebugFullDisplay = false;
        /// <summary>
        /// 调试开关开启时强制使用的完成度
        /// </summary>
        public static float DebugForceCompletion = 0f;
        /// <summary>
        /// 绘制冷却机架：游戏未开始或背包打开时不绘制
        /// </summary>
        public static void Draw(SpriteBatch spriteBatch)
        {
            // 不绘制的两种情况：1 - 还没进入游戏画面；2 - 玩家打开了背包
            // （原灾厄第三条"CalamityClientConfig.CooldownDisplay == Hidden"属客户端配置，本工程硬编码为默认的 Full，故略去）
            if (Main.gameMenu || Main.playerInventory)
                return;
            IList<CooldownInstance> cooldownsToDraw = Main.LocalPlayer.GetDisplayedCooldowns();
            if (cooldownsToDraw.Count == 0)
                return;
            float uiScale = 1f;   // 真实 UI 缩放会自动应用；此处再用 Main.UIScale 会导致缩放叠加两次而过大
            Vector2 displayPosition = BaseDrawPosition;
            int rectangleSide = (int)Math.Floor(CompactIcons ? 24 * uiScale : 52 * uiScale);
            Rectangle iconRectangle = new Rectangle((int)displayPosition.X - rectangleSide / 2, (int)displayPosition.Y - rectangleSide / 2, rectangleSide, rectangleSide);
            Rectangle mouse = new Rectangle((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 8, 8);
            string mouseHover = "";
            float iconOpacityScale = (float)Math.Sin(Main.GlobalTimeWrappedHourly) * 0.1f + 0.6f;
            Vector2 mouseCenter = mouse.Center.ToVector2();
            float opacity = MathHelper.Clamp((float)Math.Sin(Main.GlobalTimeWrappedHourly % MathHelper.Pi) * 2f, 0, 1) * 0.1f + 0.9f;
            foreach (CooldownInstance instance in cooldownsToDraw)
            {
                CooldownHandler handler = instance.handler;
                float iconOpacity = iconOpacityScale;
                // 鼠标越靠近图标越亮
                iconOpacity += 0.3f * (1 - MathHelper.Clamp(Vector2.Distance(mouseCenter, iconRectangle.Center.ToVector2()), 0f, 80f) / 80f);
                if (iconRectangle.Intersects(mouse))
                {
                    mouseHover = handler.DisplayName.ToString();
                    iconOpacity = opacity;
                }
                if (DebugFullDisplay)
                    iconOpacity = 1f;
                if (CompactIcons)
                    handler.DrawCompact(spriteBatch, displayPosition, iconOpacity, uiScale);
                else
                    handler.DrawExpanded(spriteBatch, displayPosition, iconOpacity, uiScale);
                displayPosition += Spacing;
                iconRectangle.X += (int)Spacing.X;
            }
            if (mouseHover != "")
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.instance.MouseText(mouseHover);
            }
        }
    }
}
