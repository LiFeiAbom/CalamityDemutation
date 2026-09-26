using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 沙元素（SandyWaifu，移植自灾厄经典版 1.4.2.101 的 Projectiles/Summon/SandyWaifu）- 由瓶装沙元素（WifeinaBottle）召唤的常驻召唤物，
    /// 追踪敌人并周期性发射沙之矢（<see cref="SandBolt"/>）。
    /// <para>
    /// 状态机由 <c>ai[0]</c> 区分：0 = 巡游/攻击（追敌或跟随主人），1 = 召回（离主人过远，飞回后复位）；
    /// <c>ai[1]</c> 是攻击冷却计时器。存续不靠 <c>timeLeft</c>，而是每帧由
    /// <c>modPlayer.sandyWaifu</c> 标志把 <c>timeLeft</c> 压成 2 来"续命"（见 <see cref="AI"/>）。
    /// </para>
    /// </summary>
    internal class SandyWaifu:ModProjectile
    {
        // ── 实例字段 ──
        /// <summary>出生粒子爆发计数器：前 4 帧（3→&lt;0）一次性喷出大团沙尘作登场特效</summary>
        public int dust = 3;
        // ── 生命周期方法 ──
        /// <summary>
        /// 注册 12 帧动画（0~5 空闲、6~11 攻击），并标记为可右键锁定目标（MinionTargettingFeature）、
        /// 可牺牲的召唤物。
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 12;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
        }
        /// <summary>
        /// 基础属性：42x98 碰撞箱、无召唤栏消耗（<c>minionSlots = 0</c>，靠饰品效果常驻）、无限穿透、不撞地形。
        /// 命中冷却以 20 帧打底，随 Boss 进度每解锁一项递减（石巨人 -5、月总 -5、噬神者 -4、犽戎 -3，
        /// 全解锁后为 3 帧）；该值是逐敌人生效的独立冷却（<c>usesLocalNPCImmunity</c>）。
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
            Projectile.timeLeft *= 5;   // 18000*5 帧的超长兜底寿命，实际靠召唤标志压到 2 帧续命
            Projectile.minion = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20 -
                (NPC.downedGolemBoss ? 5 : 0) -
                (NPC.downedMoonlord ? 5 : 0) -
                (BossSystem.DevourerOfGods ? 4 : 0) -
                (BossSystem.Yharon ? 3 : 0);
        }
        /// <summary>
        /// 召唤物 AI：校验饰品标志（失效即消失）、续命、喷吐登场粒子、同类相互排斥、空闲/攻击动画切换、
        /// 追踪敌人或跟随主人，并在冷却归零时发射沙之矢。
        /// </summary>
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            // 未装备瓶中老婆且未开启"全体老婆"时直接消失
            if (!modPlayer.wifeinaBottle && !modPlayer.allWaifus)
            {
                Projectile.active = false;
                return;
            }
            if (player.dead)
            {
                modPlayer.sandyWaifu = false;   // 玩家死亡时清空召唤标志，复活后由饰品重新召唤
            }
            // 召唤标志有效时持续刷新存活时间，实现常驻跟随
            if (modPlayer.sandyWaifu)
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
            if ((double)Math.Abs(Projectile.velocity.X) > 0.2)
            {
                Projectile.spriteDirection = -Projectile.direction;   // 水平移动较快时让贴图朝向运动方向
            }
            float num633 = 700f;   // 700：目标搜索半径（只锁定最近的敌人；源码遗留标记 //700，值未改）
            float num634 = 800f;   // 无目标时允许离主人的最大距离，超出即召回
            float num635 = 1200f;   // 有目标时允许离主人的最大距离，允许追出更远
            float num636 = 200f;   // 召回态判定"已回到主人身边"的距离（源码遗留标记 //150，经典版实际值即 200）
            float num = (float)Main.rand.Next(90, 111) * 0.01f;   // 光晕亮度基准：0.9~1.10 之间随机
            num *= Main.essScale;   // 再随环境光缩放，夜里变暗
            Lighting.AddLight(Projectile.Center, 0.7f * num, 0.6f * num, 0f * num);   // 沙色暖光（无蓝通道）
            // 同类召唤物之间产生排斥力，避免多只老婆叠在同一位置
            float num637 = 0.05f;
            for (int num638 = 0; num638 < 1000; num638++)
            {
                bool flag23 = (Main.projectile[num638].type == ModContent.ProjectileType<SandyWaifu>());
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
            Vector2 vector46 = Projectile.position;   // 候选目标位置（先填自身位置，供"是否更近"的比较）
            bool flag25 = false;                      // 是否找到有效目标
            if (Projectile.ai[0] != 1f)
            {
                Projectile.tileCollide = false;   // 非召回状态关闭地形碰撞（本弹幕全程不撞地形，属遗留判断）
            }
            if (Projectile.tileCollide && WorldGen.SolidTile(Framing.GetTileSafely((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16)))
            {
                Projectile.tileCollide = false;   // 中心陷入实体方块时也关闭碰撞，避免贴墙卡死
            }
            // 目标选择：优先玩家用召唤武器右键标记的目标（此分支不要求视线通畅）
            if (player.HasMinionAttackTargetNPC)
            {
                NPC npc = Main.npc[player.MinionAttackTargetNPC];
                if (npc.CanBeChasedBy(Projectile, false))
                {
                    float num646 = Vector2.Distance(npc.Center, Projectile.Center);
                    if ((Vector2.Distance(Projectile.Center, vector46) > num646 && num646 < num633) || !flag25)
                    {
                        num633 = num646;
                        vector46 = npc.Center;
                        flag25 = true;
                    }
                }
            }
            else
            {
                // 否则遍历 200 个 NPC，取最近且视线可达的敌人
                for (int num645 = 0; num645 < 200; num645++)
                {
                    NPC nPC2 = Main.npc[num645];
                    if (nPC2.CanBeChasedBy(Projectile, false))
                    {
                        float num646 = Vector2.Distance(nPC2.Center, Projectile.Center);
                        if (((Vector2.Distance(Projectile.Center, vector46) > num646 && num646 < num633) || !flag25) && Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, nPC2.position, nPC2.width, nPC2.height))
                        {
                            num633 = num646;
                            vector46 = nPC2.Center;
                            flag25 = true;
                        }
                    }
                }
            }
            float num647 = num634;   // 无目标时的"离主人距离上限"（800），超出即召回
            if (flag25)
            {
                // 有目标：切到 6~11 的攻击动画段（每 17 帧推进一帧）并在段内循环
                if (Projectile.frame < 6)
                {
                    Projectile.frame = 6;
                }
                Projectile.frameCounter++;
                if (Projectile.frameCounter > 16)
                {
                    Projectile.frame++;
                    Projectile.frameCounter = 0;
                }
                if (Projectile.frame > 11)
                {
                    Projectile.frame = 6;
                }
                num647 = num635;   // 有目标时放宽到 1200
            }
            else
            {
                // 无目标：0~5 的空闲动画循环
                Projectile.frameCounter++;
                if (Projectile.frameCounter > 16)
                {
                    Projectile.frame++;
                    Projectile.frameCounter = 0;
                }
                if (Projectile.frame > 5)
                {
                    Projectile.frame = 0;
                }
            }
            if (Vector2.Distance(player.Center, Projectile.Center) > num647)
            {
                Projectile.ai[0] = 1f;   // 离主人超出上限：进入召回状态，便于穿墙飞回
                Projectile.tileCollide = false;
                Projectile.netUpdate = true;
            }
            // 巡游态且有目标：距目标超过 200 像素全速接近，否则反向退开，维持约 200 像素的攻守距离
            if (flag25 && Projectile.ai[0] == 0f)
            {
                Vector2 vector47 = vector46 - Projectile.Center;
                float num648 = vector47.Length();
                vector47.Normalize();
                if (num648 > 200f)
                {
                    float scaleFactor2 = 8f;   // 追击速度 8
                    vector47 *= scaleFactor2;
                    Projectile.velocity = (Projectile.velocity * 40f + vector47) / 41f;
                }
                else
                {
                    float num649 = 4f;   // 过近时以 4 的速度反向退开
                    vector47 *= -num649;
                    Projectile.velocity = (Projectile.velocity * 40f + vector47) / 41f;
                }
            }
            else
            {
                // 跟随主人（无目标，或已在召回态）
                bool flag26 = false;   // 是否处于召回状态
                if (!flag26)
                {
                    flag26 = (Projectile.ai[0] == 1f);
                }
                float num650 = 6f;   // 跟随速度：巡游 6（原版遗留标记亦为 6）
                if (flag26)
                {
                    num650 = 15f;   // 召回时加速到 15（源码遗留标记 //16，实际值 15）
                }
                Vector2 center2 = Projectile.Center;
                Vector2 vector48 = player.Center - center2 + new Vector2(250f, -60f);   // 悬停点：主人右侧 250、上方 60
                float num651 = vector48.Length();
                if (num651 > 200f && num650 < 8f) //200 and 8
                {
                    num650 = 8f;   // 离悬停点过远时把巡游速度下限提到 8
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
                    Projectile.velocity.X = -0.195f;
                    Projectile.velocity.Y = -0.095f;
                }
            }
            // ai[1]：攻击冷却计时器，>0 时每帧递增 1~3，超过 220 归零（约每 220 帧可再开火一次）
            if (Projectile.ai[1] > 0f)
            {
                Projectile.ai[1] += (float)Main.rand.Next(1, 4);
            }
            if (Projectile.ai[1] > 220f)
            {
                Projectile.ai[1] = 0f;
                Projectile.netUpdate = true;
            }
            if (Projectile.ai[0] == 0f)
            {
                // 锁定目标且攻击计时归零时，朝目标方向发射沙之矢
                float scaleFactor3 = 11f;   // 沙之矢初速
                int num658 = ModContent.ProjectileType<SandBolt>();
                if (flag25 && Projectile.ai[1] == 0f)
                {
                    Projectile.ai[1] += 1f;
                    if (Main.myPlayer == Projectile.owner && Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, vector46, 0, 0))
                    {
                        Vector2 value19 = vector46 - Projectile.Center;
                        value19.Normalize();
                        value19 *= scaleFactor3;
                        int num659 = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y, value19.X, value19.Y, num658, Projectile.damage, 0f, Main.myPlayer, 0f, 0f);
                        Main.projectile[num659].timeLeft = 300;   // 沙之矢寿命限制 300 帧
                        Projectile.netUpdate = true;
                    }
                }
            }
        }
    }
}
