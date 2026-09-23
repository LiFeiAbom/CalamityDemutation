using CalamityDemutation.Content.Items.Weapons.Melee;
using CalamityDemutation.Graphics.Primitives;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 熵之舞手持挥砍体（移植自大修 <c>EntropicClaymoreHoldoutProj</c>）：
    /// 83 帧四段弧线（等待→后摇→主挥→收招）挥砍，剑身与弧光分别绘制，弧光走 ExobladeSlash 着色器；
    /// 存活到剩余 25% 时朝准心扇形撒出 9 枚熵之飞刃（档位由物品写进 <c>localAI[0]</c>）；
    /// 命中玩家时回血 4（大修原码的 <c>OnHitNPC</c> 是空实现，无任何效果，已略去）。
    /// 注意动画总长 83 帧与物品的 useTime 78 不同：挥砍体只活 78 帧，动画按 83 帧的进度曲线走。
    /// </summary>
    internal class EntropicClaymoreHoldout : ModProjectile
    {
        public Player Owner => Main.player[Projectile.owner];
        /// <summary>挥砍方向（+1 右 / -1 左）</summary>
        public int Direction => Projectile.velocity.X > 0 ? 1 : -1;
        /// <summary>挥砍动画总长（帧）——大修写死 83，与物品 useTime 无关</summary>
        public const float AnimationTime = 83f;
        /// <summary>挥砍完成度（0~1，Time/83 夹取）</summary>
        public float SwingCompletion => MathHelper.Clamp(Time / AnimationTime, 0f, 1f);
        /// <summary>拖尾起始完成度（当前完成度回退 0.2，下限 SwingCompletionRatio）</summary>
        public float SwingCompletionAtStartOfTrail => MathHelper.Clamp(SwingCompletion - 0.2f, SwingCompletionRatio, 1f);
        /// <summary>当前剑身旋转角 = 初始朝向 + 弧线偏移 × 朝向 + 45°（左向再加 90°）</summary>
        public float SwordRotation
        {
            get
            {
                float num = InitialRotation + GetSwingOffsetAngle(SwingCompletion) * Projectile.spriteDirection + MathF.PI / 4f;
                if (Projectile.spriteDirection == -1)
                    num += MathF.PI / 2f;
                return num;
            }
        }
        public Vector2 SwordDirection => SwordRotation.ToRotationVector2() * Direction;
        public ref float Time => ref Projectile.ai[0];
        public ref float InitialRotation => ref Projectile.ai[1];
        /// <summary>挥砍到主挥段的完成度阈值</summary>
        public static float SwingCompletionRatio => 0.37f;
        /// <summary>挥砍到收招段的完成度阈值</summary>
        public static float RecoveryCompletionRatio => 0.84f;
        /// <summary>武器贴图复用熵之舞物品贴图</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/EntropicClaymore";
        /// <summary>拖尾缓存 100 点、TrailingMode 2（记录位置与旋转，供弧光采样）</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 100;
        }
        /// <summary>基础属性：60×66、近战无攻速、穿透无限、不碰撞物块、存活 83 帧、MaxUpdates 2、本地免疫 14 帧</summary>
        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 66;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.timeLeft = (int)AnimationTime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.MaxUpdates = 2;
            Projectile.localNPCHitCooldown = Projectile.MaxUpdates * 7;
            Projectile.noEnchantmentVisuals = true;
        }
        /// <summary>位置由 StickToOwner 直接接管，不参与引擎位移</summary>
        public override bool ShouldUpdatePosition() => false;
        /// <summary>挥砍主体：首帧记初始朝向，逐帧缩放、跟随玩家、按帧撒刀，更新旋转角与动画进度</summary>
        public override void AI()
        {
            if (InitialRotation == 0f)
            {
                InitialRotation = Projectile.velocity.ToRotation();
                Projectile.netUpdate = true;
            }
            Projectile.scale = Utils.GetLerpValue(0f, 0.13f, SwingCompletion, clamped: true) * Utils.GetLerpValue(1f, 0.87f, SwingCompletion, clamped: true) * 0.7f + 0.3f;
            AdjustPlayerValues();
            StickToOwner();
            CreateProjectiles();
            Projectile.rotation = SwordRotation;
            // MaxUpdates 2 下每帧推进两格，动画总长按 ai[2]（物品 useTime）等比折算
            Time += AnimationTime / Projectile.ai[2];
        }
        /// <summary>把玩家朝向/手臂锁定到挥砍方向</summary>
        public void AdjustPlayerValues()
        {
            Projectile.spriteDirection = Projectile.direction = Direction;
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.itemRotation = (Projectile.direction * Projectile.velocity).ToRotation();
            float num = SwordRotation - Direction * 1.67f;
            Owner.SetCompositeArmFront(Math.Abs(num) > 0.01f, Player.CompositeArmStretchAmount.Full, num);
        }
        /// <summary>弹幕中心贴到玩家中心，登记手持物</summary>
        public void StickToOwner()
        {
            Projectile.Center = Owner.Center;
            Owner.heldProj = Projectile.whoAmI;
            Owner.SetDummyItemTime(2);
            Owner.direction = Direction;
        }
        /// <summary>
        /// 存活到剩余 25%（大修写死 <c>timeLeft == useTime * 0.25</c>）时，朝准心 ±70° 扇形撒出 9 枚熵之飞刃。
        /// 飞刃档位与伤害倍率：小 0.3（localAI 0）/ 中 0.5（1）/ 大 1.0（2）。
        /// </summary>
        public void CreateProjectiles()
        {
            if (Projectile.owner != Main.myPlayer)
                return;
            if (Projectile.timeLeft != (int)(Projectile.ai[2] * 0.25f))
                return;
            Vector2 toMouse = Main.MouseWorld - Owner.Center;
            int flechetteType = ModContent.ProjectileType<EntropicFlechetteSmall>();
            float damageFactor = 0.3f;
            switch ((int)Projectile.localAI[0])
            {
                case 1:
                    flechetteType = ModContent.ProjectileType<EntropicFlechette>();
                    damageFactor = 0.5f;
                    break;
                case 2:
                    flechetteType = ModContent.ProjectileType<EntropicFlechetteLarge>();
                    damageFactor = 1f;
                    break;
            }
            for (int i = 0; i < EntropicClaymore.FlechetteCount; i++)
            {
                float rot = toMouse.ToRotation() + MathHelper.ToRadians(-EntropicClaymore.FlechetteSpread / 2f + EntropicClaymore.FlechetteSpread / EntropicClaymore.FlechetteCount * i);
                Vector2 pos = Projectile.Center + rot.ToRotationVector2() * 130f;
                Projectile.NewProjectile(new EntitySource_ItemUse(Owner, Owner.ActiveItem()), pos, toMouse.SafeNormalize(Vector2.Zero) * 13f
                    , flechetteType, (int)(Projectile.damage * damageFactor), Projectile.knockBack, Projectile.owner);
            }
        }
        /// <summary>剑身恒为白色，透明度随弹幕</summary>
        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;
        /// <summary>绘制入口：先画弧光再画剑身</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            DrawSlash();
            DrawBlade(lightColor);
            return false;
        }
        public float SlashWidthFunction(float completionRatio, Vector2 _) => Projectile.scale * 12f;
        /// <summary>剑气颜色：淡杏仁色，越靠弧光末端越淡</summary>
        public Color SlashColorFunction(float completionRatio, Vector2 _) => Color.BlanchedAlmond * Utils.GetLerpValue(0.9f, 0.6f, completionRatio, clamped: true) * Projectile.Opacity;
        /// <summary>按当前完成度与拖尾起始完成度采样 20 个弧光点（等分插值，扣除旧旋转差）</summary>
        public IEnumerable<Vector2> GenerateSlashPoints()
        {
            for (int i = 0; i < 20; i++)
            {
                float completion = MathHelper.Lerp(SwingCompletion, SwingCompletionAtStartOfTrail, i / 20f);
                float offsetRot = Math.Abs(Projectile.oldRot[0] - Projectile.oldRot[1]) * 0.8f;
                if (SwingCompletion > RecoveryCompletionRatio)
                    offsetRot = 0.21f;
                float rots = (GetSwingOffsetAngle(completion) - offsetRot) * Direction + InitialRotation;
                yield return rots.ToRotationVector2() * Projectile.scale * 154f;
            }
        }
        /// <summary>用 ExobladeSlash 着色器沿弧光采样点画剑气弧光</summary>
        public void DrawSlash()
        {
            GameShaders.Misc["CalamityDemutation:ExobladeSlash"].UseImage1(ModContent.Request<Texture2D>("CalamityDemutation/ExtraTextures/GreyscaleGradients/VoronoiShapes"));
            GameShaders.Misc["CalamityDemutation:ExobladeSlash"].UseColor(EntropicClaymore.EntropicColor1);
            GameShaders.Misc["CalamityDemutation:ExobladeSlash"].UseSecondaryColor(EntropicClaymore.EntropicColor2);
            GameShaders.Misc["CalamityDemutation:ExobladeSlash"].Shader.Parameters["fireColor"].SetValue(EntropicClaymore.EntropicColor1.ToVector3());
            GameShaders.Misc["CalamityDemutation:ExobladeSlash"].Shader.Parameters["flipped"].SetValue(Direction == 1);
            GameShaders.Misc["CalamityDemutation:ExobladeSlash"].Apply();
            // 本机 tML 无灾厄的 EnterShaderRegion/ExitShaderRegion，改用 End + 立即模式 Begin 替代，画完恢复默认批状态
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            if (SwingCompletionAtStartOfTrail > SwingCompletionRatio)
                PrimitiveRenderer.RenderTrail(GenerateSlashPoints().ToArray(), new PrimitiveSettings(SlashWidthFunction, SlashColorFunction, (float _, Vector2 _) => Projectile.Center, smoothen: true, pixelate: false, GameShaders.Misc["CalamityDemutation:ExobladeSlash"]), 95);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
        /// <summary>画武器贴图本体（左向翻转）</summary>
        public void DrawBlade(Color lightColor)
        {
            Texture2D value = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 position = Projectile.Center - Main.screenPosition;
            Vector2 origin = value.Size() * Vector2.UnitY;
            if (Projectile.spriteDirection == -1)
                origin.X += value.Width;
            SpriteEffects effects = Projectile.spriteDirection != 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Main.spriteBatch.Draw(value, position, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, effects, 0f);
        }
        /// <summary>真近战回血 4（受 moonLeech 限制）</summary>
        public void OnHitHealEffect()
        {
            if (!Owner.moonLeech)
            {
                Owner.statLife += 4;
                Owner.HealEffect(4);
            }
        }
        /// <summary>线段碰撞：沿剑尖方向延伸 height×scale×2.5，宽 width×0.25（熵之舞判定比泰拉巨刃更长，大修原值）</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            Vector2 vector = (InitialRotation + GetSwingOffsetAngle(SwingCompletion)).ToRotationVector2() * new Vector2(Projectile.spriteDirection, 1f);
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + vector * Projectile.height * Projectile.scale * 2.5f, Projectile.width * 0.25f, ref collisionPoint);
        }
        /// <summary>PvP 命中：回血 4</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            OnHitHealEffect();
        }
        // ── 内联灾厄 PiecewiseAnimation（只保留 PolyIn/PolyOut，四段弧线专用）──
        private static float PolyIn(float amount, int degree) => (float)Math.Pow(amount, degree);
        private static float PolyOut(float amount, int degree) => 1f - (float)Math.Pow(1f - amount, degree);
        private static float PiecewiseAnimation(float progress, (float startX, float startHeight, float shift, bool polyIn, int degree)[] segments)
        {
            if (segments.Length == 0)
                return 0f;
            progress = MathHelper.Clamp(progress, 0f, 1f);
            for (int i = 0; i < segments.Length; i++)
            {
                (float startX, float startHeight, float shift, bool polyIn, int degree) = segments[i];
                float endPoint = 1f;
                if (progress < startX)
                    continue;
                if (i < segments.Length - 1)
                {
                    if (segments[i + 1].startX <= progress)
                        continue;
                    endPoint = segments[i + 1].startX;
                }
                float segmentProgress = (progress - startX) / (endPoint - startX);
                float ratio = startHeight + (polyIn ? PolyIn(segmentProgress, degree) : PolyOut(segmentProgress, degree)) * shift;
                return ratio;
            }
            return 0f;
        }
        /// <summary>四段弧线偏移：等待(0→-1.67)→后摇(-1.67→-2.72)→主挥(-2.72→1.71)→收招(1.71→2.68)，与泰拉巨刃同曲线</summary>
        private static float GetSwingOffsetAngle(float completion) => PiecewiseAnimation(completion,
        [
            (0f, -1.67f, 0f, false, 1),
            (0.14f, -1.67f, -1.05f, false, 2),
            (0.37f, -2.72f, 4.43f, true, 5),
            (0.84f, 1.71f, 0.97f, false, 3),
        ]);
    }
}
