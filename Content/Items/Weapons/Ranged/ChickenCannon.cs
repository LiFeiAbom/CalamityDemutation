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
    /// 小鸡大炮（Chicken Cannon）—— 面板与持续引导操作取灾厄 2.0；
    /// 火箭和爆炸取 2.0.4.3，爆炸附带龙焰减益。无配方，来自犽戎。
    /// </summary>
    internal class ChickenCannon : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        public override void SetDefaults()
        {
            Item.width = 126;
            Item.height = 42;
            Item.damage = 416;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = Item.useAnimation = 33;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 1.5f;
            Item.value = Item.buyPrice(1, 50, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15;
            Item.autoReuse = true;
            Item.shootSpeed = 14.5f;
            Item.shoot = ModContent.ProjectileType<ChickenRocket>();
            Item.useAmmo = AmmoID.Rocket;
            Item.channel = true;
        }
        public override void HoldItem(Player player)
        {
            Item.channel = player.altFunctionUse != 2;
            Item.noUseGraphic = player.altFunctionUse != 2;
        }
        public override Vector2? HoldoutOffset() => new Vector2(-20f, 0f);
        public override bool AltFunctionUse(Player player) => true;
        public override bool CanConsumeAmmo(Item ammo, Player player) => player.altFunctionUse != 2;
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[ModContent.ProjectileType<ChickenCannonHeld>()] <= 0 || player.altFunctionUse == 2;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                SoundEngine.PlaySound(SoundID.Item110, position);
                DetonateRockets(player);
                return false;
            }
            Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<ChickenCannonHeld>(), 0, 0f, player.whoAmI, 1f);
            return false;
        }
        private void DetonateRockets(Player player)
        {
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != player.whoAmI || projectile.type != Item.shoot)
                    continue;
                projectile.timeLeft = 1;
                projectile.netUpdate = true;
                projectile.netSpam = 0;
            }
        }
        public override float UseSpeedMultiplier(Player player) => player.altFunctionUse == 2 ? 5f : 1f;
    }
}
