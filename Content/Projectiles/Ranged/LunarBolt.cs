using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 能量弹（LunarBolt） - 月光弓把木箭转化出的"滑行能量弹"，＝灾厄 2.0 的
    /// <c>Projectiles/Ranged/LunarBolt.cs</c>（逐行照搬）。
    /// <para>
    /// 判定 10×10、穿透 1、<c>extraUpdates = 1</c>、寿命 180；本体自绘成青白色
    /// <c>(168, 247, 239)</c>，<c>alpha</c> 从 255 每帧 -25 淡入到 50 后就停住（半透明可见）。
    /// 尾迹是两组尘、各 5 颗、都朝速度反方向飘且无重力：206 = <see cref="DustID.UnusedWhiteBluePurple"/>
    /// （缩放 1.2）与 107 = <see cref="DustID.TerraBlade"/>（缩放 0.7）。
    /// </para>
    /// <para>
    /// 与源的一点差别：源给弹幕挂了灾厄的"贴脸射击"标记 <c>Projectile.Calamity().pointBlankShotDuration</c>——
    /// 本工程没有这套机制，按既有口径不做。撞墙**不消失**，只在第一次撞上时播一声 `Item10` 破空音（照源，
    /// 借 <c>ai[0]</c> 当"只播一次"的开关）。
    /// </para>
    /// </summary>
    internal class LunarBolt : ModProjectile
    {
        /// <summary>每组尾迹尘的颗粒数（照源 5）</summary>
        private const int TrailDustCount = 5;
        /// <summary>撞墙音是否已播过（源借 <c>ai[0]</c> 记这个状态）</summary>
        private ref float TileHitSoundFlag => ref Projectile.ai[0];
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
        /// <summary>AI：按速度摆正贴图 + 撒尾迹尘 + 淡入到 alpha 50（照源，一行不动）</summary>
        public override void AI()
        {
            // 朝向
            Projectile.spriteDirection = Projectile.direction = (Projectile.velocity.X > 0).ToDirectionInt();
            Projectile.rotation = Projectile.velocity.ToRotation() + (Projectile.spriteDirection == 1 ? 0f : MathHelper.Pi);

            // 淡入过程中一直撒尘；alpha 低于 170 后停撒（照着"越亮尾迹越少"的观感）
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

            // 淡入到 50 后停住（不继续变透明，保证远距离也看得见）
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
        /// <summary>固定青白色（照源 <c>new Color(168, 247, 239)</c>，不吃环境光）</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(168, 247, 239);
    }
}
