using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 渊水弹（AbyssalBullet，移植自 CalamityEntropy）：深渊分形挥砍中段朝侧向扇形射出的三发追踪弹。
    /// 出膛先直飞 19 帧，之后朝 1200 像素内最近的敌怪加速转向；飞行途中每帧在身后吐深渊粒子，
    /// 命中附加减益并再炸出一圈粒子。本体不绘制（贴图只是占位，可见部分全由深渊粒子与上屏合成呈现）。
    /// <para>
    /// 与 CE 原版的差异：① <c>CEUtils.normalize</c> 等价为 <c>SafeNormalize</c>，
    /// <c>CEUtils.randomPointInCircle</c> 已内联；② 索敌沿用 tML 自带的 <c>Projectile.FindTargetWithinRange</c>
    /// （CE 调的就是同一个 API），不必内联索敌循环；③ 减益不新建——两版灾厄都有 <c>CrushDepth</c>
    /// （CE 挂的就是它），各取各自现成的即可，统一经 <see cref="CalamityDemutationPlayer.ApplyCalamityBuff"/> 挂载；
    /// ④ 粒子改为本模组 <see cref="AbyssalParticle"/>（CE 的 <c>PRTLoader.NewParticle&lt;PRT_Abyssal&gt;</c>
    /// 对应本工程「构造后交给 DRKLoader.NewParticle」）。
    /// </para>
    /// </summary>
    internal class AbyssalBullet:ModProjectile
    {
        /// <summary>贴图只是占位，本体完全不可见（可见部分是深渊粒子）</summary>
        public override string Texture => "CalamityDemutation/Assets/ExtraTextures/white";
        /// <summary>当前锁定的追踪目标（未锁定或目标失效时为 null）</summary>
        private NPC homing = null;
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.light = 1f;
            Projectile.timeLeft = 400;
            Projectile.extraUpdates = 4;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.tileCollide = false;
        }
        public override void AI()
        {
            Projectile.localAI[0]++;
            if (homing == null)
            {
                homing = Projectile.FindTargetWithinRange(1200f);
            }
            else if (!homing.active)
            {
                homing = null;
            }
            if (homing != null && Projectile.localAI[0] > 19)
            {
                // 出膛 19 帧后才开始追踪：朝目标叠一个朝向分量再整体限速
                Projectile.velocity += (homing.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 0.5f;
                Projectile.velocity *= 0.97f;
            }
            if (Projectile.timeLeft < 40)
            {
                Projectile.Opacity -= 1 / 40f;
            }
            for (int i = 0; i < 5; i++)
            {
                SpawnParticle(Projectile.Center, RandomPointInCircle(3), 0.96f, 0.05f, 0.38f * Main.rand.NextFloat(0.8f, 1f));
            }
        }
        /// <summary>命中：附加深海减益（两版各取现成的）并炸出一圈深渊粒子</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "CrushDepth", 300);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "CrushDepth", 300);
            for (int i = 0; i < 16; i++)
            {
                SpawnParticle(Projectile.Center, RandomPointInCircle(6), 0.98f, 0.014f, 0.6f * Main.rand.NextFloat(0.4f, 1f));
            }
        }
        public override bool PreDraw(ref Color lightColor) => false;
        /// <summary>吐一颗深渊粒子（vd/ad/初透明度按 CE 各调用点的取值传入）</summary>
        private static void SpawnParticle(Vector2 position, Vector2 velocity, float vd, float ad, float opacity)
        {
            AbyssalParticle p = new AbyssalParticle();
            DRKLoader.NewParticle(p, position, velocity, Color.White);
            p.vd = vd;
            p.ad = ad;
            p.Opacity = opacity;
        }
        /// <summary>CEUtils.randomPointInCircle 的等价实现：随机角度 × [-r, r] 的随机半径</summary>
        private static Vector2 RandomPointInCircle(float r) => Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(-r, r);
    }
}
