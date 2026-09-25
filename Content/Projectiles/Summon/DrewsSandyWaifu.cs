using CalamityDemutation.Content.Projectiles.Healing;
using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 沙之老婆·Drew 版（DrewsSandyWaifu） - 由"有胸的瓶中老婆"（wifeinaBottlewithBoobs）召唤的宠物，
    /// 贴图与沙之老婆同源但动作更少，功能是每约 2 秒生成一颗治疗法珠（<see cref="CactusHealOrb"/>）为玩家回血。
    /// <para>
    /// 与沙之老婆不同：本弹幕不索敌也不攻击，只跟随主人；<c>ai[0]</c> 仅区分 0 = 跟随、1 = 召回，
    /// <c>ai[1]</c> 是生成法珠的冷却计时器、<c>localAI[0]</c> 是"出生后满 120 帧才允许回血"的启动延迟。
    /// </para>
    /// </summary>
    internal class DrewsSandyWaifu:ModProjectile
    {
        // ── 实例字段 ──
        /// <summary>出生粒子爆发计数器：前 4 帧（3→&lt;0）一次性喷出大团沙尘作登场特效</summary>
        public int dust = 3;
        // ── 生命周期方法 ──
        /// <summary>
        /// 注册 5 帧动画，标记为宠物（<c>Main.projPet</c>）且可牺牲。
        /// 注意：未注册 <c>MinionTargettingFeature</c>，故玩家无法用右键给它指定目标。
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
        }
        /// <summary>
        /// 基础属性：42x98 碰撞箱、无召唤栏消耗（<c>minionSlots = 0</c>）、无限穿透、不撞地形。
        /// 命中冷却以 20 帧打底，随 Boss 进度每解锁一项递减（歌利亚 -5、月总 -5、噬神者 -4、犽戎 -3）；
        /// 本弹幕不索敌也不开火，设置 <c>friendly</c> 只是为了与同族召唤物保持同一套配置。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 42;
            Projectile.height = 98;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.minionSlots = 0f;
            Projectile.timeLeft = 18000;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft *= 5;   // 超长兜底寿命，实际靠宠物标志压到 2 帧续命
            Projectile.minion = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20 -
                (NPC.downedGolemBoss ? 5 : 0) -
                (NPC.downedMoonlord ? 5 : 0) -
                (BossSystem.DevourerOfGods ? 4 : 0) -
                (BossSystem.Yharon ? 3 : 0);
        }
        /// <summary>
        /// 宠物 AI：校验饰品标志（失效即消失）、续命、喷吐登场沙尘、推进动画、同类分离、跟随玩家，
        /// 并在冷却结束且已过启动延迟时生成治疗法珠。
        /// </summary>
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            // 未装备瓶中老婆且未开启"全体老婆"时直接消失
            if (!modPlayer.wifeinaBottlewithBoobs && !modPlayer.allWaifus)
            {
                Projectile.active = false;
                return;
            }
            if (player.dead)
            {
                modPlayer.drewsSandyWaifu = false;   // 玩家死亡时清空宠物标志，复活后由饰品重新召唤
            }
            // 召唤标志有效时持续刷新存活时间，实现常驻跟随
            if (modPlayer.drewsSandyWaifu)
            {
                Projectile.timeLeft = 2;
            }
            dust--;   // 计数器递减，<0 后不再喷发
            if (dust >= 0)
            {
                int num501 = 50;   // 出生时一次性喷出 50 颗沙尘
                for (int num502 = 0; num502 < num501; num502++)
                {
                    int num503 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y + 16f), Projectile.width, Projectile.height - 16, DustID.Sand, 0f, 0f, 0, default(Color), 1f);
                    Main.dust[num503].velocity *= 2f;
                    Main.dust[num503].scale *= 1.15f;
                }
            }
            // 5 帧循环动画（每 17 帧推进一帧）
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 16)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 4)
            {
                Projectile.frame = 0;
            }
            if ((double)Math.Abs(Projectile.velocity.X) > 0.2)
            {
                Projectile.spriteDirection = -Projectile.direction;   // 水平移动较快时让贴图朝向运动方向
            }
            float num636 = 100f;   // 召回态判定"已回到主人身边"的距离（宠物贴得很近；原版遗留标记 150）
            float num = (float)Main.rand.Next(90, 111) * 0.01f;   // 光晕亮度基准：0.9~1.10 之间随机
            num *= Main.essScale;   // 再随环境光缩放，夜里变暗
            Lighting.AddLight(Projectile.Center, 0.7f * num, 0.6f * num, 0f * num);   // 沙色暖光（无蓝通道）
            // 同类召唤物之间产生排斥力，避免多只宠物叠在同一位置
            float num637 = 0.05f;
            for (int num638 = 0; num638 < 1000; num638++)
            {
                bool flag23 = Main.projectile[num638].type == ModContent.ProjectileType<DrewsSandyWaifu>();
                if (num638 != Projectile.whoAmI && Main.projectile[num638].active && Main.projectile[num638].owner == Projectile.owner && flag23 && Math.Abs(Projectile.position.X - Main.projectile[num638].position.X) + Math.Abs(Projectile.position.Y - Main.projectile[num638].position.Y) < (float)Projectile.width)
                {
                    if (Projectile.position.X < Main.projectile[num638].position.X)
                    {
                        Projectile.velocity.X = Projectile.velocity.X - num637;
                    }
                    else
                    {
                        Projectile.velocity.X = Projectile.velocity.X + num637;
                    }
                    if (Projectile.position.Y < Main.projectile[num638].position.Y)
                    {
                        Projectile.velocity.Y = Projectile.velocity.Y - num637;
                    }
                    else
                    {
                        Projectile.velocity.Y = Projectile.velocity.Y + num637;
                    }
                }
            }
            if (Vector2.Distance(player.Center, Projectile.Center) > 400f)
            {
                Projectile.ai[0] = 1f;   // 离主人超出 400：进入召回状态
                Projectile.tileCollide = false;
                Projectile.netUpdate = true;
            }
            // 跟随主人
            bool flag26 = false;   // 是否处于召回状态
            if (!flag26)
            {
                flag26 = (Projectile.ai[0] == 1f);
            }
            float num650 = 7f;   // 跟随速度：巡游 7（宠物比召唤物跟得更紧、更快）
            if (flag26)
            {
                num650 = 18f;   // 召回时加速到 18
            }
            Vector2 center2 = Projectile.Center;
            Vector2 vector48 = player.Center - center2 + new Vector2(-250f, -60f);   // 悬停点：主人左侧 250、上方 60
            float num651 = vector48.Length();
            if (num651 > 200f && num650 < 10f) //200 and 8
            {
                num650 = 10f;   // 离悬停点过远时把巡游速度下限提到 10
            }
            if (num651 < num636 && flag26 && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
            {
                Projectile.ai[0] = 0f;   // 已回到主人附近且无遮挡：结束召回，恢复巡游
                Projectile.netUpdate = true;
            }
            if (num651 > 2000f)
            {
                // 极端距离（>2000）直接传送到主人身上，防止永久丢失
                Projectile.position.X = Main.player[Projectile.owner].Center.X - (float)(Projectile.width / 2);
                Projectile.position.Y = Main.player[Projectile.owner].Center.Y - (float)(Projectile.height / 2);
                Projectile.netUpdate = true;
            }
            if (num651 > 70f)
            {
                vector48.Normalize();
                vector48 *= num650;
                Projectile.velocity = (Projectile.velocity * 40f + vector48) / 41f;   // 与旧速度 40:1 加权，平滑飞行
            }
            else if (Projectile.velocity.X == 0f && Projectile.velocity.Y == 0f)
            {
                // 完全静止时给一点微速，免得动画与朝向卡死
                Projectile.velocity.X = -0.22f;
                Projectile.velocity.Y = -0.12f;
            }
            // ai[1]：回血冷却计时器，>0 时每帧递增 1~3，超过 220 归零（约每 220 帧尝试回血一次）
            if (Projectile.ai[1] > 0f)
            {
                Projectile.ai[1] += (float)Main.rand.Next(1, 4);
            }
            if (Projectile.ai[1] > 220f)
            {
                Projectile.ai[1] = 0f;
                Projectile.netUpdate = true;
            }
            // localAI[0]：启动延迟计时，熬到 120 帧（约 2 秒）后才允许开始回血
            if (Projectile.localAI[0] < 120f)
            {
                Projectile.localAI[0] += 1f;
            }
            if (Projectile.ai[0] == 0f)
            {
                // 生成治疗法珠：巡游态 + 冷却归零 + 已熬过启动延迟（localAI[0]）三者齐备才生成；
                // 且只在主人端、且主人不是满血时才真正动手
                int num658 = ModContent.ProjectileType<CactusHealOrb>();
                if (Projectile.ai[1] == 0f && Projectile.localAI[0] >= 120f)
                {
                    Projectile.ai[1] += 1f;
                    if (Main.myPlayer == Projectile.owner && Main.player[Projectile.owner].statLife < Main.player[Projectile.owner].statLifeMax2)
                    {
                        SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
                        int num226 = 36;   // 生成瞬间朝四周喷一圈 36 颗绿色治疗尘
                        for (int num227 = 0; num227 < num226; num227++)
                        {
                            // SafeNormalize：速度为 0 时回退到 UnitY，避免 Normalize 产生 NaN 粉尘
                            Vector2 vector6 = Projectile.velocity.SafeNormalize(Vector2.UnitY) * new Vector2((float)Projectile.width / 2f, (float)Projectile.height) * 0.75f;
                            vector6 = vector6.RotatedBy((double)((float)(num227 - (num226 / 2 - 1)) * 6.28318548f / (float)num226), default(Vector2)) + Projectile.Center;
                            Vector2 vector7 = vector6 - Projectile.Center;
                            int num228 = Dust.NewDust(vector6 + vector7, 0, 0, DustID.Terra, vector7.X * 1.5f, vector7.Y * 1.5f, 100, new Color(0, 200, 0), 1f);
                            Main.dust[num228].noGravity = true;
                            Main.dust[num228].noLight = true;
                            Main.dust[num228].velocity = vector7;
                        }
                        // 法珠初速 (0,-6) 向上飘，传入伤害为 0（回血量由 CactusHealOrb 自身决定）
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y, 0f, -6f, num658, 0, 0f, Main.myPlayer, 0f, 0f);
                    }
                }
            }
        }
    }
}
