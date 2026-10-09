using System;
using Microsoft.Xna.Framework;
using Terraria;
namespace CalamityDemutation.Utilities
{
    /// <summary>
    /// 通用工具类（召唤/仆从部分）：移植灾厄给召唤武器用的几个共用方法，供本工程的仆从复用。
    /// </summary>
    internal static partial class CDUtil
    {
        /// <summary>
        /// 预判发射速度：按"飞到目标要多久"迭代几次，估算目标的未来位置再取方向
        ///（照灾厄 <c>ProjectileUtils.CalculatePredictiveAimToTarget</c> 逐行移植）。
        /// </summary>
        public static Vector2 CalculatePredictiveAimToTarget(Vector2 startingPosition, Vector2 targetPosition, Vector2 targetVelocity, float shootSpeed, int iterations = 4)
        {
            float previousTimeToReachDestination = 0f;
            Vector2 currentTargetPosition = targetPosition;
            for (int i = 0; i < iterations; i++)
            {
                float timeToReachDestination = Vector2.Distance(startingPosition, currentTargetPosition) / shootSpeed;
                currentTargetPosition += targetVelocity * (timeToReachDestination - previousTimeToReachDestination);
                previousTimeToReachDestination = timeToReachDestination;
            }
            return (currentTargetPosition - startingPosition).SafeNormalize(Vector2.UnitY) * shootSpeed;
        }
        /// <summary>同上，目标换成任意实体（照灾厄的同名重载）</summary>
        public static Vector2 CalculatePredictiveAimToTarget(Vector2 startingPosition, Entity target, float shootSpeed, int iterations = 4)
            => CalculatePredictiveAimToTarget(startingPosition, target.Center, target.velocity, shootSpeed, iterations);

        /// <summary>
        /// 超级追踪（照灾厄 <c>ProjectileUtils.SuperhomeTowardsTarget</c> 逐行移植）：
        /// 先按预判瞄准求出"理想速度"，再按惯性插值把当前速度推向它。
        /// </summary>
        /// <param name="homingSpeed">追踪速度</param>
        /// <param name="inertia">转向惯性（越大转得越平缓）</param>
        /// <param name="predictionStrength">预判强度：1 为正常提前量，0.01 相当于不预判（源里下限也是 0.01）</param>
        public static Vector2 SuperhomeTowardsTarget(this Projectile projectile, NPC target, float homingSpeed, float inertia, float predictionStrength = 1f)
        {
            if (predictionStrength < 0.01f)
            {
                predictionStrength = 0.01f;
            }
            Vector2 idealVelocity = CalculatePredictiveAimToTarget(projectile.Center, target, homingSpeed / predictionStrength) * predictionStrength;
            return (projectile.velocity * (inertia - 1f) + idealVelocity) / inertia;
        }

        /// <summary>
        /// 召唤物索敌：优先取玩家用召唤武器右键标记的目标（可选是否要求视线、是否检查射程），
        /// 否则退回"范围内最近的敌人"（照灾厄 <c>NPCUtils.MinionHoming</c> 移植；
        /// 灾厄的下层 <c>ClosestNPCAt</c> 在本工程里对应既有的 <see cref="FindClosestNPC"/>）。
        /// </summary>
        public static NPC MinionHoming(this Vector2 origin, float maxDistanceToCheck, Player owner, bool ignoreTiles = true, bool checksRange = false)
        {
            // 玩家或"标记目标"索引非法时直接走最近敌人
            if (owner is null || owner.whoAmI < 0 || owner.whoAmI >= Main.maxPlayers ||
                owner.MinionAttackTargetNPC < 0 || owner.MinionAttackTargetNPC >= Main.maxNPCs)
            {
                return origin.FindClosestNPC(maxDistanceToCheck, ignoreTiles);
            }
            NPC npc = Main.npc[owner.MinionAttackTargetNPC];
            bool canHit = true;
            if (!ignoreTiles)
            {
                canHit = Collision.CanHit(origin, 1, 1, npc.Center, 1, 1);
            }
            // 大体积目标（利维坦那种）按半宽半高放宽射程
            float extraDistance = (npc.width / 2) + (npc.height / 2);
            bool distCheck = Vector2.Distance(origin, npc.Center) < (maxDistanceToCheck + extraDistance) || !checksRange;
            if (owner.HasMinionAttackTargetNPC && canHit && distCheck)
            {
                return npc;
            }
            return origin.FindClosestNPC(maxDistanceToCheck, ignoreTiles);
        }

