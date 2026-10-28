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
    /// 绯红恶魔——主体取原灾 Public 2.0.4，伤害按用户要求改为 14570，
    /// 使用时间改为 30 帧。潜行命中会生成追踪弹星阵并触发百分比吸血。
    /// </summary>
    internal class ScarletDevil : ModItem
    {
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        public override void SetDefaults()
        {
            Item.width = Item.height = 108;
            Item.damage = 14570;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 8f;
            Item.UseSound = SoundID.Item60;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<ScarletDevilProjectile>();
            Item.shootSpeed = 30f;
            Item.DamageType = CDUtil.GetRogueDamageClass();
            Item.value = Item.buyPrice(2, 80, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 16;
        }
        public override bool WeaponPrefix() => true;
        public override bool RangedPrefix() => false;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int spear = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (spear >= 0 && spear < Main.maxProjectiles && CDUtil.CanStealthStrike(player))
                CDUtil.SetStealthStrike(Main.projectile[spear]);
            return false;
        }
        // Terraria 在处理 SetDefaults 内的高暴击值时不稳，照源放在独立钩子里。
        public override void ModifyWeaponCrit(Player player, ref float crit) => crit += 20f;
        public override void AddRecipes()
        {
            AddRecipeFor("CalamityMod");
            AddRecipeFor("CalamityModClassicPreTrailer");
        }
        private void AddRecipeFor(string modName)
        {
            if (!ModLoader.TryGetMod(modName, out Mod calamity)
                || !calamity.TryFind<ModItem>("BloodstoneCore", out ModItem bloodstoneCore)
                || !calamity.TryFind<ModItem>("ShadowspecBar", out ModItem shadowspecBar)
                || !calamity.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
            {
                return;
            }
            CreateRecipe().
                AddIngredient<Wrathwing>().
                AddIngredient<RealityRupture>().
                AddIngredient(bloodstoneCore.Type, 15).
                AddIngredient(ItemID.SoulofNight, 15).
                AddIngredient(shadowspecBar.Type, 5).
                AddTile(draedonsForge.Type).
                Register();
        }
    }
}
