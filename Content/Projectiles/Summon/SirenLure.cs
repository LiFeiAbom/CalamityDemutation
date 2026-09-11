using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 海妖诱饵召唤物：由魅惑之饵（lureofEnthrallment）召唤，跟随玩家并向敌人发射水矛/霜雾/海妖之歌。
    /// </summary>
    internal class SirenLure:ModProjectile
    {
        public int dust = 3;   // 出生粒子爆发计数器：前 3 帧（>0）一次性喷出大团水花粉尘作登场特效
        /// <summary>
        /// 注册 6 帧动画，并标记为可右键锁定目标、可牺牲的召唤物。
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 6;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
        }
        /// <summary>
        /// 基础属性：召唤物、无召唤栏消耗、持久存在，本体不造成接触伤害。
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
            Projectile.timeLeft *= 5;
        }
        /// <summary>
        /// 诱饵 AI：校验饰品标志、喷吐水花、吸附在玩家头顶，锁定敌人后周期性发射三种弹幕。
        /// </summary>
        public override void AI()
        {
            bool flag64 = Projectile.type == ModContent.ProjectileType<SirenLure>();
            Player player = Main.player[Projectile.owner];
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            // 未装备魅惑之饵且未开启"全体老婆"时直接消失
            if (!modPlayer.lureofEnthrallment && !modPlayer.allWaifus)
            {
                Projectile.active = false;
                return;
            }
            if (flag64)
            {
                if (player.dead)
                {
                    modPlayer.sirenLureWaifu = false;
                }
                // 召唤标志有效时持续刷新存活时间，实现常驻跟随
                if (modPlayer.sirenLureWaifu)
                {
                    Projectile.timeLeft = 2;
                }
            }
            if (dust > 0)
            {
                int num501 = 50;
                for (int num502 = 0; num502 < num501; num502++)
                {
                    int num503 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y + 16f), Projectile.width, Projectile.height - 16, DustID.Water, 0f, 0f, 0, default(Color), 1f);
                    Main.dust[num503].velocity *= 2f;
                    Main.dust[num503].scale *= 1.15f;
                }
                dust--;
            }
            Lighting.AddLight(Projectile.Center, 0f, 0.25f, 1.5f);
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
            Projectile.position.Y = Main.player[Projectile.owner].Center.Y - (float)(Projectile.height / 2) + Main.player[Projectile.owner].gfxOffY - 180f;
            if (Main.player[Projectile.owner].gravDir == -1f)
            {
                Projectile.position.Y = Projectile.position.Y + 360f;
                Projectile.rotation = 3.14f;
            }
            else
            {
                Projectile.rotation = 0f;
            }
            Projectile.position.X = (float)((int)Projectile.position.X);
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
                bool flag18 = false;
                float num506 = Projectile.Center.X;
                float num507 = Projectile.Center.Y;
                float num508 = 1000f;
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
                    float num516 = num506;
                    float num517 = num507;
                    num506 -= Projectile.Center.X;
                    num507 -= Projectile.Center.Y;
                    if (num506 < 0f)
                    {
                        Projectile.spriteDirection = 1;
                    }
                    else
                    {
                        Projectile.spriteDirection = -1;
                    }
                    // 默认水矛，约 1/9 概率替换为霜雾，再约 1/9 概率替换为海妖之歌
                    int projectileType = ModContent.ProjectileType<WaterSpearFriendly>();
                    if (Main.rand.NextBool(9))
                    {
                        projectileType = ModContent.ProjectileType<FrostMistFriendly>();
                    }
                    else if (Main.rand.NextBool(9))
                    {
                        projectileType = ModContent.ProjectileType<SirenSongFriendly>();
                    }
                    float num403 = Main.rand.Next(12, 20);
                    Vector2 vector29 = new Vector2(Projectile.position.X + (float)Projectile.width * 0.5f, Projectile.position.Y + (float)Projectile.height * 0.5f);
                    float num404 = num516 - vector29.X;
                    float num405 = num517 - vector29.Y;
                    float num406 = (float)Math.Sqrt((double)(num404 * num404 + num405 * num405));
                    num406 = num403 / num406;
                    num404 *= num406;
                    num405 *= num406;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X - 4f, Projectile.Center.Y, num404, num405, projectileType, Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, 0f);
                    Projectile.ai[0] = 12f;
                }
            }
        }
        /// <summary>
        /// 固定半透明绘制，使本体呈柔和发光质感。
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
