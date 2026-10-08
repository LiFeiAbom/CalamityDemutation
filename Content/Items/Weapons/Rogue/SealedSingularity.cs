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
    /// 封存奇点（Sealed Singularity）—— 「超新星」下位链的第六件。
    /// 老规矩取灾厄 **2.0**：34×34、伤害 260、击退 5、使用/动画各 25 帧、
    /// **月后 12 档（青绿，＝灾厄 Turquoise）**、价值 **1 铂金 20 金**（`Rarity12BuyPrice`）、
    /// 弹速 14、使用音 <c>SoundID.Item106</c>
    /// （2.0.3.9 起补了潜行倍率 0.72×、1.4.4-release 整件重做成 Holdout——本件都不取）。
    /// </summary>
    /// <remarks>
    /// 效果：命中/消散时碎裂，召出一个**黑洞**（把 500 像素内的普通敌怪往中心吸）；
    /// **潜行打击**那一发伤害 ×0.72，但召出的黑洞**多活 180 帧**、吸力 0.25（常规是 0.1）、
    /// 吸引半径也翻倍到 1000 像素。
    /// <para>
    /// 配方照 2.0：尘暴瓶 <c>DuststormInABottle</c> + 暗等离子 <c>DarkPlasma</c>×3 @ 远古操纵机。
    /// 这两味**现代版与经典版灾厄都有**（已核），站台是原版 → 只注册一条、两分支通用。
    /// </para>
    /// <para>
    /// 源里还写了一句 <c>Item.Calamity().donorItem = true</c>（灾厄的"捐赠者物品"标记，只影响外观相关处理），
    /// 本工程没有这套体系，**刻意不搬**。
    /// </para>
    /// </remarks>
    internal class SealedSingularity : ModItem
    {
        /// <summary>潜行打击那一发的伤害倍率（照 2.0 源：<c>damage * 0.72f</c>；源注释写"削 32%"）</summary>
        private const float StealthDamageMultiplier = 0.72f;

        /// <summary>研究解锁一份（源 2.0 写 SacrificeTotal = 1）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>基础属性：34×34、伤害 260、击退 5、25 帧、月后 12 档、1 铂金 20 金、弹速 14</summary>
        public override void SetDefaults()
        {
            Item.width = Item.height = 34;
            Item.damage = 260;
            Item.knockBack = 5f;
            Item.useAnimation = Item.useTime = 25;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.noMelee = Item.noUseGraphic = true;
            Item.UseSound = SoundID.Item106;
            Item.value = Item.buyPrice(1, 20, 0, 0);
            Item.rare = ItemRarityID.Red;   // 基础稀有度红色，名称颜色由 postMoonLordRarity 覆盖
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 12;   // 月后 12：青绿（＝灾厄 Turquoise）
            Item.shoot = ModContent.ProjectileType<SealedSingularityProj>();
            Item.shootSpeed = 14f;
            Item.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>潜行打击就绪时射出"加强黑洞"的那一发（伤害 ×0.72）并打上潜行标记</summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (!CDUtil.CanStealthStrike(player))
                return true;
            int singularity = Projectile.NewProjectile(source, position, velocity, type, (int)(damage * StealthDamageMultiplier), knockback, player.whoAmI);
            if (singularity >= 0 && singularity < Main.maxProjectiles)
                CDUtil.SetStealthStrike(Main.projectile[singularity]);
            return false;
        }
        /// <summary>配方照 2.0：尘暴瓶 + 暗等离子×3 @ 远古操纵机（两版同名 → 只注册一条）</summary>
        public override void AddRecipes()
        {
            if (TryAddRecipeFrom("CalamityMod") || TryAddRecipeFrom("CalamityModClassicPreTrailer"))
                return;
            Mod.Logger.Warn("封存奇点：两版灾厄都找不到 DuststormInABottle / DarkPlasma，配方未注册。");
        }
        /// <summary>从指定灾厄版本取两味材料注册一条配方；缺任何一件即返回 false（不落半条配方）</summary>
        private bool TryAddRecipeFrom(string calamityModName)
        {
            if (!ModLoader.TryGetMod(calamityModName, out Mod calamity))
                return false;
            if (!calamity.TryFind<ModItem>("DuststormInABottle", out ModItem duststorm) ||
                !calamity.TryFind<ModItem>("DarkPlasma", out ModItem darkPlasma))
                return false;
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(duststorm.Type);
            recipe.AddIngredient(darkPlasma.Type, 3);
            recipe.AddTile(TileID.LunarCraftingStation);
            recipe.Register();
            return true;
        }
    }
}
