using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 环境之刃 - 前中期近战剑
    /// 发射会随所在生物群系改变颜色与 debuff 的生物球（BiomeOrb）
    /// </summary>
    internal class BiomeBlade:ModItem
    {
        /// <summary>
        /// 图鉴研究解锁数量显式设为 1（该武器研究后即可解锁）
        /// </summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>
        /// 物品基础属性：伤害 63、使用时间 24 帧、击退 5、浅红稀有度；
        /// 主弹幕为生物球（BiomeOrb，随所在生物群系改变颜色与 debuff），每次挥砍发射一次。
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 42;
            Item.damage = 63;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 24;
            Item.useTime = 24;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 5f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.height = 42;
            Item.value = Item.buyPrice(0, 12, 0, 0);
            Item.rare = ItemRarityID.LightRed;
            Item.shoot = ModContent.ProjectileType<BiomeOrb>();
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
            {
                Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.Dirt);
            }
        }
        /// <summary>
        /// 配方：木剑 + 各类生物群系方块/材料，按腐化（黑檀石）或猩红（猩红石）世界分别注册
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) || ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.WoodenSword);
                recipe.AddIngredient(ItemID.DirtBlock, 10);
                recipe.AddIngredient(ItemID.SandBlock, 10);
                recipe.AddIngredient(ItemID.IceBlock, 10);
                recipe.AddIngredient(ItemID.EbonstoneBlock, 10);
                recipe.AddIngredient(ItemID.GlowingMushroom, 10);
                recipe.AddIngredient(ItemID.Marble, 10);
                recipe.AddIngredient(ItemID.Granite, 10);
                recipe.AddIngredient(ItemID.Hellstone, 10);
                recipe.AddIngredient(ItemID.Coral, 5);
                recipe.AddIngredient(ItemID.PearlstoneBlock, 10);
                recipe.AddTile(TileID.Anvils);
                recipe.Register();
                recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.WoodenSword);
                recipe.AddIngredient(ItemID.DirtBlock, 10);
                recipe.AddIngredient(ItemID.SandBlock, 10);
                recipe.AddIngredient(ItemID.IceBlock, 10);
                recipe.AddIngredient(ItemID.CrimstoneBlock, 10);
                recipe.AddIngredient(ItemID.GlowingMushroom, 10);
                recipe.AddIngredient(ItemID.Marble, 10);
                recipe.AddIngredient(ItemID.Granite, 10);
                recipe.AddIngredient(ItemID.Hellstone, 10);
                recipe.AddIngredient(ItemID.Coral, 5);
                recipe.AddIngredient(ItemID.PearlstoneBlock, 10);
                recipe.AddTile(TileID.Anvils);
                recipe.Register();
            }
        }
    }
}
