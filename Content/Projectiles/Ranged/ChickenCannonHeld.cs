using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 小鸡大炮的手持炮体——按住左键每 33 帧发射一枚小鸡火箭。
    /// 首枚火箭的弹药由物品生成手持体时扣除，因此这里以 ai[0] 抵消一次。
    /// </summary>
    internal class ChickenCannonHeld : ModProjectile
    {
        private const float FireRate = 33f;
        private Player Owner => Main.player[Projectile.owner];
        private ref float FreeShotLoaded => ref Projectile.ai[0];
        private ref float FramesTillNextShot => ref Projectile.ai[1];
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }
        public override void SetDefaults()
        {
            Projectile.width = 126;
            Projectile.height = 44;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
        }
        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= Main.projFrames[Type])
                Projectile.frame = 0;
            FramesTillNextShot--;
            bool shouldShoot = false;
            if (FramesTillNextShot <= 0f)
            {
                FramesTillNextShot = FireRate;
                shouldShoot = true;
            }
            bool canShoot = Owner.channel && (Owner.HasAmmo(Owner.HeldItem) || FreeShotLoaded > 0f) && !Owner.noItems && !Owner.CCed;
            if (Projectile.soundDelay <= 0 && canShoot)
            {
                Projectile.soundDelay = (int)FireRate;
                SoundEngine.PlaySound(SoundID.Item61, Projectile.position);
            }
            if (Projectile.owner == Main.myPlayer)
            {
                Item weapon = Owner.HeldItem;
                int projectileType = ModContent.ProjectileType<ChickenRocket>();
                float shootSpeed = 14.5f;
                int damage = Owner.GetWeaponDamage(weapon);
                float knockback = weapon.knockBack;
                if (canShoot)
                {
                    Vector2 armPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
                    Vector2 aim = Main.MouseWorld - armPosition;
                    if (Owner.gravDir == -1f)
                        aim.Y = Main.screenHeight - Main.mouseY + Main.screenPosition.Y - armPosition.Y;
                    Vector2 direction = aim.SafeNormalize(-Vector2.UnitY);
                    Vector2 newVelocity = direction * weapon.shootSpeed * Projectile.scale * 0.55f;
                    if (newVelocity != Projectile.velocity)
                    {
                        Projectile.velocity = newVelocity;
                        Projectile.netUpdate = true;
                        Projectile.netSpam = 0;
                    }
                    if (shouldShoot)
                    {
                        if (FreeShotLoaded > 0f)
                            FreeShotLoaded--;
                        else
                            Owner.PickAmmo(weapon, out projectileType, out shootSpeed, out damage, out knockback, out _);
                        projectileType = ModContent.ProjectileType<ChickenRocket>();
                        knockback = Owner.GetWeaponKnockback(weapon, knockback);
                        Vector2 shootVelocity = Projectile.velocity.SafeNormalize(-Vector2.UnitY) * shootSpeed;
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), armPosition, shootVelocity, projectileType, damage, knockback, Projectile.owner);
                    }
                }
                else
                {
                    Projectile.Kill();
                }
            }
            Projectile.position = Owner.RotatedRelativePoint(Owner.MountedCenter, true) - Projectile.Size * 0.5f;
            Projectile.rotation = Projectile.velocity.ToRotation() + (Projectile.spriteDirection == -1 ? MathHelper.Pi : 0f);
            Projectile.spriteDirection = Projectile.direction;
            Projectile.timeLeft = 2;
            Owner.ChangeDir(Projectile.direction);
            Owner.heldProj = Projectile.whoAmI;
            if (Owner.itemTime < 2)
                Owner.itemTime = 2;
            if (Owner.itemAnimation < 2)
                Owner.itemAnimation = 2;
            Owner.itemRotation = (Projectile.velocity * Projectile.direction).ToRotation();
        }
        public override bool? CanDamage() => false;
    }
}
