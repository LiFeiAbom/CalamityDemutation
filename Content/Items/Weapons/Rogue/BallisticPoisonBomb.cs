using CalamityDemutation.Content.Projectiles.Rogue;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Rogue
{
    /// <summary>
    /// 弹道毒炸弹（Ballistic Poison Bomb）—— 「超新星」下位链的第二件（震爆手雷 → **本件** → …）。
    /// 老规矩取灾厄 **2.0**：30×38、伤害 50、击退 6.5、使用/动画各 26 帧、**石灰档（Lime）**、
    /// 价值 60 金、弹速 12（2.0.3.9 起本体抬到 72 伤害并换成 Rarity7 价，1.4.4-release 又降到 57——本件不取）。
    /// <para>
    /// 效果：投出一枚**粘性**炸弹，落下（或命中敌人）后炸成尖刺 + 一片毒云；
    /// **潜行打击**一次抛出 3 枚、每枚伤害按 1/3 结算（照源 <c>Math.Max(damage / 3, 1)</c>）。
    /// </para>
    /// <para>
    /// 配方照 2.0：海沫炸弹 <c>SeafoamBomb</c> + 深渊细胞 <c>DepthCells</c>×10 + 硫磺沙 <c>SulphurousSand</c>×20
    /// + 暮色碎片 <c>Tenebris</c>×10 @ 秘银砧。这四件**两版灾厄都有同名物**，且站台是原版秘银砧，
    /// 所以两边材料同名同量、只注册**一条**配方（先取现代版、取不到再取经典版，都取不到就写警告，不静默消失）。
    /// </para>
    /// </summary>
    internal class BallisticPoisonBomb : ModItem
    {
        /// <summary>潜行打击一次抛出的枚数（照源）</summary>
        private const int StealthBombs = 3;

        /// <summary>研究解锁一份（源 2.0 写 SacrificeTotal = 1）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>基础属性：30×38、伤害 50、击退 6.5、26 帧、石灰档 60 金、弹速 12，伤害类型取盗贼</summary>
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 38;
            Item.damage = 50;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 26;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6.5f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(0, 60, 0, 0);
            Item.rare = ItemRarityID.Lime;
            Item.shoot = ModContent.ProjectileType<BallisticPoisonBombProj>();
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
        /// <summary>
        /// 潜行打击就绪时一次抛出 3 枚（角度扇形散开、每枚伤害取 1/3），逐枚标记成潜行打击；
        /// 否则返回 true 走默认投掷。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (!CDUtil.CanStealthStrike(player))
                return true;
            int spread = 5;
            for (int i = 0; i < StealthBombs; i++)
            {
                Vector2 perturbedSpeed = new Vector2(velocity.X + Main.rand.Next(-3, 4), velocity.Y + Main.rand.Next(-3, 4))
                    .RotatedBy(MathHelper.ToRadians(spread));
                int bomb = Projectile.NewProjectile(source, position, perturbedSpeed, type, Math.Max(damage / StealthBombs, 1), knockback, player.whoAmI);
                if (bomb >= 0 && bomb < Main.maxProjectiles)
                    CDUtil.SetStealthStrike(Main.projectile[bomb]);
                spread -= Main.rand.Next(2, 6);
            }
            return false;
        }
        /// <summary>
        /// 配方照 2.0：海沫炸弹 + 深渊细胞×10 + 硫磺沙×20 + 暮色碎片×10 @ 秘银砧
        /// （四件两版同名 → 只注册一条；先现代后经典）
        /// </summary>
        public override void AddRecipes()
        {
            if (TryAddRecipeFrom("CalamityMod") || TryAddRecipeFrom("CalamityModClassicPreTrailer"))
                return;
            Mod.Logger.Warn("弹道毒炸弹：两版灾厄都找不到 SeafoamBomb / DepthCells / SulphurousSand / Tenebris，配方未注册。");
        }
        /// <summary>从指定灾厄版本取四件材料注册一条配方；缺任何一件即返回 false（不落半条配方）</summary>
        private bool TryAddRecipeFrom(string calamityModName)
        {
            if (!ModLoader.TryGetMod(calamityModName, out Mod calamity))
                return false;
            if (!calamity.TryFind<ModItem>("SeafoamBomb", out ModItem seafoamBomb) ||
                !calamity.TryFind<ModItem>("DepthCells", out ModItem depthCells) ||
                !calamity.TryFind<ModItem>("SulphurousSand", out ModItem sulphurousSand) ||
                !calamity.TryFind<ModItem>("Tenebris", out ModItem tenebris))
                return false;
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(seafoamBomb.Type);
            recipe.AddIngredient(depthCells.Type, 10);
            recipe.AddIngredient(sulphurousSand.Type, 20);
            recipe.AddIngredient(tenebris.Type, 10);
            recipe.AddTile(TileID.MythrilAnvil);
            recipe.Register();
            return true;
        }
    }
}
