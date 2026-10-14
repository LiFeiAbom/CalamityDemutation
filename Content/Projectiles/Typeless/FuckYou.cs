using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Typeless
{
    /// <summary>
    /// 爆炸（FuckYou） - 从灾厄 2.0 的 <c>CalamityMod.Projectiles.Typeless.FuckYou</c> 搬来的**通用爆炸弹幕**
    /// （类名照源保留；CI 那边是直接调灾厄这一件，本工程按用户 2026-10-14 口径自持同款）。
    /// 它的用途是"死亡/消散时炸一下"，龙息弹（<c>DragonsBreathRound</c>）就靠它做击杀爆炸。
    /// <para>
    /// 数据逐条照源：判定 8×8 起手，**第一帧**放大到 `52 × scale`（`scale = ai[1] * 0.5`，`ai[1]` 由调用方给
    /// 0.85~2.0，所以最终约 22~52 见方）并按新尺寸重新居中，同时放 `Item14` 音效与四组粉尘
    /// （`DustID.Smoke` ×4、`DustID.Torch` ×10 两发、`DustID.Torch` ×5 沿速度方向、`DustID.Smoke` ×10 沿速度方向；
    /// 源里写的是数字 31 / 6，本工程按 tML 官方建议改写成常量）；
    /// 之后 5 帧动画（每 3 帧一换）、`alpha` 每帧 -63 淡入、`ai[0] ≥ 15` 时自毁（存活 60 帧是留给视觉的）；
    /// `penetrate = -1` 但 `usesLocalNPCImmunity + localNPCHitCooldown = 600` 保证对同一个目标只打一次；
    /// 命中挂「着火了！」180 帧（NPC 与 PvP 成对，照源）。
    /// 绘制：`ai[0] > 1` 才画本体（第一帧只出音效与粉尘），颜色恒为 (255,255,255,127)。
    /// 贴图 `FuckYou.png` = 52×260（5 帧 × 52×52）。
    /// </para>
    /// <para>
    /// 未改动的两处源写法：`Lighting.AddLight(Center, 0.9, 0.8, 0.6)` 的暖光，以及命中时把
    /// `Projectile.direction` 同步成主人的朝向（源为贴合贴图方向）。
    /// </para>
    /// </summary>
    internal class FuckYou:ModProjectile
    {
        /// <summary>5 帧动画（照源）</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 5;
        }
        /// <summary>
        /// 爆炸本体：8×8 起手、穿透 -1、存活 60 帧、隐形起手（alpha 255）、
        /// 局部无敌 600 帧保证只命中一次（照源）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;                 // 存活较久，主要是为了视觉效果
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 600;     // 同一目标绝不二次命中
        }
        /// <summary>
        /// AI：加暖光 → 按 ai[1] 缩放 → 帧计数与淡入 → 第一帧放大判定并炸出音效与粉尘 → 15 帧后自毁
        /// </summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.9f, 0.8f, 0.6f);
            Projectile.ai[1] += 0.01f;
            Projectile.scale = Projectile.ai[1] * 0.5f;
            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] >= Main.projFrames[Type] * 3f)
            {
                Projectile.Kill();
                return;
            }
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 3)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= Main.projFrames[Type])
                    Projectile.hide = true;           // 帧放完就隐藏本体（视觉部分只留粉尘）
            }
            Projectile.alpha -= 63;
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;

            if (Projectile.ai[0] == 1f)
            {
                // 第一帧：把判定放大到 52 × scale 并重新居中
                Projectile.position = Projectile.Center;
                Projectile.width = Projectile.height = (int)(52f * Projectile.scale);
                Projectile.Center = Projectile.position;
                SoundEngine.PlaySound(SoundID.Item14, Projectile.position);

                // 4 颗烟尘（DustID.Smoke）
                for (int i = 0; i < 4; i++)
                {
                    int smokeDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, default, 1.5f);
                    Main.dust[smokeDust].position = Projectile.Center + Vector2.UnitY.RotatedByRandom(Math.PI) * (float)Main.rand.NextDouble() * Projectile.width / 2f;
                }
                // 10 组火尘（每组两颗：大颗外扩、小颗淡入）
                for (int i = 0; i < 10; i++)
                {
                    int fireDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 200, default, 2.7f);
                    Main.dust[fireDust].position = Projectile.Center + Vector2.UnitY.RotatedByRandom(Math.PI) * (float)Main.rand.NextDouble() * Projectile.width / 2f;
                    Main.dust[fireDust].noGravity = true;
                    Main.dust[fireDust].velocity *= 3f;

                    int fireDustSmall = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 1.5f);
                    Main.dust[fireDustSmall].position = Projectile.Center + Vector2.UnitY.RotatedByRandom(Math.PI) * (float)Main.rand.NextDouble() * Projectile.width / 2f;
                    Main.dust[fireDustSmall].velocity *= 2f;
                    Main.dust[fireDustSmall].noGravity = true;
                    Main.dust[fireDustSmall].fadeIn = 2.5f;
                }
                // 5 颗火尘：沿速度方向铺开
                for (int i = 0; i < 5; i++)
                {
                    int fireDustAlong = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 0, default, 2.7f);
                    Main.dust[fireDustAlong].position = Projectile.Center + Vector2.UnitX.RotatedByRandom(Math.PI).RotatedBy(Projectile.velocity.ToRotation()) * Projectile.width / 2f;
                    Main.dust[fireDustAlong].noGravity = true;
                    Main.dust[fireDustAlong].velocity *= 3f;
                }
                // 10 颗灰烟：沿速度方向铺开
                for (int i = 0; i < 10; i++)
                {
                    int smokeAlong = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 0, default, 1.5f);
                    Main.dust[smokeAlong].position = Projectile.Center + Vector2.UnitX.RotatedByRandom(Math.PI).RotatedBy(Projectile.velocity.ToRotation()) * Projectile.width / 2f;
                    Main.dust[smokeAlong].noGravity = true;
                    Main.dust[smokeAlong].velocity *= 3f;
                }
            }
        }
        /// <summary>
        /// 第一帧只出音效与粉尘，不画本体（照源）
        /// </summary>
        public override bool PreDraw(ref Color lightColor) => Projectile.ai[0] > 1f;
        /// <summary>
        /// 固定半透明白色（照源）
        /// </summary>
        public override Color? GetAlpha(Color lightColor) => new Color(255, 255, 255, 127);
        /// <summary>
        /// 命中 NPC：「着火了！」180 帧，并把贴图朝向同步成主人朝向（照源）
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, 180);
            Projectile.direction = Main.player[Projectile.owner].direction;
        }
        /// <summary>
        /// PvP 命中：同上（本工程要求命中钩子成对，源 2.0 里写的也是成对的）
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 180);
            Projectile.direction = Main.player[Projectile.owner].direction;
        }
    }
}
