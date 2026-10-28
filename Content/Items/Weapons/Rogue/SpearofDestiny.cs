using CalamityDemutation.Content.Projectiles.Rogue;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Rogue
{
    /// <summary>
    /// CI 2.0.4.19 命运之矛：普通投掷三矛，中间一把为穿透型灵液标枪；
    /// 潜行改为扇形投出五把追踪矛，穿墙并额外增加穿透。
    /// </summary>
    internal class SpearofDestiny : ModItem
    {
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        public override void SetDefaults()
        {
            Item.width = Item.height = 52;
            Item.damage = 42;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 2f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.rare = ItemRarityID.Orange;
            Item.shoot = ModContent.ProjectileType<SpearofDestinyProjectile>();
            Item.shootSpeed = 20f;
            Item.value = Item.buyPrice(0, 50, 0, 0);
            Item.DamageType = CDUtil.GetRogueDamageClass();
        }
        public override bool WeaponPrefix() => true;
        public override bool RangedPrefix() => false;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            bool stealthStrike = CDUtil.CanStealthStrike(player);
            int index = 7;
            if (stealthStrike)
            {
                for (int offset = -index * 2; offset <= index * 2; offset += index)
                {
                    int spear = Projectile.NewProjectile(
                        source,
                        position,
                        velocity.RotatedBy(MathHelper.ToRadians(offset)),
                        type,
                        damage,
                        knockback,
                        player.whoAmI);
                    if (spear >= 0 && spear < Main.maxProjectiles)
                    {
                        CDUtil.SetStealthStrike(Main.projectile[spear]);
                        Main.projectile[spear].extraUpdates += 2;
                    }
                }
                return false;
            }
            for (int offset = -index; offset <= index; offset += index)
            {
                int projectileType = offset == 0 ? ModContent.ProjectileType<IchorSpearProjLegacy>() : type;
                Projectile.NewProjectile(
                    source,
                    position,
                    velocity.RotatedBy(MathHelper.ToRadians(offset)),
                    projectileType,
                    damage,
                    knockback,
                    player.whoAmI);
            }
            return false;
        }
    }
}