        /// <summary>
        /// 冲锋型仆从的通用状态机（照灾厄 <c>GenericAIUtils.ChargingMinionAI</c> 移植）：
        /// 找目标 → 离主人过远就强制回主人 → 贴近到 500 内就冲锋 → 冲锋后强制进入冷却。
        /// <para>
        /// 参数照源顺序：<paramref name="range"/> 索敌半径；
        /// <paramref name="maxPlayerDist"/> 无目标时离主人超过它就被迫回程；
        /// <paramref name="extraMaxPlayerDist"/> 有目标时放宽到的距离；
        /// <paramref name="safeDist"/> 回到这么近就算归位；
        /// <paramref name="initialUpdates"/> 常态 <c>extraUpdates</c>；
        /// <paramref name="chargeDelayTime"/> 冲完之后的冷却帧数；
        /// <paramref name="goToSpeed"/> / <paramref name="goBackSpeed"/> 逼近 / 拉开目标的速度；
        /// <paramref name="returnOffset"/> 待机点相对主人的偏移；
        /// <paramref name="chargeCounterMax"/> 冲锋计数阈值；
        /// <paramref name="chargeSpeed"/> 冲锋速度；
        /// <paramref name="tileVision"/> 索敌是否要求视线；
        /// <paramref name="ignoreTilesWhenCharging"/> 冲锋时是否无视地形；
        /// <paramref name="updateDifference"/> 冷却期额外加的 <c>extraUpdates</c>。
        /// </para>
        /// </summary>
        /// <remarks>
        /// 状态写在 <c>ai[0]</c>（0 正常 / 1 回主人 / 2 冲完冷却）与 <c>ai[1]</c>（计时）里。
        /// 源里蝴蝶法杖专属的 <c>isButterfly</c> 与猪鲨 <c>fishronCheck</c> 特判与本件无关，已删；
        /// 源里那句取出来却没被用到的 <c>CalamityPlayer</c> 也按工程口径去掉。
        /// </remarks>
        public static void ChargingMinionAI(this Projectile projectile, float range, float maxPlayerDist, float extraMaxPlayerDist, float safeDist, int initialUpdates, float chargeDelayTime, float goToSpeed, float goBackSpeed, Vector2 returnOffset, float chargeCounterMax, float chargeSpeed, bool tileVision, bool ignoreTilesWhenCharging, int updateDifference = 1)
        {
            Player player = Main.player[projectile.owner];

            // 防扎堆：先把同类同伴推开，免得几只叠在一个点上（源第一步就是这句）
            projectile.MinionAntiClump();

            // 冲完后的喘息：这段时间只数帧、不找目标，到点了才恢复正常状态
            bool chargeDelay = false;
            if (projectile.ai[0] == 2f)
            {
                projectile.ai[1] += 1f;
                projectile.extraUpdates = initialUpdates + updateDifference;
                if (projectile.ai[1] > chargeDelayTime)
                {
                    projectile.ai[1] = 1f;
                    projectile.ai[0] = 0f;
                    projectile.extraUpdates = initialUpdates;
                    projectile.numUpdates = 0;
                    projectile.netUpdate = true;
                }
                else
                {
                    chargeDelay = true;
                }
            }
            if (chargeDelay)
            {
                return;
            }

            // 找目标
            float maxDist = range;
            Vector2 targetVec = projectile.position;
            bool foundTarget = false;
            // 优先打玩家用召唤武器右键标记的那个敌人
            if (player.HasMinionAttackTargetNPC)
            {
                NPC npc = Main.npc[player.MinionAttackTargetNPC];
                if (npc.CanBeChasedBy(projectile, false))
                {
                    // 大体型目标（利维坦那种）按半宽半高放宽射程
                    float extraDist = (npc.width / 2) + (npc.height / 2);

                    float targetDist = Vector2.Distance(npc.Center, projectile.Center);
                    // 某些仆从选目标时必须看得见（源由 tileVision 控制），另一些无视地形
                    bool canHit = true;
                    if (extraDist < maxDist && !tileVision)
                        canHit = Collision.CanHit(projectile.Center, 1, 1, npc.Center, 1, 1);
                    if (!foundTarget && targetDist < (maxDist + extraDist) && canHit)
                    {
                        maxDist = targetDist;
                        targetVec = npc.Center;
                        foundTarget = true;
                    }
                }
            }
            // 没有指定目标（或指定的那个已经不合法）时，扫全场找最近的
            if (!foundTarget)
            {
                for (int npcIndex = 0; npcIndex < Main.maxNPCs; npcIndex++)
                {
                    NPC npc = Main.npc[npcIndex];
                    if (npc.CanBeChasedBy(projectile, false))
                    {
                        float extraDist = (npc.width / 2) + (npc.height / 2);
                        float targetDist = Vector2.Distance(npc.Center, projectile.Center);
                        bool canHit = true;
                        if (extraDist < maxDist && !tileVision)
                            canHit = Collision.CanHit(projectile.Center, 1, 1, npc.Center, 1, 1);
                        if (!foundTarget && targetDist < (maxDist + extraDist) && canHit)
                        {
                            maxDist = targetDist;
                            targetVec = npc.Center;
                            foundTarget = true;
                        }
                    }
                }
            }

            // 离主人太远就强制回程；正在打怪时按放宽后的距离算
            float distBeforeForcedReturn = maxPlayerDist;
            if (foundTarget)
            {
                distBeforeForcedReturn = extraMaxPlayerDist;
            }
            if (Vector2.Distance(player.Center, projectile.Center) > distBeforeForcedReturn)
            {
                projectile.ai[0] = 1f;
                projectile.netUpdate = true;
            }

            // 有目标且不在回程/冷却：贴上去
            if (foundTarget && projectile.ai[0] == 0f)
            {
                // 有些仆从冲锋时会撞地形，有些不会（源由此参数决定）
                projectile.tileCollide = !ignoreTilesWhenCharging;
                Vector2 targetSpot = targetVec - projectile.Center;
                float targetDist = targetSpot.Length();
                targetSpot.Normalize();
                // 想把它稳在离目标 200 像素左右的"舒服位"，不过它本来也会冲过去
                if (targetDist > 200f)
                {
                    float speed = goToSpeed;
                    targetSpot *= speed;
                    projectile.velocity = (projectile.velocity * 40f + targetSpot) / 41f;
                }
                else
                {
                    float speed = -goBackSpeed;
                    targetSpot *= speed;
                    projectile.velocity = (projectile.velocity * 40f + targetSpot) / 41f;
                }
            }

            // 待机 / 回主人（没有目标，或正处在回程状态）
            else
            {
                // 无视地形，免得像双筒望远镜那类仆从一样到处卡住
                projectile.tileCollide = false;

                bool returningToPlayer = false;
                if (!returningToPlayer)
                {
                    returningToPlayer = projectile.ai[0] == 1f;
                }

                Vector2 playerVec = player.Center - projectile.Center + returnOffset;
                float playerDist = playerVec.Length();

                // 回程时提速；离得有点远时也稍微快一点
                float playerHomeSpeed = 6f;
                if (returningToPlayer)
                {
                    playerHomeSpeed = 15f;
                }
                if (playerDist > 200f && playerHomeSpeed < 8f)
                {
                    playerHomeSpeed = 8f;
                }
                // 回到安全距离且不在实心块里，就算归位、退出回程状态
                if (playerDist < safeDist && returningToPlayer && !Collision.SolidCollision(projectile.position, projectile.width, projectile.height))
                {
                    projectile.ai[0] = 0f;
                    projectile.netUpdate = true;
                }
                // 远得离谱就直接瞬移到主人身上
                if (playerDist > 2000f)
                {
                    projectile.position.X = player.Center.X - (float)(projectile.width / 2);
                    projectile.position.Y = player.Center.Y - (float)(projectile.height / 2);
                    projectile.netUpdate = true;
                }
                // 离主人超过 70 像素就往回飘
                if (playerDist > 70f)
                {
                    playerVec.Normalize();
                    playerVec *= playerHomeSpeed;
                    projectile.velocity = (projectile.velocity * 40f + playerVec) / 41f;
                }
                // 仆从不能停住不动
                else if (projectile.velocity.X == 0f && projectile.velocity.Y == 0f)
                {
                    projectile.velocity.X = -0.15f;
                    projectile.velocity.Y = -0.05f;
                }
            }

            // 冲锋计时：随帧随机累加，攒够 chargeCounterMax 就清零重来
            if (projectile.ai[1] > 0f)
            {
                projectile.ai[1] += (float)Main.rand.Next(1, 4);
            }
            if (projectile.ai[1] > chargeCounterMax)
            {
                projectile.ai[1] = 0f;
                projectile.netUpdate = true;
            }

            // 不在冷却时，贴到 500 以内就发起冲锋（只在主人端决定，靠 netUpdate 同步）
            if (projectile.ai[0] == 0f)
            {
                if (projectile.ai[1] == 0f && foundTarget && maxDist < 500f)
                {
                    projectile.ai[1] += 1f;
                    if (Main.myPlayer == projectile.owner)
                    {
                        projectile.ai[0] = 2f;
                        Vector2 targetPos = targetVec - projectile.Center;
                        targetPos.Normalize();
                        projectile.velocity = targetPos * chargeSpeed;
                        projectile.netUpdate = true;
                    }
                }
            }
        }

