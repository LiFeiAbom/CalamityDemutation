using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 破坏者掷出的黑焦油瓶（照灾厄 2.0 的 <c>TotalityFlask</c>）：
    /// 20×20 判定、穿透 1、存活 180 帧、走原版燃烧瓶 AI（<c>ProjAIStyleID.MolotovCocktail</c>）；
    /// 贴图直接借用物品贴图（源就是这么写的）。
    /// <para>
    /// 潜行打击的那一瓶：每 20 帧在当前位置滴一团焦油（伤害 ×0.6）。
    /// 落地碎裂：玻璃碎裂音 + 生成 <see cref="TotalMeltdown"/> 本体爆炸（全额伤害，中心对齐）+
    /// 2~3 团焦油（伤害 ×0.3）+ 三层火尘。
    /// </para>
    /// <para>所有弹幕生成只在主人端做（第 5 节口径；源在 <c>Kill</c> 里已经这么写了，照抄）。</para>
    /// </summary>
    internal class TotalityFlask : ModProjectile
    {
        /// <summary>潜行打击滴焦油的间隔（帧）</summary>
        private const int StealthLeakInterval = 20;
        /// <summary>火尘类型（源里是裸数字 6）</summary>
        private const int FireDust = 6;

        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Rogue/TotalityBreakers";

        /// <summary>20×20、穿透 1、存活 180 帧，走原版燃烧瓶 AI，伤害类型取盗贼</summary>
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.aiStyle = ProjAIStyleID.MolotovCocktail;
            Projectile.timeLeft = 180;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>潜行弹每 20 帧滴一团焦油；同时每帧在瓶口位置冒一颗小火尘</summary>
        public override void AI()
        {
            if (CDUtil.IsStealthStrike(Projectile, out _) && Projectile.timeLeft % StealthLeakInterval == 0 && Projectile.owner == Main.myPlayer)
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<TotalityTar>(), (int)(Projectile.damage * 0.6f), Projectile.knockBack, Projectile.owner, 0f, 0f);
            Vector2 spinningPoint = new Vector2(4f, -8f);
            if (Projectile.direction == -1)
                spinningPoint.X = -4f;
            Vector2 offset = spinningPoint.RotatedBy(Projectile.rotation, new Vector2());
            int dust = Dust.NewDust(Projectile.Center + offset - Vector2.One * 5f, 4, 4, FireDust, 0f, 0f, 0, new Color(), 1f);
            Main.dust[dust].scale = 1.5f;
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity = Main.dust[dust].velocity * 0.25f + Vector2.Normalize(offset) * 1f;
            Main.dust[dust].velocity = Main.dust[dust].velocity.RotatedBy(-MathHelper.PiOver2 * Projectile.direction, new Vector2());
        }
        /// <summary>碎裂：玻璃碎响 + 熔毁爆炸（中心对齐，源特意补了那一行）+ 2~3 团焦油 + 火尘</summary>
        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner != Main.myPlayer)
                return;
            SoundEngine.PlaySound(SoundID.Shatter, Projectile.position);
            int meltdown = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<TotalMeltdown>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, 0f);
            Main.projectile[meltdown].Center = Projectile.Center;
            Vector2 size = new Vector2(20f, 20f);
            for (int i = 0; i < 5; i++)
                Dust.NewDust(Projectile.Center - size / 2f, (int)size.X, (int)size.Y, DustID.SpookyWood, 0f, 0f, 0, Color.Red, 1f);
            for (int i = 0; i < 10; i++)
                Main.dust[Dust.NewDust(Projectile.Center - size / 2f, (int)size.X, (int)size.Y, DustID.Smoke, 0f, 0f, 100, new Color(), 1.5f)].velocity *= 1.4f;
            for (int i = 0; i < 20; i++)
            {
                int dust = Dust.NewDust(Projectile.Center - size / 2f, (int)size.X, (int)size.Y, FireDust, 0f, 0f, 100, new Color(), 2.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 5f;
                dust = Dust.NewDust(Projectile.Center - size / 2f, (int)size.X, (int)size.Y, FireDust, 0f, 0f, 100, new Color(), 1.5f);
                Main.dust[dust].velocity *= 3f;
            }
            int tarAmount = Main.rand.Next(2, 4);
            for (int i = 0; i < tarAmount; i++)
            {
                Vector2 velocity = CDUtil.RandomVelocity(100f, 70f, 100f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<TotalityTar>(), (int)(Projectile.damage * 0.3f), 0f, Projectile.owner, 0f, 0f);
            }
        }
    }
}
