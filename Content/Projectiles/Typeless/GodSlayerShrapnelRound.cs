using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Typeless
{
    /// <summary>
    /// 弑神者破片弹（GodSlayerShrapnelRound） - 弑神者射手套装「发射远程武器时有几率射出」的追加弹
    /// （移植自经典版灾厄 Projectiles/Typeless/GodSlayerShrapnelRound.cs，由 CalamityDemutationGlobalItem.Shoot 以
    /// 5% 概率生成，伤害 = 本次射击伤害 ×2.1，穿金源套时 ×3.2）。
    /// 飞行时撒紫色尘尾，命中/超时后在残骸处炸开 4~6 枚 GodSlayerShrapnel（每枚伤害为破片弹的 30%）。
    /// </summary>
    internal class GodSlayerShrapnelRound:ModProjectile
    {
        /// <summary>基础属性：8x8 碰撞箱、友方、穿透 1、存活 300 帧、extraUpdates 1（照经典版）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 300;
        }
        /// <summary>AI：首帧音效 + 每帧 3 颗紫色尘（速度取 0.5 倍并叠加弹速的 0.1 倍）</summary>
        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item73, Projectile.position);
                Projectile.localAI[0] += 1f;
            }
            for (int i = 0; i < 3; i++)
            {
                int cosmilite = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default, 1.2f);
                Main.dust[cosmilite].noGravity = true;
                Main.dust[cosmilite].velocity *= 0.5f;
                Main.dust[cosmilite].velocity += Projectile.velocity * 0.1f;
            }
        }
        /// <summary>
        /// 消失时：在主人本机于残骸处向四周炸开 4~6 枚破片（每枚伤害 = 本弹幕 ×0.3），源写法即带 owner 判据
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner != Main.myPlayer)
                return;
            int shrapnelCount = Main.rand.Next(4, 7);
            for (int i = 0; i < shrapnelCount; i++)
            {
                Vector2 shrapnelVelocity = Main.rand.NextVector2Unit() * (Main.rand.Next(70, 101) * 0.1f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.oldPosition + new Vector2(Projectile.width / 2f, Projectile.height / 2f), shrapnelVelocity, ModContent.ProjectileType<GodSlayerShrapnel>(), (int)(Projectile.damage * 0.3), 0f, Projectile.owner);
            }
        }
    }
}
