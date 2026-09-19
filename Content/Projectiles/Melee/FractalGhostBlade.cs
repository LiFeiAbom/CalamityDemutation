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
    /// 出手先原地自转 30 帧并逐帧减速（期间不能命中），随后锁定 4000 范围内最近的目标：
    /// 在剑心炸一发 <see cref="ImpactParticle"/>、播一声爆响，然后朝目标猛冲（带 ±0.6 弧度的散射）
    /// 并按 0.01 的强度持续贴向目标；命中后寿命压到 4 秒，剩余 4 秒起整体淡出。
    /// 本体只靠 <see cref="PreDraw"/> 摊开的 32 帧旋转残影呈现（贴图直接借用本体的辉光图）。
    /// </summary>
    internal class FractalGhostBlade:ModProjectile
    {
        /// <summary>直接借用武器本体的辉光贴图（CE 原样）</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/SpiritFractalGlow";
        /// <summary>出手时随机一个初始自转角（只做一次）</summary>
        private bool init = true;
        /// <summary>自身帧数计数，30 帧内只自转减速、不索敌</summary>
        private float counter = 0f;
        /// <summary>是否已经命中过目标（首次命中才把寿命压到 4 秒）</summary>
        private bool hited = false;
        /// <summary>是否还在待发状态：待发期间不能命中，锁定目标的那一帧转为冲刺</summary>
        private bool launch = true;
        /// <summary>出手音只播一次</summary>
        private bool playSound = true;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 32;
        }
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
        private void Draw(Vector2 pos, Color lightColor, float rotation, int dir)
        {
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? rotation + MathHelper.PiOver4 : rotation + MathHelper.Pi * 0.75f;
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(texture, pos - Main.screenPosition, null, lightColor * Projectile.Opacity, rot, texture.Size() * 0.5f, Projectile.scale, effect);
        }
    }
}
