using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Typeless
{
    /// <summary>
    /// 龙蒿生命能量（TarraEnergy） - 龙蒿射手套装「远程弹幕消失时分裂出的生命能量」
    /// （移植自经典版灾厄 Projectiles/Typeless/TarraEnergy.cs，由 CalamityDemutationGlobalProjectile.OnKill 生成）。
    /// 行为照经典版：只按初速直线飞行（源用 aiStyle = 1 + PreAI 返回 false 关掉原版 AI，等价于纯直线运动），
    /// 命中一次即消失，存活 120 帧后自然消散。
    /// 视觉上本体全透明（引共用隐形贴图，与源一致），可见部分是每帧沿着速度方向拖出的 3 颗绿色尘（DustID.TerraBlade）。
    /// 与源的一处差异：经典版额外写了 <c>dust.alpha = Projectile.alpha</c>（= 255）会把尘也一并设成全透明、
    /// 结果整套特效完全不可见；现代版已删掉该行。本工程取可见的那一版（机械行为仍按经典），
    /// 若要严格复刻经典的全隐效果，只需补回那一行。
    /// </summary>
    internal class TarraEnergy:ModProjectile
    {
        /// <summary>本体不绘制，复用工程里的共用隐形占位贴图（Content/Projectiles/InvisibleProj.png）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 基础属性：8x8 碰撞箱、友方、穿透 1、全透明、存活 120 帧（2 秒）、extraUpdates 1（与源一致）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.alpha = 255;      // 本体全透明：可见部分只有下方的尘尾
            Projectile.timeLeft = 120;   // 存活 2 秒
            Projectile.extraUpdates = 1;
        }
        /// <summary>
        /// 每帧在弹体后方（沿速度反方向按 1/10 速度偏移）拖出 3 颗绿色尘；弹体本身不做任何机动（对齐源 PreAI 的尘段）
        /// </summary>
        public override void AI()
        {
            for (int i = 0; i < 3; i++)
            {
                float x2 = Projectile.position.X - Projectile.velocity.X / 10f * i;
                float y2 = Projectile.position.Y - Projectile.velocity.Y / 10f * i;
                int greenDust = Dust.NewDust(new Vector2(x2, y2), 1, 1, DustID.TerraBlade, 0f, 0f, 0, default, 1f);
                Main.dust[greenDust].position.X = x2;
                Main.dust[greenDust].position.Y = y2;
                Main.dust[greenDust].noGravity = true;
            }
        }
    }
}
