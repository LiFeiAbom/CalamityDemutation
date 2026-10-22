using CalamityDemutation.Content.Projectiles.Magic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Magic
{
    /// <summary>
    /// 绝学法杖（Fabstaff）—— 面板与机制照灾厄 2.0，基础伤害按用户指定改为 330。
    /// 发射会反弹的 <see cref="FabRay"/>，射线在敌人附近分裂出 <see cref="FabBolt"/>。
    /// </summary>
    internal class Fabstaff : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.staff[Type] = true;
            Item.ResearchUnlockCount = 1;
        }
        public override void SetDefaults()
        {
            Item.width = 84;
            Item.height = 84;
            Item.damage = 330;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 50;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 5f;
            Item.value = Item.buyPrice(2, 0, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 16;
            Item.UseSound = SoundID.Item60;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<FabRay>();
            Item.shootSpeed = 13.5f;
        }
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity)
                && calamity.TryFind<ModItem>("Necroplasm", out ModItem necroplasm)
                && calamity.TryFind<ModItem>("ShadowspecBar", out ModItem shadowspecBar)
                && calamity.TryFind<ModTile>("DraedonsForge", out ModTile forge))
            {
                CreateRecipe().
                    AddIngredient(ItemID.RainbowRod).
                    AddIngredient(necroplasm.Type, 10).
                    AddIngredient(shadowspecBar.Type, 5).
                    AddTile(forge.Type).
                    Register();
            }
            else
            {
                Mod.Logger.Warn("绝学法杖：现代版灾厄里找不到 Necroplasm / ShadowspecBar / DraedonsForge，现代配方未注册。");
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic)
                && classic.TryFind<ModItem>("Phantoplasm", out ModItem phantoplasm)
                && classic.TryFind<ModItem>("ShadowspecBar", out ModItem classicShadowspecBar)
                && classic.TryFind<ModTile>("DraedonsForge", out ModTile classicForge))
            {
                CreateRecipe().
                    AddIngredient(ItemID.RainbowRod).
                    AddIngredient(phantoplasm.Type, 10).
                    AddIngredient(classicShadowspecBar.Type, 5).
                    AddTile(classicForge.Type).
                    Register();
            }
            else
            {
                Mod.Logger.Warn("绝学法杖：经典版灾厄里找不到 Phantoplasm / ShadowspecBar / DraedonsForge，经典配方未注册。");
            }
        }
    }
}
