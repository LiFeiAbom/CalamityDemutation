using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 机械蠕虫·身体 2（MechwormBody2） - 弑神者召唤头蠕虫的第二节身体
    ///（按经典版灾厄 Projectiles/Summon/MechwormBody2.cs 1:1 移植）。
    /// 行为与 <see cref="MechwormBody"/> 完全相同，只是贴图与类型不同（源如此，两节用不同贴图）。
    /// </summary>
    public class MechwormBody2 : ModProjectile
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
        /// 基础属性：与 <see cref="MechwormBody"/> 一致（20×20、占 0.5 仆从栏、存活 18000×5 帧、局部无敌 4 帧）
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
        /// AI：与 <see cref="MechwormBody"/> 同构（钉在前一段身后并把段序回写）
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
                Main.projectile[parent].localAI[0] = Projectile.localAI[0] + 1f;
            }
            else
            {
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
