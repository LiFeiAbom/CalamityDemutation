using CalamityDemutation.Content.Projectiles.Rogue;
using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Rogue
{
    /// <summary>
    /// 餐具拨火棍（Utensil Poker）——Seared Pan 的 2.0 下位武器。
    /// 三连发随机投掷餐叉、餐刀或分肉叉；潜行打击替换成伤害 ×2、弹速 ×1.4 的屠刀。
    /// 无配方，按 2.0 的月亮领主武器池口径加入普通击杀与专家宝藏袋。
    /// </summary>
    internal class UtensilPoker : ModItem
    {
        private const float InflatedDamage = 1331f;
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 66;
            Item.damage = 333;
            Item.knockBack = 8f;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 15;
            Item.reuseDelay = 15;
            Item.useAnimation = 45;
            Item.useLimitPerAnimation = 3; // 2.0 在旧 API 下自动三连发，现代 tML 需显式补回。
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(1, 10, 0, 0);
            Item.rare = ItemRarityID.Purple;
            Item.shoot = ModContent.ProjectileType<Fork>();
            Item.shootSpeed = 12f;
            Item.DamageType = CDUtil.GetRogueDamageClass();
        }
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base = BaseDamage;
        }
        public override bool WeaponPrefix() => true;
        public override bool RangedPrefix() => false;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (CDUtil.CanStealthStrike(player))
            {
                int knife = Projectile.NewProjectile(source, position, velocity * 1.4f, ModContent.ProjectileType<ButcherKnife>(), damage * 2, knockback, player.whoAmI);
                if (knife >= 0 && knife < Main.maxProjectiles)
                    CDUtil.SetStealthStrike(Main.projectile[knife]);
            }
            else
            {
                int utensil;
                float damageMultiplier;
                float knockbackMultiplier;
                switch (Main.rand.Next(3))
                {
                    case 0:
                        utensil = ModContent.ProjectileType<Fork>();
                        damageMultiplier = 1.1f;
                        knockbackMultiplier = 2f;
                        break;
                    case 1:
                        utensil = ModContent.ProjectileType<Knife>();
                        damageMultiplier = 1.2f;
                        knockbackMultiplier = 1f;
                        break;
                    default:
                        utensil = ModContent.ProjectileType<CarvingFork>();
                        damageMultiplier = 1f;
                        knockbackMultiplier = 1f;
                        break;
                }
                Projectile.NewProjectile(source, position, velocity, utensil, (int)(damage * damageMultiplier), knockback * knockbackMultiplier, player.whoAmI);
            }
            return false;
        }
    }
}
