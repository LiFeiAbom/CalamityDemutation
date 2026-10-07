using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 纳米刀刃（纳米技术留下的弹幕）—— 行为整体照 CI 的 <c>NanotechOld</c> 弹幕
    /// （用户 2026-10-07 指定「纳米刀刃改成 CI」）：26×26、穿透 1、不撞墙不撞水；
    /// **前 30 帧不能命中**（<see cref="CanHitNPC"/>），第 30 帧起在 800 像素内追踪最近的敌人
    /// （速度 12 / 惯性 20 = <c>velocity = (velocity × 20 + 方向 × 12) / 21</c>，对应灾厄
    /// <c>CalamityUtils.HomeInOnNPC(proj, ignoreTiles: true, 800f, 12f, 20f)</c> 的手写展开——
    /// 本工程软依赖，不能直接调灾厄工具）；第 60 帧起每帧淡出 5 点直至消失；
    /// 死亡时炸 2 颗绿色纳米尘（<see cref="DustID.TerraBlade"/>，色相 0.4~0.6、去重力、缩放 0.7）。
    /// <para>
    /// 与旧写法的差别（1.4.2 / cal-1.2 都是）：旧版是"每 30 次命中留一枚、且射出即可命中"，
    /// CI 是"每 30 帧留一枚、刀刃先飞 30 帧不能打、之后才追踪"。
    /// </para>
    /// <para>
    /// 伤害类型按用户要求设为**盗贼**：<see cref="CDUtil.GetRogueDamageClass"/> 现代版取灾厄的真·盗贼类
    /// （<c>CalamityMod/RogueDamageClass</c>），只装经典版时回退 tML 的 Throwing。
    /// CI 原版把这枚刀刃强设成 Generic（避免二次吃盗贼加成），本工程按用户口径改成盗贼。
    /// </para>
    /// </summary>
    internal class Nanotech : ModProjectile
    {
        /// <summary>起追踪的帧数（照 CI：ai[1] ≥ 30 才允许命中并开始归航）</summary>
        private const float HomeDelay = 30f;
        /// <summary>起淡出的帧数（照 CI：ai[1] &gt; 60 后每帧 alpha +5）</summary>
        private const float FadeDelay = 60f;
        /// <summary>
        /// 基础属性：26×26、友好弹幕、穿透 1、不撞墙不撞水，伤害类型设为盗贼（用户指定）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>
        /// 命中判定：照 CI，只有飞出 30 帧之后才允许命中（且目标得是可追击的敌人）。
        /// 这样刀刃会在敌人身上"擦身而过"一小段，视觉上先铺一片纳米刃再回头咬上去。
        /// </summary>
        public override bool? CanHitNPC(NPC target) => Projectile.ai[1] >= HomeDelay && target.CanBeChasedBy(Projectile);
        /// <summary>
        /// 每帧：绿色照明 + 按速度自转（X 正负各偏 0.08 让翻转自然），计时到 60 帧后淡出、到 255 销毁，
        /// 30 帧起转入追踪
        /// </summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, new Vector3(0.075f, 0.4f, 0.15f));
            Projectile.rotation += Projectile.velocity.X * 0.2f;
            if (Projectile.velocity.X > 0f)
                Projectile.rotation += 0.08f;
            else
                Projectile.rotation -= 0.08f;
            Projectile.ai[1] += 1f;
            if (Projectile.ai[1] > FadeDelay)
            {
                Projectile.alpha += 5;
                if (Projectile.alpha >= 255)
                {
                    Projectile.alpha = 255;
                    Projectile.Kill();
                    return;
                }
            }
            if (Projectile.ai[1] >= HomeDelay)
                HomeInOnClosestNPC();
        }
        /// <summary>
        /// 800 像素内追踪最近敌人：照灾厄 <c>CalamityUtils.HomeInOnNPC</c> 的手写展开
        /// （本工程是软依赖，不能直接调它的工具方法）。距离用曼哈顿距离，与灾厄工具一致。
        /// </summary>
        private void HomeInOnClosestNPC()
        {
            Vector2 targetCenter = Projectile.Center;
            float closest = 800f;
            bool found = false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile))
                    continue;
                float distance = Math.Abs(Projectile.Center.X - npc.Center.X) + Math.Abs(Projectile.Center.Y - npc.Center.Y);
                if (distance >= closest)
                    continue;
                closest = distance;
                targetCenter = npc.Center;
                found = true;
            }
            if (!found)
                return;
            Vector2 toTarget = targetCenter - Projectile.Center;
            float length = toTarget.Length();
            if (length < 0.001f)
                return;
            toTarget *= 12f / length;                       // 归航速度 12
            Projectile.velocity = (Projectile.velocity * 20f + toTarget) / 21f;   // 惯性 20
        }
        /// <summary>
        /// 消失时炸 2 颗纳米尘：绿色（HSL 0.4~0.6）并往白里混 30%，去重力、缩放 0.7
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 2; i++)
            {
                int dustScale = (int)(10f * Projectile.scale);
                int dustIndex = Dust.NewDust(Projectile.Center - Vector2.One * dustScale, dustScale * 2, dustScale * 2, DustID.TerraBlade, 0f, 0f, 0, default, 1f);
                Dust dust = Main.dust[dustIndex];
                Vector2 outward = Vector2.Normalize(dust.position - Projectile.Center);
                dust.position = Projectile.Center + outward * dustScale * Projectile.scale;
                dust.velocity = outward * (Main.rand.Next(45, 91) / 10f);
                dust.color = Main.hslToRgb((float)(0.4f + Main.rand.NextDouble() * 0.2f), 0.9f, 0.5f);
                dust.color = Color.Lerp(dust.color, Color.White, 0.3f);
                dust.noGravity = true;
                dust.scale = 0.7f;
            }
        }
    }
}
