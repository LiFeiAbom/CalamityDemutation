using CalamityDemutation.Content.Dusts;
using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.NPCs;
using CalamityDemutation.Sounds;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 虚影光束（WohLaser，移植自 CalamityEntropy）：虚影薄锋右键突刺途中甩出的一道横向光束。
    /// 生成时带 <c>ai[2] = 2</c>（虚空侵蚀的层数上限），<c>ai[2] == 0</c> 时才贴住玩家
    /// ——虚影薄锋传的是 2，所以它按自身的速度飞出去。
    /// 生命的头 5 帧（<c>ai[0] &lt;= 4</c>）才有命中判定，判定是从弹幕中心沿速度方向伸出
    /// <c>scale × 2400</c> 像素的长条（线宽 90）；同时每帧沿整条光路撒 20 团浓烟，
    /// 前 5 帧越撒越粗（<c>ai[1] += 0.25</c>）、之后逐渐收细，让光束看起来"炸开后收束"。
    /// <para>
    /// 与 CE 原版的差异：① 音效走本模组的 <see cref="CalamityDemutationSounds.RuneBoltHit"/>
    /// （与符文脉冲束命中同一份文件，CE 原名 beast_lavaball_rise1），音高按既有口径取 CE 值减 1；
    /// ② 粒子换成 <see cref="HeavySmokeParticle"/>（CE 的 PRT_HeavySmokeCal）与 <see cref="LineParticleCal"/>
    /// （PRT_LineCal）；③ 尘走工程既有的 <see cref="SquashDust"/>；④ 命中挂的虚空侵蚀走
    /// <see cref="CalamityDemutationGlobalNPC.AddVoidTouch"/>；⑤ <c>CEUtils</c> 工具内联
    /// （<c>LineThroughRect</c>→<see cref="CDUtil.LineThroughRect"/>、<c>normalize</c>→SafeNormalize、
    /// <c>getDistance</c>→<c>Vector2.Distance</c>、<c>randomPointInCircle</c>→<see cref="RandomPointInCircle"/>）；
    /// ⑥ CE 的 <c>UseBlendState</c>/<c>ExitShaderRegion</c> 本机 tML 没有，改用 End + 立即模式 Begin，
    /// 画完补上默认批次的恢复。
    /// </para>
    /// <para>
    /// <b>CE 原状、刻意照抄的一处</b>：命中火花的颜色写着
    /// <c>Main.rand.NextBool() ? Color.Purple : Color.Purple</c> —— 两个分支同色，是个写废了的三元（CE 原样），
    /// 移植时按定值 <c>Color.Purple</c> 折叠。
    /// </para>
    /// </summary>
    internal class WohLaser:ModProjectile
    {
        /// <summary>刀光底纹贴图（CE 的 CEExtraAssets.Streak1）：一张横向条纹，绘制时按滚动 UV 采样</summary>
        private const string StreakTexture = "CalamityDemutation/Assets/ExtraTextures/Streak1";
        /// <summary>光束的基础长度（实际长度还要乘 <c>Projectile.scale</c>）</summary>
        public int length = 2400;
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Ranged;   // CE 原样是远程系，虚影薄锋生成后会把它改判为近战
            Projectile.width = 1;                         // 命中判定完全由 Colliding 的线段接管，碰撞箱取最小
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;                    // 无限穿透
            Projectile.tileCollide = false;
            Projectile.timeLeft = 12;
            Projectile.usesLocalNPCImmunity = true;       // 每个敌人独立计算无敌帧
            Projectile.localNPCHitCooldown = 5;
            Projectile.ai[1] = 1;                         // 浓烟尺寸系数，随生命先胀后收
            Projectile.ArmorPenetration = 36;             // 护甲穿透 36 点
        }
        public override bool ShouldUpdatePosition() => false;
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (Projectile.ai[2] == 0)
            {
                Projectile.Center = owner.MountedCenter;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            Vector2 normal = Projectile.velocity.SafeNormalize(Vector2.Zero);
            // 沿整条光路每 100 像素撒一团浓烟，20 团/帧（CE 原样，故意铺满整条光束）
            for (float i = 0; i < 2000; i += 100)
            {
                HeavySmokeParticle smoke = new HeavySmokeParticle();
                DRKLoader.NewParticle(smoke,
                    Projectile.Center - owner.velocity + normal * (i + Main.rand.NextFloat(0, 200)),
                    normal * 26 * Main.rand.NextFloat() + RandomPointInCircle(1) + owner.velocity,
                    Color.Lerp(Color.DeepSkyBlue, Color.DarkBlue, Main.rand.NextFloat()),
                    Main.rand.NextFloat(1.8f, 2f) * Projectile.ai[1]);
                smoke.Configure(0.3f, 12, Main.rand.NextFloat(-0.1f, 0.1f), false);
            }
            // 前 5 帧把烟胀粗，之后按 1/8 的步长收细
            if (Projectile.ai[0] > 4)
            {
                Projectile.ai[1] -= 1 / 8f;
            }
            else
            {
                Projectile.ai[1] += 0.25f;
            }
            Projectile.ai[0]++;
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.netUpdate = true;
            }
        }
        /// <summary>只有前 5 帧（<c>ai[0] &lt;= 4</c>）能命中，判定是从中心沿速度方向伸出的一条长线（线宽 90）</summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.ai[0] > 4)
            {
                return false;
            }
            float laserLength = Projectile.scale * length;
            return CDUtil.LineThroughRect(Projectile.Center, Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.One) * laserLength, targetHitbox, 90);
        }
        /// <summary>
        /// 命中时：播命中音，在目标身上撒 16 条紫色细线火花，沿光束方向撒 29 颗压扁光球尘，
        /// 按 <c>ai[2]</c> 给敌人叠虚空侵蚀（&gt;0 时把它当层数上限），最后把自身伤害打到 80%（同一次光束越打越弱）。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(CalamityDemutationSounds.RuneBoltHit with { Pitch = Main.rand.NextFloat(1.4f, 1.8f) - 1f }, target.Center);
            for (int i = 0; i < 16; i++)
            {
                Vector2 sparkVelocity = Projectile.velocity.RotatedByRandom(0.2f) * Main.rand.NextFloat(0.5f, 1.8f);
                int sparkLifetime = Main.rand.Next(20, 24);
                float sparkScale = Main.rand.NextFloat(0.95f, 1.8f);
                LineParticleCal line = new LineParticleCal();
                DRKLoader.NewParticle(line, target.Center + Main.rand.NextVector2Circular(target.width * 0.5f, target.height * 0.5f) + Projectile.velocity * 1.2f,
                    sparkVelocity, Color.Purple, sparkScale);
                line.Configure(false, sparkLifetime);
            }
            for (int i = 0; i < 29; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Projectile.rotation.ToRotationVector2() * Vector2.Distance(target.Center, Projectile.Center),
                    ModContent.DustType<SquashDust>(), -Projectile.velocity);
                dust.scale = Main.rand.NextFloat(3f, 3.5f);
                dust.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(0.4f) * Main.rand.NextFloat(8, 36);
                dust.noGravity = true;
                dust.color = Color.LightBlue;
                dust.fadeIn = 2f;
            }
            if (Projectile.ai[2] > 0)
            {
                CalamityDemutationGlobalNPC.AddVoidTouch(target, 50, 5, 600, (int)Projectile.ai[2]);
            }
            else
            {
                CalamityDemutationGlobalNPC.AddVoidTouch(target, 50, 5);
            }
            Projectile.damage = (int)(Projectile.damage * 0.8f);
        }
        /// <summary>
        /// 自绘：在加法混合 + 线性包裹采样下把条纹贴图按滚动 UV 拉成两道叠着的光束（内层更细更亮），
        /// 画完恢复默认批次。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Texture2D tex = ModContent.Request<Texture2D>(StreakTexture).Value;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, new Rectangle(-(int)(Main.GlobalTimeWrappedHourly * 900), 0, (int)(Projectile.scale * length), tex.Height),
                new Color(80, 60, 255), Projectile.rotation, new Vector2(0, tex.Height * 0.5f), new Vector2(1, Projectile.scale * Projectile.ai[1] * 0.37f), SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, new Rectangle(-(int)(Main.GlobalTimeWrappedHourly * 1400), 0, (int)(Projectile.scale * length), tex.Height),
                new Color(160, 140, 255), Projectile.rotation, new Vector2(0, tex.Height * 0.5f), new Vector2(1, Projectile.scale * Projectile.ai[1] * 0.22f), SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        /// <summary>CEUtils.randomPointInCircle 的等价实现：随机角度 × [-r, r] 的随机半径</summary>
        private static Vector2 RandomPointInCircle(float r) => Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(-r, r);
    }
}
