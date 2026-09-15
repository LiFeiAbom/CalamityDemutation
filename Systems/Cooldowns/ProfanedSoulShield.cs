using System;
using CalamityDemutation.Content.Items.Accessories.Function;
using CalamityDemutation.Effects;
using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
namespace CalamityDemutation.Systems.Cooldowns
{
    /// <summary>
    /// 亵渎之魂护盾"耐久条"冷却（移植自灾厄 2.2.2 的 ProfanedSoulShield）：
    /// duration 固定为耐久上限，timeLeft 始终等于当前耐久，因此圆环长度就是护盾剩余量；
    /// 只在装备神器且护盾耐久大于 0 时显示，展开/紧凑模式下都会在图标中央画出剩余耐久数字。
    /// </summary>
    internal class ProfanedSoulShield:CooldownHandler
    {
        /// <summary>
        /// 本模组玩家数据（护盾耐久字段的持有者）
        /// </summary>
        private CalamityDemutationPlayer CalPlayer => instance.player.CWR();
        /// <summary>
        /// 环形进度条的起点/终点颜色（对应灾厄神器形态的 GetColor(true) / GetColor(false)）
        /// </summary>
        private static readonly Color StartColor = new Color(235, 178, 96);
        private static readonly Color EndColor = new Color(219, 179, 121);
        /// <summary>
        /// 按耐久上限（而非 duration）计算的完成度，令条形长度精确对应剩余耐久
        /// </summary>
        private float AdjustedCompletion => instance.timeLeft / (float)ProfanedSoulArtifact.ShieldDurabilityMax;
        /// <summary>
        /// 冷却的唯一字符串 ID（灾厄原值即 ProfanedSoulShieldDurability）
        /// </summary>
        public static new string ID => "ProfanedSoulShieldDurability";
        /// <summary>
        /// 装备神器且还有护盾时不自行倒计时（耐久由玩家类的护盾逻辑驱动）；未装备或耐久归零时才允许倒计时以清空该条
        /// </summary>
        public override bool CanTickDown => !CalPlayer.profanedSoulArtifact || instance.timeLeft <= 0;
        /// <summary>
        /// 仅在装备神器且护盾耐久大于 0 时显示
        /// </summary>
        public override bool ShouldDisplay => CalPlayer.profanedSoulArtifact && CalPlayer.profanedSoulShieldDurability > 0;
        /// <summary>
        /// 冷却名称（原为 CalamityUtils.GetText 的灾厄本地化条目）
        /// </summary>
        public override LocalizedText DisplayName => Language.GetOrRegister("Mods.CalamityDemutation.Systems.UI.Cooldowns.ProfanedSoulShieldDurability", () => "Profaned Soul Shield Durability");
        /// <summary>
        /// 图标/描边/遮罩贴图
        /// </summary>
        public override string Texture => "CalamityDemutation/Systems/Cooldowns/ProfanedSoulShieldActive";
        public override string OutlineTexture => "CalamityDemutation/Systems/Cooldowns/ProfanedSoulShieldOutline";
        public override string OverlayTexture => "CalamityDemutation/Systems/Cooldowns/ProfanedSoulShieldOverlay";
        /// <summary>
        /// 描边与遮罩颜色，以及按完成度在起止色之间插值的环形渐变色
        /// </summary>
        public override Color OutlineColor => new Color(255, 191, 73);
        public override Color CooldownStartColor => Color.Lerp(StartColor, EndColor, instance.Completion);
        public override Color CooldownEndColor => Color.Lerp(StartColor, EndColor, instance.Completion);
        /// <summary>
        /// 不随存档保存、不跨死亡保留
        /// </summary>
        public override bool SavedWithPlayer => false;
        public override bool PersistsThroughDeath => false;
        /// <summary>
        /// 环形进度条改用"按耐久上限计算的完成度"，令圆弧长度直接反映剩余耐久
        /// </summary>
        public override void ApplyBarShaders(float opacity)
        {
            GameShaders.Misc[CDShaders.CircularBarShaderName].UseOpacity(opacity);
            GameShaders.Misc[CDShaders.CircularBarShaderName].UseSaturation(AdjustedCompletion);
            GameShaders.Misc[CDShaders.CircularBarShaderName].UseColor(CooldownStartColor);
            GameShaders.Misc[CDShaders.CircularBarShaderName].UseSecondaryColor(CooldownEndColor);
            GameShaders.Misc[CDShaders.CircularBarShaderName].Apply();
        }
        /// <summary>
        /// 展开模式：先按基类画圆环与图标，再在中央叠上剩余耐久的描边数字
        /// </summary>
        public override void DrawExpanded(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            base.DrawExpanded(spriteBatch, position, opacity, scale);
            // 两位数与一位数用不同的横向偏移，使数字大致居中
            float Xoffset = instance.timeLeft > 9 ? -10f : -5;
            CDUtil.DrawBorderStringEightWay(spriteBatch, FontAssets.MouseText.Value, instance.timeLeft.ToString(), position + new Vector2(Xoffset, 4) * scale, Color.Lerp(StartColor, Color.OrangeRed, 1 - instance.Completion), Color.Black, scale);
        }
        /// <summary>
        /// 紧凑模式：描边 + 图标 + 按耐久占比自下而上裁剪的遮罩，并在中央叠上剩余耐久数字
        /// </summary>
        public override void DrawCompact(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            Texture2D sprite = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D outline = ModContent.Request<Texture2D>(OutlineTexture).Value;
            Texture2D overlay = ModContent.Request<Texture2D>(OverlayTexture).Value;
            // 绘制描边
            spriteBatch.Draw(outline, position, null, OutlineColor * opacity, 0, outline.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            // 绘制图标
            spriteBatch.Draw(sprite, position, null, Color.White * opacity, 0, sprite.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            // 绘制小遮罩：注意此处用的同样是 AdjustedCompletion（与基类取 Completion 不同）
            int lostHeight = (int)Math.Ceiling(overlay.Height * AdjustedCompletion);
            Rectangle crop = new Rectangle(0, lostHeight, overlay.Width, overlay.Height - lostHeight);
            spriteBatch.Draw(overlay, position + Vector2.UnitY * lostHeight * scale, crop, OutlineColor * opacity * 0.9f, 0, sprite.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            float Xoffset = instance.timeLeft > 9 ? -10f : -5;
            CDUtil.DrawBorderStringEightWay(spriteBatch, FontAssets.MouseText.Value, instance.timeLeft.ToString(), position + new Vector2(Xoffset, 4) * scale, Color.Lerp(StartColor, Color.OrangeRed, 1 - instance.Completion), Color.Black, scale);
        }
    }
    /// <summary>
    /// 亵渎之魂护盾"回充条"冷却（移植自灾厄 2.2.2 的 ProfanedSoulShieldRecharge）：
    /// 只在护盾被打空后的回充延迟期间显示，进度即剩余延迟；
    /// 延迟走完（玩家类开始逐帧回充）时由玩家类移除该冷却，不再像灾厄那样额外赠 1 点耐久。
    /// </summary>
    internal class ProfanedSoulShieldRecharge:CooldownHandler
    {
        /// <summary>
        /// 环形进度条的起止插值颜色
        /// </summary>
        private static Color ringColorLerpStart = new Color(217, 159, 78);
        private static Color ringColorLerpEnd = new Color(214, 185, 144);
        /// <summary>
        /// 冷却的唯一字符串 ID
        /// </summary>
        public static new string ID => "ProfanedSoulShieldRecharge";
        /// <summary>
        /// 只要实例存在就显示（加入/移除由玩家类的护盾逻辑控制）
        /// </summary>
        public override bool ShouldDisplay => true;
        /// <summary>
        /// 冷却名称（原为 CalamityUtils.GetText 的灾厄本地化条目）
        /// </summary>
        public override LocalizedText DisplayName => Language.GetOrRegister("Mods.CalamityDemutation.Systems.UI.Cooldowns.ProfanedSoulShieldRecharge", () => "Profaned Soul Shield Recharge");
        /// <summary>
        /// 图标/描边/遮罩贴图
        /// </summary>
        public override string Texture => "CalamityDemutation/Systems/Cooldowns/ProfanedSoulShieldRecharge";
        public override string OutlineTexture => "CalamityDemutation/Systems/Cooldowns/ProfanedSoulShieldOutline";
        public override string OverlayTexture => "CalamityDemutation/Systems/Cooldowns/ProfanedSoulShieldOverlay";
        /// <summary>
        /// 不随存档保存、不跨死亡保留
        /// </summary>
        public override bool SavedWithPlayer => false;
        public override bool PersistsThroughDeath => false;
        /// <summary>
        /// 描边颜色与环形渐变色
        /// </summary>
        public override Color OutlineColor => new Color(57, 195, 237);
        public override Color CooldownStartColor => Color.Lerp(ringColorLerpStart, ringColorLerpEnd, instance.Completion);
        public override Color CooldownEndColor => Color.Lerp(ringColorLerpStart, ringColorLerpEnd, instance.Completion);
        /// <summary>
        /// 延迟结束时的音效：原为灾厄的 Providence.BurnStartSound，本工程改用原版等价音效
        /// </summary>
        public override SoundStyle? EndSound => SoundID.Item29;
        /// <summary>
        /// 仅在装备神器时播放结束音效
        /// </summary>
        public override bool ShouldPlayEndSound => instance.player.CWR().profanedSoulArtifact;
    }
}
