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
    /// 机械蠕虫·身体（MechwormBody） - 弑神者召唤头蠕虫的第一节身体
    ///（按经典版灾厄 Projectiles/Summon/MechwormBody.cs 1:1 移植）。
    /// 每节身体都把自己钉在"前一段"（ai[0] 指向的弹幕）身后 16×缩放 像素处，并把段序 localAI[0]+1 回写给前一段，
    /// 于是从尾巴节往前逐节递增，头部的缩放随之变大（见 MechwormHead）。本类占 0.5 仆从栏。
    /// </summary>
    public class MechwormBody : ModProjectile
    {
        /// <summary>创建时的仆从栏上限，用于在玩家降低上限时自杀</summary>
        private int playerMinionSlots = 0;
        /// <summary>首帧闩锁：初始化 playerMinionSlots</summary>
        private bool runCheck = true;
        /// <summary>注册为可牺牲的召唤物</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
        }
        /// <summary>
        /// 基础属性：20×20；友方、入水不减速、初始全透明（淡入）、占 0.5 仆从栏、
        /// 存活 18000 帧（×5 后为 90000）、无限穿透、穿地形、召唤物、局部无敌 4 帧
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.minionSlots = 0.5f;
            Projectile.timeLeft = 18000;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft *= 5;
            Projectile.minion = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;
        }
        /// <summary>
        /// AI：淡淡投光；维持 playerMinionSlots 检查；玩家死亡或仆从栏上限被下调时自毁；
        /// 随后钉在前一段（ai[0]）身后，并把段序回写给前一段
        /// </summary>
        public override void AI()
        {
            Lighting.AddLight((int)((Projectile.position.X + Projectile.width / 2) / 16f), (int)((Projectile.position.Y + Projectile.height / 2) / 16f), 0.15f, 0.01f, 0.15f);
            Player player = Main.player[Projectile.owner];
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
            if ((int)Main.time % 120 == 0)
            {
                Projectile.netUpdate = true;
            }
            if (!player.active || player.maxMinions < playerMinionSlots)
            {
                Projectile.active = false;
                return;
            }
            const int bodyHitbox = 10;
            if (player.dead)
            {
                modPlayer.mWorm = false;
            }
            if (modPlayer.mWorm)
            {
                Projectile.timeLeft = 2;
            }
            if (Projectile.ai[1] == 1f)
            {
                Projectile.ai[1] = 0f;
                Projectile.netUpdate = true;
            }
            int parent = (int)Projectile.ai[0];
            Vector2 parentCenter;
            float parentRotation;
            float parentScale;
            if (parent >= 0 && Main.projectile[parent].active)
            {
                parentCenter = Main.projectile[parent].Center;
                parentRotation = Main.projectile[parent].rotation;
                parentScale = MathHelper.Clamp(Main.projectile[parent].scale, 0f, 50f);
                Main.projectile[parent].localAI[0] = Projectile.localAI[0] + 1f;   // 段序回写：让前一段的缩放递增
            }
            else
            {
                // 前一段不在（源同样只是返回，不自杀）
                return;
            }
            Projectile.alpha -= 42;
            if (Projectile.alpha < 0)
            {
                Projectile.alpha = 0;
            }
            Projectile.velocity = Vector2.Zero;
            Vector2 toParent = parentCenter - Projectile.Center;
            if (parentRotation != Projectile.rotation)
            {
                float angleDelta = MathHelper.WrapAngle(parentRotation - Projectile.rotation);
                toParent = toParent.RotatedBy(angleDelta * 0.1f);
            }
            Projectile.rotation = toParent.ToRotation() + MathHelper.PiOver2;
            Projectile.position = Projectile.Center;
            Projectile.scale = parentScale;
            Projectile.width = Projectile.height = (int)(bodyHitbox * Projectile.scale);
            Projectile.Center = Projectile.position;
            if (toParent != Vector2.Zero)
            {
                Projectile.Center = parentCenter - Vector2.Normalize(toParent) * 16f * parentScale;
            }
            Projectile.spriteDirection = (toParent.X > 0f) ? 1 : -1;
        }
        /// <summary>
        /// 自绘本体：按旋转与缩放画一张身体贴图
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
