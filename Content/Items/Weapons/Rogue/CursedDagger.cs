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
    /// CI 2.0.4.19 诅咒短剑：普通投掷一把会弹跳的咒焰匕首，飞行途中按速度反向生成下落咒火；
    /// 潜行投掷速度提升，并沿途交替生成两种咒火弹幕。
    /// </summary>
    internal class CursedDagger : ModItem
    {
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        public override void SetDefaults()
        {
            Item.width = Item.height = 34;
            Item.damage = 34;
            Item.DamageType = CDUtil.GetRogueDamageClass();
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 16;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 4.5f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(0, 24, 0, 0);
            Item.rare = ItemRarityID.Pink;
            Item.shoot = ModContent.ProjectileType<CursedDaggerProj>();
            Item.shootSpeed = 12f;
        }
        public override bool WeaponPrefix() => true;
        public override bool RangedPrefix() => false;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (!CDUtil.CanStealthStrike(player))
                return true;
            int dagger = Projectile.NewProjectile(source, position, velocity * 2.5f, type, damage, knockback, player.whoAmI);
            if (dagger >= 0 && dagger < Main.maxProjectiles)
            {
                CDUtil.SetStealthStrike(Main.projectile[dagger]);
                Main.projectile[dagger].penetrate = -1;
            }
            return false;
        }
        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.CursedFlame, 20).
                AddIngredient(ItemID.RottenChunk, 15).
                AddTile(TileID.WorkBenches).
                Register();
        }
    }
}
