using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Typeless
{
    /// <summary>
    /// 幽灵魔弹（GhostlyBolt） - 血炎法师头（BloodflareHornedMask）套装「魔法武器有时射出幽灵魔弹」的弹幕
    ///（按经典版灾厄 Projectiles/Typeless/GhostlyBolt.cs 1:1 移植；现代版同源、只是把贴图显式指向隐形图）。
    /// 本体不可见（透明度恒 255、无绘制代码，源与现代版皆如此），全靠尘表现：
    /// 第 6 帧播一次魔法音并喷 40 粒 GiantCursedSkullBolt 尘（源裸数字 181 已 Cecil 反查），
    /// 之后每帧再喷 3 粒带自身速度的拖尾尘；无限穿透、每敌 6 帧局部无敌、存活 900 帧、穿地形、入水不减速。
    /// 贴图沿用工程共用的隐形占位图（见工程记忆第 4 节：隐形弹幕都引 Content/Projectiles/InvisibleProj）。
    /// </summary>
    public class GhostlyBolt : ModProjectile
    {
        /// <summary>共用隐形占位贴图（本体不绘制，改用尘表现）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 基础属性：6×6 碰撞箱、友方、初始全透明、无限穿透、额外 1 次更新（更快）、
        /// 每敌 6 帧局部无敌、存活 900 帧、穿地形、入水不减速（经典版原样）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
            Projectile.timeLeft = 900;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
        }
        /// <summary>
        /// AI：第 6 帧播一次入场音并喷一圈 40 粒尘（速度 ×3 并叠加自身速度的 0.75）；
        /// 之后每帧喷 3 粒带自身速度 20% 的拖尾尘（速度 ×0.6、尺寸 ×1.4）
        /// </summary>
        public override void AI()
        {
            if (Projectile.localAI[0] == 6f)
            {
                SoundEngine.PlaySound(SoundID.Item8, Projectile.position);
                for (int i = 0; i < 40; i++)
                {
                    int cursedDust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.GiantCursedSkullBolt, 0f, 0f, 100, default(Color), 1f);
                    Main.dust[cursedDust].velocity *= 3f;
                    Main.dust[cursedDust].velocity += Projectile.velocity * 0.75f;
                    Main.dust[cursedDust].scale *= 1.2f;
                    Main.dust[cursedDust].noGravity = true;
                }
            }
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] > 6f)
            {
                for (int j = 0; j < 3; j++)
                {
                    int cursedDust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.GiantCursedSkullBolt, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default(Color), 1f);
                    Main.dust[cursedDust].velocity *= 0.6f;
                    Main.dust[cursedDust].scale *= 1.4f;
                    Main.dust[cursedDust].noGravity = true;
                }
            }
        }
    }
}
