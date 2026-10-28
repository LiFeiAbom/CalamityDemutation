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
    /// 狂龙翼火——现代 Wrathwing 外观与名称，玩法采用 CI 2.0.4.19 的龙炎投矛：
    /// 400 盗贼伤害、800 像素追踪长矛，命中时喷出 3 枚追踪火球。
    /// </summary>
    internal class Wrathwing : ModItem
    {
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        public override void SetDefaults()
        {
            Item.width = 152;
            Item.height = 148;
            Item.damage = 400;
            Item.knockBack = 8f;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = Item.useAnimation = 12;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item1;
            Item.value = Terraria.Item.buyPrice(2, 40, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15;
            Item.shoot = ModContent.ProjectileType<WrathwingSpear>();
            Item.shootSpeed = 28f;
            Item.DamageType = CDUtil.GetRogueDamageClass();
        }
        public override bool WeaponPrefix() => true;
        public override bool RangedPrefix() => false;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int spear = Projectile.NewProjectile(source, position, velocity,
                ModContent.ProjectileType<WrathwingSpear>(), damage, knockback, player.whoAmI);
            if (spear >= 0 && spear < Main.maxProjectiles && CDUtil.CanStealthStrike(player))
                CDUtil.SetStealthStrike(Main.projectile[spear]);
            return false;
        }
    }
}
