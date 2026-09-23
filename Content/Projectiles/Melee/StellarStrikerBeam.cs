using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 月炎之锋的光束弹幕（移植自灾厄大修 0.4.0.1.3 的 <c>StellarStrikerBeam</c>）。
    /// 本体不可见，靠两种环形尘埃（受玩家盾牌染色着色器影响）表现轨迹；
    /// 速度极快（<c>MaxUpdates = 5</c>），首次命中敌人时在四周炸出 6 颗短命月炎火球。
    /// </summary>
    internal class StellarStrikerBeam : ModProjectile
    {
        /// <summary>本体不绘制，借用本模组的空白贴图</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>持有者玩家</summary>
        private Player Owner => Main.player[Projectile.owner];
        /// <summary>保证一整条光束只触发一次"命中炸火球"（大修原字段名 onhitNPCBool）</summary>
        private bool onhitNPCBool = true;
        /// <summary>
        /// 基础属性：22×22、近战伤害、友方、可穿墙、忽略水、每帧更新 5 次、存活 120×5 帧
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 22;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = false;
            Projectile.MaxUpdates = 5;
            Projectile.timeLeft = 120 * Projectile.MaxUpdates;
        }
        /// <summary>
        /// 每帧在弹体后方生成两颗不同灰尘（<c>DustID.WitherLightning</c>（源码写 272）与 <c>DustID.Electric</c>（源码写 226）），
        /// 交给 <see cref="SpanCycleDust"/> 重新摆到弹体四周；服务端不生成尘埃。
        /// </summary>
        public override void AI()
        {
            if (Main.netMode == NetmodeID.Server)
                return;
            Vector2 dustVel = Projectile.velocity;
            Dust dust = Dust.NewDustPerfect(Projectile.Center + dustVel * 2, DustID.WitherLightning, dustVel, 0, default, 1f);
            dust.shader = GameShaders.Armor.GetSecondaryShader(Owner.cShield, Owner);
            dust.noGravity = true;
            Dust dust2 = Dust.NewDustPerfect(Projectile.Center + dustVel * 2, DustID.Electric, dustVel, 0, default, 1f);
            dust2.shader = GameShaders.Armor.GetSecondaryShader(Owner.cShield, Owner);
            dust2.noGravity = true;
            SpanCycleDust(Projectile, dust, dust2);
        }
        /// <summary>首次命中敌人：在弹体周围 255 像素内炸出 6 颗月炎火球（半伤害、存活 30 帧、伤害类型改回近战）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.numHits != 0 || !onhitNPCBool)
                return;
            for (int i = 0; i < 6; i++)
            {
                int proj = Projectile.NewProjectile(new EntitySource_Parent(Projectile), Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.Next(0, 255), Vector2.Zero, ProjectileID.LunarFlare, (int)(Projectile.damage * 0.5), 0, Main.myPlayer, 0f, Main.rand.Next(3));
                if (proj >= 0 && proj < Main.maxProjectiles)
                {
                    Main.projectile[proj].DamageType = DamageClass.Melee;
                    Main.projectile[proj].timeLeft = 30;
                }
            }
            onhitNPCBool = false;
        }
        /// <summary>
        /// 等价于大修的 <c>CWRDust.SpanCycleDust(Projectile, Dust, Dust)</c>：
        /// 随机挑一颗尘，把它连同另一颗一起摆到弹体四周随机方位上，给一个切向初速并加淡入/缩放。
        /// </summary>
        private static void SpanCycleDust(Projectile projectile, Dust dust1, Dust dust2)
        {
            if (Main.rand.NextBool())
            {
                Vector2 vector3 = Vector2.UnitY.RotatedByRandom(MathHelper.TwoPi);
                Dust dust = dust1;
                dust.noGravity = true;
                dust.position = projectile.Center - vector3 * Main.rand.Next(10, 21);
                dust.velocity = vector3.RotatedBy(MathHelper.PiOver2) * 6f;
                dust.scale = 0.9f + Main.rand.NextFloat();
                dust.fadeIn = 0.5f;
                dust.customData = projectile;
                vector3 = Vector2.UnitY.RotatedByRandom(MathHelper.TwoPi);
                dust.noGravity = true;
                dust.position = projectile.Center - vector3 * Main.rand.Next(10, 21);
                dust.velocity = vector3.RotatedBy(MathHelper.PiOver2) * 6f;
                dust.scale = 0.9f + Main.rand.NextFloat();
                dust.fadeIn = 0.5f;
                dust.customData = projectile;
                dust.color = Color.Crimson;
            }
            else
            {
                Vector2 vector4 = Vector2.UnitY.RotatedByRandom(MathHelper.TwoPi);
                Dust dust = dust2;
                dust.noGravity = true;
                dust.position = projectile.Center - vector4 * Main.rand.Next(20, 31);
                dust.velocity = vector4.RotatedBy(-MathHelper.PiOver2) * 5f;
                dust.scale = 0.9f + Main.rand.NextFloat();
                dust.fadeIn = 0.5f;
                dust.customData = projectile;
            }
        }
    }
}
