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
    /// 原灾 2.0.7.2 现实撕裂：普通投掷交替为双追踪碎片或单发大型穿甲长矛；
    /// 潜行改为高速现实粉碎长矛，命中后产生大范围爆炸。
    /// </summary>
    internal class RealityRupture : ModItem
    {
        private bool bigSpear;
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        public override void SetDefaults()
        {
            Item.width = 86;
            Item.height = 102;
            Item.damage = 225;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 37;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 9f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 12;
            Item.shoot = ModContent.ProjectileType<RealityRuptureMini>();
            Item.shootSpeed = 10f;
            Item.value = Item.buyPrice(1, 50, 0, 0);
            Item.DamageType = CDUtil.GetRogueDamageClass();
        }
        public override bool WeaponPrefix() => true;
        public override bool RangedPrefix() => false;
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (CDUtil.CanStealthStrike(player))
                damage = (int)(damage * 3.9f);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (CDUtil.CanStealthStrike(player))
            {
                int spear = Projectile.NewProjectile(
                    source,
                    position,
                    velocity,
                    ModContent.ProjectileType<RealityRuptureStealth>(),
                    damage,
                    knockback,
                    player.whoAmI);
                SoundEngine.PlaySound(CalamityDemutationSounds.RealityRuptureStealth, player.Center);
                if (spear >= 0 && spear < Main.maxProjectiles)
                {
                    CDUtil.SetStealthStrike(Main.projectile[spear]);
                    Main.projectile[spear].usesLocalNPCImmunity = true;
                }
                return false;
            }
            if (bigSpear)
            {
                SoundEngine.PlaySound(CalamityDemutationSounds.LanceofDestinyStrong, player.Center);
                Projectile.NewProjectile(
                    source,
                    position,
                    velocity,
                    ModContent.ProjectileType<RealityRuptureLance>(),
                    damage * 4,
                    knockback * 1.5f,
                    player.whoAmI);
            }
            else
            {
                SoundEngine.PlaySound(CalamityDemutationSounds.RealityRupture, player.Center);
                const int index = 4;
                for (int offset = -index; offset <= index; offset += index)
                {
                    Projectile.NewProjectile(
                        source,
                        position,
                        velocity.RotatedBy(MathHelper.ToRadians(offset)),
                        type,
                        damage,
                        knockback,
                        player.whoAmI);
                }
            }
            bigSpear = !bigSpear;
            return false;
        }
        public override void AddRecipes()
        {
            if (!ModLoader.TryGetMod("CalamityMod", out Mod calamity)
                || !calamity.TryFind<ModItem>("ArmoredShell", out ModItem armoredShell)
                || !calamity.TryFind<ModItem>("TwistingNether", out ModItem twistingNether)
                || !calamity.TryFind<ModItem>("DarkPlasma", out ModItem darkPlasma))
            {
                Mod.Logger.Warn("现实撕裂：现代灾厄缺少 ArmoredShell / TwistingNether / DarkPlasma，配方未注册。");
                return;
            }
            CreateRecipe().
                AddIngredient<SpearofDestiny>().
                AddIngredient(armoredShell.Type).
                AddIngredient(twistingNether.Type).
                AddIngredient(darkPlasma.Type).
                AddTile(TileID.LunarCraftingStation).
                Register();
        }
    }
}
