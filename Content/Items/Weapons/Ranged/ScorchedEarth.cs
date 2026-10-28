using CalamityDemutation.Content.Projectiles.Ranged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Ranged
{
    /// <summary>
    /// 焦土——Public 2.0 旧版：500 伤害、四连发、两发耗一枚火箭，
    /// 火箭终爆后生成五枚 25% 伤害的集束炸弹。
    /// </summary>
    internal class ScorchedEarth : ModItem
    {
        public static readonly SoundStyle ShootSound = new("CalamityDemutation/Sounds/Item/ScorchedEarthShot", 3);
        private int burstCounter;
        public override void SetDefaults()
        {
            Item.width = 104;
            Item.height = 44;
            Item.damage = 500;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 8;
            Item.useAnimation = 32;
            Item.reuseDelay = 60;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 8.7f;
            Item.value = Terraria.Item.buyPrice(2, 0, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;
            Item.autoReuse = true;
            Item.shootSpeed = 12.6f;
            Item.shoot = ModContent.ProjectileType<ScorchedEarthRocket>();
            Item.useAmmo = AmmoID.Rocket;
        }
        public override bool CanConsumeAmmo(Item ammo, Player player) => burstCounter % 2 == 0;
        public override Vector2? HoldoutOffset() => new Vector2(-30f, 0f);
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int rocketType = ModContent.ProjectileType<ScorchedEarthRocket>();
            Projectile.NewProjectile(source, position, velocity, rocketType, damage, knockback, player.whoAmI);
            if (burstCounter == 0)
                SoundEngine.PlaySound(ShootSound, position);
            burstCounter++;
            if (burstCounter == 4)
                burstCounter = 0;
            return false;
        }
        public override void AddRecipes()
        {
            // 本地极乐升天炮已删除；只有灾厄侧仍提供同名旧件时才注册原版配方。
            if (ModLoader.TryGetMod("CalamityMod", out Mod modern)
                && modern.TryFind<ModItem>("BlissfulBombardier", out ModItem modernBlissful)
                && modern.TryFind<ModItem>("DarksunFragment", out ModItem modernDarksun)
                && modern.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
            {
                CreateRecipe()
                    .AddIngredient(modernBlissful.Type)
                    .AddIngredient(modernDarksun.Type, 10)
                    .AddIngredient(ItemID.FragmentSolar, 50)
                    .AddRecipeGroup("AnyAdamantiteBar", 15)
                    .AddTile(cosmicAnvil.Type)
                    .Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic)
                && classic.TryFind<ModItem>("BlissfulBombardier", out ModItem classicBlissful)
                && classic.TryFind<ModItem>("DarksunFragment", out ModItem classicDarksun)
                && classic.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
            {
                CreateRecipe()
                    .AddIngredient(classicBlissful.Type)
                    .AddIngredient(classicDarksun.Type, 10)
                    .AddIngredient(ItemID.FragmentSolar, 50)
                    .AddRecipeGroup("AnyAdamantiteBar", 15)
                    .AddTile(draedonsForge.Type)
                    .Register();
            }
        }
    }
}
