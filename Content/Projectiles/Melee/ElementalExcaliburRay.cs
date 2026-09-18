using CalamityDemutation.Content.Projectiles.BaseProjectiles;
using CalamityDemutation.Graphics.Primitives;
using CalamityDemutation.Players;
using CalamityDemutation.Sounds;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 元素王者激光（移植自灾厄 PrismaticRay）：元素王者魔力阵蓄满后射出的主伤害光束。
    /// 本体不长按贴图绘制，而是沿激光线用 PrimitiveRenderer + ArtemisLaser 着色器拉出一条动态光束；
    /// 命中施加元素混合并伴随撞击音（9 帧节流），期间持续给主人加屏幕震动。
    /// 与灾厄原版的区别：伤害类型由自定义的"近战+远程混合"改为纯近战；Owner.SetScreenshake 改用本工程的 GeneralScreenShakePower。
    /// </summary>
    internal class ElementalExcaliburRay : BaseLaserbeamCO
    {
        /// <summary>本体贴图指向起始段贴图（灾厄 PrismaticRay 也是这么指向 PrismaticRayStart 的）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/Melee/ElementalExcaliburRayStart";
        /// <summary>主人引用</summary>
        public Player Owner => Main.player[Projectile.owner];
        /// <summary>起始段贴图 = 本体贴图（即 ElementalExcaliburRayStart）</summary>
        public override Texture2D LaserBeginTexture => TextureAssets.Projectile[Type].Value;
        /// <summary>中段循环贴图</summary>
        public override Texture2D LaserMiddleTexture => ModContent.Request<Texture2D>("CalamityDemutation/Assets/ExtraTextures/ElementalExcaliburRayMid", AssetRequestMode.ImmediateLoad).Value;
        /// <summary>末端贴图</summary>
        public override Texture2D LaserEndTexture => ModContent.Request<Texture2D>("CalamityDemutation/Assets/ExtraTextures/ElementalExcaliburRayEnd", AssetRequestMode.ImmediateLoad).Value;
        /// <summary>缩放上限</summary>
        public override float MaxScale => 5f;
        /// <summary>激光长度上限</summary>
        public override float MaxLaserLength => 2400f;
        /// <summary>激光寿命帧数</summary>
        private const float RayLifetimeFrames = 360f;
        /// <summary>
        /// 存活帧数：恒为 360——基类 Behavior 到寿即自毁，末 30 帧再由 DetermineScale 随剩余时间收细，
        /// 也就是激光会"衰减"消失，想再放必须重新蓄力（灾厄口径）
        /// </summary>
        public override float Lifetime => RayLifetimeFrames;
        /// <summary>光束叠加色取迪斯科色（随全局时间循环变色）</summary>
        public override Color LaserOverlayColor => Main.DiscoColor;
        /// <summary>撞击音效的节流剩余帧数</summary>
        public int HitSoundCooldown = 0;
        /// <summary>绘制判定范围放宽到 5000，使超长光束在屏幕外也参与绘制</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 5000;
        }
        /// <summary>
        /// 基础属性：36×36、友方近战、伤害动态刷新、无限穿透、不撞地形、存活 360 帧、
        /// 每敌 9 帧命中冷却（激光会持续压在同一个敌人身上），hide 让本体绘制排在弹幕层之后
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ContinuouslyUpdateDamageStats = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 360;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 9;
            Projectile.hide = true;
        }
        /// <summary>
        /// 附着：主人无法继续持握时把剩余时间压到 30 帧（快速收束），并始终把光束起点贴在主人身前 20 像素处
        /// </summary>
        public override void AttachToSomething()
        {
            if (Owner.HoldoutReleased())
            {
                if (Projectile.timeLeft > 30)
                    Projectile.timeLeft = 30;
            }
            if (Owner.active && !Owner.dead)
                Projectile.Center = Owner.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 20f;
        }
        /// <summary>
        /// 指向更新：用"鼠标方向 与 当前方向"按 0.94 作滞后插值（对齐原版 Last Prism 的迟滞手感，越大越慢）
        /// </summary>
        public override void UpdateLaserMotion()
        {
            Vector2 aimVector = (Main.MouseWorld - Owner.RotatedRelativePoint(Owner.MountedCenter, true)).SafeNormalize(Vector2.UnitY);
            aimVector = Vector2.Normalize(Vector2.Lerp(aimVector, Vector2.Normalize(Projectile.velocity), ElementalExcaliburBreakerHoldout.LaserAimLag));
            if (aimVector != Projectile.velocity)
                Projectile.netUpdate = true;
            Projectile.velocity = aimVector;
        }
        /// <summary>缩放：前 30 帧从 0 拉到满，之后随剩余时间线性收缩，两者都乘 MaxScale</summary>
        public override void DetermineScale()
        {
            if (Time < 30f)
                Projectile.scale = MathHelper.Lerp(0f, 1f, Time / 30f) * MaxScale;
            else
                Projectile.scale = Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true) * MaxScale;
        }
        /// <summary>常规 AI 之后的额外逻辑：朝向对齐速度、持续加屏震、撞击音节流递减</summary>
        public override void ExtraBehavior()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Owner.CD().GeneralScreenShakePower = 3f;
            if (HitSoundCooldown > 0)
                HitSoundCooldown--;
        }
        /// <summary>
        /// 命中敌人：施加元素混合减益（现代版/经典版分别软依赖查找）
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "ElementalMix", 300);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "ElementalMix", 300);
            if (HitSoundCooldown == 0)
            {
                SoundEngine.PlaySound(CalamityDemutationSounds.ExobladeDashImpact, target.Center);
                HitSoundCooldown = 9;
            }
        }
        /// <summary>顶点宽度函数：当前缩放乘本体宽度</summary>
        public float LaserWidthFunction(float _, Vector2 vertexPos) => Projectile.scale * Projectile.width;
        /// <summary>顶点颜色函数：统一迪斯科色</summary>
        public Color LaserColorFunction(float completionRatio, Vector2 vertexPos) => Main.DiscoColor;
        /// <summary>
        /// 自绘：把激光线等分成 10 个采样点交给 PrimitiveRenderer 拉出光束，
        /// 绑定 ArtemisLaser 着色器（迪斯科色 + MeltyNoise 噪声 + LeviathanBomb 遮罩）
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            // 速度为零时（理论上不该发生）直接不画，避免除零
            if (Projectile.velocity == Vector2.Zero)
                return false;
            Vector2 laserEnd = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * LaserLength;
            Vector2[] drawPoints = new Vector2[10];
            for (int i = 0; i < drawPoints.Length; i++)
                drawPoints[i] = Vector2.Lerp(Projectile.Center, laserEnd, i / (float)(drawPoints.Length - 1f));
            GameShaders.Misc["CalamityDemutation:ArtemisLaser"].UseColor(Main.DiscoColor);
            GameShaders.Misc["CalamityDemutation:ArtemisLaser"].UseImage1(ModContent.Request<Texture2D>("CalamityDemutation/Assets/ExtraTextures/MeltyNoise"));
            GameShaders.Misc["CalamityDemutation:ArtemisLaser"].UseImage2(ModContent.Request<Texture2D>("CalamityDemutation/Assets/ExtraTextures/LeviathanBomb"));
            // 本机 tML 无 EnterShaderRegion/ExitShaderRegion，PrimitiveRenderer 自带批状态管理（与 DefenseBeam 同款写法）
            GameShaders.Misc["CalamityDemutation:ArtemisLaser"].Apply();
            PrimitiveRenderer.RenderTrail(drawPoints, new PrimitiveSettings(LaserWidthFunction, LaserColorFunction, (float _, Vector2 _) => Vector2.Zero, smoothen: true, pixelate: false, GameShaders.Misc["CalamityDemutation:ArtemisLaser"]), 60);
            return false;
        }
        /// <summary>本体绘制排在弹幕层之后，使激光压在其它弹幕之下</summary>
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindProjectiles.Add(index);
        }
    }
}
