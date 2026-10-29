using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 遗弃之刃（Forsaken Saber）—— 红日链第一件下位，按灾厄 Public 2.0.4 移植。
    /// 每次挥砍射出两枚沙刃：一枚随机偏转，另一枚沿准星方向飞行。
    /// </summary>
    internal class ForsakenSaber : ModItem
    {
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        public override void SetDefaults()
        {
            Item.width = 46;
            Item.height = 56;
            Item.damage = 80;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = Item.useTime = 15;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTurn = true;
            Item.knockBack = 6f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(0, 24, 0, 0);
            Item.rare = ItemRarityID.Pink;
            Item.shoot = ModContent.ProjectileType<SandBlade>();
            Item.shootSpeed = 15f;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 spreadVelocity = velocity.RotatedByRandom(MathHelper.ToRadians(15f))
                * Main.rand.NextFloat(0.8f, 1.2f);
            Projectile.NewProjectile(source, position, spreadVelocity, type, damage / 2,
                knockback * 0.5f, player.whoAmI);
            Projectile.NewProjectile(source, position, velocity, type, damage / 2,
                knockback * 0.5f, player.whoAmI);
            return false;
        }
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.NextBool(3))
                Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height,
                    DustID.Teleporter);
        }
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out _))
            {
                CreateRecipe()
                    .AddRecipeGroup("AnyAdamantiteBar", 5)
                    .AddIngredient(ItemID.AncientBattleArmorMaterial, 2)
                    .AddTile(TileID.MythrilAnvil)
                    .Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out _))
            {
                CreateRecipe()
                    .AddIngredient(ItemID.AdamantiteBar, 5)
                    .AddIngredient(ItemID.AncientBattleArmorMaterial, 2)
                    .AddTile(TileID.MythrilAnvil)
                    .Register();
                CreateRecipe()
                    .AddIngredient(ItemID.TitaniumBar, 5)
                    .AddIngredient(ItemID.AncientBattleArmorMaterial, 2)
                    .AddTile(TileID.MythrilAnvil)
                    .Register();
            }
        }
    }
}
