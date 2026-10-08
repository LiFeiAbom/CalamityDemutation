using CalamityDemutation.Content.Items.Weapons.Summon;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 苍华之庭·荆棘球（PlantationStaffThornball，移植自灾厄 2.0.3.9 的同名弹幕）——
    /// 树灵朝目标甩出的带刺球：命中第一个敌人就**钉在它身上**（此后速度跟随该敌人），
    /// 钉住之后再被判定命中一次就消失。
    /// </summary>
    internal class PlantationStaffThornball:ModProjectile
    {
        public Player Owner => Main.player[Projectile.owner];
        public NPC Target => Projectile.Center.MinionHoming(PlantationStaff.EnemyDistanceDetection, Owner);

        /// <summary>是否已钉住（0 未钉 / 1 已钉）</summary>
        public ref float IsSticked => ref Projectile.ai[0];

        /// <summary>标记为召唤物射击弹幕</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
        }
        /// <summary>基础属性（照源）：30×30、300 帧寿命、无限穿透、逐敌 60 帧独立冷却、不撞地形</summary>
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.localNPCHitCooldown = 60;
            Projectile.width = Projectile.height = 30;
            Projectile.timeLeft = 300;
            Projectile.penetrate = -1;

            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
        }
        public override void AI()
        {
            if (IsSticked == 0f)
            {
                // Home towards the target, if there's one.
                if (Target is not null)
                    Projectile.velocity = (Projectile.velocity * 25f + Projectile.SafeDirectionTo(Target.Center) * PlantationStaff.ThornballSpeed) / 26f;

                Projectile.rotation += MathHelper.ToRadians(Projectile.velocity.X);
            }
            else
            {
                // Making the projectile's velocity same as the target's means it stays with it and moves with it,
                // giving it an effect of attachment.
                if (Target is not null)
                    Projectile.velocity = Target.velocity;
                else
                    Projectile.Kill();
            }
        }
        /// <summary>命中：第一次让它钉上去，钉住后再命中一次直接消亡（源写法）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // If the projectile hits once after it's attached: die.
            if (IsSticked == 1f)
                Projectile.Kill();

            // On enemy hit, make it stick.
            IsSticked = 1f;

            Projectile.netUpdate = true;
        }
        /// <summary>消亡时喷一圈粉色丛林植物尘（源用 40 号尘 + Color.Pink）</summary>
        public override void OnKill(int timeLeft)
        {
            for (int dustIndex = 0; dustIndex < 10; dustIndex++)
                Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.JunglePlants, newColor: Color.Pink);
        }
    }
}
