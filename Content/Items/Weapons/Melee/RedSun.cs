using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 红日（Red Sun）—— 机制取灾厄 Public 2.0.4；基础伤害按用户要求改为 750，
    /// 使用与动画均改为 14 帧，物品贴图使用经典 1.4.2.101 版本。
    /// 每次挥砍从天空召下三枚太阳耀斑，耀斑消散后分裂成追踪沙火。
    /// </summary>
    internal class RedSun : ModItem
    {
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        public override void SetDefaults()
        {
            Item.width = 72;
            Item.height = 72;
            Item.damage = 750;
            Item.DamageType = DamageClass.Melee;
            Item.useTime = Item.useAnimation = 14;
            Item.reuseDelay = 0;
            Item.useTurn = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 4f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<RSSolarFlare>();
            Item.shootSpeed = 15f;
            Item.value = Item.buyPrice(2, 80, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 16;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            float flareSpeed = Item.shootSpeed;
            Vector2 playerCenter = player.RotatedRelativePoint(player.MountedCenter, true);
            float mouseXDist = Main.mouseX + Main.screenPosition.X - playerCenter.X;
            float mouseYDist = Main.mouseY + Main.screenPosition.Y - playerCenter.Y;
            if (player.gravDir == -1f)
                mouseYDist = Main.screenPosition.Y + Main.screenHeight - Main.mouseY - playerCenter.Y;
            float mouseDistance = (float)Math.Sqrt(mouseXDist * mouseXDist + mouseYDist * mouseYDist);
            if ((float.IsNaN(mouseXDist) && float.IsNaN(mouseYDist))
                || (mouseXDist == 0f && mouseYDist == 0f))
            {
                mouseXDist = player.direction;
                mouseYDist = 0f;
                mouseDistance = flareSpeed;
            }
            else
            {
                mouseDistance = flareSpeed / mouseDistance;
            }
            for (int i = 0; i < 3; i++)
            {
                playerCenter = new Vector2(
                    player.position.X + player.width * 0.5f
                    + Main.rand.Next(201) * -player.direction
                    + (Main.mouseX + Main.screenPosition.X - player.position.X),
                    player.MountedCenter.Y - 600f);
                playerCenter.X = (playerCenter.X + player.Center.X) / 2f + Main.rand.Next(-200, 201);
                playerCenter.Y -= 100 * i;
                mouseXDist = Main.mouseX + Main.screenPosition.X - playerCenter.X;
                mouseYDist = Main.mouseY + Main.screenPosition.Y - playerCenter.Y;
                if (mouseYDist < 0f)
                    mouseYDist *= -1f;
                if (mouseYDist < 20f)
                    mouseYDist = 20f;
                mouseDistance = (float)Math.Sqrt(mouseXDist * mouseXDist + mouseYDist * mouseYDist);
                mouseDistance = flareSpeed / mouseDistance;
                mouseXDist *= mouseDistance;
                mouseYDist *= mouseDistance;
                float speedX = mouseXDist + Main.rand.Next(-1000, 1001) * 0.02f;
                float speedY = mouseYDist + Main.rand.Next(-1000, 1001) * 0.02f;
                Projectile.NewProjectile(source, playerCenter.X, playerCenter.Y, speedX, speedY,
                    ModContent.ProjectileType<RSSolarFlare>(), (int)(damage * 0.7f), knockback,
                    player.whoAmI, 0f, Main.rand.Next(10));
            }
            return false;
        }
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.NextBool(3))
                Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height,
                    DustID.YellowTorch);
        }
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "HolyFlames", 300);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "HolyLight", 300);
        }
        public override void OnHitPvp(Player player, Player target, Player.HurtInfo hurtInfo)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "HolyFlames", 300);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "HolyLight", 300);
        }
        public override void AddRecipes()
        {
            AddRecipeFor("CalamityMod");
            AddRecipeFor("CalamityModClassicPreTrailer");
        }
        private void AddRecipeFor(string modName)
        {
            if (!ModLoader.TryGetMod(modName, out Mod calamity)
                || !calamity.TryFind<ModItem>("ShadowspecBar", out ModItem shadowspecBar)
                || !calamity.TryFind<ModTile>("DraedonsForge", out ModTile forge))
                return;
            CreateRecipe()
                .AddIngredient(ItemID.AntlionClaw)
                .AddIngredient(ModContent.ItemType<ForsakenSaber>())
                .AddIngredient(ModContent.ItemType<CoreofCinder>(), 5)
                .AddIngredient(shadowspecBar.Type, 5)
                .AddTile(forge.Type)
                .Register();
        }
    }
}
