using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 誓剑之火：追踪敌人的近战火苗，命中/消失时迸发火光
    /// </summary>
    internal class OathswordFlame:ModProjectile
    {
        /// <summary>
        /// 基础属性：20x20 碰撞箱；近战伤害、友方、单次穿透、撞实心块销毁（原版默认行为，无反弹）、存活 240 帧；
        /// 初始完全不透明度的逆值 Opacity=0（完全透明），随 AI 渐显
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.friendly = true;
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 240;
            Projectile.Opacity = 0f;
        }
        /// <summary>
        /// 逐渐显现并追踪附近敌人
        /// </summary>
        public override void AI()
        {
            // 每帧把 Opacity 提高 0.08（封顶 1），约 13 帧（≈0.2 秒）完成渐显
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.08f, 0f, 1f);
            // 向 700 像素内的敌人平滑追踪（转向 15、惯性 10，不穿墙判定）
            Projectile.HomeInNPC(700f, 15f, 10f, null, false);
        }
        /// <summary>
        /// 命中敌人时施加灼烧 debuff
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, 180);
        }
        /// <summary>
        /// 命中玩家（PvP）时施加灼烧 debuff
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 180);
        }
        /// <summary>
        /// 消失时播放音效、放大碰撞箱并迸发粉红火光
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            Projectile.Resize(100, 100);
            for (int k = 0; k < 10; k++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.PinkFairy, Projectile.oldVelocity.X * 2.5f, Projectile.oldVelocity.Y * 2.5f);
        }
        /// <summary>
        /// 浅灰色着色，透明度由 Opacity（0~1）换算为 0~255 实现渐显
        /// </summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(200, 200, 200, (byte)(Projectile.Opacity * 255f));
        }
    }
}
