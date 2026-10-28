using CalamityDemutation.Content.Projectiles.Rogue;
using CalamityDemutation.Sounds;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Rogue
{
    /// <summary>灵液长矛：灵液黏黏怪掉落，潜行命中后会钉在目标身上持续造成伤害。</summary>
    internal class IchorSpear : ModItem
    {
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        public override void SetDefaults()
        {
            Item.width = Item.height = 52;
            Item.damage = 132;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 39;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 7f;
            Item.UseSound = CalamityDemutationSounds.IchorSpearThrow;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(0, 12, 0, 0);
            Item.rare = ItemRarityID.LightRed;
            Item.shoot = ModContent.ProjectileType<IchorSpearProj>();
            Item.shootSpeed = 17f;
            Item.DamageType = CDUtil.GetRogueDamageClass();
        }
        public override bool WeaponPrefix() => true;
        public override bool RangedPrefix() => false;
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (CDUtil.CanStealthStrike(player))
                damage = (int)(damage * 0.38f);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (!CDUtil.CanStealthStrike(player))
                return true;
            int spear = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (spear >= 0 && spear < Main.maxProjectiles)
            {
                CDUtil.SetStealthStrike(Main.projectile[spear]);
                Main.projectile[spear].usesLocalNPCImmunity = true;
            }
            return false;
        }
    }
}
