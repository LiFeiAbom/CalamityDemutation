using CalamityDemutation.Sounds;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 元素王者魔力阵（移植自灾厄 PrismaticMagicCircle）：元素王者之剑蓄满后出现的无伤害法阵。
    /// 它本身不造成伤害（<see cref="CanDamage"/> 恒 false），作用有二：① 生成并挂住主伤害光束 ElementalExcaliburRay；
    /// ② 用一层噪声法阵把激光"被地形截断"的断口遮住。跟随在主人身前并把瞄准做 0.94 的滞后跟随。
    /// 本体隐形贴图，视觉全部由 PreDraw 里的 ExoVortex 着色器承担。
    /// </summary>
    internal class ElementalExcaliburMagicCircle : ModProjectile
    {
        /// <summary>本体隐形：视觉全部由 ExoVortex 着色器绘制</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>存在帧数（存于 ai[1]，与灾厄一致）</summary>
        public ref float Timer => ref Projectile.ai[1];
        /// <summary>主人引用</summary>
        public Player Owner => Main.player[Projectile.owner];
        /// <summary>法阵存活时长（帧）</summary>
        public int Lifetime = 360;
        /// <summary>
        /// 基础属性：512×512（决定噪声法阵的铺开尺寸）、友方近战、伤害动态刷新、不撞地形、存活 360 帧
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 512;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ContinuouslyUpdateDamageStats = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = Lifetime;
        }
        /// <summary>法阵本身不造成任何伤害</summary>
        public override bool? CanDamage() => false;
        /// <summary>位置由 AI 每帧指定，不走原版的位置积分</summary>
        public override bool ShouldUpdatePosition() => false;
        /// <summary>
        /// AI：计帧 → 主人无法继续持握时把剩余时间压到 30 帧 → 跟随在主人身前 60 像素 →
        /// 以 0.94 的滞后跟随瞄准 → 朝向对齐速度 → 前 30 帧放大、之后随剩余时间收缩 →
        /// 首帧连播三个音效并生成主伤害光束 ElementalExcaliburRay（伤害按 originalDamage 的 LaserDamageMult 倍）
        /// </summary>
        public override void AI()
        {
            Timer++;
            if (Owner.HoldoutReleased())
            {
                if (Projectile.timeLeft > 30)
                    Projectile.timeLeft = 30;
            }
            else
            {
                // 按住不放：持续续命，法阵与激光随持握常驻（松开后走上面那条 30 帧快速收束）
                Projectile.timeLeft = Lifetime;
            }
            if (Owner.active && !Owner.dead)
                Projectile.Center = Owner.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 60f;
            Vector2 aimVector = (Main.MouseWorld - Owner.RotatedRelativePoint(Owner.MountedCenter, true)).SafeNormalize(Vector2.UnitY);
            aimVector = Vector2.Normalize(Vector2.Lerp(aimVector, Vector2.Normalize(Projectile.velocity), ElementalExcaliburBreakerHoldout.LaserAimLag));
            if (aimVector != Projectile.velocity)
                Projectile.netUpdate = true;
            Projectile.velocity = aimVector;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Timer < 30f)
                Projectile.scale = MathHelper.Lerp(0f, 1f, Timer / 30f);
            else
                Projectile.scale = Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true);
            if (Timer == 1f && Main.myPlayer == Projectile.owner)
            {
                SoundEngine.PlaySound(SoundID.Item67, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item68, Projectile.Center);
                SoundEngine.PlaySound(CalamityDemutationSounds.TeslaCannonFire with { Pitch = 1f });
                Vector2 spawnPos = Vector2.Lerp(Projectile.Center, Owner.Center, 0.5f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPos, Projectile.velocity, ModContent.ProjectileType<ElementalExcaliburRay>(), Projectile.originalDamage, Projectile.knockBack, Projectile.owner);
            }
        }
        /// <summary>
        /// 自绘：加色混合下用 ExoVortex 着色器把 MeltyNoise 噪声按本体尺寸压扁铺开 6 层，
        /// 形成旋转的噪声法阵（正好挡住激光在地形处的断口）
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            // 本机 tML 无 EnterShaderRegion/ExitShaderRegion：用 End + 立即模式 Begin(Additive) 替代，画完再恢复默认批状态
            Texture2D howNoisy = ModContent.Request<Texture2D>("CalamityDemutation/Assets/ExtraTextures/MeltyNoise").Value;
            Vector2 squishScale = new Vector2(Projectile.width / howNoisy.Width * 0.55f, Projectile.height / howNoisy.Height * 2f) * Projectile.scale * 0.36f;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            GameShaders.Misc["CalamityDemutation:ExoVortex"].Apply();
            for (int i = 0; i < 6; i++)
                Main.spriteBatch.Draw(howNoisy, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, howNoisy.Size() / 2f, squishScale, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
