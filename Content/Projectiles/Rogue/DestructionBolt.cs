using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 毁灭鬼弹（照灾厄 2.0 的 <c>DestructionBolt</c>）：
    /// 12×12 判定、穿透 1、<c>extraUpdates = 1</c>、起手全透明（每帧 -8 淡入，<c>alpha &lt; 128</c> 之前不能造成伤害）、
    /// 存活 300 帧、不撞物块；带 10 点拖影（模式 1，本工程走 <c>CDUtil.DrawAfterimagesCentered</c>）。
    /// <para>
    /// 归航是**两段式状态机**（照源逐行）：<c>ai[1] = 0</c> 初始化（给 <c>localAI[0]</c> 一个随机负相位）→
    /// <c>ai[1] = 1</c> 时（仅主人端、且已看得见）在 300 像素内找一个**有视线**的目标，锁定后 <c>ai[1] = 6</c> 进入追击；
    /// 追击期目标失效就退回 1 重找、贴到 10 像素内直接自爆；<c>ai[1]</c> 在 1~5 之间来回只是"抖相位"，
    /// 让弹体的可见性/拖影有呼吸感（源就是靠这套相位驱动外观的）。
    /// </para>
    /// <para>
    /// 消失时把判定框撑到 50×50 并**补结算一次伤害**（<c>Projectile.Damage()</c>，源写法），
    /// 再炸 20 颗烟尘 + 45 组星尘 + 一片烟（非服务端）。
    /// </para>
    /// </summary>
    internal class DestructionBolt : ModProjectile
    {
        /// <summary>尘类型（源里是裸数字 191 的字段，本工程改成常量）</summary>
        private const int BoltDust = DustID.SpookyWood;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        }
        /// <summary>12×12、穿透 1、一倍额外更新、全透明起步、存活 300 帧、不撞物块，伤害类型取盗贼</summary>
        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 1;
            Projectile.alpha = 255;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = false;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>淡入 + 朝向速度 + 两段式归航状态机（照源逐行）</summary>
        public override void AI()
        {
            if (Main.rand.Next(8) == 0)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, BoltDust, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f);
            if (Projectile.alpha > 0)
                Projectile.alpha -= 8;
            Projectile.rotation = (float)Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X) + MathHelper.PiOver2;
            const float searchPhaseEnd = 5f;
            const float searchRange = 300f;
            const float homingSpeed = 6f;
            if (Projectile.ai[1] == 0f)
            {
                Projectile.ai[1] = 1f;
                Projectile.localAI[0] = -Main.rand.Next(48);
            }
            else if (Projectile.ai[1] == 1f && Projectile.owner == Main.myPlayer)
            {
                if (Projectile.alpha < 128)
                {
                    int targetIndex = -1;
                    float closest = searchRange;
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC npc = Main.npc[i];
                        if (!npc.active || !npc.CanBeChasedBy(Projectile, false))
                            continue;
                        float distance = Vector2.Distance(npc.Center, Projectile.Center);
                        if (distance < closest && targetIndex == -1 && Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1))
                        {
                            closest = distance;
                            targetIndex = i;
                        }
                    }
                    if (closest < 4f)
                    {
                        Projectile.Kill();
                        return;
                    }
                    if (targetIndex != -1)
                    {
                        Projectile.ai[1] = searchPhaseEnd + 1f;
                        Projectile.ai[0] = targetIndex;
                        Projectile.netUpdate = true;
                    }
                }
            }
            else if (Projectile.ai[1] > searchPhaseEnd)
            {
                Projectile.ai[1] += 1f;
                int targetIndex = (int)Projectile.ai[0];
                if (!Main.npc[targetIndex].active || !Main.npc[targetIndex].CanBeChasedBy(Projectile, false))
                {
                    Projectile.ai[1] = 1f;
                    Projectile.ai[0] = 0f;
                    Projectile.netUpdate = true;
                }
                else
                {
                    Vector2 toTarget = Main.npc[targetIndex].Center - Projectile.Center;
                    if (toTarget.Length() < 10f)
                    {
                        Projectile.Kill();
                        return;
                    }
                    if (toTarget != Vector2.Zero)
                    {
                        toTarget.Normalize();
                        toTarget *= homingSpeed;
                    }
                    const float inertia = 30f;
                    Projectile.velocity = (Projectile.velocity * (inertia - 1f) + toTarget) / inertia;
                }
            }
            if (Projectile.ai[1] >= 1f && Projectile.ai[1] < searchPhaseEnd)
            {
                Projectile.ai[1] += 1f;
                if (Projectile.ai[1] == searchPhaseEnd)
                    Projectile.ai[1] = 1f;
            }
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] == 48f)
                Projectile.localAI[0] = 0f;
        }
        /// <summary>10 点拖影（模式 1、抽稀 2 帧；照源调灾厄的 DrawAfterimagesCentered）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 2);
            return false;
        }
        /// <summary>淡入完成前不造成伤害（照源）</summary>
        public override bool? CanDamage() => Projectile.alpha < 128 ? null : false;
        /// <summary>消失：判定框撑到 50 补结算一次伤害 + 爆炸音 + 烟尘/星尘/烟（非服务端）</summary>
        public override void OnKill(int timeLeft)
        {
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 50;
            Projectile.position -= Projectile.Size * 0.5f;
            Projectile.maxPenetrate = -1;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.Damage();
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
            for (int i = 0; i < 20; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, default, 2f);
                Main.dust[dust].velocity *= 3f;
                if (Main.rand.NextBool(2))
                {
                    Main.dust[dust].scale = 0.5f;
                    Main.dust[dust].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
            }
            for (int i = 0; i < 45; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, BoltDust, 0f, 0f, 100, default, 3f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 5f;
                dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, BoltDust, 0f, 0f, 100, default, 2f);
                Main.dust[dust].velocity *= 2f;
            }
            if (Main.netMode != NetmodeID.Server)
                SpawnExplosionSmoke();
        }
        /// <summary>照源在非服务端撒 36 团原版烟（9 组 × 四方向、三种速度档）</summary>
        private void SpawnExplosionSmoke()
        {
            Vector2 source = Projectile.Center - new Vector2(24f, 24f);
            const int goreAmount = 9;
            for (int i = 0; i < goreAmount; i++)
            {
                float velocityMult = i < goreAmount / 3 ? 0.66f : (i >= 2 * goreAmount / 3 ? 1f : 0.33f);
                for (int direction = 0; direction < 4; direction++)
                {
                    int type = Main.rand.Next(61, 64);
                    int smoke = Gore.NewGore(Projectile.GetSource_Death(), source, default, type, 1f);
                    Gore gore = Main.gore[smoke];
                    gore.velocity *= velocityMult;
                    gore.velocity.X += direction == 0 || direction == 2 ? 1f : -1f;
                    gore.velocity.Y += direction < 2 ? 1f : -1f;
                }
            }
        }
    }
}
