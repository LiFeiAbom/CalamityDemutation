using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 天罚蓄力大招（NemesisAlt，移植自 CalamityEntropy 的 <c>Nemesis/NemesisAlt.cs</c>）：
    /// 254×254、穿透 -1、extraUpdates 2、存活 180 帧，出生 60 帧后开始缓慢放大（每帧 +0.004）。
    /// 命中时以自身速度为中心洒 60 颗红/橙尘并**逐次衰减伤害**（每次 ×0.9）。
    /// <para>
    /// 与 CE 的差异：① 描边改调本工程的 <see cref="CDUtil.DrawRotatingMarginEffect"/>（InnoVault 那套的移植版）；
    /// ② 拖影与本体绘制照抄源。
    /// </para>
    /// </summary>
    internal class NemesisAlt:ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 1;
        }
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 254;
            Projectile.timeLeft = 180;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 2;
            Projectile.tileCollide = false;
        }
        /// <summary>朝向跟随速度；出生 60 帧后开始缓慢放大（照抄源的 <c>ai[0]</c> 计时）</summary>
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (++Projectile.ai[0] > 60)
            {
                Projectile.scale += 0.004f;
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            float thirdDustScale = Main.rand.NextFloat(2, 4);
            Vector2 dustRotation = (target.rotation - MathHelper.PiOver2).ToRotationVector2();
            Vector2 dustVelocity = dustRotation * target.velocity.Length() / 6;
            _ = SoundEngine.PlaySound(SoundID.Item14, target.Center);
            for (int j = 0; j < 60; j++)
            {
                thirdDustScale = Main.rand.NextFloat(2, 4);
                bool noGvk = true;
                int dustId = DustID.InfernoFork;
                if (Main.rand.NextBool(2))
                {
                    thirdDustScale /= 6f;
                    noGvk = false;
                    dustId = DustID.FireworkFountain_Red;
                }
                int contactDust2 = Dust.NewDust(new Vector2(target.position.X, target.position.Y), target.width
                    , target.height, dustId, 0f, 0f, 0, default, thirdDustScale);
                Dust dust = Main.dust[contactDust2];
                dust.position = target.Center + Vector2.UnitX.RotatedByRandom(MathHelper.Pi)
                    .RotatedBy(target.velocity.ToRotation()) * target.width / 3f * Main.rand.NextFloat();
                dust.noGravity = noGvk;
                dust.velocity = Projectile.velocity;
                dust.velocity *= 1.5f;
                dust.velocity += dustVelocity * (0.6f + 13.6f * Main.rand.NextFloat());
            }
            Projectile.damage = (int)(Projectile.damage * 0.9f);
        }
        /// <summary>绘制：先按 oldPos 铺拖影（品红渐隐），再叠金色旋转描边光圈，最后画本体</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawOrigin = texture.Size() / 2;
            for (int k = 0; k < Projectile.oldPos.Length; k++)
            {
                Vector2 offsetPos = Projectile.oldPos[k].To(Projectile.position);
                Vector2 drawPos = Projectile.Center - Main.screenPosition - offsetPos;
                Color color = Projectile.GetAlpha(Color.Pink) * ((Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length);
                Main.EntitySpriteDraw(texture, drawPos, null, color, Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None, 0);
            }
            CDUtil.DrawRotatingMarginEffect(Main.spriteBatch, texture, Projectile.timeLeft, Projectile.Center - Main.screenPosition
                , null, Color.Gold, Projectile.rotation, drawOrigin, Projectile.scale * 1.05f, 0);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White
                , Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
