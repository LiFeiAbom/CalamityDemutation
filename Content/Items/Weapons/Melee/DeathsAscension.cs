using CalamityDemutation.Content.Projectiles.Melee;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 死神擢升（Death's Ascension，移植自灾厄 2.0.4 的 <c>DeathsAscension</c>）——
    /// 挥砍时甩出一串追踪飞镰。按用户口径去掉源的左右键双模式（源左键是引导挥砍、右键才甩镰），
    /// 只保留单左键：挥砍本体有近战判定（照抄源 <c>noMelee=false</c>），每次甩出 4 把追踪飞镰（伤害 ×0.125）。
    /// 1200 伤害、24 帧、击退 9、射速 12；纯绿稀有度（灾厄 Rarity 13）对应本工程月后稀有度 13（荧光绿）。
    /// </summary>
    internal class DeathsAscension:ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>
        /// 物品基础属性：70×70、伤害 1200、24 帧使用、击退 9、挥砍风格（带音效与自动复用）；
        /// 挥砍本体参与判定（<c>noMelee=false</c>），弹幕固定指向 <see cref="DeathsAscensionProjectile"/>。
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 70;
            Item.height = 70;
            Item.damage = 1200;
            Item.knockBack = 9f;
            Item.useTime = Item.useAnimation = 24;
            Item.DamageType = DamageClass.Melee;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item71;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.noMelee = false;
            Item.shoot = ModContent.ProjectileType<DeathsAscensionProjectile>();
            Item.shootSpeed = 12f;
            Item.value = Item.buyPrice(1, 75, 0, 0);
            Item.rare = ItemRarityID.Red;                   // 基础稀有度红，真正的名称颜色由 postMoonLordRarity 覆盖
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 13;  // 月后稀有度 13（荧光绿，同 Bloodflare/OmegaBlue）
        }
        /// <summary>
        /// 出手：每次挥砍甩出 4 把追踪飞镰，伤害为面板的 1/8（×0.125），带 ±9 的随机散布。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int spreadfactor = 9;
            for (int index = 0; index < 4; ++index)
            {
                float SpeedX = velocity.X + Main.rand.NextFloat(-spreadfactor, spreadfactor + 1);
                float SpeedY = velocity.Y + Main.rand.NextFloat(-spreadfactor, spreadfactor + 1);
                Projectile.NewProjectile(source, position.X, position.Y, SpeedX, SpeedY, type, (int)(damage * 0.125f), knockback, player.whoAmI, 0f, 0f);
            }
            return false;
        }
        /// <summary>
        /// 配方：死神镰刀 + 毁灭之魂×4 + 扭曲虚空×1 + 暗影之魂×15，在远古操纵台合成。
        /// 四个材料在现代版 / 经典版灾厄中均存在，故分两条同名配方注册。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("RuinousSoul", out ModItem ruinousSoul)
                    && calamity.TryFind<ModItem>("TwistingNether", out ModItem twistingNether))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.DeathSickle);
                    recipe.AddIngredient(ruinousSoul.Type, 4);
                    recipe.AddIngredient(twistingNether.Type);
                    recipe.AddIngredient(ItemID.SoulofNight, 15);
                    recipe.AddTile(TileID.LunarCraftingStation);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if (classic.TryFind<ModItem>("RuinousSoul", out ModItem classicRuinousSoul)
                    && classic.TryFind<ModItem>("TwistingNether", out ModItem classicTwistingNether))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient(ItemID.DeathSickle);
                    recipeClassic.AddIngredient(classicRuinousSoul.Type, 4);
                    recipeClassic.AddIngredient(classicTwistingNether.Type);
                    recipeClassic.AddIngredient(ItemID.SoulofNight, 15);
                    recipeClassic.AddTile(TileID.LunarCraftingStation);
                    recipeClassic.Register();
                }
            }
        }
    }
}
