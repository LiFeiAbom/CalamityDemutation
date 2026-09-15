using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 三只迷你神圣守卫共用的局部选敌辅助：逐式复刻灾厄 2.2.2 的 CalamityUtils.MinionHoming
    /// 及其内部调用的 ClosestNPCAt（NPCUtils.cs 约 583-707 行）。
    /// 之所以不用公共工具 ProjUtil.FindClosestNPC：后者的半径判据是 dist &lt; maxDistance + extraDistance
    /// （把敌人体型余量算进了搜索半径，等于放宽了索敌距离），而原版 ClosestNPCAt 的判据是 dist &lt; maxDistance，
    /// extraDistance 只用于决定"是否需要做地形（CanHit）遮挡检测"，不参与半径。
    /// </summary>
    internal static class MiniGuardianTargeting
    {
        /// <summary>
        /// 选择守卫要追击的敌人（复刻 CalamityUtils.MinionHoming）：
        /// 主人有右键锁定目标且该目标通过视线/距离判定时优先用它，否则退回 maxDistanceToCheck 内最近的敌人。
        /// 原版两个调用点（MiniGuardianDefense 传 1500、MiniGuardianAttack 传 3000）都用默认的
        /// ignoreTiles = true、checksRange = false，因此右键锁定目标不做视线检测、也不受距离限制。
        /// </summary>
        public static NPC MinionHoming(Vector2 origin, float maxDistanceToCheck, Player owner, bool ignoreTiles = true, bool checksRange = false)
        {
            // 主人引用非法 / 没有右键锁定目标 / 锁定索引越界：直接走就近索敌
            if (owner == null || !owner.HasMinionAttackTargetNPC || owner.MinionAttackTargetNPC < 0 || owner.MinionAttackTargetNPC >= Main.maxNPCs)
                return ClosestNPCAt(origin, maxDistanceToCheck, ignoreTiles);
            NPC npc = Main.npc[owner.MinionAttackTargetNPC];
            bool canHit = true;
            if (!ignoreTiles)
                canHit = Collision.CanHit(origin, 1, 1, npc.Center, 1, 1);
            float extraDistance = (npc.width / 2) + (npc.height / 2);
            // checksRange 为 false（原版调用点的默认值）时完全不限制距离
            bool distCheck = Vector2.Distance(origin, npc.Center) < (maxDistanceToCheck + extraDistance) || !checksRange;
            if (owner.HasMinionAttackTargetNPC && canHit && distCheck)
                return npc;
            return ClosestNPCAt(origin, maxDistanceToCheck, ignoreTiles);
        }
        /// <summary>
        /// 取 origin 周围 maxDistanceToCheck 像素内最近的、可被追击（CanBeChasedBy(null, false)）的敌人
        /// （复刻 CalamityUtils.ClosestNPCAt）。bossPriority 为 true 时优先 Boss（含血肉墙之眼）。
        /// </summary>
        public static NPC ClosestNPCAt(Vector2 origin, float maxDistanceToCheck, bool ignoreTiles = true, bool bossPriority = false)
        {
            NPC closestTarget = null;
            float distance = maxDistanceToCheck;
            if (bossPriority)
            {
                bool bossFound = false;
                for (int index = 0; index < Main.npc.Length; index++)
                {
                    // 已锁定 Boss 后，忽略所有非 Boss 目标（血肉墙之眼按 Boss 处理）
                    if (bossFound && !(Main.npc[index].boss || Main.npc[index].type == NPCID.WallofFleshEye))
                        continue;
                    if (Main.npc[index].CanBeChasedBy(null, false))
                    {
                        // extraDistance 只参与地形检测：只有"体型余量小于当前最近距离"时才值得做视线检测
                        float extraDistance = (Main.npc[index].width / 2) + (Main.npc[index].height / 2);
                        bool canHit = true;
                        if (extraDistance < distance && !ignoreTiles)
                            canHit = Collision.CanHit(origin, 1, 1, Main.npc[index].Center, 1, 1);
                        if (Vector2.Distance(origin, Main.npc[index].Center) < distance && canHit)
                        {
                            if (Main.npc[index].boss || Main.npc[index].type == NPCID.WallofFleshEye)
                                bossFound = true;
                            distance = Vector2.Distance(origin, Main.npc[index].Center);
                            closestTarget = Main.npc[index];
                        }
                    }
                }
            }
            else
            {
                for (int index = 0; index < Main.npc.Length; index++)
                {
                    if (Main.npc[index].CanBeChasedBy(null, false))
                    {
                        float extraDistance = (Main.npc[index].width / 2) + (Main.npc[index].height / 2);
                        bool canHit = true;
                        if (extraDistance < distance && !ignoreTiles)
                            canHit = Collision.CanHit(origin, 1, 1, Main.npc[index].Center, 1, 1);
                        if (Vector2.Distance(origin, Main.npc[index].Center) < distance && canHit)
                        {
                            distance = Vector2.Distance(origin, Main.npc[index].Center);
                            closestTarget = Main.npc[index];
                        }
                    }
                }
            }
            return closestTarget;
        }
    }
}
