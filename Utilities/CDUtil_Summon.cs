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
    }
}
