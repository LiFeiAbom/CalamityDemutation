using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 太阳之灵（SolarPixie，移植自灾厄 2.0 的 Projectiles/Summon/SolarPixie）—— 太阳之灵法杖（SunSpiritStaff）召唤的
    /// 常驻召唤物，悬停在主人头顶，索敌后朝目标发射原版热射线（ProjectileID.HeatRay）。
    /// <para>
    /// 状态机极简：ai[0] 只是开火冷却（50 帧）；存续不靠 timeLeft，而是每帧由 modPlayer.sunSpirit 标志把
    /// timeLeft 压成 2 来"续命"（见 AI），该标志由同名召唤增益 SolarSpirit 维护。
    /// </para>
    /// </summary>
    internal class SolarPixie:ModProjectile
    {
        /// <summary>
        /// 标记为可牺牲的召唤物，并允许玩家用召唤武器右键锁定目标（MinionTargettingFeature）。本弹幕无动画帧。
        /// </summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
        }
        /// <summary>
        /// 基础属性：50x50 碰撞箱、占 1 个召唤栏、无限穿透、不撞地形，超长兜底寿命（实际靠召唤标志压到 2 帧续命）。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.minionSlots = 1f;
            Projectile.timeLeft = 18000;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft *= 5;   // 18000*5 帧的超长兜底寿命，实际靠召唤标志压到 2 帧续命
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Summon;
        }
        /// <summary>
        /// 召唤物 AI：挂增益、按召唤标志续命、贴住主人头顶（重力翻转时翻面）、随鼠标文字色脉动缩放、
        /// 首帧喷出登场火花，最后在主人端索敌并按 50 帧冷却朝目标发射热射线。
        /// </summary>
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            player.AddBuff(ModContent.BuffType<SolarSpirit>(), 3600);
            bool correctMinion = Projectile.type == ModContent.ProjectileType<SolarPixie>();
            if (correctMinion)
            {
                if (player.dead)
                {
                    modPlayer.sunSpirit = false;   // 玩家死亡时清空召唤标志，复活后需重新召唤
                }
                // 召唤标志有效时持续刷新存活时间，实现常驻跟随
                if (modPlayer.sunSpirit)
                {
                    Projectile.timeLeft = 2;
                }
            }
            // 悬停在主人头顶上方 60 像素处（跟随玩家的图形偏移，骑坐骑时也对齐）
            Projectile.Center = player.Center + Vector2.UnitY * (player.gfxOffY - 60f);
            if (player.gravDir == -1f)
            {
                Projectile.position.Y += 120f;   // 反转重力时翻到脚下
                Projectile.rotation = MathHelper.Pi;
            }
            else
            {
                Projectile.rotation = 0f;
            }
            Projectile.position.X = (int)Projectile.position.X;
            Projectile.position.Y = (int)Projectile.position.Y;
            // 用鼠标文字色做轻微脉动缩放（源写法）
            float sizeScale = (float)Main.mouseTextColor / 200f - 0.35f;
            sizeScale *= 0.2f;
            Projectile.scale = sizeScale + 0.95f;
            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.25f / 255f, (255 - Projectile.alpha) * 0.25f / 255f, 0f);   // 暖黄光（无蓝通道）
            if (Projectile.localAI[0] == 0f)
            {
                // 登场一次：喷出 25 颗火花（源写裸值 244，即 DustID.CopperCoin 的小金点，照源保留同一编号）
                int dustAmt = 25;
                for (int d = 0; d < dustAmt; d++)
                {
                    int fire = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y + 16f), Projectile.width, Projectile.height - 16, DustID.CopperCoin, 0f, 0f, 0, default, 1f);
                    Main.dust[fire].velocity *= 2f;
                    Main.dust[fire].scale *= 1.15f;
                }
                Projectile.localAI[0] += 1f;
            }
            // 索敌与开火都只在主人端跑（弹幕 AI 两端都会执行）
            if (Projectile.owner == Main.myPlayer)
            {
                if (Projectile.ai[0] != 0f)
                {
                    Projectile.ai[0] -= 1f;   // 开火冷却：50 帧一发
                    return;
                }
                Vector2 targetPos = Projectile.position;
                float maxDistance = 700f;
                bool foundTarget = false;
                // 优先取玩家用召唤武器右键标记的目标
                if (player.HasMinionAttackTargetNPC)
                {
                    NPC npc = Main.npc[player.MinionAttackTargetNPC];
                    if (npc.CanBeChasedBy(Projectile, false))
                    {
                        float npcDist = Vector2.Distance(Projectile.Center, npc.Center);
                        if (npcDist < maxDistance && Collision.CanHit(Projectile.position, Projectile.width, Projectile.height, npc.position, npc.width, npc.height))
                        {
                            targetPos = npc.Center;
                            foundTarget = true;
                        }
                    }
                }
                if (!foundTarget)
                {
                    // 否则遍历全部 NPC，取 700 像素内最近且视线可达的敌人
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC npc = Main.npc[i];
                        if (npc.CanBeChasedBy(Projectile, false))
                        {
                            float npcDist = Vector2.Distance(Projectile.Center, npc.Center);
                            if (npcDist < maxDistance && Collision.CanHit(Projectile.position, Projectile.width, Projectile.height, npc.position, npc.width, npc.height))
                            {
                                maxDistance = npcDist;
                                targetPos = npc.Center;
                                foundTarget = true;
                            }
                        }
                    }
                }
                if (foundTarget)
                {
                    float shootSpeed = 30f;   // 热射线初速 30
                    Vector2 source = Projectile.Center;
                    Vector2 velocity = targetPos - source;
                    velocity.Normalize();
                    velocity *= shootSpeed;
                    int beam = Projectile.NewProjectile(Projectile.GetSource_FromThis(), source, velocity, ProjectileID.HeatRay, Projectile.damage, Projectile.knockBack, Projectile.owner);
                    if (Main.projectile.IndexInRange(beam))
                    {
                        // 原版热射线的职业与原始伤害都要改写，才能吃召唤加成
                        Main.projectile[beam].DamageType = DamageClass.Summon;
                        Main.projectile[beam].originalDamage = Projectile.originalDamage;
                    }
                    Projectile.ai[0] = 50f;
                }
            }
        }
        /// <summary>
        /// 半透明固定染色（源写法）：让灵体不随环境光变暗
        /// </summary>
        public override Color? GetAlpha(Color lightColor) => new Color(200, 200, 200, 200);
        /// <summary>
        /// 本体不造成接触伤害，伤害全部由热射线结算
        /// </summary>
        public override bool? CanDamage() => false;
    }
}
