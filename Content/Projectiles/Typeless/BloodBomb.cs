using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Typeless
{
    /// <summary>
    /// 血液爆炸光球（BloodBomb） - 血炎射手套装的「远程武器有几率射出」效果
    /// （移植自经典版灾厄 Projectiles/Typeless/BloodBomb.cs，由 CalamityDemutationGlobalItem.Shoot 追加生成，
    /// 伤害 = 本次射击伤害 ×1.6，穿上金源套（auricSet）时 ×2.2）。
    /// 飞行途中撒血尘，命中或超时后在原地生成 BloodBombExplosion。
    /// </summary>
    internal class BloodBomb:ModProjectile
    {
        /// <summary>基础属性：20x20 碰撞箱、友方、穿透 1、存活 600 帧（照经典版）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 600;
        }
        /// <summary>AI：红光 + 首帧音效 + 每帧 3 颗血尘（速度取 0.5 倍并叠加弹速的 0.1 倍）</summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.4f / 255f, 0f, 0f);
            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item73, Projectile.position);
                Projectile.localAI[0] += 1f;
            }
            for (int i = 0; i < 3; i++)
            {
                int bloody = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Blood, 0f, 0f, 100, default, 1.2f);
                Main.dust[bloody].noGravity = true;
                Main.dust[bloody].velocity *= 0.5f;
                Main.dust[bloody].velocity += Projectile.velocity * 0.1f;
            }
        }
        /// <summary>消失时在主人本机生成爆炸弹幕（伤害/击退继承，经典版写法即带 owner 判据）</summary>
        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<BloodBombExplosion>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
        }
    }
}
