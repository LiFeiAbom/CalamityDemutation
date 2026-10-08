using CalamityDemutation.Utilities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 破坏者的熔毁爆炸（照灾厄 2.0 的 <c>TotalMeltdown</c>）：
    /// 120×122 判定、13 帧动画（每帧 5 tick，整段 65 帧）、穿透无限、同一敌人每 30 帧可再吃一次；
    /// 命中挂 5 秒「燃烧」。贴图是类同名隐式贴图 <c>TotalMeltdown.png</c>（120×1586 = 13 帧）。
    /// </summary>
    internal class TotalMeltdown : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 13;
        }
        /// <summary>120×122、穿透无限、存活 = 帧数 × 5、同一敌人每 30 帧可再命中，伤害类型取盗贼</summary>
        public override void SetDefaults()
        {
            Projectile.width = 120;
            Projectile.height = 122;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Main.projFrames[Projectile.type] * 5;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>每 5 帧推进一帧（照源用 timeLeft % 5 == 4 来卡拍）</summary>
        public override void AI()
        {
            if (Projectile.timeLeft % 5f == 4f)
                Projectile.frame++;
        }
        /// <summary>命中敌人挂 5 秒「燃烧」</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, 300);
        }
        /// <summary>PvP 同理</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 300);
        }
    }
}
