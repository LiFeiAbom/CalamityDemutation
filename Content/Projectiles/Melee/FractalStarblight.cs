using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Particles;
using CalamityDemutation.Sounds;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using PulseRingParticle = CalamityDemutation.Particles.DirectionalPulseRing;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 分形渊星（FractalStarblight，移植自 CalamityEntropy 的 <c>FractalStarblight</c>，CE 把它和另两把武器的弹幕挤在
    /// 同一个 Starblight.cs 里）：分形之星消散时朝四个正交方向射出的追踪星辰。
    /// 出膛后先直飞，飞近敌怪（200 像素内且已飞过 30 帧）时减速转向；16 帧后开始持续朝 1200 像素内的敌怪加速，
    /// 只要还锁着目标就把寿命续回 60 帧。全程拖一条 <see cref="StarTrailParticle"/> 星轨，消散时炸一个定向脉冲环。
    /// <para>
    /// 与 CE 原版的差异：① 贴图用本模组的 Assets/ExtraTextures/white 占位（本体完全由星轨与粒子呈现）；
    /// ② <c>CEUtils.getDistance</c> → <c>Vector2.Distance</c>、<c>CEUtils.FindTarget_HomingProj</c> 内联，
    /// 范围内的短距离索敌沿用 tML 自带的 <c>Projectile.FindTargetWithinRange</c>（CE 调的就是同一个 API）；
    /// ③ CE 的 <c>PRT_StarTrailParticle</c> → <see cref="StarTrailParticle"/>；④ 消散时的 <c>PRT_DirectionalPulseRing</c>
    /// 改用本工程既有的 <c>Particles.DirectionalPulseRing</c>（弑神者冲刺那支同源粒子，同一个灾厄祖先，不重复移植）；
    /// ⑤ <c>CEUtils.PlaySound("metalhit", …)</c> → 本模组 <see cref="CalamityDemutationSounds.FractalBlightFade"/>，
    /// 音高按既有口径取 CE 值减 1（1.6~2 → 0.6~1.0），<c>CEUtils.WeapSound</c> 按 1.0；
    /// ⑥ 去掉 CE 里只写不读的 <c>std</c> / <c>homingTime</c> / <c>tofs</c> / <c>alpha_</c> 与 <c>Main.projFrames</c> 设置。
    /// </para>
    /// </summary>
    internal class FractalStarblight:ModProjectile
    {
        /// <summary>占位贴图（本体不可见，可见部分是星轨）</summary>
        public override string Texture => "CalamityDemutation/Assets/ExtraTextures/white";
        /// <summary>自身帧数计数</summary>
        private int counter = 0;
        /// <summary>追踪强度（16 帧后每帧 +0.1，上限 4）</summary>
        private float homing = 0f;
        /// <summary>跟随本弹幕的星轨粒子：只在生成时建一次，之后每帧更新它的速度与寿命</summary>
        private StarTrailParticle starTrail = null;
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 260;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 0;
            Projectile.ArmorPenetration = 12;
        }
        public override void AI()
        {
            if (starTrail == null)
            {
                starTrail = new StarTrailParticle();
                DRKLoader.NewParticle(starTrail, Projectile.Center, Vector2.Zero, Color.LightBlue, 1f);
                starTrail.Configure(true);
                starTrail.maxLength = 12;
            }
            // 每帧把星轨的速度对齐自身、并把寿命续回去（星轨因此能一直跟着跑到弹幕消失）
            starTrail.Velocity = Projectile.velocity;
            starTrail.Lifetime = 30;
            counter++;
            Projectile.ai[0]++;
            NPC target = Projectile.FindTargetWithinRange(1600f, false);
            if (target != null && Vector2.Distance(target.Center, Projectile.Center) < 200 && counter > 30)
            {
                // 近距离：减速并朝目标偏一下
                Projectile.velocity *= 0.9f;
                Vector2 v = target.Center - Projectile.Center;
                v.Normalize();
                Projectile.velocity += v * 1.5f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.velocity.Length() > 3)
            {
                Projectile.velocity *= 0.995f - homing * 0.018f;
            }
            if (counter > 16)
            {
                // 16 帧后开始追踪：锁到目标就把寿命续到 60 帧并持续加速
                if (homing < 4)
                {
                    homing += 0.1f;
                }
                NPC lockOn = FindTargetHomingProj(1200f);
                if (lockOn != null)
                {
                    if (Projectile.timeLeft < 60)
                    {
                        Projectile.timeLeft = 60;
                    }
                    Projectile.velocity += (lockOn.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * homing * 2;
                }
            }
        }
        public override bool PreDraw(ref Color lightColor) => false;
        /// <summary>消散：炸一个定向脉冲环（用工程既有那支）并播一次金属撞击音</summary>
        public override void OnKill(int timeLeft)
        {
            GeneralParticleHandler.SpawnParticle(new PulseRingParticle(Projectile.Center, Vector2.Zero, Color.LightBlue,
                new Vector2(2f, 2f), 0f, 0.02f, 0.85f * 0.4f, 18));
            SoundEngine.PlaySound(CalamityDemutationSounds.FractalBlightFade with { Pitch = Main.rand.NextFloat(0.6f, 1f), Volume = 0.35f }, Projectile.Center);
        }
        /// <summary>CEUtils.FindTarget_HomingProj 的等价实现：取 maxDistance 内最近的可攻击敌怪</summary>
        private NPC FindTargetHomingProj(float maxDistance)
        {
            NPC target = null;
            float nearest = maxDistance;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile) || npc.friendly)
                {
                    continue;
                }
                float distance = Vector2.Distance(npc.Center, Projectile.Center);
                if (distance <= nearest)
                {
                    nearest = distance;
                    target = npc;
                }
            }
            return target;
        }
    }
}
