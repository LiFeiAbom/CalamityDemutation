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
    /// 犽戎之子（SonYharon，移植自 CalamityInheritance 的 Content/Projectiles/Summon/SonYharon.cs）：
    /// 巨龙七星灯召唤的贴身飞行仆从（4 帧贴图），占 2 个仆从栏位。
    /// 常态跟在主人身边游荡；锁定到附近敌人后会先加速撞过去再退回，
    /// <c>ai[0] == 2</c> 是"贴身撕咬"的短状态（30 帧、贴图切到第 3 帧、extraUpdates 临时抬到 2）。
    /// <para>
    /// 与 CI 原版的差异：① <c>CIDustID.DustCopperCoin</c>（CI 自己的原版尘埃别名表）→直接写它对应的原版尘埃 ID
    /// <c>244</c>；② <c>CIFunction.FramesChanger(proj, fCounter, fMax)</c> → 本类私有 <see cref="FramesChanger"/>
    /// 等价内联；③ <c>player.CIMod()</c> → <c>player.GetModPlayer&lt;CalamityDemutationPlayer&gt;()</c>，
    /// 标记 <c>OwnSonYharon</c> → <c>ownSonYharon</c>（本模组字段小写开头）；④ 去掉 CI 的 <c>ILocalizedModType</c> 标记。
    /// </para>
    /// </summary>
    internal class SonYharon:ModProjectile
    {
        /// <summary>四帧贴图；可被牺牲（不参与原版"牺牲复活"）、参与原版仆从索敌目标机制</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.MinionSacrificable[Type] = false;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        }
        public override void SetDefaults()
        {
            Projectile.width = 100;
            Projectile.height = 100;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            // 命中无敌帧：CI 原样是 1（每 1~2 帧判决一次、一秒几十次命中），那样会让按"次"结算的噬渊标记平伤
            // （+90/次）白嫖出每秒数千点伤害；2026-09-22 用户拍板改成 4（约 12 次/秒）
            Projectile.localNPCHitCooldown = 4;
            Projectile.extraUpdates = 1;
            Projectile.minion = true;
            // 占用仆从栏位：CI 原样是 4，2026-09-22 用户拍板降到 2（配合上面压命中频率后的输出）
            Projectile.minionSlots = 2f;
            Projectile.timeLeft = 18000;
            Projectile.timeLeft *= 5;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
        }
        /// <summary>
        /// CI 的 <c>CIFunction.FramesChanger</c> 等价内联：按 <paramref name="frameRate"/> 帧推进动画帧，
        /// 到 <paramref name="frameCount"/> 帧后绕回 0，返回当前帧号。
        /// </summary>
        private static int FramesChanger(Projectile projectile, int frameRate, int frameCount)
        {
            projectile.frameCounter++;
            if (projectile.frameCounter > frameRate)
            {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= frameCount)
            {
                projectile.frame = 0;
            }
            return projectile.frame;
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            // 召唤出场那一下撒 100 颗铜钱尘（DustID.CopperCoin = CI 的 CIDustID.DustCopperCoin，同为原版尘埃 244）
            if (Projectile.localAI[0] == 0f)
            {
                int dustCount = 100;
                for (int i = 0; i < dustCount; i++)
                {
                    int dustType = Dust.NewDust(Projectile.Center, Projectile.width, Projectile.height, DustID.CopperCoin);
                    Main.dust[dustType].velocity *= 2f;
                    Main.dust[dustType].scale *= 1.15f;
                }
                Projectile.localAI[0] = 1f;
            }
            // 贴图朝向跟随水平速度
            if (Math.Abs(Projectile.velocity.X) > 0.2f)
            {
                Projectile.spriteDirection = -Projectile.direction;
            }
            float chaseRange = 800f;
            float teleportRange = 1200f;
            float chaseRangeWithTarget = 3000f;
            float settleRange = 500f;
            float colorValue = Main.rand.Next(90, 111) * 0.01f;
            colorValue *= Main.essScale;
            // 犽戎之子的暖色光（偏红，蓝通道只有红的三分之二）
            Lighting.AddLight(Projectile.Center, 1.2f * colorValue, 0.8f * colorValue, 0f);
            bool ifHasMinion = Projectile.type == ModContent.ProjectileType<SonYharon>();
            player.AddBuff(ModContent.BuffType<SonYharonBuff>(), 1200);
            if (ifHasMinion)
            {
                if (player.dead)
                {
                    modPlayer.ownSonYharon = false;
                }
                if (modPlayer.ownSonYharon)
                {
                    Projectile.timeLeft = 2;
                }
            }
            // 与主人的其它同名仆从挤在一起时互相推开，避免叠成一坨
            float accele = 0.15f;
            for (int i = 0; i < 1000; i++)
            {
                if (i != Projectile.whoAmI && Main.projectile[i].active && Main.projectile[i].owner == Projectile.owner && ifHasMinion && Math.Abs(Projectile.position.X - Main.projectile[i].position.X) + Math.Abs(Projectile.position.Y - Main.projectile[i].position.Y) < Projectile.width)
                {
                    if (Projectile.position.X < Main.projectile[i].position.X)
                    {
                        Projectile.velocity.X = Projectile.velocity.X - accele;
                    }
                    else
                    {
                        Projectile.velocity.X = Projectile.velocity.X + accele;
                    }
                    if (Projectile.position.Y < Main.projectile[i].position.Y)
                    {
                        Projectile.velocity.Y = Projectile.velocity.Y - accele;
                    }
                    else
                    {
                        Projectile.velocity.Y = Projectile.velocity.Y + accele;
                    }
                }
            }
            // 贴身撕咬状态：30 帧内切到第 3 帧并抬更新频率，走完就复位回常态
            bool canUpdate = false;
            if (Projectile.ai[0] == 2f)
            {
                Projectile.ai[1] += 1f;
                Projectile.extraUpdates = 2;
                Projectile.frame = FramesChanger(Projectile, 3, 3);
                if (Projectile.ai[1] > 30f)
                {
                    Projectile.ai[1] = 1f;
                    Projectile.ai[0] = 0f;
                    Projectile.extraUpdates = 1;
                    Projectile.netUpdate = true;
                }
                else
                {
                    canUpdate = true;
                }
            }
            if (canUpdate)
            {
                return;
            }
            Vector2 getMinionPos = Projectile.position;
            bool canChase = false;
            // 优先打玩家标记的召唤目标
            if (player.HasMinionAttackTargetNPC)
            {
                NPC npc = Main.npc[player.MinionAttackTargetNPC];
                if (npc.CanBeChasedBy(Projectile, false))
                {
                    float getDistance = Vector2.Distance(npc.Center, Projectile.Center);
                    if ((Vector2.Distance(Projectile.Center, getMinionPos) > getDistance && getDistance < chaseRange) || !canChase)
                    {
                        chaseRange = getDistance;
                        getMinionPos = npc.Center;
                        canChase = true;
                    }
                }
            }
            else
            {
                for (int j = 0; j < 200; j++)
                {
                    NPC npc2 = Main.npc[j];
                    if (npc2.CanBeChasedBy(Projectile, false))
                    {
                        float npc2Dist = Vector2.Distance(npc2.Center, Projectile.Center);
                        if ((Vector2.Distance(Projectile.Center, getMinionPos) > npc2Dist && npc2Dist < chaseRange) || !canChase)
                        {
                            chaseRange = npc2Dist;
                            getMinionPos = npc2.Center;
                            canChase = true;
                        }
                    }
                }
            }
            float maxFollowDistance = teleportRange;
            if (canChase)
            {
                maxFollowDistance = chaseRangeWithTarget;
            }
            if (Vector2.Distance(player.Center, Projectile.Center) > maxFollowDistance)
            {
                Projectile.ai[0] = 1f;
                Projectile.netUpdate = true;
            }
            if (canChase && Projectile.ai[0] == 0f)
            {
                Vector2 newPos = getMinionPos - Projectile.Center;
                float num648 = newPos.Length();
                newPos.Normalize();
                // 离目标还远就扑过去（8 倍），已经贴脸就反向弹开（-4 倍）
                float scaleFac = num648 > 200f ? 8f : -4f;
                newPos *= scaleFac;
                Projectile.velocity = (Projectile.velocity * 40f + newPos) / 41f;
            }
            else
            {
                // 回主人身边：ai[0] == 1（被拉太远）时飞得快些，贴近到 settleRange 内就解除该状态
                bool returning = Projectile.ai[0] == 1f;
                float num650 = 6f;
                if (returning)
                {
                    num650 = 15f;
                }
                Vector2 newVec = player.Center - Projectile.Center + new Vector2(0f, -60f);
                float num651 = newVec.Length();
                if (num651 > 200f && num650 < 8f)
                {
                    num650 = 8f;
                }
                if (num651 < settleRange && returning && !Collision.SolidCollision(Projectile.Center, Projectile.width, Projectile.height))
                {
                    Projectile.ai[0] = 0f;
                    Projectile.netUpdate = true;
                }
                if (num651 > 2000f)
                {
                    // 离得太离谱就直接挪回主人身上
                    Projectile.position.X = Main.player[Projectile.owner].Center.X - Projectile.width / 2;
                    Projectile.position.Y = Main.player[Projectile.owner].Center.Y - Projectile.height / 2;
                    Projectile.netUpdate = true;
                }
                if (num651 > 70f)
                {
                    newVec.Normalize();
                    newVec *= num650;
                    Projectile.velocity = (Projectile.velocity * 40f + newVec) / 41f;
                }
                else if (Projectile.velocity.X == 0f && Projectile.velocity.Y == 0f)
                {
                    // 完全静止时给一点微速，免得帧动画和朝向卡死
                    Projectile.velocity.X = -0.15f;
                    Projectile.velocity.Y = -0.05f;
                }
            }
            Projectile.frame = FramesChanger(Projectile, 12, 4);
            if (Projectile.ai[1] > 0f)
            {
                Projectile.ai[1] += Main.rand.Next(1, 4);
            }
            if (Projectile.ai[1] > 40f)
            {
                Projectile.ai[1] = 0f;
                Projectile.netUpdate = true;
            }
            if (Projectile.ai[0] == 0f)
            {
                // 已经贴到 500 像素内的敌人时进贴身撕咬状态（ai[0] = 2），并朝它猛冲
                if (Projectile.ai[1] == 0f && canChase && chaseRange < 500f)
                {
                    Projectile.ai[0] = 2f;
                    Vector2 getNewVec = getMinionPos - Projectile.Center;
                    getNewVec.Normalize();
                    Projectile.velocity = getNewVec * 8f;
                    Projectile.netUpdate = true;
                }
            }
        }
    }
}