        /// <summary>
        /// 同类仆从互相推开（照灾厄 <c>ProjectileUtils.MinionAntiClump</c> 逐行移植）：
        /// 同一主人、同一类型、曼哈顿距离小于自身宽度的另一个仆从，就朝反方向轻推一点，
        /// 免得几只叠成一坨。
        /// </summary>
        public static void MinionAntiClump(this Projectile projectile, float pushForce = 0.05f)
        {
            for (int k = 0; k < Main.maxProjectiles; k++)
            {
                Projectile otherProj = Main.projectile[k];
                // 短路判断，让这个循环尽量便宜：不同主人 / 不是仆从 / 自己，都跳过
                if (!otherProj.active || otherProj.owner != projectile.owner || !otherProj.minion || k == projectile.whoAmI)
                    continue;

                // 同主人、同类型的另一个仆从离得太近，就轻轻推开
                bool sameProjType = otherProj.type == projectile.type;
                float taxicabDist = Math.Abs(projectile.position.X - otherProj.position.X) + Math.Abs(projectile.position.Y - otherProj.position.Y);
                if (sameProjType && taxicabDist < projectile.width)
                {
                    if (projectile.position.X < otherProj.position.X)
                        projectile.velocity.X -= pushForce;
                    else
                        projectile.velocity.X += pushForce;

                    if (projectile.position.Y < otherProj.position.Y)
                        projectile.velocity.Y -= pushForce;
                    else
                        projectile.velocity.Y += pushForce;
                }
            }
        }
    }
}
