using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 真·生物群系之剑 - 生物群系之剑的进阶形态
    /// 发射自动追踪、随生物群系变化的真·生物球（TrueBiomeOrb）
    /// </summary>
    internal class TrueBiomeBlade:ModItem
    {
        /// <summary>
        /// 图鉴研究解锁数量显式设为 1（该武器研究后即可解锁）
        /// </summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>
        /// 物品基础属性：伤害 160、使用时间 21 帧、击退 7.5、黄色稀有度；
        /// 主弹幕为真·生物球（TrueBiomeOrb），每次挥砍发射一次。
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 54;
            Item.damage = 160;
            Item.DamageType = DamageClass.Melee/* tModPorter Suggestion: Consider MeleeNoSpeed for no attack speed scaling */;
            Item.useAnimation = 21;
            Item.useTime = 21;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 7.5f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.height = 54;
            Item.value = Item.buyPrice(0, 80, 0, 0);
            Item.rare = ItemRarityID.Yellow;
            Item.shoot = ModContent.ProjectileType<TrueBiomeOrb>();
            Item.shootSpeed = 12f;
            Item.shootsEveryUse = true;
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
        /// 配方（分版本）：生物群系之剑 + 断裂英雄剑 + 灵气 + 深渊/虚空类灾厄材料
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient<BiomeBlade>();
                recipe.AddIngredient(ItemID.BrokenHeroSword);
                recipe.AddIngredient(ItemID.Ectoplasm, 5);
                recipe.AddIngredient(calamity.Find<ModItem>("DepthCells").Type, 10);
                recipe.AddIngredient(calamity.Find<ModItem>("Lumenyl").Type, 10);
                recipe.AddIngredient(calamity.Find<ModItem>("Voidstone").Type, 5);
                recipe.AddTile(TileID.MythrilAnvil);
                recipe.Register();
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient<BiomeBlade>();
                recipe1.AddIngredient(ItemID.BrokenHeroSword);
                recipe1.AddIngredient(ItemID.Ectoplasm, 5);
                recipe1.AddIngredient(calamity1.Find<ModItem>("LivingShard").Type, 5);
                recipe1.AddIngredient(calamity1.Find<ModItem>("DepthCells").Type, 10);
                recipe1.AddIngredient(calamity1.Find<ModItem>("Lumenite").Type, 10);
                recipe1.AddIngredient(calamity1.Find<ModItem>("Tenebris").Type, 5);
                recipe1.AddTile(TileID.MythrilAnvil);
                recipe1.Register();
            }
        }
    }
}
