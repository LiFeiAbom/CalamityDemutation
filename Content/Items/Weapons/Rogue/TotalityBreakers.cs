using CalamityDemutation.Content.Projectiles.Rogue;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Rogue
{
    /// <summary>
    /// 破坏者（Totality Breakers）—— 「超新星」下位链的第三件。
    /// 老规矩取灾厄 **2.0**：32×42、伤害 55、击退 5、使用/动画各 28 帧、**石灰档（Lime）**、
    /// 价值 60 金、弹速 12、使用音 <c>SoundID.Item106</c>
    /// （2.0.3.9 起本体改成 50 伤害 + 潜行倍率 1.3×、1.4.4-release 是 64——本件不取）。
    /// <para>
    /// 效果：掷出一瓶烈性黑焦油，落地炸成一大片燃烧的**黑焦油**（把敌人"油浸"后再点燃）;
    /// **潜行打击**这一瓶伤害 ×1.15，并且飞行途中每 20 帧往身后滴一团焦油。
    /// </para>
    /// <para>
    /// 配方照 2.0：燃烧瓶×50（原版）+ 圣化水 <c>ConsecratedWater</c> + 亵渎水 <c>DesecratedWater</c>
    /// + 乏燃料容器 <c>SpentFuelContainer</c> + 日纱 <c>SolarVeil</c>×10 @ 秘银砧。
    /// **经典版灾厄没有后四件**（它们比经典版那条线晚），所以这条配方只有现代分支——照源逐件取，
    /// 缺件就写警告，不静默消失。
    /// </para>
    /// </summary>
    internal class TotalityBreakers : ModItem
    {
        /// <summary>潜行打击的伤害倍率（照 2.0 源：<c>damage * 1.15f</c>）</summary>
        private const float StealthDamageMultiplier = 1.15f;

        /// <summary>研究解锁一份（源 2.0 写 SacrificeTotal = 1）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>基础属性：32×42、伤害 55、击退 5、28 帧、石灰档 60 金、弹速 12，伤害类型取盗贼</summary>
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 42;
            Item.damage = 55;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 28;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 5f;
            Item.UseSound = SoundID.Item106;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(0, 60, 0, 0);
            Item.rare = ItemRarityID.Lime;
            Item.shoot = ModContent.ProjectileType<TotalityFlask>();
            Item.shootSpeed = 12f;
            Item.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>
        /// 允许本件进「任意武器」前缀池（掷出类武器可附魔的关键）：照灾厄 <c>RogueWeapon</c> 的做法，
        /// 把 <c>WeaponPrefix()</c> 置真——tML 默认对自定义伤害类返回 false，详见震爆手雷里的长注释。
        /// </summary>
        public override bool WeaponPrefix() => true;
        /// <summary>照灾厄 <c>RogueWeapon</c> 显式关掉远程前缀</summary>
        public override bool RangedPrefix() => false;
        /// <summary>潜行打击就绪时掷出一瓶"加大号"（伤害 ×1.15）并打上潜行标记；否则走默认投掷</summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (!CDUtil.CanStealthStrike(player))
                return true;
            int flask = Projectile.NewProjectile(source, position, velocity, type, (int)(damage * StealthDamageMultiplier), knockback, player.whoAmI);
            if (flask >= 0 && flask < Main.maxProjectiles)
                CDUtil.SetStealthStrike(Main.projectile[flask]);
            return false;
        }
        /// <summary>
        /// 配方照 2.0：燃烧瓶×50 + 圣化水 + 亵渎水 + 乏燃料容器 + 日纱×10 @ 秘银砧（仅现代分支）
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) &&
                calamity.TryFind<ModItem>("ConsecratedWater", out ModItem consecratedWater) &&
                calamity.TryFind<ModItem>("DesecratedWater", out ModItem desecratedWater) &&
                calamity.TryFind<ModItem>("SpentFuelContainer", out ModItem spentFuelContainer) &&
                calamity.TryFind<ModItem>("SolarVeil", out ModItem solarVeil))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.MolotovCocktail, 50);
                recipe.AddIngredient(consecratedWater.Type);
                recipe.AddIngredient(desecratedWater.Type);
                recipe.AddIngredient(spentFuelContainer.Type);
                recipe.AddIngredient(solarVeil.Type, 10);
                recipe.AddTile(TileID.MythrilAnvil);
                recipe.Register();
                return;
            }
            Mod.Logger.Warn("破坏者：现代版灾厄里找不到 圣化水 / 亵渎水 / 乏燃料容器 / 日纱，配方未注册（经典版没有这四件，故无经典分支）。");
        }
    }
}
