using CalamityDemutation.Content.Projectiles.Healing;
using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 沙之老婆（Drew 版）宠物：由"有胸的瓶中老婆"（wifeinaBottlewithBoobs）召唤，周期性生成治疗法珠为玩家回血。
    /// </summary>
    internal class DrewsSandyWaifu:ModProjectile
    {
        public int dust = 3;   // 出生粒子爆发计数器：前 4 帧（3→<0）一次性喷出大团粉尘作登场特效
        /// <summary>
        /// 注册 5 帧动画，标记为宠物且可牺牲。
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
        }
        /// <summary>
        /// 基础属性：无召唤栏消耗、持久存在；命中冷却随 Boss 进度递减。
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
            Projectile.timeLeft *= 5;
            Projectile.minion = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20 -
                (NPC.downedGolemBoss ? 5 : 0) -
                (NPC.downedMoonlord ? 5 : 0) -
                (BossSystem.DevourerOfGods ? 4 : 0) -
                (BossSystem.Yharon ? 3 : 0);
        }
        /// <summary>
        /// 宠物 AI：校验饰品标志、喷吐沙尘、跟随玩家，并在冷却结束后生成治疗法珠。
        /// </summary>
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            // 未装备瓶中老婆且未开启"全体老婆"时直接消失
            if (!modPlayer.wifeinaBottlewithBoobs && !modPlayer.allWaifus)
            {
                Projectile.active = false;
                return;
            }
            if (player.dead)
            {
                modPlayer.drewsSandyWaifu = false;
            }
            // 召唤标志有效时持续刷新存活时间，实现常驻跟随
            if (modPlayer.drewsSandyWaifu)
            {
                Projectile.timeLeft = 2;
            }
            dust--;
            if (dust >= 0)
            {
                int num501 = 50;
                for (int num502 = 0; num502 < num501; num502++)
                {
                    int num503 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y + 16f), Projectile.width, Projectile.height - 16, DustID.Sand, 0f, 0f, 0, default(Color), 1f);
                    Main.dust[num503].velocity *= 2f;
                    Main.dust[num503].scale *= 1.15f;
                }
            }
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 16)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 4)
            {
                Projectile.frame = 0;
            }
            if ((double)Math.Abs(Projectile.velocity.X) > 0.2)
            {
                Projectile.spriteDirection = -Projectile.direction;
            }
            float num636 = 100f; //150
            float num = (float)Main.rand.Next(90, 111) * 0.01f;
            num *= Main.essScale;
            Lighting.AddLight(Projectile.Center, 0.7f * num, 0.6f * num, 0f * num);
            // 同类召唤物之间产生排斥力，避免多只宠物叠在同一位置
            float num637 = 0.05f;
            for (int num638 = 0; num638 < 1000; num638++)
            {
                bool flag23 = Main.projectile[num638].type == ModContent.ProjectileType<DrewsSandyWaifu>();
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
            if (Vector2.Distance(player.Center, Projectile.Center) > 400f)
            {
                Projectile.ai[0] = 1f;
                Projectile.tileCollide = false;
                Projectile.netUpdate = true;
            }
            bool flag26 = false;
            if (!flag26)
            {
                flag26 = (Projectile.ai[0] == 1f);
            }
            float num650 = 7f; //6
            if (flag26)
            {
                num650 = 18f; //15
            }
            Vector2 center2 = Projectile.Center;
            Vector2 vector48 = player.Center - center2 + new Vector2(-250f, -60f); //-60
            float num651 = vector48.Length();
            if (num651 > 200f && num650 < 10f) //200 and 8
            {
                num650 = 10f; //8
            }
            if (num651 < num636 && flag26 && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
            {
                Projectile.ai[0] = 0f;
                Projectile.netUpdate = true;
            }
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
                Projectile.velocity = (Projectile.velocity * 40f + vector48) / 41f;
            }
            else if (Projectile.velocity.X == 0f && Projectile.velocity.Y == 0f)
            {
                Projectile.velocity.X = -0.22f;
                Projectile.velocity.Y = -0.12f;
            }
            if (Projectile.ai[1] > 0f)
            {
                Projectile.ai[1] += (float)Main.rand.Next(1, 4);
            }
            if (Projectile.ai[1] > 220f)
            {
                Projectile.ai[1] = 0f;
                Projectile.netUpdate = true;
            }
            if (Projectile.localAI[0] < 120f)
            {
                Projectile.localAI[0] += 1f;
            }
            if (Projectile.ai[0] == 0f)
            {
                // 生成治疗法珠：需满足攻击计时归零且已存在满 2 秒（localAI[0]）
                int num658 = ModContent.ProjectileType<CactusHealOrb>();
                if (Projectile.ai[1] == 0f && Projectile.localAI[0] >= 120f)
                {
                    Projectile.ai[1] += 1f;
                    if (Main.myPlayer == Projectile.owner && Main.player[Projectile.owner].statLife < Main.player[Projectile.owner].statLifeMax2)
                    {
                        SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
                        int num226 = 36;
                        for (int num227 = 0; num227 < num226; num227++)
                        {
                            // SafeNormalize：速度为 0 时回退到 UnitY，避免 Normalize 产生 NaN 粉尘
                            Vector2 vector6 = Projectile.velocity.SafeNormalize(Vector2.UnitY) * new Vector2((float)Projectile.width / 2f, (float)Projectile.height) * 0.75f;
                            vector6 = vector6.RotatedBy((double)((float)(num227 - (num226 / 2 - 1)) * 6.28318548f / (float)num226), default(Vector2)) + Projectile.Center;
                            Vector2 vector7 = vector6 - Projectile.Center;
                            int num228 = Dust.NewDust(vector6 + vector7, 0, 0, DustID.Terra, vector7.X * 1.5f, vector7.Y * 1.5f, 100, new Color(0, 200, 0), 1f);
                            Main.dust[num228].noGravity = true;
                            Main.dust[num228].noLight = true;
                            Main.dust[num228].velocity = vector7;
                        }
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y, 0f, -6f, num658, 0, 0f, Main.myPlayer, 0f, 0f);
                    }
                }
            }
        }
    }
}
