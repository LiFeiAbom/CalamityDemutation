using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon.Umbrella
{
    /// <summary>
    /// 魔法球棒（MagicBat，按 CI 的 MagicBatOld 移植；类名去掉 Old 后缀）——
    /// 由青色魔法伞（<see cref="MagicUmbrella"/>）命中时从画面上方砸下的追加打击。
    /// 20×20、穿透 1、穿地形、存活 150 帧，唯一的 AI 是让贴图顺着飞行方向旋转 45 度。
    /// </summary>
    /// <remarks>
    /// 两点照源：① 伤害类型被源写成了盗贼（RogueDamageClass，应是当年的复制粘贴遗留），
    /// 本工程按既有口径用 CDUtil.GetRogueDamageClass()（软依赖；拿不到时退回 Throwing）；
    /// ② 源的 PreDraw 只是手动把贴图按 GetAlpha 再画一遍，而 tML 的默认绘制本就会应用 GetAlpha，
    /// 故本工程省掉那段重复绘制。
    /// </remarks>
    internal class MagicBat:ModProjectile
    {
        /// <summary>标记为仆从射弹（源原样）</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
        }
        /// <summary>基础属性（源原样）：20×20、穿透 1、穿地形、不受水影响、150 帧、伤害类型盗贼</summary>
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 150;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>AI：贴图沿速度方向旋转 45 度（源原样）</summary>
        public override void AI()
        {
            Projectile.rotation = (float)Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X) + 0.785f;
        }
        /// <summary>绘制色固定为草绿（源原样）</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(80, 200, 120, Projectile.alpha);
        /// <summary>消亡：喷 10 粒冰杖尘（源原样）</summary>
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 speed = new Vector2(Main.rand.NextFloat(-7f, 7f), Main.rand.NextFloat(-7f, 7f));
                int dust = Dust.NewDust(Projectile.Center, 1, 1, DustID.IceRod, speed.X, speed.Y, 50, default, 1.2f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
