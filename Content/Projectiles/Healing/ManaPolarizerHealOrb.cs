using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Healing
{
    /// <summary>
    /// 魔能谐振仪治疗球（ManaPolarizerHealOrb）—— 按灾厄 2.0.3.9 的
    /// <c>Projectiles/Healing/ManaPolarizerHealOrb</c> 移植，由
    /// <c>CalamityDemutationGlobalProjectile.OnHitNPC</c> 在 manaOverloader 触发魔法吸血时生成，
    /// 参数 ai[0] = 目标玩家编号、ai[1] = 本次应回复的生命值。
    /// 4×4、穿透 1、穿地形、存活 180 帧、额外 3 次更新；朝目标玩家以速度 3 平滑追踪
    /// （源走 <c>HealingProjectile((int)ai[1], (int)ai[0], 3f, 15f)</c>，本工程照同目录 GodSlayerHealOrb / SilvaOrb
    /// 的既有写法把它展开：50 像素内且与玩家碰撞箱重叠即回血并销毁，治疗与同步只在弹幕主人端执行，
    /// 主人处于吸血减益（moonLeech）时跳过）。
    /// 每帧喷 1 粒幽魂法杖尘（源裸数字 175 经 Cecil 反查即 <c>DustID.SpectreStaff</c>），缩放 1.3。
    /// <para>
    /// 与源的差异：源里那句 <c>player.lifeMagnet</c> 加速 1.5 倍没有保留——本工程其余 5 个治疗球
    /// （SilvaOrb / AuricOrb / GodSlayerHealOrb / CactusHealOrb / FungalHeal）同样都没带，这里与它们保持一致。
    /// </para>
    /// </summary>
    public class ManaPolarizerHealOrb : ModProjectile
    {
        /// <summary>隐形贴图：视觉效果完全由粉尘承担</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 基础属性：4×4 碰撞箱、友方、单次穿透、穿地形、存活 180 帧、额外 3 次更新（照 2.0.3.9）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 180;
            Projectile.extraUpdates = 3;
        }
        /// <summary>
        /// AI：朝 ai[0] 指定的玩家平滑追踪（速度上限 3）；进入 50 像素且与目标碰撞箱重叠时为其回复 ai[1] 点生命并销毁；
        /// 每帧在原地喷 1 粒幽魂法杖尘
        /// </summary>
        public override void AI()
        {
            int targetPlayer = (int)Projectile.ai[0];   // ai[0]：目标玩家索引（由生成方写入）
            const float speed = 3f;                     // 追踪飞行速度（照源的 homingVelocity = 3f）
            Vector2 center = new Vector2(Projectile.position.X + Projectile.width * 0.5f, Projectile.position.Y + Projectile.height * 0.5f);
            float dirX = Main.player[targetPlayer].Center.X - center.X;
            float dirY = Main.player[targetPlayer].Center.Y - center.Y;
            float dist = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
            // 进入 50 像素且与目标碰撞箱重叠：治疗并消失
            if (dist < 50f && Projectile.position.X < Main.player[targetPlayer].position.X + Main.player[targetPlayer].width && Projectile.position.X + Projectile.width > Main.player[targetPlayer].position.X && Projectile.position.Y < Main.player[targetPlayer].position.Y + Main.player[targetPlayer].height && Projectile.position.Y + Projectile.height > Main.player[targetPlayer].position.Y)
            {
                // 仅主人端执行治疗，且主人处于月亮吸血诅咒（moonLeech）时跳过，避免联机重复触发
                if (Projectile.owner == Main.myPlayer && !Main.LocalPlayer.moonLeech)
                {
                    int healAmount = (int)Projectile.ai[1];   // ai[1]：治疗量
                    Main.player[targetPlayer].HealEffect(healAmount, false);
                    Main.player[targetPlayer].statLife += healAmount;
                    if (Main.player[targetPlayer].statLife > Main.player[targetPlayer].statLifeMax2)
                    {
                        Main.player[targetPlayer].statLife = Main.player[targetPlayer].statLifeMax2;
                    }
                    NetMessage.SendData(MessageID.SpiritHeal, -1, -1, null, targetPlayer, healAmount, 0f, 0f, 0, 0, 0);
                }
                Projectile.Kill();
            }
            // 朝目标加速：每帧把速度按 15/16 衰减后叠加目标方向的分量（照 2.0.3.9 的 HealingProjectile，N = 15）
            dist = speed / dist;
            dirX *= dist;
            dirY *= dist;
            Projectile.velocity.X = (Projectile.velocity.X * 15f + dirX) / 16f;
            Projectile.velocity.Y = (Projectile.velocity.Y * 15f + dirY) / 16f;
            // 每帧 1 粒幽魂法杖尘（照 2.0.3.9：缩放 1.3，且只做位置偏移、不把速度清零）
            int healDust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.SpectreStaff, 0f, 0f, 100, default(Color), 1.3f);
            Main.dust[healDust].noGravity = true;
            Main.dust[healDust].position.X -= Projectile.velocity.X * 0.2f;
            Main.dust[healDust].position.Y += Projectile.velocity.Y * 0.2f;
        }
    }
}
