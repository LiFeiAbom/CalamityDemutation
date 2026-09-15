using System;
using CalamityDemutation.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.Localization;
using Terraria.ModLoader;
namespace CalamityDemutation.Systems.Cooldowns
{
    /// <summary>
    /// 冷却处理器基类（移植自灾厄 2.2.2）：每个 CooldownInstance 都对应一个本类子类实例，
    /// 由它实现该冷却的游戏行为（Tick / OnCompleted）与 UI 绘制（DrawCompact / DrawExpanded）；
    /// 子类以 ModType 形式自动注册，并由 CooldownRegistry 在 ResizeArrays 阶段分配 netID。
    /// </summary>
    internal abstract class CooldownHandler:ModType
    {
        /// <summary>
        /// 冷却的唯一字符串 ID，由各子类以静态属性隐藏重写
        /// </summary>
        public static string ID => null;
        /// <summary>
        /// 本处理器所属的冷却实例（由 CooldownInstance 在构造时回填）
        /// </summary>
        public CooldownInstance instance;
        protected sealed override void Register()
        {
            ModTypeLookup<CooldownHandler>.Register(this);
        }
        #region Gameplay Behavior
        /// <summary>
        /// 冷却实例存活期间每帧调用一次
        /// </summary>
        public virtual void Tick() { }
        /// <summary>
        /// 冷却自然走完时调用（玩家死亡导致删除时不调用）
        /// </summary>
        public virtual void OnCompleted() { }
        /// <summary>
        /// 本帧是否允许冷却倒计时；例如可让"有 boss 存活时不计时"或"死亡期间不计时"的冷却沿用此开关
        /// </summary>
        public virtual bool CanTickDown => true;
        /// <summary>
        /// 置 true 时该冷却可跨死亡保留；为 false 的冷却在玩家死亡时立即消失
        /// </summary>
        public virtual bool PersistsThroughDeath => false;
        /// <summary>
        /// 置 true 时该冷却会随玩家存档序列化写入 modded player 文件
        /// </summary>
        public virtual bool SavedWithPlayer => true;
        /// <summary>
        /// 冷却结束时播放的音效，null 表示不播放
        /// </summary>
        public virtual SoundStyle? EndSound => null;
        /// <summary>
        /// 是否在冷却到期时播放结束音效，默认 true
        /// </summary>
        public virtual bool ShouldPlayEndSound => true;
        #endregion
        #region Display & Rendering
        /// <summary>
        /// 冷却名称，鼠标悬停在图标上时显示
        /// </summary>
        public virtual LocalizedText DisplayName => LocalizedText.Empty;
        /// <summary>
        /// 该冷却是否应该出现在冷却机架 UI 中
        /// </summary>
        public virtual bool ShouldDisplay => true;
        /// <summary>
        /// 冷却图标贴图<br/>
        /// <b>2x2 缩放时必须为 20x20 像素</b>
        /// </summary>
        public virtual string Texture => "";
        /// <summary>
        /// 紧凑模式下叠在图标之上的遮罩贴图
        /// </summary>
        public virtual string OverlayTexture => $"{Texture}Overlay";
        /// <summary>
        /// 绘制在图标底图外围的描边贴图
        /// </summary>
        public virtual string OutlineTexture => $"{Texture}Outline";
        internal static string DefaultChargeBarTexture = "CalamityDemutation/Systems/Cooldowns/BarBase";
        /// <summary>
        /// 冷却"充能条"（即着色器绘制的圆环）所用贴图；默认贴图走环形着色器，<br/>
        /// 一般只在展开模式下使用<br/>
        /// <b>2x2 缩放时必须为 44x44 像素</b>
        /// </summary>
        public virtual string ChargeBarTexture => DefaultChargeBarTexture;
        /// <summary>
        /// 充能条的背景贴图；保持默认时充能条下方不绘制任何东西<br/>
        /// <b>2x2 缩放时必须为 44x44 像素</b>
        /// </summary>
        public virtual string ChargeBarBackTexture => DefaultChargeBarTexture;
        /// <summary>
        /// 展开模式下图标描边的颜色；紧凑模式下该颜色用于图标上方的遮罩
        /// </summary>
        public virtual Color OutlineColor => Color.White;
        /// <summary>
        /// 着色器绘制的环形计时条起点颜色（仅在展开模式、且未指定充能条贴图时生效）
        /// </summary>
        public virtual Color CooldownStartColor => Color.Gray;
        /// <summary>
        /// 着色器绘制的环形计时条终点颜色（仅在展开模式、且未指定充能条贴图时生效）
        /// </summary>
        public virtual Color CooldownEndColor => Color.White;
        /// <summary>
        /// 展开模式下绘制该冷却：先画着色器圆环，再画描边与图标
        /// </summary>
        public virtual void DrawExpanded(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            Texture2D sprite = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D outline = ModContent.Request<Texture2D>(OutlineTexture).Value;
            Texture2D barBase = ModContent.Request<Texture2D>(ChargeBarTexture).Value;
            // 绘制圆环：需切换成 Immediate 模式以便逐次应用着色器参数
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, null, null, Main.UIScaleMatrix);
            ApplyBarShaders(opacity);
            spriteBatch.Draw(barBase, position, null, Color.White * opacity, 0, barBase.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);
            // 绘制描边
            spriteBatch.Draw(outline, position, null, OutlineColor * opacity, 0, outline.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            // 绘制图标
            spriteBatch.Draw(sprite, position, null, Color.White * opacity, 0, sprite.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }
        /// <summary>
        /// 紧凑模式下绘制该冷却：描边 + 图标 + 按完成度自下而上裁剪的小遮罩
        /// </summary>
        public virtual void DrawCompact(SpriteBatch spriteBatch, Vector2 position, float opacity, float scale)
        {
            Texture2D sprite = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D outline = ModContent.Request<Texture2D>(OutlineTexture).Value;
            Texture2D overlay = ModContent.Request<Texture2D>(OverlayTexture).Value;
            Color outlineColor = OutlineColor;
            // 绘制描边
            spriteBatch.Draw(outline, position, null, outlineColor * opacity, 0, outline.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            // 绘制图标
            spriteBatch.Draw(sprite, position, null, Color.White * opacity, 0, sprite.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            // 绘制小遮罩：顶部按已耗时长裁剪，剩余部分随完成度变高而变高
            int lostHeight = (int)Math.Ceiling(overlay.Height * (1 - instance.Completion));
            Rectangle crop = new Rectangle(0, lostHeight, overlay.Width, overlay.Height - lostHeight);
            spriteBatch.Draw(overlay, position + Vector2.UnitY * lostHeight * scale, crop, outlineColor * opacity * 0.9f, 0, sprite.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }
        /// <summary>
        /// 用环形着色器渲染冷却计时条：
        /// 本工程只移植了着色器圆环这一档（默认 BarBase 贴图），
        /// 原灾厄"自定义 ChargeBarTexture 走 CircularBarSpriteShader"的分支未移植（见下方注释）
        /// </summary>
        public virtual void ApplyBarShaders(float opacity)
        {
            // 原灾厄在此还有 else 分支：ChargeBarTexture 非默认时改用 CircularBarSpriteShader 采样自定义贴图。
            // 本工程未移植该着色器，且唯一的冷却（亵渎之魂护盾）使用默认 BarBase，故此处只保留环形着色器路径
            GameShaders.Misc[CDShaders.CircularBarShaderName].UseOpacity(opacity);
            GameShaders.Misc[CDShaders.CircularBarShaderName].UseSaturation(1 - instance.Completion);
            GameShaders.Misc[CDShaders.CircularBarShaderName].UseColor(CooldownStartColor);
            GameShaders.Misc[CDShaders.CircularBarShaderName].UseSecondaryColor(CooldownEndColor);
            GameShaders.Misc[CDShaders.CircularBarShaderName].Apply();
        }
        #endregion
    }
}
