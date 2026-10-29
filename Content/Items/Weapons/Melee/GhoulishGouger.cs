using CalamityDemutation.Content.Projectiles.Melee;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 食尸鬼凿刃（GhoulishGouger）—— 按灾厄经典 1.4.2.101 移植，并按用户口径改为近战伤害。
    /// 单发投掷一把高速回旋镰刀；无潜行特殊效果。
    /// </summary>
    internal class GhoulishGouger : ModItem
    {
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        public override void SetDefaults()
        {
            Item.width = 68;
            Item.height = 60;
            Item.damage = 160;
            Item.knockBack = 7.5f;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 12;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(1, 40, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 13;
            Item.shoot = ModContent.ProjectileType<GhoulishGougerProjectile>();
            Item.shootSpeed = 20f;
            Item.DamageType = DamageClass.Melee;
        }
    }
}
