using CalamityDemutation.Content.Projectiles.Ranged;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Ranged
{
    /// <summary>
    /// 硫酸炮——取灾厄 2.0.7.2 的无限弹药黏附酸弹；伤害按用户要求调整为 220。
    /// 现代版由硫海遗爵宝藏袋 1/4 开出；经典版恢复巨螯虾 1/3 掉落。
    /// </summary>
    internal class SulphuricAcidCannon : ModItem
    {
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        public override void SetDefaults()
        {
            Item.width = 90;
            Item.height = 30;
            Item.damage = 220;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = Item.useAnimation = 18;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 6f;
            Item.autoReuse = true;
            Item.value = Terraria.Item.buyPrice(1, 75, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 13;
            Item.UseSound = SoundID.Item95;
            Item.shoot = ModContent.ProjectileType<SulphuricBlast>();
            Item.shootSpeed = 16f;
        }
        public override Vector2? HoldoutOffset() => new Vector2(-15f, 0f);
    }
}
