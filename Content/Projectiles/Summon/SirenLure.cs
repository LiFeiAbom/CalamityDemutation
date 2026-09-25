using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 海妖诱饵（SirenLure） - 由魅惑之饵（lureofEnthrallment）召唤的常驻召唤物，
    /// 钉在主人头顶上方，锁定敌人后随机发射水矛（<see cref="WaterSpearFriendly"/>）、
    /// 霜雾（<see cref="FrostMistFriendly"/>）或海妖之歌（<see cref="SirenSongFriendly"/>）。
    /// <para>
    /// 本体不造成接触伤害（见 <see cref="CanDamage"/> 返回 false），移动完全由"每帧硬贴到主人头顶"实现，
    /// 因此不存在巡游/召回状态机；<c>ai[0]</c> 只是发射冷却计时器。
    /// </para>
    /// </summary>
    internal class SirenLure:ModProjectile
    {
        // ── 实例字段 ──
        /// <summary>出生粒子爆发计数器：&gt;0 的 3 帧里一次性喷出大团水花粉尘作登场特效</summary>
        public int dust = 3;
        // ── 生命周期方法 ──
        /// <summary>
        /// 注册 6 帧动画，并标记为可右键锁定目标（MinionTargettingFeature）、可牺牲的召唤物。
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 6;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
        }
        /// <summary>
        /// 基础属性：70x120 碰撞箱、召唤物、无召唤栏消耗（<c>minionSlots = 0</c>）、无限穿透、不撞地形。
        /// 未设 <c>localNPCHitCooldown</c>，因为本体不参与伤害结算。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 70;
            Projectile.height = 120;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.minion = true;
            Projectile.minionSlots = 0f;
            Projectile.timeLeft = 18000;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft *= 5;   // 超长兜底寿命，实际靠召唤标志压到 2 帧续命
        }
        /// <summary>
        /// 诱饵 AI：校验饰品标志（失效即消失）、续命、喷吐登场水花、吸附到主人头顶，
        /// 并在锁定敌人后周期性发射三种弹幕之一。
        /// </summary>
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            // 未装备魅惑之饵且未开启"全体老婆"时直接消失
            if (!modPlayer.lureofEnthrallment && !modPlayer.allWaifus)
            {
                Projectile.active = false;
                return;
            }
            if (player.dead)
            {
                modPlayer.sirenLureWaifu = false;   // 玩家死亡时清空召唤标志，复活后由饰品重新召唤
            }
            // 召唤标志有效时持续刷新存活时间，实现常驻跟随
            if (modPlayer.sirenLureWaifu)
            {
                Projectile.timeLeft = 2;
            }
            if (dust > 0)
            {
                int num501 = 50;   // 出生时一次性喷出 50 颗水花粉尘
                for (int num502 = 0; num502 < num501; num502++)
                {
                    int num503 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y + 16f), Projectile.width, Projectile.height - 16, DustID.Water, 0f, 0f, 0, default(Color), 1f);
                    Main.dust[num503].velocity *= 2f;
                    Main.dust[num503].scale *= 1.15f;
                }
                dust--;
            }
            Lighting.AddLight(Projectile.Center, 0f, 0.25f, 1.5f);   // 水蓝色光源
            // 6 帧循环动画（每 13 帧推进一帧）
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 12)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 5)
            {
                Projectile.frame = 0;
            }
            Projectile.position.X = Main.player[Projectile.owner].Center.X - (float)(Projectile.width / 2);
            Projectile.position.Y = Main.player[Projectile.owner].Center.Y - (float)(Projectile.height / 2) + Main.player[Projectile.owner].gfxOffY - 180f;   // 钉在主人头顶上方 180 像素
            if (Main.player[Projectile.owner].gravDir == -1f)
            {
                Projectile.position.Y = Projectile.position.Y + 360f;   // 主人处于反向重力时，翻到脚下并整体倒转 180°
                Projectile.rotation = 3.14f;
            }
            else
            {
                Projectile.rotation = 0f;
            }
            Projectile.position.X = (float)((int)Projectile.position.X);   // 坐标取整，避免亚像素抖动导致贴图闪烁
            Projectile.position.Y = (float)((int)Projectile.position.Y);
            // 仅在召唤者客户端执行攻击逻辑，避免多人下重复发射
            if (Projectile.owner == Main.myPlayer)
            {
                // ai[0] 作为发射间隔计时：非零时递减并跳过本次攻击
                if (Projectile.ai[0] != 0f)
                {
                    Projectile.ai[0] -= 1f;
                    return;
                }
                bool flag18 = false;   // 是否找到有效目标
                float num506 = Projectile.Center.X;   // 目标 X（找不到时保持为自身位置）
                float num507 = Projectile.Center.Y;   // 目标 Y
                float num508 = 1000f;                 // 索敌半径
                // 目标选择：优先玩家用召唤武器右键标记的目标
                if (player.HasMinionAttackTargetNPC)
                {
                    NPC npc = Main.npc[player.MinionAttackTargetNPC];
                    if (npc.CanBeChasedBy(Projectile, false))
                    {
                        float num539 = npc.position.X + (float)(npc.width / 2);
                        float num540 = npc.position.Y + (float)(npc.height / 2);
                        float num541 = Math.Abs(Projectile.position.X + (float)(Projectile.width / 2) - num539) + Math.Abs(Projectile.position.Y + (float)(Projectile.height / 2) - num540);
                        if (num541 < num508 && Collision.CanHit(Projectile.position, Projectile.width, Projectile.height, npc.position, npc.width, npc.height))
                        {
                            num508 = num541;
                            num506 = num539;
                            num507 = num540;
                            flag18 = true;
                        }
                    }
                }
                else
                {
                    // 否则遍历 200 个 NPC，取曼哈顿距离最近且视线通畅的敌人
                    for (int num512 = 0; num512 < 200; num512++)
                    {
                        if (Main.npc[num512].CanBeChasedBy(Projectile, false))
                        {
                            float num513 = Main.npc[num512].position.X + (float)(Main.npc[num512].width / 2);
                            float num514 = Main.npc[num512].position.Y + (float)(Main.npc[num512].height / 2);
                            float num515 = Math.Abs(Projectile.position.X + (float)(Projectile.width / 2) - num513) + Math.Abs(Projectile.position.Y + (float)(Projectile.height / 2) - num514);
                            if (num515 < num508 && Collision.CanHit(Projectile.position, Projectile.width, Projectile.height, Main.npc[num512].position, Main.npc[num512].width, Main.npc[num512].height))
                            {
                                num508 = num515;
                                num506 = num513;
                                num507 = num514;
                                flag18 = true;
                            }
                        }
                    }
                }
                if (flag18)
                {
                    float num516 = num506;   // 记下目标坐标（下面会就地改写成"目标 - 本体"的向量）
                    float num517 = num507;
                    num506 -= Projectile.Center.X;
                    num507 -= Projectile.Center.Y;
                    if (num506 < 0f)
                    {
                        Projectile.spriteDirection = 1;   // 目标在左侧，贴图朝左
                    }
                    else
                    {
                        Projectile.spriteDirection = -1;
                    }
                    // 默认水矛，约 1/9 概率替换为霜雾，再约 1/9 概率替换为海妖之歌
                    // （两者独立判定，故实际权重约为 8/9 水矛、1/9 霜雾、8/81 海妖之歌）
                    int projectileType = ModContent.ProjectileType<WaterSpearFriendly>();
                    if (Main.rand.NextBool(9))
                    {
                        projectileType = ModContent.ProjectileType<FrostMistFriendly>();
                    }
                    else if (Main.rand.NextBool(9))
                    {
                        projectileType = ModContent.ProjectileType<SirenSongFriendly>();
                    }
                    float num403 = Main.rand.Next(12, 20);   // 弹速 12~19
                    Vector2 vector29 = new Vector2(Projectile.position.X + (float)Projectile.width * 0.5f, Projectile.position.Y + (float)Projectile.height * 0.5f);
                    float num404 = num516 - vector29.X;
                    float num405 = num517 - vector29.Y;
                    float num406 = (float)Math.Sqrt((double)(num404 * num404 + num405 * num405));
                    num406 = num403 / num406;   // 归一化并乘上目标速度
                    num404 *= num406;
                    num405 *= num406;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X - 4f, Projectile.Center.Y, num404, num405, projectileType, Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, 0f);   // 从本体略偏左处发出
                    Projectile.ai[0] = 12f;   // 每 12 帧发一枚
                }
            }
        }
        // ── 覆写方法 ──
        /// <summary>
        /// 固定半透明绘制（固定 200 alpha），使本体呈柔和发光质感、不受环境光影响。
        /// </summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(200, 200, 200, 200);
        }
        /// <summary>
        /// 本体不参与伤害结算（由发射的弹幕造成伤害）。
        /// </summary>
        public override bool? CanDamage()
        {
            return false;
        }
    }
}
