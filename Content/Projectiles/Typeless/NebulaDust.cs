using System;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Typeless
{
    /// <summary>
    /// 星云尘埃 - 星云星命中敌人时散射的辅助弹幕
    /// 本体不可见，缓慢减速并淡出；无限穿透并快速连续命中
    /// </summary>
    internal class NebulaDust:ModProjectile
    {
        /// <summary>
        /// 基础属性：32x32 全透明碰撞箱；友方、无限穿透、无视地形与水体、存活 3600 帧；
        /// 每个敌人独立 3 帧命中冷却（配合高速连击）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.alpha = 255;   // 全透明，仅靠粉尘表现
            Projectile.penetrate = -1;   // 无限穿透
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 3600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 3;   // 同一敌人每 3 帧最多受击一次
        }
        /// <summary>
        /// AI：随水平/垂直速度翻滚旋转并每帧减速 2%；前 60 帧快速淡入到 alpha 80，
        /// 60 帧后开始淡出，完全透明后由主人端销毁，实现"尘埃飘散消失"
        /// </summary>
        public override void AI()
        {
            // 依据水平速度旋转，模拟尘埃翻滚
            Projectile.rotation += Projectile.velocity.X * 0.02f;
            if (Projectile.velocity.X < 0f)
            {
                Projectile.rotation -= Math.Abs(Projectile.velocity.Y) * 0.02f;
            }
            else
            {
                Projectile.rotation += Math.Abs(Projectile.velocity.Y) * 0.02f;
            }
            Projectile.velocity *= 0.98f;   // 逐渐减速
            Projectile.ai[0] += 1f;
            // 60 帧后开始淡出，完全透明后由主人端销毁
            if (Projectile.ai[0] >= 60f)
            {
                if (Projectile.alpha < 255)
                {
                    Projectile.alpha += 5;
                    if (Projectile.alpha > 255)
                    {
                        Projectile.alpha = 255;
                        return;
                    }
                }
                else if (Projectile.owner == Main.myPlayer)
                {
                    Projectile.Kill();
                    return;
                }
            }
            // 前 60 帧快速淡入，alpha 降到 80 后保持
            else if (Projectile.alpha > 80)
            {
                Projectile.alpha -= 30;
                if (Projectile.alpha < 80)
                {
                    Projectile.alpha = 80;
                    return;
                }
            }
        }
    }
}
