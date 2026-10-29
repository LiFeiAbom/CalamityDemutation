using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Content.Projectiles.Ranged;
using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 灵魂收割者（SoulHarvester）—— 按灾厄经典 1.4.2.101 原样移植的秘银砧近战武器。
    /// 挥砍发射灵魂镰刀；命中施加瘟疫与诅咒地狱，敌人生命值不高于 15% 时额外引爆。
    /// </summary>
    internal class SoulHarvester : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        public override void SetDefaults()
        {
            Item.width = 62;
            Item.height = 64;
            Item.damage = 98;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTurn = true;
            Item.knockBack = 7.5f;
            Item.UseSound = SoundID.Item71;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(0, 80, 0, 0);
            Item.rare = ItemRarityID.Yellow;
            Item.shoot = ModContent.ProjectileType<SoulScythe>();
            Item.shootSpeed = 18f;
        }
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.NextBool(3))
                Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.CursedTorch);
        }
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            ApplyPlague(target, 200);
            target.AddBuff(BuffID.CursedInferno, 200);
            if (target.life > target.lifeMax * 0.15f)
                return;
            SoundEngine.PlaySound(SoundID.Item14, player.position);
            int blastDamage = (int)(Item.damage * player.GetTotalDamage<MeleeDamageClass>().Multiplicative);
            Projectile.NewProjectile(player.GetSource_ItemUse(Item), target.Center, Vector2.Zero,
                ModContent.ProjectileType<HiveBombExplosion>(), blastDamage, Item.knockBack, player.whoAmI);
            for (int i = 0; i < 30; i++)
            {
                int dust = Dust.NewDust(player.position, player.width, player.height, DustID.GemEmerald, 0f, 0f, 100, default, 2f);
                Main.dust[dust].velocity *= 3f;
                if (Main.rand.NextBool(2))
                {
                    Main.dust[dust].scale = 0.5f;
                    Main.dust[dust].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
            }
            for (int i = 0; i < 50; i++)
            {
                int dust = Dust.NewDust(player.position, player.width, player.height, DustID.GemEmerald, 0f, 0f, 100, default, 3f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 5f;
                dust = Dust.NewDust(player.position, player.width, player.height, DustID.GemEmerald, 0f, 0f, 100, default, 2f);
                Main.dust[dust].velocity *= 2f;
            }
        }
        internal static void ApplyPlague(NPC target, int duration)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "Plague", duration);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "Plague", duration);
        }
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity)
                && calamity.TryFind<ModItem>("PlagueCellCanister", out ModItem plagueCellCanister))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.DeathSickle);
                recipe.AddIngredient(plagueCellCanister.Type, 10);
                recipe.AddIngredient(ItemID.CursedFlame, 20);
                recipe.AddTile(TileID.MythrilAnvil);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic)
                && classic.TryFind<ModItem>("PlagueCellCluster", out ModItem plagueCellCluster))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.DeathSickle);
                recipe.AddIngredient(plagueCellCluster.Type, 10);
                recipe.AddIngredient(ItemID.CursedFlame, 20);
                recipe.AddTile(TileID.MythrilAnvil);
                recipe.Register();
            }
        }
    }
}
