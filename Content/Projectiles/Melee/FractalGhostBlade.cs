using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 聚魂之影（FractalGhostBlade，移植自 CalamityEntropy）：聚魂分形挥砍时甩出的剑影，伤害与本体相同。
    /// 出手先原地自转 30 帧（门槛写作 30 × MaxUpdates）、期间逐帧减速且不能命中，随后锁定 4000 像素内最近的目标：
    /// 在剑心炸一发 <see cref="ImpactParticle"/>、播一声爆响，然后朝目标猛冲（带 ±0.6 弧度的散射）
    /// 并按 0.01 的强度持续贴向目标；首次命中把寿命压到 4 × 60 = 240 次更新（MaxUpdates = 4，实为 1 秒）并开始线性淡出。
    /// 本体只靠 <see cref="PreDraw"/> 摊开的 32 帧旋转残影呈现（贴图直接借用本体的辉光图）。
    /// <para>
    /// 与 CE 原版的差异：① 索敌沿用 tML 自带的 <c>Projectile.FindTargetWithinRange</c>（CE 的
    /// <c>CEUtils.FindTarget_HomingProj</c> 是同类 API）；② <c>CEUtils.SmoothHomingBehavior</c> 换成本工程的
    /// <c>ChasingBehavior2</c>（同为 RotTowards 限角转向，参数语义一致）；③ 冲击粒子由 InnoVault 的
    /// <c>PRT_ImpactParticle</c> 换成本模组的 <see cref="ImpactParticle"/>；④ CE 就地改静态 SoundStyle 后播放，
    /// 这里用 with 复制一份以免污染全局音效；⑤ CE 的 <c>Entropy().MouseWorldListener</c> 属 CE 玩家系统，未移植。
    /// </para>
    /// </summary>
    internal class FractalGhostBlade:ModProjectile
    {
        /// <summary>直接借用武器本体的辉光贴图（CE 原样）</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/SpiritFractalGlow";
        /// <summary>出手时随机一个初始自转角（只做一次）</summary>
        private bool init = true;
        /// <summary>自身帧数计数，30 帧内只自转减速、不索敌</summary>
        private float counter = 0f;
        /// <summary>是否已经命中过目标（首次命中才把寿命压到 4 × 60 次更新）</summary>
        private bool hited = false;
        /// <summary>是否还在待发状态：待发期间不能命中，锁定目标的那一帧转为冲刺</summary>
        private bool launch = true;
        /// <summary>出手音只播一次</summary>
        private bool playSound = true;
        /// <summary>拖尾缓存 32 帧：TrailingMode 2 同时记录 oldPos 与 oldRot，供 PreDraw 摊成一串剑影</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 32;
        }
        /// <summary>基础属性：80×80、近战、无限穿透、存活 1440 次更新（6 × 60 × MaxUpdates 即 6 秒）、本地免疫 -1、MaxUpdates 4、不碰撞物块</summary>
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.friendly = true;
            Projectile.light = 1f;
            Projectile.penetrate = -1;                   // 无限穿透
            Projectile.timeLeft = 6 * 60 * 4;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.MaxUpdates = 4;
            Projectile.tileCollide = false;
        }
        /// <summary>待发期间不参与命中判定（CE 原样的起手保护）</summary>
        public override bool? CanHitNPC(NPC target) => launch ? false : null;
        /// <summary>出手音只播一次；前 30 帧原地自转减速，之后锁最近目标并转入冲刺/持续追踪；寿命不足 240 时按剩余比例淡出</summary>
        public override void AI()
        {
            if (playSound)
            {
                playSound = false;
                SoundEngine.PlaySound(SoundID.DD2_BetsyFireballShot, Projectile.position);
            }
            if (Projectile.timeLeft < 240)
            {
                Projectile.Opacity = Projectile.timeLeft / 240f;
            }
            if (init)
            {
                Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                init = false;
            }
            Projectile.rotation += 0.08f;
            if (counter < 30 * Projectile.MaxUpdates)
            {
                Projectile.velocity *= 0.96f;
            }
            else
            {
                NPC target = Projectile.FindTargetWithinRange(4000f, false);
                if (target != null)
                {
                    if (launch)
                    {
                        // 锁定目标：炸一发冲击波粒子、播爆响，然后朝目标（±0.6 弧度散射）猛冲
                        ImpactParticle impact = new ImpactParticle();
                        DRKLoader.NewParticle(impact, Projectile.Center, Vector2.Zero, Color.IndianRed, 0.18f);
                        impact.Configure(1f, true, ImpactParticle.DrawModeEnum.AdditiveBlend, (target.Center - Projectile.Center).ToRotation());
                        // CE 是就地改静态 SoundStyle（Pitch/Volume/MaxInstances）后播放，这里用 with 复制一份，避免污染全局音效
                        SoundEngine.PlaySound(SoundID.NPCDeath39 with { MaxInstances = 6, Pitch = -0.6f, Volume = 0.6f }, Projectile.Center);
                        Projectile.velocity = (target.Center - Projectile.Center).RotatedByRandom(0.6f).SafeNormalize(Vector2.Zero) * 16;
                        launch = false;
                    }
                    Projectile.ChasingBehavior2(target.Center, 1f, 0.01f);
                }
            }
            counter++;
        }
        /// <summary>首次命中才把寿命压到 4 × 60 次更新（即 1 秒淡出窗口），之后不再重置</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!hited)
            {
                Projectile.timeLeft = 4 * 60;
            }
            hited = true;
        }
        /// <summary>本体不直接绘制，而是把 32 帧位置历史摊成一串旋转剑影（越旧越偏蓝、越淡）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Type]; i++)
            {
                float prog = i / (float)ProjectileID.Sets.TrailCacheLength[Type];
                Color clr = Color.Lerp(new Color(242, 201, 190), new Color(48, 52, 79), prog) * 0.2f;
                Draw(Projectile.oldPos[i] + new Vector2(Projectile.width, Projectile.height) * 0.5f, clr, Projectile.oldRot[i], (int)Projectile.ai[1]);
            }
            return false;
        }
        /// <summary>绘制单帧剑影：dir &gt; 0 时附加 PiOver4，否则水平翻转并附加 Pi * 0.75（dir 取 ai[1]，本工程召唤处未传故为 0）</summary>
        private void Draw(Vector2 pos, Color lightColor, float rotation, int dir)
        {
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? rotation + MathHelper.PiOver4 : rotation + MathHelper.Pi * 0.75f;
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(texture, pos - Main.screenPosition, null, lightColor * Projectile.Opacity, rot, texture.Size() * 0.5f, Projectile.scale, effect);
        }
    }
}
