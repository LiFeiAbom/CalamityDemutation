using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 机械蠕虫·头（MechwormHead） - 弑神者召唤头套装召唤的蠕虫头部
    ///（按经典版灾厄 Projectiles/Summon/MechwormHead.cs 1:1 移植）。
    /// 有目标时以最高 50 像素/帧的速度追击玩家选定的目标（无目标时悬停在玩家身边）；
    /// 用 localAI[0] 在链路上传递"自头往后的段序"，据此把缩放从 1.0 逐节放大（每节 +1%，上限 +50%）；
    /// 入场时喷一圈 BoneTorch 尘并淡入；头不占仆从栏，每节身体占 0.5。
    /// </summary>
    public class MechwormHead : ModProjectile
    {
        /// <summary>入场尘的剩余帧数（>0 时每帧喷 50 粒）</summary>
        private int dust = 3;
        /// <summary>创建时的仆从栏上限，用于在玩家降低上限时自杀</summary>
        private int playerMinionSlots = 0;
        /// <summary>首帧闩锁：初始化 playerMinionSlots</summary>
        private bool runCheck = true;
        /// <summary>
        /// 注册为可牺牲的召唤物，并接受玩家用"目标锁定"键指定的攻击目标
        /// </summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
        }
        /// <summary>
        /// 基础属性：20×20；友方、入水不减速、网络重要、无限穿透、存活 18000 帧（×5 后为 90000）、
        /// 初始全透明（淡入）、穿地形、召唤物、局部无敌 4 帧
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18000;
            Projectile.alpha = 255;
            Projectile.tileCollide = false;
            Projectile.timeLeft *= 5;
            Projectile.minion = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;
        }
        /// <summary>
        /// AI：入场喷尘；维持 Mechworm 增益；玩家死亡或仆从栏上限被下调时自毁；
        /// 有目标就追击（按距离分级加速，限速 50），无目标就回到玩家身边（限速 25）；
        /// 最后按 localAI[0] 更新缩放与碰撞箱
        /// </summary>
        public override void AI()
        {
            Lighting.AddLight((int)((Projectile.position.X + Projectile.width / 2) / 16f), (int)((Projectile.position.Y + Projectile.height / 2) / 16f), 0.15f, 0.01f, 0.15f);
            Player player = Main.player[Projectile.owner];
            if (dust > 0)
            {
                // 入场尘：连续 3 帧、每帧 50 粒 BoneTorch 尘
                for (int i = 0; i < 50; i++)
                {
                    int d = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y + 16f), Projectile.width, Projectile.height - 16, DustID.BoneTorch, 0f, 0f, 0, default(Color), 1f);
                    Main.dust[d].velocity *= 2f;
                    Main.dust[d].scale *= 1.15f;
                }
                dust--;
            }
            if (player.maxMinions > playerMinionSlots)
            {
                playerMinionSlots = player.maxMinions;
            }
            if (runCheck)
            {
                runCheck = false;
                playerMinionSlots = player.maxMinions;
            }
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            player.AddBuff(ModContent.BuffType<Mechworm>(), 3600);
            if ((int)Main.time % 120 == 0)
            {
                Projectile.netUpdate = true;
            }
            if (!player.active || player.maxMinions < playerMinionSlots)
            {
                Projectile.active = false;
                return;
            }
            const int headHitbox = 30;
            if (player.dead)
            {
                modPlayer.mWorm = false;
            }
            if (modPlayer.mWorm)
            {
                Projectile.timeLeft = 2;   // 由增益续命，蠕虫消失时自然到期
            }
            Vector2 playerCenter = player.Center;
            const float chaseRange = 1800f;
            const float targetSearchRange = 2200f;
            int targetIndex = -1;
            if (Projectile.Distance(playerCenter) > 3000f)
            {
                Projectile.Center = playerCenter;   // 掉队太远直接拉回
                Projectile.netUpdate = true;
            }
            if (player.HasMinionAttackTargetNPC)
            {
                NPC marked = Main.npc[player.MinionAttackTargetNPC];
                if (marked.CanBeChasedBy(Projectile, false) && player.Distance(marked.Center) < targetSearchRange
                    && Projectile.Distance(marked.Center) < chaseRange)
                {
                    targetIndex = marked.whoAmI;
                }
            }
            else
            {
                for (int i = 0; i < 200; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.CanBeChasedBy(Projectile, false) && player.Distance(npc.Center) < targetSearchRange
                        && Projectile.Distance(npc.Center) < chaseRange)
                    {
                        targetIndex = i;
                    }
                }
            }
            if (targetIndex != -1)
            {
                // 追击目标：距离越近加速越猛（0.3 → 0.8），限速 50
                NPC target = Main.npc[targetIndex];
                Vector2 toTarget = target.Center - Projectile.Center;
                float accel = 0.3f;
                if (toTarget.Length() < 900f)
                {
                    accel = 0.45f;
                }
                if (toTarget.Length() < 600f)
                {
                    accel = 0.6f;
                }
                if (toTarget.Length() < 300f)
                {
                    accel = 0.8f;
                }
                if (toTarget.Length() > target.Size.Length() * 0.75f)
                {
                    Projectile.velocity += Vector2.Normalize(toTarget) * accel * 1.5f;
                    if (Vector2.Dot(Projectile.velocity, toTarget) < 0.25f)
                    {
                        Projectile.velocity *= 0.8f;
                    }
                }
                if (Projectile.velocity.Length() > 50f)
                {
                    Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 50f;
                }
            }
            else
            {
                // 无目标：向玩家靠拢（越近越缓），限速 25
                float accel = 0.2f;
                Vector2 toPlayer = playerCenter - Projectile.Center;
                if (toPlayer.Length() < 200f)
                {
                    accel = 0.12f;
                }
                if (toPlayer.Length() < 140f)
                {
                    accel = 0.06f;
                }
                if (toPlayer.Length() > 100f)
                {
                    if (Math.Abs(playerCenter.X - Projectile.Center.X) > 20f)
                    {
                        Projectile.velocity.X += accel * Math.Sign(playerCenter.X - Projectile.Center.X);
                    }
                    if (Math.Abs(playerCenter.Y - Projectile.Center.Y) > 10f)
                    {
                        Projectile.velocity.Y += accel * Math.Sign(playerCenter.Y - Projectile.Center.Y);
                    }
                }
                else if (Projectile.velocity.Length() > 2f)
                {
                    Projectile.velocity *= 0.96f;
                }
                if (Math.Abs(Projectile.velocity.Y) < 1f)
                {
                    Projectile.velocity.Y -= 0.1f;
                }
                if (Projectile.velocity.Length() > 25f)
                {
                    Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 25f;
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            int oldDirection = Projectile.direction;
            Projectile.direction = Projectile.spriteDirection = (Projectile.velocity.X > 0f) ? 1 : -1;
            if (oldDirection != Projectile.direction)
            {
                Projectile.netUpdate = true;
            }
            float scaleBonus = MathHelper.Clamp(Projectile.localAI[0], 0f, 50f);
            Projectile.position = Projectile.Center;
            Projectile.scale = 1f + scaleBonus * 0.01f;
            Projectile.width = Projectile.height = (int)(headHitbox * Projectile.scale);
            Projectile.Center = Projectile.position;
            if (Projectile.alpha > 0)
            {
                Projectile.alpha -= 42;
                if (Projectile.alpha < 0)
                {
                    Projectile.alpha = 0;
                }
            }
        }
        /// <summary>
        /// 自绘本体：按旋转与缩放画一张头贴图（关掉原版绘制以避开帧图集逻辑）
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
        /// <summary>
        /// 在本体之上叠加发光层（Texture + "Glow" 后缀拼接，见 AGENTS.md 第 4 节）
        /// </summary>
        public override void PostDraw(Color lightColor)
        {
            Vector2 origin = new Vector2(21f, 25f);
            Main.spriteBatch.Draw(ModContent.Request<Texture2D>("CalamityDemutation/Content/Projectiles/Summon/MechwormHeadGlow").Value, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0f);
        }
    }
}
