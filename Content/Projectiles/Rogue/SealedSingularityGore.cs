using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 封存奇点碎裂时溅出的碎片（照灾厄 2.0 的 <c>SealedSingularityGore</c>）：
    /// 25×25 判定、存活 300 帧、边转边落（重力 0.27、封顶 16）；
    /// 贴图按 <c>ai[0]</c> 在 <c>SealedSingularityGore</c> / <c>...Gore2</c> / <c>...Gore3</c> 三张里选一张
    /// （三张尺寸各不相同，故绘制时按各自尺寸取中心与原图整幅）。消失时一声挖掘音 + 4 颗烟尘。
    /// </summary>
    internal class SealedSingularityGore : ModProjectile
    {
        /// <summary>碎片贴图张数（源在弹幕侧用 ai[0] 记 0/1/2 决定用哪张）</summary>
        private const float FragmentVariants = 3f;

        /// <summary>25×25、存活 300 帧，伤害类型取盗贼</summary>
        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.width = Projectile.height = 25;
            Projectile.timeLeft = 300;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>自转 + 受重力下落（封顶 16）</summary>
        public override void AI()
        {
            Projectile.rotation += 0.6f * Projectile.direction;
            Projectile.velocity.Y += 0.27f;
            if (Projectile.velocity.Y > 16f)
                Projectile.velocity.Y = 16f;
        }
        /// <summary>消失：挖掘音 + 4 颗烟尘</summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
            for (int i = 0; i < 4; i++)
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, -Projectile.velocity.X * 0.15f, -Projectile.velocity.Y * 0.10f, 150, default, 0.9f);
        }
        /// <summary>按 ai[0] 三选一贴图绘制（三张尺寸不同，不能用单一隐式贴图）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture;
            if (Projectile.ai[0] == 1f)
                texture = ModContent.Request<Texture2D>(Texture + "2").Value;
            else if (Projectile.ai[0] >= 2f && Projectile.ai[0] < FragmentVariants)
                texture = ModContent.Request<Texture2D>(Texture + "3").Value;
            else
                texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, new Rectangle(0, 0, texture.Width, texture.Height), Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
