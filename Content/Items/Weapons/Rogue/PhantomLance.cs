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
    /// 幻影投矛（Phantom Lance）—— 幻魂归墟 CI 配方的最下位武器。
    /// 面板与投掷逻辑照 CI Beta 1.12；每次消耗一根，命中前持续放出追踪幻影。
    /// </summary>
    internal class PhantomLance : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 99;
        }
        public override void SetDefaults()
        {
            Item.width = 62;
            Item.height = 68;
            Item.damage = 70;
            Item.knockBack = 5f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useTime = 23;
            Item.useAnimation = 23;
            Item.autoReuse = true;
            Item.consumable = true;
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(0, 60, 0, 0);
            Item.rare = ItemRarityID.Yellow;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<PhantomLanceProj>();
            Item.shootSpeed = 10f;
            Item.DamageType = CDUtil.GetRogueDamageClass();
        }
        public override bool WeaponPrefix() => true;
        public override bool RangedPrefix() => false;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (!CDUtil.CanStealthStrike(player))
                return true;
            int lance = Projectile.NewProjectile(source, position, velocity, type, (int)(damage * 1.75f), knockback, player.whoAmI);
            if (lance >= 0 && lance < Main.maxProjectiles)
                CDUtil.SetStealthStrike(Main.projectile[lance]);
            return false;
        }
        public override void AddRecipes()
        {
            CreateRecipe(150).
                AddIngredient(ItemID.SpectreBar).
                AddTile(TileID.MythrilAnvil).
                Register();
        }
    }
}
