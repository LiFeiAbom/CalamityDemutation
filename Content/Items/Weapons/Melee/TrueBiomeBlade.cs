using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 真·环境之刃 - 环境之刃的进阶形态
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
        /// 物品基础属性：伤害 160（源值；数值膨胀开关开启时面板回调到 400）、
        /// 使用时间 21 帧、击退 7.5、黄色稀有度；
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
        }
        /// <summary>
        /// 数值膨胀后的面板伤害（用户 2026-10-01 指定：真·环境之刃 160 → 400）。
        /// </summary>
        private const float InflatedDamage = 400f;
        /// <summary>
        /// 当前生效的面板基础伤害：膨胀开关开启时用 <see cref="InflatedDamage"/>，否则维持 <c>Item.damage</c> 的源值 160。
        /// </summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>
        /// 数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>（运行时读配置，游戏内切换即时生效）。
        /// 真·生物球走默认发射路径，取的就是本次修正后的面板值，会自动跟随。
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base = BaseDamage;
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
        /// 配方（分版本）：环境之刃 + 断裂英雄剑 + 灵气 + 深渊/虚空类灾厄材料
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("DepthCells", out ModItem depthCells)
                    && calamity.TryFind<ModItem>("Lumenyl", out ModItem lumenyl)
                    && calamity.TryFind<ModItem>("Voidstone", out ModItem voidstone))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<BiomeBlade>();
                    recipe.AddIngredient(ItemID.BrokenHeroSword);
                    recipe.AddIngredient(ItemID.Ectoplasm, 5);
                    recipe.AddIngredient(depthCells.Type, 10);
                    recipe.AddIngredient(lumenyl.Type, 10);
                    recipe.AddIngredient(voidstone.Type, 5);
                    recipe.AddTile(TileID.MythrilAnvil);
                    recipe.Register();
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if (classic.TryFind<ModItem>("LivingShard", out ModItem livingShard)
                    && classic.TryFind<ModItem>("DepthCells", out ModItem classicDepthCells)
                    && classic.TryFind<ModItem>("Lumenite", out ModItem lumenite)
                    && classic.TryFind<ModItem>("Tenebris", out ModItem tenebris))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient<BiomeBlade>();
                    recipeClassic.AddIngredient(ItemID.BrokenHeroSword);
                    recipeClassic.AddIngredient(ItemID.Ectoplasm, 5);
                    recipeClassic.AddIngredient(livingShard.Type, 5);
                    recipeClassic.AddIngredient(classicDepthCells.Type, 10);
                    recipeClassic.AddIngredient(lumenite.Type, 10);
                    recipeClassic.AddIngredient(tenebris.Type, 5);
                    recipeClassic.AddTile(TileID.MythrilAnvil);
                    recipeClassic.Register();
                }
            }
        }
    }
}
