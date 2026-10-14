using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 能量弹·强（LunarBolt2） - 宇宙弩炮把木箭转化出的能量弹，＝灾厄 2.0 的
    /// <c>Projectiles/Ranged/LunarBolt2.cs</c>（逐行照搬）。
    /// <para>
    /// 与 <see cref="LunarBolt"/> 几乎同构：10×10 判定、穿透 1、<c>extraUpdates = 1</c>、寿命 180、
    /// alpha 255→50、两组尘尾迹（<see cref="DustID.UnusedWhiteBluePurple"/> + <see cref="DustID.TerraBlade"/>）、
    /// 撞墙不死只响一声 `Item10`。两点差别：① **贴图直接复用 <c>LunarBolt.png</c>**（源就是
    /// <c>Texture => ".../Ranged/LunarBolt"</c>，故本工程不另拷贴图）；② 自绘色是
    /// <c>(168, 247, 193)</c>，比能量弹的 <c>(168, 247, 239)</c> 偏绿一点。
    /// </para>
    /// <para>
    /// 与源的一点差别：源的 <c>pointBlankShotDuration</c>（灾厄贴脸射击）本工程不做。
    /// </para>
    /// </summary>
    internal class LunarBolt2 : ModProjectile
    {
        /// <summary>每组尾迹尘的颗粒数（照源 5）</summary>
        private const int TrailDustCount = 5;
        /// <summary>撞墙音是否已播过（源借 <c>ai[0]</c> 记这个状态）</summary>
        private ref float TileHitSoundFlag => ref Projectile.ai[0];
        /// <summary>复用能量弹的贴图（照源写法）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/Ranged/LunarBolt";
        /// <summary>
        /// 基础属性：10×10 判定、穿透 1、额外更新 1 帧、寿命 180 帧、射手伤害类型、初始全透明（照源）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.alpha = 255;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 180;
        }
        /// <summary>AI：按速度摆正贴图 + 撒尾迹尘 + 淡入到 alpha 50（照源）</summary>
        public override void AI()
        {
            Projectile.spriteDirection = Projectile.direction = (Projectile.velocity.X > 0).ToDirectionInt();
            Projectile.rotation = Projectile.velocity.ToRotation() + (Projectile.spriteDirection == 1 ? 0f : MathHelper.Pi);

            if (Projectile.alpha < 170)
            {
                Vector2 dustVelocity = -Projectile.velocity * 0.5f;
                for (int i = 0; i < TrailDustCount; i++)
                {
                    int index = Dust.NewDust(Projectile.Center, 1, 1, DustID.UnusedWhiteBluePurple, 0f, 0f, 0, default, 1.2f);
                    Main.dust[index].alpha = Projectile.alpha;
                    Main.dust[index].velocity = dustVelocity;
                    Main.dust[index].noGravity = true;
                }
                for (int i = 0; i < TrailDustCount; i++)
                {
                    int index = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.TerraBlade, 0f, 0f, 0, default, 0.7f);
                    Main.dust[index].alpha = Projectile.alpha;
                    Main.dust[index].velocity = dustVelocity;
                    Main.dust[index].noGravity = true;
                }
            }

            if (Projectile.alpha > 50)
                Projectile.alpha -= 25;
            if (Projectile.alpha < 50)
                Projectile.alpha = 50;
        }
        /// <summary>撞墙：砸一次瓦片尘、播一声 `Item10`（只播一次），然后**继续飞**（返回 false）——照源</summary>
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (TileHitSoundFlag == 0f)
            {
                Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
                SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
                TileHitSoundFlag++;
            }
            return false;
        }
        /// <summary>固定青绿色（照源 <c>new Color(168, 247, 193)</c>，比能量弹偏绿）</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(168, 247, 193);
    }
}
