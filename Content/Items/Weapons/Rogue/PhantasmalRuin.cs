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
    /// 幻魂归墟（Phantasmal Ruin）—— CI Beta 1.12 的旧版同名重制件。
    /// 普通投矛沿途释放迷失灵魂，命中爆出幻影灵魂；潜伏投矛沿途留下幻影并提高单次伤害。
    /// </summary>
    internal class PhantasmalRuin : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        public override void SetDefaults()
        {
            Item.width = 102;
            Item.height = 98;
            Item.damage = 955;
            Item.knockBack = 8f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useTime = 35;
            Item.useAnimation = 35;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(1, 75, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 13;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<PhantasmalRuinProj>();
            Item.shootSpeed = 14.5f;
            Item.DamageType = CDUtil.GetRogueDamageClass();
        }
        public override bool WeaponPrefix() => true;
        public override bool RangedPrefix() => false;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (!CDUtil.CanStealthStrike(player))
                return true;
            int spear = Projectile.NewProjectile(source, position, velocity, type, (int)(damage * 1.3f), knockback, player.whoAmI);
            if (spear >= 0 && spear < Main.maxProjectiles)
                CDUtil.SetStealthStrike(Main.projectile[spear]);
            return false;
        }
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity)
                && calamity.TryFind<ModItem>("RuinousSoul", out ModItem ruinousSoul)
                && calamity.TryFind<ModItem>("Necroplasm", out ModItem necroplasm))
            {
                CreateRecipe().
                    AddIngredient<LumiStriker>().
                    AddIngredient<PhantomLance>(500).
                    AddIngredient(ruinousSoul.Type, 4).
                    AddIngredient(necroplasm.Type, 20).
                    AddTile(TileID.LunarCraftingStation).
                    Register();
            }
            else
            {
                Mod.Logger.Warn("幻魂归墟：现代版灾厄里找不到 RuinousSoul / Necroplasm，现代配方未注册。");
            }
        }
    }
}
