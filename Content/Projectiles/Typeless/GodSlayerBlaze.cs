using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Typeless
{
    /// <summary>
    /// 弑神者烈焰（GodSlayerBlaze） - 弑神者法师头（GodSlayerVisage）套装「受到伤害时释放魔法弑神爆炸」的弹幕
    ///（按经典版灾厄 Projectiles/Typeless/GodSlayerBlaze.cs 1:1 移植）。
    /// 250×250 的大判定框、无限穿透、每敌 5 帧局部无敌、存活 60 帧；
    /// ai[0] 在这里被源当作**半径累加器**（每帧 +4），随半径把每帧喷出的 ShadowbeamStaff 尘数量
    /// 从 25 递减到 0，半径超过 230 时自毁——所以实际寿命约 58 帧。
    /// 源里那两段 comparing `position` 与 ai[0]/ai[1] 的 flag 判据只在弹幕带速度时才有意义，
    /// 而本弹幕唯一的生成路径速度恒为 0，故永不触发（照搬保留，仅加此说明）。
    /// 命中敌人附加 500 帧弑神者地狱火。
    /// </summary>
    public class GodSlayerBlaze : ModProjectile
    {
        /// <summary>
        /// 基础属性：250×250 碰撞箱、友方、无限穿透、穿地形、存活 60 帧、
        /// 每敌 5 帧局部无敌（经典版原样）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 250;
            Projectile.height = 250;
            Projectile.friendly = true;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 5;
        }
        /// <summary>
        /// AI：紫色照明；半径累加 + 递减的尘量；半径超过 230 时自毁
        /// </summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.5f / 255f, (255 - Projectile.alpha) * 0f / 255f, (255 - Projectile.alpha) * 0.75f / 255f);
            // 源的 flag 判据（velocity 为 0 时永不成立，见类注释）
            bool passedX = false;
            bool passedY = false;
            if (Projectile.velocity.X < 0f && Projectile.position.X < Projectile.ai[0])
            {
                passedX = true;
            }
            if (Projectile.velocity.X > 0f && Projectile.position.X > Projectile.ai[0])
            {
                passedX = true;
            }
            if (Projectile.velocity.Y < 0f && Projectile.position.Y < Projectile.ai[1])
            {
                passedY = true;
            }
            if (Projectile.velocity.Y > 0f && Projectile.position.Y > Projectile.ai[1])
            {
                passedY = true;
            }
            if (passedX && passedY)
            {
                Projectile.Kill();
            }
            float dustAmount = 25f;
            if (Projectile.ai[0] > 180f)
            {
                dustAmount -= (Projectile.ai[0] - 180f) / 2f;
            }
            if (dustAmount <= 0f)
            {
                dustAmount = 0f;
                Projectile.Kill();
            }
            dustAmount *= 0.7f;
            Projectile.ai[0] += 4f;   // 源把 ai[0] 当半径累加器用
            int dustCount = 0;
            while (dustCount < dustAmount)
            {
                float dustVelX = Main.rand.Next(-30, 31);
                float dustVelY = Main.rand.Next(-30, 31);
                float dustSpeed = Main.rand.Next(9, 27);
                float dustDist = (float)Math.Sqrt(dustVelX * dustVelX + dustVelY * dustVelY);
                dustDist = dustSpeed / dustDist;
                dustVelX *= dustDist;
                dustVelY *= dustDist;
                int blazeDust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default(Color), 1.5f);
                Main.dust[blazeDust].noGravity = true;
                Main.dust[blazeDust].position.X = Projectile.Center.X;
                Main.dust[blazeDust].position.Y = Projectile.Center.Y;
                Main.dust[blazeDust].position.X += Main.rand.Next(-10, 11);
                Main.dust[blazeDust].position.Y += Main.rand.Next(-10, 11);
                Main.dust[blazeDust].velocity.X = dustVelX;
                Main.dust[blazeDust].velocity.Y = dustVelY;
                dustCount++;
            }
        }
        /// <summary>
        /// 命中敌人：附加 500 帧弑神者地狱火（按现代/经典两版分别查找，未安装则不生效）
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "GodSlayerInferno", 500);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "GodSlayerInferno", 500);
        }
        /// <summary>
        /// PvP 命中：与 OnHitNPC 同构，给玩家挂 500 帧弑神者地狱火
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "GodSlayerInferno", 500);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "GodSlayerInferno", 500);
        }
    }
}
