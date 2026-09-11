using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 血刃 - 猩红风格的近战回旋刀刃弹幕
    /// 以死神镰刀（DeathSickle）为模板配置 AI 但未调用 base.AI()，实际不执行旋转/回旋行为，
    /// 仅保持生成时初速度直线飞行并发出红光；可穿透 3 个敌人，命中施加灵液并改写免疫帧。
    /// </summary>
    internal class BloodyBlade:ModProjectile
    {
        /// <summary>
        /// 基础属性：48x48 碰撞箱；借用死神镰刀 AI 模板、友方、近战伤害、可穿透 3 个敌人、存活 300 帧
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.aiStyle = ProjAIStyleID.Sickle;
            AIType = ProjectileID.DeathSickle;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 300;
        }
        /// <summary>
        /// AI：每帧在弹幕中心叠加橙红色光照（alpha 越高光越弱），无额外运动逻辑
        /// </summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, ((255 - Projectile.alpha) * 0.3f) / 255f, ((255 - Projectile.alpha) * 0.3f) / 255f, ((255 - Projectile.alpha) * 0f) / 255f);
        }
        /// <summary>
        /// 命中敌人：改写目标对该主人的免疫帧为 9，并施加灵液 debuff 60 帧
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.immune[Projectile.owner] = 9;
            target.AddBuff(BuffID.Ichor, 60);
        }
        /// <summary>
        /// 命中玩家（PvP）：施加灵液 debuff 60 帧
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Ichor, 60);
        }
        /// <summary>
        /// 自定义绘制：以弹幕中心为轴绘制整张贴图（替换默认绘制）
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
        /// <summary>
        /// 颜色/透明度：剩余时间不足 85 帧时按剩余时间线性淡出变暗（临消失警告），否则白色半透明
        /// </summary>
        public override Color? GetAlpha(Color lightColor)
        {
            if (Projectile.timeLeft < 85)
            {
                byte b2 = (byte)(Projectile.timeLeft * 3);
                byte a2 = (byte)(100f * ((float)b2 / 255f));
                return new Color((int)b2, (int)b2, (int)b2, (int)a2);
            }
            return new Color(255, 255, 255, 100);
        }
    }
}
