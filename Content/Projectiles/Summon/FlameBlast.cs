using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 圣化火花炮台的**直射火弹**（照灾厄 2.0.3.9 <c>Projectiles/Summon/FlameBlast.cs</c> 移植）：
    /// 6×6 出生、登场即凭特效撑到 20×20，300 像素内吸附敌人（25 速度），命中挂圣焰 180 帧。
    /// 贴图沿用工程共用隐形图 `Content/Projectiles/InvisibleProj`（源同样引隐形图，不新增素材）。
    /// </summary>
    internal class FlameBlast:ModProjectile
    {
        /// <summary>贴图引工程共用的隐形占位图（源也走隐形图）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";

        /// <summary>登场特效只放一次的开关（照源）</summary>
        private float count = 0f;

        /// <summary>属于哨兵弹药（SentryShot）</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.SentryShot[Projectile.type] = true;
        }
        /// <summary>基础属性：照源（6×6 起手、可穿地形判定、寿命 180、召唤伤害）</summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 6;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 180;
            Projectile.DamageType = DamageClass.Summon;
        }
        /// <summary>登场喷焰并撑大判定箱，之后留拖尾尘，并在 300 像素内吸附敌人</summary>
        public override void AI()
        {
            if (count == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item73, Projectile.position);
                // 以中心为锚把判定箱从 6×6 撑到 20×20（照源的原地写法）
                Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
                Projectile.width = 20;
                Projectile.height = 20;
                Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
                for (int i = 0; i < 10; i++)
                {
                    int dust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.CopperCoin, 0f, 0f, 100, default, 2f);
                    Main.dust[dust].velocity *= 3f;
                    if (Main.rand.NextBool())
                    {
                        Main.dust[dust].scale = 0.5f;
                        Main.dust[dust].fadeIn = 1f + (float)Main.rand.Next(10) * 0.1f;
                    }
                }
                for (int j = 0; j < 20; j++)
                {
                    int dust2 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.GoldCoin, 0f, 0f, 100, default, 3f);
                    Main.dust[dust2].noGravity = true;
                    Main.dust[dust2].velocity *= 5f;
                    dust2 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.GoldCoin, 0f, 0f, 100, default, 2f);
                    Main.dust[dust2].velocity *= 2f;
                }
                count += 1f;
            }
            // 飞过 4 帧之后每帧原地留 5 颗圣火尘（源写法）
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] > 4f)
            {
                for (int k = 0; k < 5; k++)
                {
                    int otherDust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.CopperCoin, 0f, 0f, 100, default, 0.75f);
                    Main.dust[otherDust].velocity *= 0f;
                }
            }
            // 300 像素内吸附敌人（源用灾厄 MinionHoming，本工程用同名移植件）
            NPC potentialTarget = Projectile.Center.MinionHoming(300f, Main.player[Projectile.owner]);
            if (potentialTarget != null)
                Projectile.velocity = (Projectile.velocity * 20f + Projectile.SafeDirectionTo(potentialTarget.Center) * 25f) / 21f;
        }
        /// <summary>命中挂圣焰 180 帧（经典版没有 HolyFlames，退回原版燃烧）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "HolyFlames", 180, BuffID.OnFire);
        /// <summary>消失时补一小撮圣火尘（照源）</summary>
        public override void OnKill(int timeLeft)
        {
            for (int k = 0; k < 5; k++)
            {
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, DustID.CopperCoin, Projectile.oldVelocity.X * 0f, Projectile.oldVelocity.Y * 0f);
            }
        }
    }
}
