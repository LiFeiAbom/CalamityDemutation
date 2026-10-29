using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 精华剥离者（EssenceFlayer）—— 按灾厄经典 1.4.2.101 原样移植的宇宙砧档近战武器。
    /// 挥砍发射会追踪敌人的精华镰刀；击杀生成治疗火焰，每次回复 8 生命。
    /// </summary>
    internal class EssenceFlayer : ModItem
    {
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        public override void SetDefaults()
        {
            Item.width = 60;
            Item.height = 56;
            Item.damage = 450;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = Item.useTime = 19;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTurn = true;
            Item.knockBack = 8f;
            Item.UseSound = SoundID.Item71;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(1, 80, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;
            Item.shoot = ModContent.ProjectileType<EssenceScythe>();
            Item.shootSpeed = 21f;
        }
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.NextBool(3))
                Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.ShadowbeamStaff);
        }
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            ApplyGodSlayerInferno(target, 500);
        }
        internal static void ApplyGodSlayerInferno(NPC target, int duration)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "GodSlayerInferno", duration);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "GodSlayerInferno", duration);
        }
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity)
                && calamity.TryFind<ModItem>("CosmiliteBar", out ModItem cosmiliteBar)
                && calamity.TryFind<ModTile>("DraedonsForge", out ModTile forge))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(cosmiliteBar.Type, 11);
                recipe.AddTile(forge.Type);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic)
                && classic.TryFind<ModItem>("CosmiliteBar", out ModItem classicCosmiliteBar)
                && classic.TryFind<ModTile>("DraedonsForge", out ModTile classicForge))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(classicCosmiliteBar.Type, 11);
                recipe.AddTile(classicForge.Type);
                recipe.Register();
            }
        }
    }
}
