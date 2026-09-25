using CalamityDemutation.Particles;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 宙宇波能刃的右键炸弹（移植自灾厄大修 0.4.0.1.3 ExcelsusBomb）：
    /// 右键投出的蓝色炸弹，命中即爆，爆炸时放大到 600×600 结算伤害并喷紫色光粒与三圈尘土。贴图复用 StreamGouge。
    /// </summary>
    internal class ExcelsusBomb : ModProjectile
    {
        /// <summary>贴图复用流刃（StreamGouge）的贴图</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/Melee/StreamGouge";
        /// <summary>基础属性：32×32、碰撞物块、穿透 1、本地免疫 -1、不受水减速、MaxUpdates 5</summary>
        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ignoreWater = true;
            Projectile.MaxUpdates = 5;   // 每帧跑 5 次 AI，故下方向 AI 里的一次喷尘实际每帧会喷 5~10 次
        }
        /// <summary>朝向对齐速度并喷尘，1/8 概率额外喷一次</summary>
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            SpanDust();
            if (Main.rand.NextBool(8))
                SpanDust();
        }
        /// <summary>喷一圈蓝/粉尘土（随机蓝精灵或粉火炬尘，带 customData 供着色）</summary>
        public void SpanDust()
        {
            int dustType = Main.rand.NextBool(3) ? DustID.BlueFairy : DustID.PinkTorch;   // 1/3 概率蓝精灵尘，否则粉火炬尘
            if (Main.rand.NextBool())
            {
                // 上半分支：随机取一个方向，在弹幕后方 10~20 像素处生成一颗沿切线甩出的尘
                Vector2 vector3 = Vector2.UnitY.RotatedByRandom(MathHelper.TwoPi);
                Dust obj3 = Main.dust[Dust.NewDust(Projectile.Center - vector3 * 30f, 0, 0, dustType)];
                obj3.noGravity = true;
                obj3.position = Projectile.Center - vector3 * Main.rand.Next(10, 21);
                obj3.velocity = vector3.RotatedBy(MathHelper.PiOver2) * 6f;
                obj3.scale = 0.9f + Main.rand.NextFloat();
                obj3.fadeIn = 0.5f;
                obj3.customData = Projectile;
                // 原码在此只是换方向重新摆一次上面那颗尘（未新建第二颗），并额外染成猩红
                vector3 = Vector2.UnitY.RotatedByRandom(MathHelper.TwoPi);
                obj3.noGravity = true;
                obj3.position = Projectile.Center - vector3 * Main.rand.Next(10, 21);
                obj3.velocity = vector3.RotatedBy(MathHelper.PiOver2) * 6f;
                obj3.scale = 0.9f + Main.rand.NextFloat();
                obj3.fadeIn = 0.5f;
                obj3.customData = Projectile;
                obj3.color = Color.Crimson;
            }
            else
            {
                // 下半分支：方向同上，但半径更远（20~30）、切线方向相反、速度略慢
                Vector2 vector4 = Vector2.UnitY.RotatedByRandom(MathHelper.TwoPi);
                Dust obj4 = Main.dust[Dust.NewDust(Projectile.Center - vector4 * 30f, 0, 0, dustType)];
                obj4.noGravity = true;
                obj4.position = Projectile.Center - vector4 * Main.rand.Next(20, 31);
                obj4.velocity = vector4.RotatedBy(-MathHelper.PiOver2) * 5f;
                obj4.scale = 0.9f + Main.rand.NextFloat();
                obj4.fadeIn = 0.5f;
                obj4.customData = Projectile;
            }
        }
        /// <summary>命中时发出蓝光</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Lighting.AddLight(Projectile.position, Color.Blue.ToVector3());
            base.OnHitNPC(target, hit, damageDone);
        }
        /// <summary>死亡即爆炸：播音、放大到 600×600 结算伤害、喷三圈尘土与紫色光粒</summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
            Lighting.AddLight(Projectile.position, Color.Blue.ToVector3() * 3);
            Projectile.width = 600;   // 先把判定框放大，再补一次范围伤害
            Projectile.height = 600;
            Projectile.Center = Projectile.position;   // 以放大前的左上角为新框中心（原码如此，会整体偏出半个原框）
            Projectile.Damage();
            for (int j = 0; j < 3; j++)
            {
                int dustType = Main.rand.NextBool(3) ? DustID.BlueFairy : DustID.PinkTorch;
                float scale = Main.rand.NextFloat(1f, 1.35f);
                for (float spikeAngle = 0f; spikeAngle < MathHelper.TwoPi; spikeAngle += 0.15f)   // 每圈约 42 个方向
                {
                    Vector2 offset = spikeAngle.ToRotationVector2() * Main.rand.NextFloat(3.95f, 7.05f);
                    Dust dust = Dust.NewDustPerfect(Projectile.Center + GetRandomVector(0, 360, Main.rand.Next(16, 220)), dustType, offset, 0, default, scale);
                    dust.customData = 0.025f;
                    dust.scale *= dustType == DustID.BlueFairy ? 0.5f : 1;
                }
                if (Main.netMode != NetmodeID.Server)
                {
                    // 每圈再补 10 颗紫色光粒（纯表现，服务端不生成）；速度模长由 -8*(i/20) 从 0 变到 -3.6，负值即方向取反
                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 particleSpeed = GetRandomVector(60, 120, -8 * (i / 20f));
                        Vector2 pos = Projectile.Center + new Vector2(Main.rand.Next(-16, 6), Main.rand.Next(0, 76)) + new Vector2(Main.rand.Next(-166, 166), 0);
                        GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(pos, particleSpeed, Main.rand.NextFloat(0.6f, 1.1f), Color.Purple, 60, 1f, 1.5f));
                    }
                }
            }
        }
        /// <summary>等价大修 CWRUtils.GetRandomVevtor：在 [startAngle, targetAngle] 度范围随机方向乘模长</summary>
        private static Vector2 GetRandomVector(float startAngle, float targetAngle, float length)
        {
            float angle = (startAngle + (targetAngle - startAngle) * Main.rand.NextFloat()) * (MathHelper.Pi / 180f);
            return new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * length;
        }
        /// <summary>旋转 PiOver4 绘制 StreamGouge 贴图</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D mainValue = CDUtil.GetT2DValue(Texture);
            Main.EntitySpriteDraw(mainValue, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation + MathHelper.PiOver4, CDUtil.GetOrig(mainValue), Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
