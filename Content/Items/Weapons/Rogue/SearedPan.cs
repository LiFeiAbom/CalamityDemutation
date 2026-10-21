using CalamityDemutation.Content.Items;
using CalamityDemutation.Content.Projectiles.Rogue;
using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Rogue
{
    /// <summary>
    /// 炙热平底锅（Seared Pan）——2.0 盗贼武器。
    /// 普通锅每命中一次叠加连段，40 帧内第三次命中后生成金锅；金锅使全部火球追踪，
    /// 潜行打击额外生成金色火花与更多火球。
    /// </summary>
    internal class SearedPan : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        public override void SetDefaults()
        {
            Item.width = 60;
            Item.height = 36;
            Item.damage = 2222;
            Item.knockBack = 10f;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = Item.useAnimation = 25;
            Item.reuseDelay = 1;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(1, 50, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15;
            Item.shoot = ModContent.ProjectileType<SearedPanProjectile>();
            Item.shootSpeed = 15f;
            Item.DamageType = CDUtil.GetRogueDamageClass();
        }
        public override bool WeaponPrefix() => true;
        public override bool RangedPrefix() => false;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            float mode = 1f;
            if (modPlayer.searedPanCounter >= 3)
            {
                modPlayer.ResetSearedPanCounter();
                mode = 2f;
            }
            if (CDUtil.CanStealthStrike(player))
            {
                modPlayer.ResetSearedPanCounter();
                mode = 3f;
            }
            int pan = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, mode);
            if (mode > 1f && pan >= 0 && pan < Main.maxProjectiles)
            {
                Main.projectile[pan].extraUpdates++;
                if (mode == 3f)
                    CDUtil.SetStealthStrike(Main.projectile[pan]);
            }
            return false;
        }
        public override void AddRecipes()
        {
            if (!ModLoader.TryGetMod("CalamityMod", out Mod calamity) ||
                !calamity.TryFind<ModItem>("AuricBar", out ModItem auricBar) ||
                !calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
            {
                Mod.Logger.Warn("炙热平底锅：找不到 AuricBar 或 CosmicAnvil，2.0 配方未注册。");
                return;
            }
            CreateRecipe().
                AddIngredient<UtensilPoker>().
                AddIngredient(auricBar.Type, 5).
                AddIngredient(ItemID.Bacon, 4).
                AddIngredient(ItemID.LifeCrystal).
                AddIngredient(ItemID.ManaCrystal).
                AddIngredient(ItemID.Bone, 92).
                AddTile(cosmicAnvil.Type).
                Register();
        }
    }
}
