using CalamityDemutation.Content.Items.Weapons.Melee;
using CalamityDemutation.Graphics.Primitives;
using CalamityDemutation.Players;
using CalamityDemutation.Sounds;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 泰拉巨刃手持挥砍体（移植自大修 RTerratomereHoldoutProj，改用本模组 PrimitiveRenderer + ExobladeSlash 着色器）：
    /// 83 帧四段弧线（等待→后摇→主挥→收招）挥砍，挥砍途中发射 3 发泰拉闪电、收招时放出一道大光束剑气；
    /// 真近战命中回血并施加冰川状态、召唤小刀光创造者。
    /// </summary>
    internal class TerratomereHoldout : ModProjectile
    {
        public Player Owner => Main.player[Projectile.owner];
        /// <summary>挥砍方向（+1 右 / -1 左）</summary>
        public int Direction => Projectile.velocity.X > 0 ? 1 : -1;
        /// <summary>挥砍完成度（0~1，Time/83 夹取）</summary>
        public float SwingCompletion => MathHelper.Clamp(Time / 83f, 0f, 1f);
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
        /// <summary>武器贴图复用现代版灾厄泰拉巨刃贴图</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/Terratomere";
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
            Projectile.timeLeft = 83;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.MaxUpdates = 2;
            Projectile.localNPCHitCooldown = Projectile.MaxUpdates * 7;
            Projectile.noEnchantmentVisuals = true;
        }
        /// <summary>位置由 StickToOwner 直接接管，不参与引擎位移</summary>
        public override bool ShouldUpdatePosition() => false;
        /// <summary>挥砍主体：首帧记初始朝向，逐帧缩放、跟随玩家、按帧发射子弹幕、喷尘、更新旋转角</summary>
        public override void AI()
        {
            if (InitialRotation == 0f)
            {
                InitialRotation = Projectile.velocity.ToRotation();
                Projectile.netUpdate = true;
            }
            Projectile.scale = Utils.GetLerpValue(0f, 0.13f, SwingCompletion, clamped: true) * Utils.GetLerpValue(1f, 0.87f, SwingCompletion, clamped: true) * 0.7f + 0.3f + 1f;
            AdjustPlayerValues();
            StickToOwner();
            CreateProjectiles();
            if (SwingCompletion > SwingCompletionRatio + 0.2f && SwingCompletion < RecoveryCompletionRatio)
                CreateSlashSparkleDust();
            Projectile.rotation = SwordRotation;
            Time += 1f;
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
        /// <summary>按帧发射：43 帧播挥砍音、58 帧发 3 发泰拉闪电、74 帧发大光束剑气</summary>
        public void CreateProjectiles()
        {
            if (Time == (int)(83f * (SwingCompletionRatio + 0.15f)))
                SoundEngine.PlaySound(CalamityDemutationSounds.TerratomereSwing, Projectile.Center);
            if (Main.myPlayer == Projectile.owner && Time == (int)(83f * (SwingCompletionRatio + 0.34f)))
            {
                Vector2 vector = Projectile.DirectionTo(Main.MouseWorld) * Owner.ActiveItem().shootSpeed;
                if (Math.Abs(MathHelper.WrapAngle(vector.ToRotation() - InitialRotation)) > 1.456f)
                    vector = InitialRotation.ToRotationVector2() * vector.Length();
                for (int i = 0; i < 3; i++)
                {
                    Vector2 vr = (Main.MouseWorld - Owner.Center).SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.ToRadians(-10 + 10 * i));
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center - vector * 0.4f, vr * 15
                        , ModContent.ProjectileType<TerratomereBolts>(), (int)(Projectile.damage * Terratomere.SmallSlashDamageFactor), Projectile.knockBack, Projectile.owner);
                }
            }
            if (Main.myPlayer == Projectile.owner && Time == (int)(83f * RecoveryCompletionRatio) + 5f)
            {
                Vector2 vector2 = InitialRotation.ToRotationVector2() * Owner.HeldItem.shootSpeed / 6f;
                Vector2 position = Projectile.Center + vector2.SafeNormalize(Vector2.UnitY) * 64f;
                int num = Projectile.NewProjectile(Projectile.GetSource_FromThis(), position, vector2, ModContent.ProjectileType<TerratomereBeams>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                if (Main.projectile.IndexInRange(num))
                {
                    Main.projectile[num].ai[0] = Direction == 1 ? 1 : 0;
                    (Main.projectile[num].ModProjectile as TerratomereBeams).ControlPoints = GenerateSlashPoints().ToArray();
                }
            }
        }
        /// <summary>主挥段沿弧线喷绿/黄尘土</summary>
        public void CreateSlashSparkleDust()
        {
            Vector2 vector = InitialRotation.ToRotationVector2();
            Vector2 position = Projectile.Center + (GetSwingOffsetAngle(SwingCompletion) * Direction + InitialRotation).ToRotationVector2() * Main.rand.NextFloat(8f, 66f) + vector * 76f;
            int type = Main.rand.NextBool() ? 267 : 264;
            Dust dust = Dust.NewDustPerfect(position, type, Vector2.Zero);
            dust.color = Color.Lerp(Terratomere.TerraColor1, Terratomere.TerraColor2, Main.rand.NextFloat());
            dust.color = Color.Lerp(dust.color, Color.Yellow, (float)Math.Pow(Main.rand.NextFloat(), 1.63));
            dust.fadeIn = Main.rand.NextFloat(1f, 2f);
            dust.scale = 0.4f;
            dust.velocity = vector * Main.rand.NextFloat(0.5f, 15f);
            dust.noLight = true;
            dust.noGravity = true;
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
        public float SlashWidthFunction(float completionRatio, Vector2 _) => Projectile.scale * 22f;
        public Color SlashColorFunction(float completionRatio, Vector2 _) => Color.Lime * Utils.GetLerpValue(0.9f, 0.4f, completionRatio, clamped: true) * Projectile.Opacity;
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
                yield return rots.ToRotationVector2() * Projectile.scale * 54f;
            }
        }
        /// <summary>用 ExobladeSlash 着色器沿弧光采样点画剑气弧光</summary>
        public void DrawSlash()
        {
            GameShaders.Misc["CalamityDemutation:ExobladeSlash"].UseImage1(ModContent.Request<Texture2D>("CalamityDemutation/ExtraTextures/GreyscaleGradients/VoronoiShapes"));
            GameShaders.Misc["CalamityDemutation:ExobladeSlash"].UseColor(Terratomere.TerraColor1);
            GameShaders.Misc["CalamityDemutation:ExobladeSlash"].UseSecondaryColor(Terratomere.TerraColor2);
            GameShaders.Misc["CalamityDemutation:ExobladeSlash"].Shader.Parameters["fireColor"].SetValue(Terratomere.TerraColor1.ToVector3());
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
                Owner.statLife += Terratomere.TrueMeleeHitHeal;
                Owner.HealEffect(Terratomere.TrueMeleeHitHeal);
            }
        }
        /// <summary>线段碰撞：沿剑尖方向延伸 height×scale，宽 width×0.25</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            Vector2 vector = (InitialRotation + GetSwingOffsetAngle(SwingCompletion)).ToRotationVector2() * new Vector2(Projectile.spriteDirection, 1f);
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + vector * Projectile.height * Projectile.scale, Projectile.width * 0.25f, ref collisionPoint);
        }
        /// <summary>命中：施加冰川状态（找不到退回霜火）、回血、召唤小刀光创造者</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "GlacialState", Terratomere.TrueMeleeGlacialStateTime, BuffID.Frostburn);
            if (target.canGhostHeal)
                OnHitHealEffect();
            int num = ModContent.ProjectileType<TerratomereSlashCreator>();
            if (Owner.ownedProjectileCounts[num] < 2)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero, num, Projectile.damage, Projectile.knockBack, Projectile.owner, target.whoAmI, Main.rand.NextFloat(MathF.PI * 2f));
                Owner.ownedProjectileCounts[num]++;
            }
        }
        /// <summary>PvP 命中：施加冰川状态（找不到退回霜火）、回血</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "GlacialState", Terratomere.TrueMeleeGlacialStateTime, BuffID.Frostburn);
            OnHitHealEffect();
        }
        // ── 内联灾厄 PiecewiseAnimation（只保留 PolyIn/PolyOut，Terratomere 四段弧线专用）──
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
        /// <summary>四段弧线偏移：等待(0→-1.67)→后摇(-1.67→-2.72)→主挥(-2.72→1.71)→收招(1.71→2.68)</summary>
        private static float GetSwingOffsetAngle(float completion) => PiecewiseAnimation(completion,
        [
            (0f, -1.67f, 0f, false, 1),
            (0.14f, -1.67f, -1.05f, false, 2),
            (0.37f, -2.72f, 4.43f, true, 5),
            (0.84f, 1.71f, 0.97f, false, 3),
        ]);
    }
}
