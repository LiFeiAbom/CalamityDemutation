using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Typeless
{
    /// <summary>
    /// 有毒海水 - 瞬逝的毒性水花/海泡弹幕（由相关武器命中后溅出）
    /// 本体不可见（隐形贴图），仅以蓝水泡/毒泡粉尘表现，存活极短（6 帧），
    /// 命中附加中毒与剧毒 debuff 各 120 帧
    /// </summary>
    internal class PoisonousSeawater:ModProjectile
    {
        // 使用隐形贴图，视觉效果完全由粉尘承担
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 基础属性：30x30 碰撞箱；友方、入水不减速、无视地形、无限穿透、存活仅 6 帧（瞬发判定）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 6;
        }
        /// <summary>
        /// AI：每帧叠加淡蓝色光照，并随机生成 2 粒水花/毒泡粉尘作为溅射外观
        /// </summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, ((255 - Projectile.alpha) * 0f) / 255f, ((255 - Projectile.alpha) * 0.15f) / 255f, ((255 - Projectile.alpha) * 0.4f) / 255f);
            // 掷 0~3：前三个结果映射为水泡尘 33（约 75%），第四个映射为毒泡尘 89（约 25%）
            int randomDust = Main.rand.Next(4);
            if (randomDust == 0)
            {
                randomDust = 33;
            }
            else if (randomDust == 1)
            {
                randomDust = 33;
            }
            else if (randomDust == 2)
            {
                randomDust = 33;
            }
            else
            {
                randomDust = 89;
            }
            // 生成 2 粒选定类型的粉尘；毒泡尘（89）缩小到 35% 呈现"小毒泡"质感；粉尘速度清零定格在原地
            for (int num468 = 0; num468 < 2; num468++)
            {
                int num469 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, randomDust, 0f, 0f, 100, default(Color), 1f);
                if (randomDust == 89)
                {
                    Main.dust[num469].scale *= 0.35f;
                }
                Main.dust[num469].velocity *= 0f;
            }
        }
        /// <summary>
        /// 命中敌人：施加剧毒与中毒 debuff 各 120 帧
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Venom, 120);
            target.AddBuff(BuffID.Poisoned, 120);
        }
        /// <summary>
        /// 命中玩家（PvP）：施加剧毒与中毒 debuff 各 120 帧
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Venom, 120);
            target.AddBuff(BuffID.Poisoned, 120);
        }
    }
}
