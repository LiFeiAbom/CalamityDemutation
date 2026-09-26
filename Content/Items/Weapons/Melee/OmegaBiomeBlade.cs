using CalamityDemutation.Content.Items.Materials;
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
    /// 欧米茄环境之刃 - 环境之刃的终级形态
    /// 每次挥砍散射 5 颗自动追踪、随生物群系与月相变化的 Ω 生物球（OmegaBiomeOrb）
    /// </summary>
    internal class OmegaBiomeBlade:ModItem
    {
        /// <summary>
        /// 图鉴研究解锁数量显式设为 1（该武器研究后即可解锁）
        /// </summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>
        /// 物品基础属性：伤害 220、使用时间 18 帧、击退 8、红色稀有度；
        /// 主弹幕为 Ω 生物球（OmegaBiomeOrb），每次挥砍散射 5 颗，月后稀有度 12。
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 62;
            Item.damage = 220;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 18;
            Item.useTime = 18;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 8;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.height = 62;
            Item.value = Item.buyPrice(1, 20, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.shoot = ModContent.ProjectileType<OmegaBiomeOrb>();
            Item.shootSpeed = 15f;
            Item.shootsEveryUse = true;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 12;
        }
        /// <summary>
        /// 射击逻辑：每次挥砍发射 5 颗带随机速度偏转的 Ω 生物球
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            for (int projectiles = 0; projectiles < 5; projectiles++)
            {
                // 每颗球在基础速度上随机 ±2 个单位（0.05 * 40）
                float speedX = velocity.X + Main.rand.Next(-40, 41) * 0.05f;
                float speedY = velocity.Y + Main.rand.Next(-40, 41) * 0.05f;
                Projectile.NewProjectile(source, position, new Vector2(speedX, speedY), type, damage, knockback, player.whoAmI);
            }
            return false;
        }
        /// <summary>
        /// 挥砍特效：修正挥舞位置，偶尔扬起泥土粉尘
        /// </summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.NextBool(5))
                Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.Dirt);
        }
        /// <summary>
        /// 配方（分版本）：真·环境之刃 + 灾厄核心 + 生命合金/生命锭 + 银河奇点 + 月亮锭
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("CoreofCalamity", out ModItem coreofCalamity)
                    && calamity.TryFind<ModItem>("LifeAlloy", out ModItem lifeAlloy))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<TrueBiomeBlade>();
                    recipe.AddIngredient(coreofCalamity.Type,3);
                    recipe.AddIngredient(lifeAlloy.Type,3);
                    recipe.AddIngredient<GalacticaSingularity>(3);
                    recipe.AddIngredient(ItemID.LunarBar, 5);
                    recipe.AddTile(TileID.LunarCraftingStation);
                    recipe.Register();
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModItem>("CoreofCalamity", out ModItem classicCoreofCalamity)
                    && calamity1.TryFind<ModItem>("BarofLife", out ModItem barofLife)
                    && calamity1.TryFind<ModItem>("GalacticaSingularity", out ModItem galacticaSingularity))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient<TrueBiomeBlade>();
                    recipe1.AddIngredient(classicCoreofCalamity.Type, 3);
                    recipe1.AddIngredient(barofLife.Type, 3);
                    recipe1.AddIngredient(galacticaSingularity.Type, 3);
                    recipe1.AddIngredient(ItemID.LunarBar, 5);
                    recipe1.AddTile(TileID.LunarCraftingStation);
                    recipe1.Register();
                }
            }
        }
    }
}
