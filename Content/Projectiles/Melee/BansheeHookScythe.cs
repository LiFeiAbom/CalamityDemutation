using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 哀怨之镰（BansheeHookScythe） - 女妖之爪重制版的散射镰刀。
    /// 源为灾厄本体同名弹幕，重制版把命中后的行为改成"逐步减速 + 每命中一次自身伤害 -25"，
    /// 并在剩余时间不足 65 帧时转为索敌追踪（本体只会沿固定曲线飞）。
    /// </summary>
    internal class BansheeHookScythe : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;      // 记录位置与旋转，供拖尾使用
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
        }
        public override void SetDefaults()
        {
            Projectile.width = 38;
            Projectile.height = 38;
            Projectile.scale = 1.5f;
            Projectile.alpha = 100;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;                     // 无限穿透，靠伤害递减控制强度
            Projectile.timeLeft = 90;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }
        /// <summary>
        /// 每帧自转，并在剩余时间不足 65 帧后转向最近的敌怪（600 像素内、最高 15 像素/帧）
        /// </summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.6f / 255f, 0f, 0f);
            Projectile.ai[0] += MathHelper.ToRadians(35);
            NPC target = CDUtil.FindClosestNPC(Projectile.Center, 600f);
            if (Projectile.timeLeft < 65 && target != null)
            {
                Vector2 toTarget = Projectile.Center.To(target.Center).UnitVector();
                Projectile.rotation = Projectile.rotation.RotTowards(toTarget.ToRotation(), 0.07f);
                Projectile.velocity = Projectile.rotation.ToRotationVector2() * 15;
            }
        }
        /// <summary>
        /// 命中后减速并削弱自身伤害（重制版新增，越砍越轻）
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.velocity *= 0.95f;
            Projectile.damage -= 25;
        }
        public override Color? GetAlpha(Color lightColor)
        {
            if (Projectile.timeLeft < 85)
            {
                byte b = (byte)(Projectile.timeLeft * 3);
                byte alpha = (byte)(100f * (b / 255f));
                return new Color(b, b, b, alpha);
            }
            return new Color(255, 255, 255, 100);
        }
        /// <summary>
        /// 本体（红）与金色描边混合绘制，并叠 8 层拖尾残影
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            float alp = Projectile.timeLeft / 30f;
            if (alp > 1) alp = 1;
            Color color = Color.Lerp(Color.Red, Projectile.GetAlpha(Color.Gold), 0.7f);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, color * alp, Projectile.ai[0],
                texture.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float alp2 = 1 - i / (float)Projectile.oldPos.Length;
                float slp = 1 - i / (float)Projectile.oldPos.Length * 0.5f;
                Main.EntitySpriteDraw(texture, Projectile.oldPos[i] + Projectile.Center - Projectile.position - Main.screenPosition,
                    null, color * alp * alp2 * 0.5f, Projectile.ai[0], texture.Size() / 2f,
                    Projectile.scale * slp, SpriteEffects.None, 0);
            }
            return false;
        }
    }
}
