using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 天狼星（SiriusMinion，移植自灾厄 2.0 的 Projectiles/Summon/SiriusMinion）—— 天狼星法杖召唤的星灵，
    /// 悬停主人头顶，超远距离（7000 像素、不查视线）索敌后朝目标发射光束
    /// （<see cref="SiriusBeam"/>），30 帧一发。
    /// </summary>
    /// <remarks>
    /// 与太阳神那套的关键差异：**它吃掉主人全部剩余召唤栏**——出手时由武器把算出的剩余栏位写进 <c>ai[0]</c>，
    /// 仆从每帧把 <c>minionSlots = ai[0]</c>（tML 每帧重新汇总，故实时生效）；
    /// 光束伤害按 ×<c>(ln(栏位数) + 1)</c> 放大、**穿透数 = 栏位数**。
    /// 存续靠每帧由 <c>modPlayer.sirius</c> 标志把 <c>timeLeft</c> 压成 2 续命。
    /// </remarks>
    internal class SiriusMinion:ModProjectile
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
        /// 基础属性（照源 2.0）：38×48 碰撞箱、无限穿透、不撞地形，超长兜底寿命
        /// （实际靠召唤标志压到 2 帧续命；召唤栏占用每帧由 <c>ai[0]</c> 覆写）。
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 38;
            Projectile.height = 48;
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
        /// 星灵 AI：挂增益、按召唤标志续命、把召唤栏占用刷成 <c>ai[0]</c>、贴住主人头顶（重力翻转翻面）、
        /// 随鼠标文字色脉动缩放、首帧喷出登场光点，最后在主人端按 30 帧冷却朝目标发射光束。
        /// </summary>
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            bool correctMinion = Projectile.type == ModContent.ProjectileType<SiriusMinion>();
            if (correctMinion)
            {
                if (player.dead)
                {
                    modPlayer.sirius = false;   // 玩家死亡时清空召唤标志，复活后需重新召唤
                }
                // 召唤标志有效时持续刷新存活时间，实现常驻跟随
                if (modPlayer.sirius)
                {
                    Projectile.timeLeft = 2;
                }
            }
            player.AddBuff(ModContent.BuffType<SiriusBuff>(), 3600);
            // 吃光出手时剩余的全部召唤栏（数值由武器写进 ai[0]，tML 每帧重新汇总 minionSlots，故实时生效）
            Projectile.minionSlots = Projectile.ai[0];
            Lighting.AddLight(Projectile.Center, 1f, 0.5f, 5f);   // 照源 2.0 的夸张写法（蓝通道 5，确实"太亮了"）
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
            float scalar = (float)Main.mouseTextColor / 200f - 0.35f;
            scalar *= 0.2f;
            Projectile.scale = scalar + 0.95f;
            if (Projectile.localAI[0] == 0f)
            {
                // 登场一次：喷出 50 颗白蓝光点（源写裸值 20，即 DustID.PurificationPowder，照源保留同一编号）
                int dustAmt = 50;
                for (int d = 0; d < dustAmt; d++)
                {
                    int sirius = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y + 16f), Projectile.width, Projectile.height - 16, DustID.PurificationPowder, 0f, 0f, 0, default, 1f);
                    Main.dust[sirius].velocity *= 2f;
                    Main.dust[sirius].scale *= 1.15f;
                }
                Projectile.localAI[0] += 1f;
            }
            // 索敌与开火都只在主人端跑（弹幕 AI 两端都会执行）
            if (Projectile.owner == Main.myPlayer)
            {
                if (Projectile.ai[1] != 0f)
                {
                    Projectile.ai[1] -= 1f;   // 开火冷却：30 帧一发（出手时由武器写入 30）
                    return;
                }
                Vector2 targetVec = Projectile.position;
                float maxDistance = 7000f;   // 照源：全屏级别的索敌半径，且**不查视线**
                bool hasTarget = false;
                // 优先取玩家用召唤武器右键标记的目标
                if (player.HasMinionAttackTargetNPC)
                {
                    NPC npc = Main.npc[player.MinionAttackTargetNPC];
                    if (npc.CanBeChasedBy(Projectile, false))
                    {
                        float extraDistance = (npc.width / 2) + (npc.height / 2);
                        if (Vector2.Distance(npc.Center, Projectile.Center) < (maxDistance + extraDistance))
                        {
                            targetVec = npc.Center;
                            hasTarget = true;
                        }
                    }
                }
                if (!hasTarget)
                {
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC npc = Main.npc[i];
                        if (npc.CanBeChasedBy(Projectile, false))
                        {
                            float extraDistance = (npc.width / 2) + (npc.height / 2);
                            if (Vector2.Distance(npc.Center, Projectile.Center) < (maxDistance + extraDistance))
                            {
                                targetVec = npc.Center;
                                hasTarget = true;
                            }
                        }
                    }
                }
                if (hasTarget)
                {
                    float projSpeed = 15f;
                    Vector2 source = new Vector2(Projectile.Center.X - 4f, Projectile.Center.Y);
                    Vector2 velocity = targetVec - Projectile.Center;
                    float targetDist = velocity.Length();
                    targetDist = projSpeed / targetDist;
                    velocity.X *= targetDist;
                    velocity.Y *= targetDist;
                    // 消耗的召唤栏越多越强：伤害 ×(ln(栏位数) + 1)，穿透数 = 栏位数
                    float damageMult = ((float)Math.Log(Projectile.ai[0], MathHelper.E)) + 1f;
                    int beam = Projectile.NewProjectile(Projectile.GetSource_FromThis(), source, velocity, ModContent.ProjectileType<SiriusBeam>(), (int)(Projectile.damage * damageMult), Projectile.knockBack, Projectile.owner);
                    if (Main.projectile.IndexInRange(beam))
                    {
                        Main.projectile[beam].originalDamage = Projectile.originalDamage;
                        Main.projectile[beam].penetrate = (int)Projectile.ai[0];
                    }
                    Projectile.ai[1] = 30f;
                }
            }
        }
        /// <summary>
        /// 半透明固定染色（源写法）：让星灵不随环境光变暗
        /// </summary>
        public override Color? GetAlpha(Color lightColor) => new Color(200, 200, 200, 200);
        /// <summary>
        /// 本体不造成接触伤害，伤害全部由光束与爆炸结算
        /// </summary>
        public override bool? CanDamage() => false;
    }
}
