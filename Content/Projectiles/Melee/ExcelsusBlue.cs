using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 宙宇波能刃的蓝刃弹幕（移植自灾厄 2.0.3.9 ExcelsusBlue）：
    /// 左键三弹幕中的蓝刀，与主刃同构，仅速度阈值（16）与尘土色（蓝）不同，命中施加弑神炼狱。
    /// </summary>
    internal class ExcelsusBlue : ModProjectile
    {
        /// <summary>基础属性：34×34、近战、穿透 3、存活 300 帧、初始透明度 100、本地免疫 10 帧</summary>
        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 300;
            Projectile.alpha = 100;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }
        /// <summary>速度低于 16 时加速、寿命最后 85 帧减速，旋转随速度，随机喷蓝尘土</summary>
        public override void AI()
        {
            if (Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y) < 16f && Projectile.timeLeft > 85)
                Projectile.velocity *= 1.05f;
            if (Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y) > 0f && Projectile.timeLeft <= 85)
                Projectile.velocity *= 0.98f;
            Projectile.rotation += (Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y)) * 0.02f;
            if (Main.rand.NextBool(8))
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.BlueFairy, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f);
        }
        /// <summary>撞墙后不再撞物块，寿命压到 85 帧进入收缩阶段</summary>
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.tileCollide = false;
            if (Projectile.timeLeft > 85)
                Projectile.timeLeft = 85;
            return false;
        }
        /// <summary>收缩阶段按剩余寿命线性淡出</summary>
        public override Color? GetAlpha(Color lightColor)
        {
            if (Projectile.timeLeft < 85)
            {
                byte b2 = (byte)(Projectile.timeLeft * 3);
                byte a2 = (byte)(100f * (b2 / 255f));
                return new Color(b2, b2, b2, a2);
            }
            return default(Color);
        }
        /// <summary>主贴图绘制</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
        /// <summary>叠加 Glow 发光贴图</summary>
        public override void PostDraw(Color lightColor)
        {
            Color color;
            if (Projectile.timeLeft < 85)
            {
                byte b2 = (byte)(Projectile.timeLeft * 3);
                byte a2 = (byte)(100f * (b2 / 255f));
                color = new Color(b2, b2, b2, a2);
            }
            else
                color = new Color(255, 255, 255, 100);
            Vector2 origin = new Vector2(39f, 46f);
            Main.EntitySpriteDraw(ModContent.Request<Texture2D>("CalamityDemutation/Content/Projectiles/Melee/ExcelsusBlueGlow").Value, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
        }
        /// <summary>命中后压到收缩阶段并施加弑神炼狱（找不到退回诅咒狱火）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.timeLeft > 85)
                Projectile.timeLeft = 85;
            CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "GodSlayerInferno", 180, BuffID.CursedInferno);
        }
    }
}
