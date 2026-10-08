using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 超新星炸出的尖刺（照灾厄 2.0 的 <c>SupernovaSpike</c>）：
    /// 10×10 判定、初始全透明（每帧 -10 淡入）、穿透 3、<c>extraUpdates = 2</c>、
    /// 走原版钉子 AI（<c>ProjAIStyleID.Nail</c> + <c>ProjectileID.NailFriendly</c>）、同一敌人每 5 帧可再吃一次；
    /// 贴图复用本工程的毒炸弹尖刺（源也是复用灾厄那张 <c>BallisticPoisonBombSpike</c>）。
    /// </summary>
    /// <remarks>
    /// 尖刺是**满亮彩虹色**：起手从 6 个基色里随机挑一个、每帧用 <c>CDUtil.IterateDisco</c> 推进色相，
    /// 并叠一点发光照明。与源一致，配色状态放在实例字段上（tML 里 <c>ModProjectile</c> 是每类型单例，
    /// 同类尖刺会共用这一个配色状态——**只影响配色，不影响伤害/判定**，这里照源保留）。
    /// </remarks>
    internal class SupernovaSpike : ModProjectile
    {
        /// <summary>淡入步长（照源）</summary>
        private const int AlphaFadeStep = 10;
        /// <summary>尘的三种配色（源里是 107/234/269 三个裸号）</summary>
        private static readonly int[] SpikeDust = { DustID.Terra, DustID.BoneTorch, DustID.Sandnado };

        /// <summary>当前彩虹色（照源放在实例字段上；见类注释）</summary>
        private Color currentColor = Color.Black;

        public override string Texture => "CalamityDemutation/Content/Projectiles/Rogue/BallisticPoisonBombSpike";

        /// <summary>10×10、全透明起步、穿透 3、两倍额外更新、走原版钉子 AI、同一敌人每 5 帧一次</summary>
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.alpha = 255;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 3;
            Projectile.extraUpdates = 2;
            Projectile.aiStyle = ProjAIStyleID.Nail;
            AIType = ProjectileID.NailFriendly;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 5;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>淡入 + 每 5 帧留一颗彩色尘 + 推进彩虹色与发光</summary>
        public override void AI()
        {
            Projectile.alpha -= AlphaFadeStep;
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;
            Projectile.localAI[1] += 1f;
            if (Projectile.localAI[1] > 4f)
            {
                int dustType = SpikeDust[Main.rand.Next(SpikeDust.Length)];
                int dust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, dustType, 0f, 0f, 100, default, 0.75f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0f;
            }
            if (currentColor == Color.Black)
            {
                int startPoint = Main.rand.Next(6);
                Projectile.localAI[0] = startPoint;
                currentColor = GetStartingColor(startPoint);
            }
            CDUtil.IterateDisco(ref currentColor, ref Projectile.localAI[0], 15);
            Vector3 compositeColor = 0.1f * Color.White.ToVector3() + 0.05f * currentColor.ToVector3();
            Lighting.AddLight(Projectile.Center, compositeColor);
        }
        /// <summary>命中敌人挂整套星云系减益</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.ExoDebuffs();
        }
        /// <summary>PvP 同理</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.ExoDebuffs();
        }
        /// <summary>尖刺永远满亮（直接用当前彩虹色绘制）</summary>
        public override Color? GetAlpha(Color lightColor) => currentColor;
        /// <summary>起手基色（照源的 6 色表）</summary>
        private static Color GetStartingColor(int startPoint)
        {
            switch (startPoint)
            {
                case 1: return new Color(1f, 1f, 0f, 0f);   // 黄
                case 2: return new Color(0f, 1f, 0f, 0f);   // 绿
                case 3: return new Color(0f, 1f, 1f, 0f);   // 青
                case 4: return new Color(0f, 0f, 1f, 0f);   // 蓝
                case 5: return new Color(1f, 0f, 1f, 0f);   // 紫
                default: return new Color(1f, 0f, 0f, 0f);  // 红
            }
        }
        /// <summary>消失：把判定框撑到 32 再撒彩色尘与带彩虹色的火尘</summary>
        public override void OnKill(int timeLeft)
        {
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 32;
            Projectile.position -= Projectile.Size * 0.5f;
            int dustType = SpikeDust[Main.rand.Next(SpikeDust.Length)];
            Color sparkColor = new Color(Main.DiscoR, 203, 103);
            for (int i = 0; i < 2; i++)
            {
                int dust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, dustType, 0f, 0f, 100, default, 1.2f);
                Main.dust[dust].velocity *= 3f;
                if (Main.rand.NextBool(2))
                {
                    Main.dust[dust].scale = 0.5f;
                    Main.dust[dust].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
            }
            for (int i = 0; i < 2; i++)
            {
                int dust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, sparkColor, 1.7f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 5f;
                dust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, sparkColor, 1f);
                Main.dust[dust].velocity *= 2f;
            }
        }
    }
}
