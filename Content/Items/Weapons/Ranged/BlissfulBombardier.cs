using CalamityDemutation.Content.Projectiles.Ranged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Ranged
{
    /// <summary>
    /// 极乐升天炮——取 CI Beta 1.12 的 ProfanedLancher：270 伤害、追踪渎神核弹与伤害粉尘。
    /// </summary>
    internal class BlissfulBombardier : ModItem
    {
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        public override void SetDefaults()
        {
            Item.width = 66;
            Item.height = 28;
            Item.damage = 270;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 7.5f;
            Item.autoReuse = true;
            Item.value = Terraria.Item.buyPrice(1, 50, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 12;
            Item.UseSound = SoundID.Item61;
            Item.shootSpeed = 22f;
            Item.shoot = ModContent.ProjectileType<ProfanedNuke>();
            Item.useAmmo = AmmoID.Rocket;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.PickAmmo(Item, out _, out _, out _, out _, out int rocketType);
            if (Main.zenithWorld)
            {
                for (int i = 0; i < 12; i++)
                {
                    Vector2 spreadVelocity = new Vector2(velocity.Length(), 0f).RotatedByRandom(MathHelper.Pi);
                    Projectile.NewProjectile(source, position, spreadVelocity, type, damage, knockback, player.whoAmI, rocketType);
                }
                return false;
            }
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, rocketType);
            return false;
        }
    }
}
