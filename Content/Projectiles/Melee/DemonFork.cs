using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 恶魔叉：高速飞行的近战弹幕，带暗影火焰拖尾，命中施加暗影焰与灼烧
    /// </summary>
    internal class DemonFork:ModProjectile
    {
        /// <summary>
        /// 静态属性：预留 8 格残影缓存并启用残影绘制（配合 PreDraw）
        /// </summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        }
        /// <summary>
        /// 基础属性：30x30 碰撞箱；友方、单次穿透、每帧额外更新 5 次（高速）、无视地形、近战伤害、
        /// 初始半透明（alpha 180）、存活 300 帧
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 5;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.alpha = 180;
            Projectile.timeLeft = 300;
        }
        /// <summary>
        /// 生成随机暗影系拖尾粉尘，并让弹幕朝向飞行方向
        /// </summary>
        public override void AI()
        {
            // 从三种暗影系粉尘（暗焰/星云紫/影火）中随机选一种作拖尾
            int dustType = Utils.SelectRandom(Main.rand, new int[]
            {
                173,
                235,
                172
            });
            // 坐标出现 NaN 时直接销毁，防止弹幕卡死
            if (Projectile.position.HasNaNs())
            {
                Projectile.Kill();
                return;
            }
            // 检测脚下是否压在实心块上（若在块内则关闭粉尘受光，避免在墙里过亮）
            bool tileCheck = WorldGen.SolidTile(Framing.GetTileSafely((int)Projectile.position.X / 16, (int)Projectile.position.Y / 16));
            Dust dust = Main.dust[Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, 0f, 0f, 0, default, 1f)];
            dust.position = Projectile.Center;
            dust.velocity = Vector2.Zero;
            dust.noGravity = true;
            if (tileCheck)
                dust.noLight = true;
            // 让贴图朝向飞行方向，并补 45° 使叉子图形看起来"正对"目标
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(45);
        }
        /// <summary>
        /// 命中敌人时施加暗影焰与灼烧 debuff
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.ShadowFlame, 90);
            target.AddBuff(BuffID.OnFire, 180);
        }
        /// <summary>
        /// 命中玩家（PvP）时施加暗影焰与灼烧 debuff
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.ShadowFlame, 90);
            target.AddBuff(BuffID.OnFire, 180);
        }
        /// <summary>
        /// 消失时播放音效并迸发多圈暗影系粉尘
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            bool tileCheck = WorldGen.SolidTile(Framing.GetTileSafely((int)Projectile.position.X / 16, (int)Projectile.position.Y / 16));
            // 第一轮：随机生成 4 粒普通暗影粉尘（受重力，默认光照）
            for (int m = 0; m < 4; m++)
            {
                int dustType = Utils.SelectRandom(Main.rand, new int[]
                {
                    173,
                    235,
                    172
                });
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, 0f, 0f, 100, default, 1.5f);
            }
            // 第二轮：生成大号无重力爆尘与小号喷尘，模拟能量迸裂
            for (int n = 0; n < 4; n++)
            {
                int dustType = Utils.SelectRandom(Main.rand, new int[]
                {
                    173,
                    DustID.LifeDrain,
                    172
                });
                int dustInt = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, 0f, 0f, 0, default, 2.5f);
                Main.dust[dustInt].noGravity = true;
                Main.dust[dustInt].velocity *= 3f;
                if (tileCheck)
                {
                    Main.dust[dustInt].noLight = true;
                }
                dustInt = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, 0f, 0f, 100, default, 1.5f);
                Main.dust[dustInt].velocity *= 2f;
                Main.dust[dustInt].noGravity = true;
                if (tileCheck)
                {
                    Main.dust[dustInt].noLight = true;
                }
            }
        }
        /// <summary>
        /// 绘制残影拖尾
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            CDUtil.DrawAfterimages(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
        /// <summary>
        /// 浅灰色着色，透明度跟随 alpha（半透明质感）
        /// </summary>
        public override Color? GetAlpha(Color lightColor) => new Color(200, 200, 200, Projectile.alpha);
    }
}
