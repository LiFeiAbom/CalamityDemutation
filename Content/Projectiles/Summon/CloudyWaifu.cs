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
    /// 云元素（CloudyWaifu，移植自灾厄经典版 1.4.2.101 的 Projectiles/Summon/CloudyWaifu）- 由风暴之眼（EyeoftheStorm）召唤的常驻召唤物，
    /// 追踪敌人并在贴近时发起冲刺撞击。
    /// <para>
    /// 状态机由 <c>ai[0]</c> 区分：0 = 巡游/攻击，1 = 召回（离主人过远），
    /// 2 = 冲刺（撞向目标，<c>extraUpdates</c> 临时抬到 2 且不主动改变朝向，30 帧后自动复位）；
    /// <c>ai[1]</c> 既是攻击冷却计时器、也是冲刺的剩余帧数。存续靠 <c>modPlayer.cloudWaifu</c> 标志续命。
    /// </para>
    /// </summary>
    internal class CloudyWaifu:ModProjectile
    {
        // ── 实例字段 ──
        /// <summary>出生粒子爆发计数器：前 4 帧（3→&lt;0）一次性喷出大团云尘作登场特效</summary>
        public int dust = 3;
        // ── 生命周期方法 ──
        /// <summary>
        /// 注册 8 帧动画（全程循环），并标记为可右键锁定目标（MinionTargettingFeature）、可牺牲的召唤物。
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 8;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
        }
        /// <summary>
        /// 基础属性：58x116 碰撞箱、无召唤栏消耗（<c>minionSlots = 0</c>）、无限穿透、不撞地形。
        /// 命中冷却以 20 帧打底，随 Boss 进度每解锁一项递减（石巨人 -5、月总 -5、噬神者 -4、犽戎 -3，
        /// 全解锁后为 3 帧）；该值是逐敌人生效的独立冷却（<c>usesLocalNPCImmunity</c>）。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 58;
            Projectile.height = 116;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.minionSlots = 0f;
            Projectile.timeLeft = 18000;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft *= 5;   // 超长兜底寿命，实际靠召唤标志压到 2 帧续命
            Projectile.minion = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20 -
                (NPC.downedGolemBoss ? 5 : 0) -
                (NPC.downedMoonlord ? 5 : 0) -
                (BossSystem.DevourerOfGods ? 4 : 0) -
                (BossSystem.Yharon ? 3 : 0);
        }
        /// <summary>
        /// 召唤物 AI：校验饰品标志（失效即消失）、续命、喷吐登场粒子、冲刺状态、同类分离、
        /// 追踪敌人并发起冲撞，或跟随主人。
        /// </summary>
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            // 未装备风暴之眼且未开启"全体老婆"时直接消失，避免残留召唤物
            if (!modPlayer.eyeoftheStorm && !modPlayer.allWaifus)
            {
                Projectile.active = false;
                return;
            }
            if (player.dead)
            {
                modPlayer.cloudWaifu = false;   // 玩家死亡时清空召唤标志，复活后由饰品重新召唤
            }
            // 召唤标志有效时持续刷新存活时间，实现常驻跟随
            if (modPlayer.cloudWaifu)
            {
                Projectile.timeLeft = 2;
            }
            dust--;   // 计数器递减，<0 后不再喷发
            if (dust >= 0)
            {
                int num501 = 50;   // 出生时一次性喷出 50 颗云尘
                for (int num502 = 0; num502 < num501; num502++)
                {
                    int num503 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y + 16f), Projectile.width, Projectile.height - 16, DustID.Cloud, 0f, 0f, 0, default(Color), 1f);
                    Main.dust[num503].velocity *= 2f;
                    Main.dust[num503].scale *= 1.15f;
                }
            }
            if ((double)Math.Abs(Projectile.velocity.X) > 0.2)
            {
                Projectile.spriteDirection = -Projectile.direction;   // 水平移动较快时让贴图朝向运动方向
            }
            float num633 = 500f;   // 500：目标搜索半径（云朵老婆比沙之老婆收得更近；原版遗留标记 700）
            float num634 = 800f;   // 无目标时允许离主人的最大距离，超出即召回
            float num635 = 1200f;   // 有目标时允许离主人的最大距离，允许追出更远
            float num636 = 400f;   // 召回态判定"已回到主人身边"的距离（原版遗留标记 150）
            float num = (float)Main.rand.Next(90, 111) * 0.01f;   // 光晕亮度基准：0.9~1.10 之间随机
            num *= Main.essScale;   // 再随环境光缩放，夜里变暗
            Lighting.AddLight(Projectile.Center, 0.25f * num, 0.55f * num, 0.75f * num);   // 天蓝色冷光
            // 同类召唤物之间产生排斥力，避免多只老婆叠在同一位置
            float num637 = 0.05f;
            for (int num638 = 0; num638 < 1000; num638++)
            {
                bool flag23 = (Main.projectile[num638].type == ModContent.ProjectileType<Projectiles.Summon.CloudyWaifu>());
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
            bool flag24 = false;   // 冲刺期间置位，用于提前 return 跳过常规索敌/移动
            // ai[0]=2：冲刺状态——临时把更新频率抬到 2 倍（等效半速帧），动画放慢到每 49 帧一帧，
            // 30 帧后复位回巡游（ai[1] 归 1，让它接着当攻击冷却用）
            if (Projectile.ai[0] == 2f)
            {
                Projectile.ai[1] += 1f;
                Projectile.extraUpdates = 2;
                Projectile.frameCounter++;
                if (Projectile.frameCounter > 48)
                {
                    Projectile.frame++;
                    Projectile.frameCounter = 0;
                }
                if (Projectile.frame > 7)
                {
                    Projectile.frame = 0;
                }
                if (Projectile.ai[1] > 30f)
                {
                    Projectile.ai[1] = 1f;
                    Projectile.ai[0] = 0f;
                    Projectile.extraUpdates = 0;
                    Projectile.numUpdates = 0;
                    Projectile.netUpdate = true;
                }
                else
                {
                    flag24 = true;
                }
            }
            if (flag24)
            {
                return;
            }
            Vector2 vector46 = Projectile.position;   // 候选目标位置（先填自身位置，供"是否更近"的比较）
            bool flag25 = false;                      // 是否找到有效目标
            // 目标选择：优先玩家用召唤武器右键标记的目标（此分支同样要求视线通畅）
            if (player.HasMinionAttackTargetNPC)
            {
                NPC npc = Main.npc[player.MinionAttackTargetNPC];
                if (npc.CanBeChasedBy(Projectile, false))
                {
                    float num646 = Vector2.Distance(npc.Center, Projectile.Center);
                    if (((Vector2.Distance(Projectile.Center, vector46) > num646 && num646 < num633) || !flag25) && Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, npc.position, npc.width, npc.height))
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
                num647 = num635;   // 有目标时放宽到 1200
            }
            if (Vector2.Distance(player.Center, Projectile.Center) > num647)
            {
                Projectile.ai[0] = 1f;   // 离主人超出上限：进入召回状态
                Projectile.netUpdate = true;
            }
            // 巡游态且有目标：距目标超过 100 像素全速接近，否则反向退开（贴脸准备冲刺）
            if (flag25 && Projectile.ai[0] == 0f)
            {
                Vector2 vector47 = vector46 - Projectile.Center;
                float num648 = vector47.Length();
                vector47.Normalize();
                if (num648 > 100f) //200
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
                float num650 = 6f;   // 跟随速度：巡游 6
                if (flag26)
                {
                    num650 = 15f;   // 召回时加速到 15
                }
                Vector2 center2 = Projectile.Center;
                Vector2 vector48 = player.Center - center2 + new Vector2(500f, -60f);   // 悬停点：主人右侧 500、上方 60
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
                    Projectile.velocity.X = -0.15f;
                    Projectile.velocity.Y = -0.05f;
                }
            }
            // 常规动画：8 帧循环（每 17 帧推进一帧）
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 16)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 7)
            {
                Projectile.frame = 0;
            }
            // ai[1]：攻击冷却计时器，>0 时每帧递增 1~3，超过 40 归零（约每 40 帧可再冲刺一次）
            if (Projectile.ai[1] > 0f)
            {
                Projectile.ai[1] += (float)Main.rand.Next(1, 4);
            }
            if (Projectile.ai[1] > 40f)
            {
                Projectile.ai[1] = 0f;
                Projectile.netUpdate = true;
            }
            if (Projectile.ai[0] == 0f)
            {
                // 目标足够近（num633 已被索敌分支改写成实际距离，<500 即射程内）且攻击计时归零时，
                // 进入冲刺状态（ai[0]=2）以 8 的速度直冲目标；只有主人端敢改状态，其余端等同步
                if (Projectile.ai[1] == 0f && flag25 && num633 < 500f)
                {
                    Projectile.ai[1] += 1f;
                    if (Main.myPlayer == Projectile.owner)
                    {
                        Projectile.ai[0] = 2f;
                        Vector2 value20 = vector46 - Projectile.Center;
                        value20.Normalize();
                        Projectile.velocity = value20 * 8f;   // 冲刺初速 8
                        Projectile.netUpdate = true;
                        return;
                    }
                }
            }
        }
    }
}
