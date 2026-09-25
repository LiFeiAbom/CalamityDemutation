using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 红魔（RedDevil） - 恶魔罩（Demonshade）头部套装奖励召唤的友方恶魔随从弹幕。
    /// 由 DemonshadeHelm 在套装生效时生成（场上同类仅保留一只），锁定 700 像素内的敌人后靠近并周期性投射弹幕 114；
    /// 本体不造成接触伤害（CanDamage 返回 false），当 modPlayer.redDevil 套装标志失效（卸下套装/死亡重置）时自行消失。
    /// 注意：与 Content/Buffs/SummonBuffs/RedDevil.cs（同名的维持用 Buff）是两个不同的类，不要混淆。
    /// </summary>
    internal class RedDevil:ModProjectile
    {
        // ── 实例字段 ──
        /// <summary>出生粒子爆发计数器：前 4 帧（3→&lt;0）一次性喷出大团 LifeDrain 粉尘作登场特效</summary>
        public int dust = 3;
        // ── 生命周期方法 ──
        /// <summary>
        /// 注册 5 帧动画，并标记为可右键锁定目标、可牺牲的召唤物。
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;      // 允许作为召唤物牺牲品
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true; // 支持玩家用召唤武器右键锁定目标
        }
        /// <summary>
        /// 基础属性：48x48 碰撞箱、需网络同步；友方、不受水阻、存活 18000*5 帧（近乎永久，靠召唤标志维持）、
        /// 无限穿透、不碰撞地形。未设置 minion/minionSlots，故不占用召唤栏（由套装直接生成）。
        /// 伤害类型为召唤：红魔是召唤物定位。注意本体 CanDamage 返回 false、不造成任何伤害，
        /// 真正打人的是它射出的三叉戟，那枚弹幕的伤害类型另行决定。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 48;             // 贴图/碰撞箱宽（像素）
            Projectile.height = 48;            // 贴图/碰撞箱高（像素）
            Projectile.netImportant = true;    // 重要弹幕，联机时始终同步
            Projectile.friendly = true;        // 友方弹幕
            Projectile.DamageType = DamageClass.Summon;  // 召唤伤害类型（红魔本体不造成伤害，此项仅表征其召唤物定位）
            Projectile.ignoreWater = true;     // 不受水流减速
            Projectile.timeLeft = 18000;       // 初始存活帧数
            Projectile.penetrate = -1;         // 无限穿透
            Projectile.tileCollide = false;    // 不碰撞地形（可穿墙跟随）
            Projectile.timeLeft *= 5;          // 再乘 5，配合常驻逻辑实现近乎永久存在
        }
        /// <summary>
        /// 召唤物 AI 状态机：ai[0]=0 巡游/攻击（有目标时贴近到约 200 像素并周期开火，无目标时悬停在玩家上方 30 像素）；
        /// ai[0]=1 召回（离玩家过远时加速飞回，回到 150 像素内且无遮挡后复位）；ai[0]=2 为遗留的快速更新状态。
        /// 无论哪种状态，都会先校验套装标志、喷吐登场粒子、推进动画并与同类召唤物保持间距。
        /// </summary>
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            // 套装标志失效（卸下恶魔罩头部/玩家重置）时直接消失，避免残留召唤物
            if (!modPlayer.redDevil)
            {
                Projectile.active = false;
                return;
            }
            if (player.dead)
            {
                modPlayer.redDevil2 = false;   // 玩家死亡时清空维持标志
            }
            // 维持标志有效（RedDevil Buff 检测到场上存在本弹幕）时每帧刷新存活时间，实现常驻跟随
            if (modPlayer.redDevil2)
            {
                Projectile.timeLeft = 2;
            }
            dust--;   // 计数器递减，<0 后不再喷发
            if (dust >= 0)
            {
                int num501 = 50;   // 出生时一次性喷出 50 颗 LifeDrain 粉尘
                for (int num502 = 0; num502 < num501; num502++)
                {
                    int num503 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y + 16f), Projectile.width, Projectile.height - 16, DustID.LifeDrain, 0f, 0f, 0, default, 1f);
                    Main.dust[num503].velocity *= 2f;
                    Main.dust[num503].scale *= 1.15f;
                }
            }
            // 每 8 帧推进一帧动画，5 帧循环播放
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 8)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 4)
            {
                Projectile.frame = 0;
            }
            float num = (float)Main.rand.Next(90, 111) * 0.01f;
            num *= Main.essScale;
            Lighting.AddLight(Projectile.Center, 1f * num, 0f * num, 0.15f * num);   // 红色微光，亮度随环境光起伏
            Projectile.rotation = Projectile.velocity.X * 0.04f;                    // 依水平速度轻微倾斜
            if ((double)Math.Abs(Projectile.velocity.X) > 0.2)
            {
                Projectile.spriteDirection = -Projectile.direction;   // 水平移动较快时让贴图朝向运动方向
            }
            // —— 距离阈值常量 ——
            float num633 = 700f;    // 目标搜索半径（只锁定最近且在范围内的敌人）
            float num634 = 2000f;   // 无目标时允许离玩家的最大距离，超出即召回
            float num635 = 3000f;   // 有目标时允许离玩家的最大距离（追击时可离得更远）
            float num636 = 150f;    // 召回状态下判定"已回到玩家身边"、可复位为巡游的距离
            float num637 = 0.05f;   // 同类召唤物相互排斥的推力步长
            // 同类召唤物距离小于自身宽度时互相推开，避免多只红魔叠在同一位置
            for (int num638 = 0; num638 < 1000; num638++)
            {
                bool flag23 = (Main.projectile[num638].type == ModContent.ProjectileType<RedDevil>());
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
            bool flag24 = false;
            // ai[0]=2：遗留的爆发状态（本文件从未写入该值），效果是临时提高更新频率并在 40 帧后自动复位
            if (Projectile.ai[0] == 2f)
            {
                Projectile.ai[1] += 1f;
                Projectile.extraUpdates = 1;
                if (Projectile.ai[1] > 40f)
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
            if (Projectile.ai[0] != 1f)
            {
                Projectile.tileCollide = false;   // 非召回状态强制关闭地形碰撞
            }
            // 中心陷入实体方块时也关闭碰撞，避免贴墙卡死
            if (Projectile.tileCollide && WorldGen.SolidTile(Framing.GetTileSafely((int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16)))
            {
                Projectile.tileCollide = false;
            }
            // 目标选择：优先攻击玩家用召唤武器右键锁定的 NPC
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
            // 离玩家的距离上限：有目标时更宽松，允许追出更远
            float num647 = num634;
            if (flag25)
            {
                num647 = num635;
            }
            // 超出上限：切换到召回状态（ai[0]=1）并关闭地形碰撞，便于穿墙飞回
            if (Vector2.Distance(player.Center, Projectile.Center) > num647)
            {
                Projectile.ai[0] = 1f;
                Projectile.tileCollide = false;
                Projectile.netUpdate = true;
            }
            // 巡游状态下若已锁定目标：进入攻击机动
            if (flag25 && Projectile.ai[0] == 0f)
            {
                Vector2 vector47 = vector46 - Projectile.Center;
                float num648 = vector47.Length();
                vector47.Normalize();
                // 距离超过 200 像素则全速接近（16），否则反向拉开，维持约 200 像素的攻守距离
                if (num648 > 200f)
                {
                    float scaleFactor2 = 16f; //8
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
                bool flag26 = false;   // 是否处于召回状态
                if (!flag26)
                {
                    flag26 = (Projectile.ai[0] == 1f);
                }
                float num650 = 5f;   // 跟随速度：巡游 5
                if (flag26)
                {
                    num650 = 12f;   // 召回时加速到 12
                }
                Vector2 center2 = Projectile.Center;
                Vector2 vector48 = player.Center - center2 + new Vector2(0f, -30f);   // 悬停点在玩家中心上方 30 像素
                float num651 = vector48.Length();
                if (num651 > 200f && num650 < 6.5f) //200 and 8
                {
                    num650 = 6.5f;   // 离悬停点过远时把巡游速度下限提到 6.5
                }
                // 已回到玩家附近（150 像素内）且头顶无遮挡：结束召回，恢复巡游
                if (num651 < num636 && flag26 && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
                {
                    Projectile.ai[0] = 0f;
                    Projectile.netUpdate = true;
                }
                // 极端距离（>2000）直接传送到玩家身上，防止永久丢失
                if (num651 > 2000f)
                {
                    Projectile.position.X = Main.player[Projectile.owner].Center.X - (float)(Projectile.width / 2);
                    Projectile.position.Y = Main.player[Projectile.owner].Center.Y - (float)(Projectile.height / 2);
                    Projectile.netUpdate = true;
                }
                if (num651 > 70f)
                {
                    vector48.Normalize();
                    vector48 *= num650;
                    Projectile.velocity = (Projectile.velocity * 40f + vector48) / 41f;   // 与旧速度做 40:1 加权，平滑飞行
                }
                else if (Projectile.velocity.X == 0f && Projectile.velocity.Y == 0f)
                {
                    // 完全静止时给一个微小初速度，避免多只红魔重叠卡死
                    Projectile.velocity.X = -0.2f;
                    Projectile.velocity.Y = -0.1f;
                }
            }
            // ai[1]：攻击冷却计时器。>0 时每次递增 1~3，超过 80 归零，即约每 80 帧可再开火一次
            if (Projectile.ai[1] > 0f)
            {
                Projectile.ai[1] += (float)Main.rand.Next(1, 4);
            }
            if (Projectile.ai[1] > 80f)
            {
                Projectile.ai[1] = 0f;
                Projectile.netUpdate = true;
            }
            // 巡游状态且冷却归零时，向目标发射一枚弹幕
            if (Projectile.ai[0] == 0f)
            {
                float scaleFactor3 = 24f;   // 弹幕初速
                int num658 = 114;           // 发射的弹幕类型 ID（伤害沿用本体伤害）
                if (flag25 && Projectile.ai[1] == 0f)
                {
                    Projectile.ai[1] += 1f;
                    // 仅主人端生成弹幕，并要求与目标之间视线通畅
                    if (Main.myPlayer == Projectile.owner && Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, vector46, 0, 0))
                    {
                        Vector2 value19 = vector46 - Projectile.Center;
                        value19.Normalize();
                        value19 *= scaleFactor3;
                        int num659 = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y, value19.X, value19.Y, num658, Projectile.damage, 0f, Main.myPlayer, 0f, 0f);
                        // NewProjectile 失败时返回 -1，直接索引 Main.projectile[-1] 会越界崩溃，故先做边界校验
                        if (num659 >= 0 && num659 < Main.maxProjectiles)
                        {
                            Main.projectile[num659].timeLeft = 300;                  // 子弹幕寿命限制 300 帧
                            Main.projectile[num659].usesLocalNPCImmunity = true;     // 启用逐 NPC 独立命中冷却
                            Main.projectile[num659].localNPCHitCooldown = 10;        // 穿透时对同一敌人 10 帧内不重复结算
                            // 伤害职业跟随穿戴者所戴的魔影头部件（近战/远程/…），读不到就退回近战。
                            // 三叉戟只在拥有者客户端生成（上方有 Main.myPlayer == Projectile.owner 守卫），
                            // 伤害也在该端结算，故此处直接读本机 ModPlayer 即可，不需要网络同步。
                            DamageClass devilClass = modPlayer.demonshadeClass ?? DamageClass.Melee;
                            Main.projectile[num659].DamageType = devilClass;  // 114 是原版弹幕，默认吃不到加成；红魔本体不造成伤害，真正打人的是它，故必须同步改成对应职业
                            // 暴击必须显式重设：tML 在生成时把暴击快照进 Projectile.CritChance，且按弹幕自己的 DamageType 取值、
                            // 再由父弹幕继承。红魔本体是召唤类型，于是叉子继承到的是召唤暴击（输出职业 build 基本为 0），
                            // 不覆盖就永远不暴击。这里直接改写成玩家当前职业的暴击。
                            Main.projectile[num659].CritChance = (int)player.GetTotalCritChance(devilClass);
                        }
                        Projectile.netUpdate = true;
                    }
                }
            }
        }
        // ── 覆写方法 ──
        /// <summary>
        /// 本体不参与接触伤害判定（伤害全部由投射的弹幕 114 承担），返回 false 屏蔽碰撞伤害。
        /// </summary>
        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of true */
        {
            return false;
        }
    }
}
