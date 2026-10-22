using CalamityDemutation.Content.Projectiles.Rogue;
using CalamityDemutation.Sounds;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Rogue
{
    /// <summary>
    /// 月影投矛（LumiStriker）—— CI 幻魂归墟配方的另一件前置。
    /// 普通投掷沿途释放上升星尘碎片；潜伏投掷提高弹速、压低单次伤害并切换为多段追踪形态。
    /// </summary>
    internal class LumiStriker : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        public override void SetDefaults()
        {
            Item.width = 102;
            Item.height = 86;
            Item.damage = 180;
            Item.knockBack = 16f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(1, 0, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<LumiStrikerProj>();
            Item.shootSpeed = 22f;
            Item.DamageType = CDUtil.GetRogueDamageClass();
        }
        public override bool WeaponPrefix() => true;
        public override bool RangedPrefix() => false;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (!CDUtil.CanStealthStrike(player))
                return true;
            int spear = Projectile.NewProjectile(source, position, velocity * 1.2f, type, (int)(damage * 0.7f), knockback, player.whoAmI);
            SoundEngine.PlaySound(CalamityDemutationSounds.HammerReturn);
            if (spear >= 0 && spear < Main.maxProjectiles)
                CDUtil.SetStealthStrike(Main.projectile[spear]);
            return false;
        }
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity)
                && calamity.TryFind<ModItem>("SpearofPaleolith", out ModItem paleolith)
                && calamity.TryFind<ModItem>("Turbulance", out ModItem turbulence))
            {
                CreateRecipe().
                    AddIngredient(paleolith.Type).
                    AddIngredient(turbulence.Type).
                    AddIngredient(ItemID.FragmentStardust, 6).
                    AddTile(TileID.LunarCraftingStation).
                    Register();
            }
            else
            {
                Mod.Logger.Warn("月影投矛：现代版灾厄里找不到 SpearofPaleolith / Turbulance，配方未注册。");
            }
        }
    }
}
