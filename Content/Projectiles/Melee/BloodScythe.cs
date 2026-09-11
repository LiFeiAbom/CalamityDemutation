using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 血镰刀：旋转飞行的近战弹幕，命中敌人施加灼烧
    /// </summary>
    internal class BloodScythe:ModProjectile
    {
        /// <summary>
        /// 基础属性：28x28 半透明（alpha 100）碰撞箱；友方、近战伤害、撞实心块反弹、单次穿透、存活 600 帧
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.alpha = 100;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
        }
        /// <summary>
        /// 旋转飞行、产生血液拖尾粉尘，并在后期缓慢加速
        /// </summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, ((255 - Projectile.alpha) * 0.35f) / 255f, ((255 - Projectile.alpha) * 0.05f) / 255f, ((255 - Projectile.alpha) * 0.075f) / 255f);
            // 速度总和低于 16 且剩余时间不足 420 帧（进入后半程）时，每帧加速 5%，保证末端仍有杀伤力
            if (Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y) < 16f && Projectile.timeLeft < 420)
            {
                Projectile.velocity *= 1.05f;
            }
            // 旋转角随飞行速度增加，速度越快转得越快，模拟镰刀旋转
            Projectile.rotation += (Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y)) * 0.1f;
            // 1/5 概率在路径上生成一粒受速度影响的血液粉尘作拖尾
            if (Main.rand.NextBool(5))
            {
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.Blood, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f);
            }
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
        /// 自定义绘制：按旋转角绘制镰刀贴图
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
