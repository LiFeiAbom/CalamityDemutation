using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Typeless
{
    /// <summary>
    /// 星云之星 - 追踪敌人的魔法弹幕（移植自灾厄经典版 1.4.2.101 的同名弹幕，额外补了命中玩家的分支）
    /// 旋转发光、随时间改变尺寸与轨迹，命中时散射星云之尘（NebulaDust）
    /// </summary>
    internal class NebulaStar:ModProjectile
    {
        /// <summary>
        /// 基础属性：34x34 碰撞箱；友方、初始全透明随后淡入、单次穿透、无视地形与水、
        /// 存活上限 3600 帧（60 秒，防止无限漂移）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.alpha = 255;   // 初始全透明，随后淡入
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 3600;   // 存活上限 60 秒，防止弹幕无限漂移
        }
        /// <summary>
        /// AI：按透明度叠加紫色星光；做呼吸式缩放与持续旋转；按弹体编号 (identity%6) 决定漂移方向并周期性反向，形成 S 形轨迹；
        /// ai[0] 累到 5400 后进入"伤害清零 + 淡出"（累速随与主人的距离递增，离得越远越早淡出）；
        /// 曼哈顿距离 600 内有敌人时平滑追踪，否则缓慢减速漂移
        /// </summary>
        public override void AI()
        {
            // 依据当前透明度计算发光强度
            float num944 = 1f - (float)Projectile.alpha / 255f;
            num944 *= Projectile.scale;
            Lighting.AddLight(Projectile.Center, 0.25f * num944, 0.025f * num944, 0.275f * num944);
            Projectile.localAI[0] += 1f;
            // 每 90 帧反转一次尺寸变化方向，形成呼吸式缩放
            if (Projectile.localAI[0] >= 90f)
            {
                Projectile.localAI[0] *= -1f;
            }
            if (Projectile.localAI[0] >= 0f)
            {
                Projectile.scale += 0.003f;
            }
            else
            {
                Projectile.scale -= 0.003f;
            }
            Projectile.rotation += 0.0025f * Projectile.scale;
            float num945 = 1f;
            float num946 = 1f;
            // 依据弹体编号决定漂移方向，避免同类弹幕轨迹完全一致
            if (Projectile.identity % 6 == 0)
            {
                num946 *= -1f;
            }
            if (Projectile.identity % 6 == 1)
            {
                num945 *= -1f;
            }
            if (Projectile.identity % 6 == 2)
            {
                num946 *= -1f;
                num945 *= -1f;
            }
            if (Projectile.identity % 6 == 3)
            {
                num946 = 0f;
            }
            if (Projectile.identity % 6 == 4)
            {
                num945 = 0f;
            }
            Projectile.localAI[1] += 1f;
            // 周期性切换漂移方向，让轨迹呈 S 形摆动
            if (Projectile.localAI[1] > 60f)
            {
                Projectile.localAI[1] = -180f;
            }
            if (Projectile.localAI[1] >= -60f)
            {
                Projectile.velocity.X = Projectile.velocity.X + 0.002f * num946;
                Projectile.velocity.Y = Projectile.velocity.Y + 0.002f * num945;
            }
            else
            {
                Projectile.velocity.X = Projectile.velocity.X - 0.002f * num946;
                Projectile.velocity.Y = Projectile.velocity.Y - 0.002f * num945;
            }
            Projectile.ai[0] += 1f;
            // 存在过久（含远离主人的距离加成）后：伤害清零并开始淡出
            if (Projectile.ai[0] > 5400f)
            {
                Projectile.damage = 0;
                Projectile.ai[1] = 1f;
                if (Projectile.alpha < 255)
                {
                    Projectile.alpha += 5;
                    if (Projectile.alpha > 255)
                    {
                        Projectile.alpha = 255;
                    }
                }
                else if (Projectile.owner == Main.myPlayer)
                {
                    Projectile.Kill();
                }
            }
            else
            {
                // 距主人越远，ai[0] 增长越快，越早进入淡出阶段
                float num947 = (Projectile.Center - Main.player[Projectile.owner].Center).Length() / 100f;
                if (num947 > 4f)
                {
                    num947 *= 1.1f;
                }
                if (num947 > 5f)
                {
                    num947 *= 1.2f;
                }
                if (num947 > 6f)
                {
                    num947 *= 1.3f;
                }
                if (num947 > 7f)
                {
                    num947 *= 1.4f;
                }
                if (num947 > 8f)
                {
                    num947 *= 1.5f;
                }
                if (num947 > 9f)
                {
                    num947 *= 1.6f;
                }
                if (num947 > 10f)
                {
                    num947 *= 1.7f;
                }
                Projectile.ai[0] += num947;
                if (Projectile.alpha > 50)
                {
                    Projectile.alpha -= 10;
                    if (Projectile.alpha < 50)
                    {
                        Projectile.alpha = 50;
                    }
                }
            }
            bool flag49 = false;
            Vector2 center12 = new(0f, 0f);
            // 在 600 像素内寻找最近的敌人进行追踪
            float num948 = 600f;
            for (int num949 = 0; num949 < 200; num949++)
            {
                if (Main.npc[num949].CanBeChasedBy(Projectile, false))
                {
                    float num950 = Main.npc[num949].position.X + (float)(Main.npc[num949].width / 2);
                    float num951 = Main.npc[num949].position.Y + (float)(Main.npc[num949].height / 2);
                    float num952 = Math.Abs(Projectile.position.X + (float)(Projectile.width / 2) - num950) + Math.Abs(Projectile.position.Y + (float)(Projectile.height / 2) - num951);
                    if (num952 < num948)
                    {
                        num948 = num952;
                        center12 = Main.npc[num949].Center;
                        flag49 = true;
                    }
                }
            }
            if (flag49)
            {
                Vector2 vector101 = center12 - Projectile.Center;
                vector101.Normalize();
                vector101 *= 0.75f;
                Projectile.velocity = (Projectile.velocity * 10f + vector101) / 11f;
                return;
            }
            if ((double)Projectile.velocity.Length() > 0.2)
            {
                Projectile.velocity *= 0.98f;
            }
        }
        /// <summary>
        /// 命中敌人（且尚未进入淡出阶段，ai[1] == 0）：主人端在弹体处随机方向生成一颗星云尘埃（NebulaDust）作溅射
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.owner == Main.myPlayer && Projectile.ai[1] == 0f)
            {
                Vector2 value10 = new((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                value10.Normalize();
                value10 *= 0.3f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y, value10.X, value10.Y, ModContent.ProjectileType<NebulaDust>(), Projectile.damage, 0f, Projectile.owner, 0f, 0f);
            }
        }
        /// <summary>
        /// 命中玩家（PvP）：逻辑与命中敌人相同，主人端散射一颗星云尘埃
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Projectile.owner == Main.myPlayer && Projectile.ai[1] == 0f)
            {
                Vector2 value10 = new((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                value10.Normalize();
                value10 *= 0.3f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y, value10.X, value10.Y, ModContent.ProjectileType<NebulaDust>(), Projectile.damage, 0f, Projectile.owner, 0f, 0f);
            }
        }
        /// <summary>
        /// 后绘制：在弹体中心叠加一张随旋转缩放的发光贴图（NebulaStarGlow），强化星光视觉
        /// </summary>
        public override void PostDraw(Color lightColor)
        {
            Vector2 origin = new(17f, 17f);   // 17×17 的贴图中心（贴图 34×34）
            Main.spriteBatch.Draw(ModContent.Request<Texture2D>("CalamityDemutation/Content/Projectiles/Typeless/NebulaStarGlow").Value, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
        }
    }
}
