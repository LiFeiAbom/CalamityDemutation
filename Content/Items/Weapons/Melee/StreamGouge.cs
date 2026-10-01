using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 宇宙暗流（Stream Gouge）—— 移植自灾厄大修 Beta1.12 的 <c>StreamGougeOld</c>（CI 里的旧版宇宙暗流）。
    /// 月后宇宙锭长矛：本体不挥砍也不画本体，出手即从手里刺出一根宇宙长矛（<see cref="StreamGougeProj"/>），
    /// 长矛刺出的第一帧再额外甩出一束可无限穿透的宇宙精粹（<see cref="EssenceBeam"/>）。
    /// <para>
    /// 与 CI 源的差异（其余数值全部照搬）：
    /// ① 稀有度走本工程的月后体系：CI 的 <c>DeepBlue</c> 稀有度（Rarity14）→ <c>ItemRarityID.Red</c> +
    ///    <c>postMoonLordRarity = 14</c>（同为蓝名），价值同为 2 铂金（CI 的 <c>RarityPriceDeepBlue</c>）；
    /// ② 配方按本工程口径分现代版 / 经典版各一条（源只写了现代版的宇宙砧）；
    /// ③ 源里的 <c>ProjShootSpeed</c> / <c>FadeoutSpeed</c> 两个静态字段从未被任何代码读取，不再保留；
    /// ④ 武器基类由 CI 的 <c>CIMelee</c> 展开成本工程的写法：它只提供了创造栏归类与挥砍位置修正
    ///    （本工程对应 <c>CDUtil.BetterSwing</c>），而本武器 <c>noMelee = true</c>，挥砍修正不会被触发，故不写空壳。
    /// </para>
    /// </summary>
    internal class StreamGouge : ModItem
    {
        /// <summary>静态属性：登记为原版意义上的"长矛"，并设为唯一物品（照源）</summary>
        public override void SetStaticDefaults()
        {
            ItemID.Sets.Spears[Type] = true;
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>
        /// 物品基础属性：100×100、伤害 600（源值；数值膨胀开关开启时面板回调到 1800）、
        /// 18 帧出手、击退 9.75、自动挥舞；
        /// 本体无挥砍判定也不画本体，伤害全部由长矛弹幕承担，月后稀有度 14（蓝）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 100;
            Item.height = 100;
            Item.damage = 600;
            Item.DamageType = DamageClass.Melee;
            Item.noMelee = true;        // 本体不挥砍：伤害全在长矛弹幕上
            Item.noUseGraphic = true;   // 不画本体：表现交给长矛弹幕
            Item.useAnimation = 18;
            Item.useTime = 18;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 9.75f;
            Item.UseSound = SoundID.Item20;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(2, 0, 0, 0);
            Item.rare = ItemRarityID.Red;                  // 基础稀有度红色，名称颜色由 postMoonLordRarity 覆盖
            Item.shoot = ModContent.ProjectileType<StreamGougeProj>();
            Item.shootSpeed = 25f;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;   // 月后稀有度 14：蓝（对应 CI 的 DeepBlue）
        }
        /// <summary>
        /// 数值膨胀后的面板伤害（用户 2026-10-01 指定：宇宙暗流 600 → 1800，
        /// 即 LAP 分档表的「神吞后 ×3」——本武器由宇宙锭制作，宇宙锭出自神明吞噬者宝藏袋）。
        /// </summary>
        private const float InflatedDamage = 1800f;
        /// <summary>
        /// 当前生效的面板基础伤害：膨胀开关开启时用 <see cref="InflatedDamage"/>，否则维持 <c>Item.damage</c> 的源值 600。
        /// </summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>
        /// 数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>（运行时读配置，游戏内切换即时生效）。
        /// 本体不出伤，伤害全在长矛弹幕上，而弹幕取的是 <c>Shoot</c> 传进来的 <c>damage</c>，会自动跟随。
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base = BaseDamage;
        }
        /// <summary>出手：在身前一段距离处刺出长矛弹幕（照源写法：位置取 position + velocity，速度原样传入）</summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position + velocity, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
        /// <summary>掉落在地上时叠一层 Glow 发光贴图（照源 PostDrawInWorld，绘制接口换成本工程通用的单帧发光绘制）</summary>
        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Item.DrawItemGlowmaskSingleFrame(spriteBatch, rotation, ModContent.Request<Texture2D>("CalamityDemutation/Content/Items/Weapons/Melee/StreamGougeGlow").Value);
        }
        /// <summary>
        /// 配方（分版本）：宇宙锭 ×14，于月后站台合成。
        /// 现代版灾厄 = 宇宙砧（CosmicAnvil，照源）；经典版灾厄对应站台为嘉登熔炉（DraedonsForge），材料数量沿用源的 14。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("CosmiliteBar", out ModItem cosmiliteBar)
                    && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(cosmiliteBar.Type, 14);
                    recipe.AddTile(cosmicAnvil.Type);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if (classic.TryFind<ModItem>("CosmiliteBar", out ModItem classicCosmiliteBar)
                    && classic.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient(classicCosmiliteBar.Type, 14);
                    recipeClassic.AddTile(draedonsForge.Type);
                    recipeClassic.Register();
                }
            }
        }
    }
}
