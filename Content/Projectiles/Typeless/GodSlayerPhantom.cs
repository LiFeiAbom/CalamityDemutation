using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Typeless
{
    /// <summary>
    /// 弑神幻影（GodSlayerPhantom） - 弑神者召唤头套装的"命中召唤"追加弹幕
    ///（按经典版灾厄 Projectiles/Typeless/GodSlayerPhantom.cs 1:1 移植）。
    /// 由 <c>CalamityDemutationGlobalProjectile.OnHitNPC</c> 在"召唤物/哨兵命中敌人且节流预算归零"时生成：
    /// 三帧动画、朝 400 像素内最近的敌人加速飞行（限速 15）、离主人超过 600 像素时先飞回主人；
    /// 命中附加 600 帧弑神者地狱火，消亡时炸一圈 ShadowbeamStaff 尘并结算一次范围伤害。
    /// ai[1] 由生成方给定（随机 0.5~1.5），用于放大"回主人"时的机动力。
    /// </summary>
    public class GodSlayerPhantom : ModProjectile
    {
        /// <summary>三帧循环动画</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 3;
        }
        /// <summary>
        /// 基础属性：20×20；半透明、友方、入水不减速、局部无敌 10 帧、无限穿透、存活 300 帧
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.alpha = 100;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
        }
        /// <summary>
        /// AI：推进三帧动画并喷尘；离主人超过 600 像素时优先飞回，否则锁定 400 像素内最近的敌人加速追击
        /// </summary>
        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 6)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 2)
            {
                Projectile.frame = 0;
            }
            int dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.ShadowbeamStaff, 0f, 0f, 0, default(Color), 0.5f);
            Main.dust[dustIndex].velocity *= 0.1f;
            Main.dust[dustIndex].scale = 1.3f;
            Main.dust[dustIndex].noGravity = true;
            float inertia = 40f * Projectile.ai[1];
            float returnSpeed = 8f * Projectile.ai[1];
            const float leashRange = 600f;
            Projectile.rotation = (float)Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X) - MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.5f, 0.2f, 0.9f);
            if (Main.player[Projectile.owner].active && !Main.player[Projectile.owner].dead)
            {
                if (Projectile.Distance(Main.player[Projectile.owner].Center) > leashRange)
                {
                    Vector2 toOwner = Projectile.DirectionTo(Main.player[Projectile.owner].Center);
                    if (toOwner.HasNaNs())
                    {
                        toOwner = Vector2.UnitY;
                    }
                    Projectile.velocity = (Projectile.velocity * (inertia - 1f) + toOwner * returnSpeed) / inertia;
                    return;
                }
                float targetX = Projectile.Center.X;
                float targetY = Projectile.Center.Y;
                float nearest = 400f;
                bool foundTarget = false;
                for (int i = 0; i < 200; i++)
                {
                    if (Main.npc[i].CanBeChasedBy(Projectile, false) && Collision.CanHit(Projectile.Center, 1, 1, Main.npc[i].Center, 1, 1))
                    {
                        float npcCenterX = Main.npc[i].position.X + Main.npc[i].width / 2;
                        float npcCenterY = Main.npc[i].position.Y + Main.npc[i].height / 2;
                        float manhattan = Math.Abs(Projectile.position.X + Projectile.width / 2 - npcCenterX) + Math.Abs(Projectile.position.Y + Projectile.height / 2 - npcCenterY);
                        if (manhattan < nearest)
                        {
                            nearest = manhattan;
                            targetX = npcCenterX;
                            targetY = npcCenterY;
                            foundTarget = true;
                        }
                    }
                }
                if (foundTarget)
                {
                    const float homingSpeed = 15f;
                    Vector2 center = new Vector2(Projectile.position.X + Projectile.width * 0.5f, Projectile.position.Y + Projectile.height * 0.5f);
                    float dx = targetX - center.X;
                    float dy = targetY - center.Y;
                    float length = (float)Math.Sqrt(dx * dx + dy * dy);
                    length = homingSpeed / length;
                    dx *= length;
                    dy *= length;
                    Projectile.velocity.X = (Projectile.velocity.X * 20f + dx) / 21f;
                    Projectile.velocity.Y = (Projectile.velocity.Y * 20f + dy) / 21f;
                    return;
                }
            }
            else if (Projectile.timeLeft > 30)
            {
                Projectile.timeLeft = 30;   // 主人没了就尽快消散
            }
        }
        /// <summary>
        /// 自绘：按当前帧画一段三帧动画贴图（关掉原版绘制）
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            int frameY = frameHeight * Projectile.frame;
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Rectangle(0, frameY, texture.Width, frameHeight), Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2(texture.Width / 2f, frameHeight / 2f), Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
        /// <summary>
        /// 绘制颜色：散场前 85 帧随剩余寿命渐隐，其余时间保持半透明白
        /// </summary>
        public override Color? GetAlpha(Color lightColor)
        {
            if (Projectile.timeLeft < 85)
            {
                byte brightness = (byte)(Projectile.timeLeft * 3);
                byte alpha = (byte)(100f * (brightness / 255f));
                return new Color(brightness, brightness, brightness, alpha);
            }
            return new Color(255, 255, 255, 100);
        }
        /// <summary>
        /// 命中敌人：附加 600 帧弑神者地狱火（按现代/经典两版分别查找，未安装则不生效）
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "GodSlayerInferno", 600);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "GodSlayerInferno", 600);
        }
        /// <summary>
        /// PvP 命中：与 OnHitNPC 同构，给玩家挂 600 帧弑神者地狱火（按现代/经典两版分别查找，未安装则不生效）
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "GodSlayerInferno", 600);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "GodSlayerInferno", 600);
        }
        /// <summary>
        /// 消亡：把判定框撑到 40×40，炸一圈 ShadowbeamStaff 尘，并结算一次范围伤害
        ///（<c>Projectile.Damage()</c> 只会由主人端结算，非主人端自动跳过，无需判据）
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 40;
            Projectile.position.X -= Projectile.width / 2;
            Projectile.position.Y -= Projectile.height / 2;
            const int dustCount = 36;
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 offset = Vector2.Normalize(Projectile.velocity) * new Vector2(Projectile.width / 2f, Projectile.height) * 0.75f;
                offset = offset.RotatedBy((i - (dustCount / 2 - 1)) * MathHelper.TwoPi / dustCount) + Projectile.Center;
                Vector2 dustVelocity = offset - Projectile.Center;
                int dustIndex = Dust.NewDust(offset + dustVelocity, 0, 0, DustID.ShadowbeamStaff, dustVelocity.X * 1.5f, dustVelocity.Y * 1.5f, 100, default(Color), 0.5f);
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].noLight = true;
                Main.dust[dustIndex].velocity = dustVelocity;
            }
            Projectile.Damage();
        }
    }
}
