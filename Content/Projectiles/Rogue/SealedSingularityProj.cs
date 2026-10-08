using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 封存奇点飞出的那枚"封印块"（照灾厄 2.0 的 <c>SealedSingularityProj</c>）：
    /// 34×34 判定、穿透 1、存活 180 帧、贴图借用物品贴图；前 70 帧边飞边转，之后减速（每帧 ×0.96）。
    /// </summary>
    /// <remarks>
    /// 碎裂（<c>OnKill</c>，只在主人端）：玻璃碎响 + 召出 <see cref="SealedSingularityBlackhole"/>（中心对齐、
    /// **潜行打击时以 <c>ai[0] = -180</c> 生成**＝多活 180 帧，并把潜行标记同步过去）+
    /// 3 片 <see cref="SealedSingularityGore"/>（伤害 ×0.25，<c>ai[0]</c> 记 0/1/2 决定用哪张碎片贴图）。
    /// </remarks>
    internal class SealedSingularityProj : ModProjectile
    {
        /// <summary>开始减速的帧数（照源）</summary>
        private const float SlowdownDelay = 70f;
        /// <summary>潜行黑洞的生成相位（照源：-180 = 多活 180 帧）</summary>
        private const float StealthStartPhase = -180f;
        /// <summary>碎裂时溅出的碎片数量（照源）</summary>
        private const int GoreAmount = 3;

        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Rogue/SealedSingularity";

        /// <summary>34×34、穿透 1、存活 180 帧，伤害类型取盗贼</summary>
        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>前 70 帧旋转，之后持续减速</summary>
        public override void AI()
        {
            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] >= SlowdownDelay)
                Projectile.velocity *= 0.96f;
            else
                Projectile.rotation += 0.3f * Projectile.direction;
        }
        /// <summary>碎裂：碎响 + 黑洞 + 3 片碎片（全在主人端生成，第 5 节口径）</summary>
        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner != Main.myPlayer)
                return;
            SoundEngine.PlaySound(SoundID.Shatter, Projectile.position);
            bool stealthStrike = CDUtil.IsStealthStrike(Projectile, out _);
            int blackhole = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<SealedSingularityBlackhole>(), Projectile.damage, Projectile.knockBack, Projectile.owner, stealthStrike ? StealthStartPhase : 0f, 0f);
            if (blackhole >= 0 && blackhole < Main.maxProjectiles)
            {
                Main.projectile[blackhole].Center = Projectile.Center;
                if (stealthStrike)
                    CDUtil.SetStealthStrike(Main.projectile[blackhole]);
            }
            for (int i = 0; i < GoreAmount; i++)
            {
                float speedX = -Projectile.velocity.X * Main.rand.Next(40, 70) * 0.01f + Main.rand.Next(-20, 21) * 0.4f;
                float speedY = -Projectile.velocity.Y * Main.rand.Next(40, 70) * 0.01f + Main.rand.Next(-20, 21) * 0.4f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X + speedX, Projectile.Center.Y + speedY, speedX, speedY, ModContent.ProjectileType<SealedSingularityGore>(), (int)(Projectile.damage * 0.25f), 0f, Projectile.owner, i, 0f);
            }
        }
    }
}
