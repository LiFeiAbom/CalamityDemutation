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
    /// 半影（Penumbra）—— 「超新星」下位链的第七件（链上最后一个下位件）。
    /// 老规矩取灾厄 **2.0**：46×32、伤害 **1008**、击退 8、使用/动画各 40 帧、
    /// **红底 + 月后 14 档（蓝，＝灾厄 DarkBlue）**、价值 **1 铂金 80 金**、弹速 **8**
    /// （2.0.3.9/2.0.4 是 830 伤害 + 弹速 9、1.4.4-release 是 725 / 35 帧 / 紫档——本件都不取）。
    /// </summary>
    /// <remarks>
    /// 效果：掷出一枚暗影炸弹，炸开时沿圆周迸出**会追踪的暗影魂**（常规 6 枚 ∶ 每枚伤害 ×0.15）；
    /// **潜行打击**改成**在鼠标位置当场显现**（速度 (0, -0.5) 缓慢上飘），炸出的魂更多（9 枚）但每枚更小（×0.08）。
    /// 另外本件自带 **+16% 暴击**——源特意用 <c>ModifyWeaponCrit</c> 而不是 <c>Item.crit</c>
    /// （源注释：tML 不喜欢 <c>SetDefaults</c> 里写高暴击值），本工程照此办理。
    /// <para>
    /// 配方照 2.0：灾厄魂 <c>RuinousSoul</c>×6 + 夜魇锭 <c>CosmiliteBar</c>×8 + 夜魇燃料 <c>NightmareFuel</c>×20；
    /// 站台按工程惯例分版本——现代版宇宙砧 <c>CosmicAnvil</c>、经典版嘉登熔炉 <c>DraedonsForge</c>
    /// （**经典版灾厄没有宇宙砧**）。三味材料两版都有（已核），缺件写警告、不静默消失。
    /// </para>
    /// </remarks>
    internal class Penumbra : ModItem
    {
        /// <summary>弹速（源 2.0 的 <c>ShootSpeed</c>；暗影魂的归航速度取它的 1.5 倍）</summary>
        public const float ShootSpeed = 8f;
        /// <summary>暴击加成（源用 <c>ModifyWeaponCrit</c> 累加的那 16 点）</summary>
        private const float CritBonus = 16f;

        /// <summary>研究解锁一份（源 2.0 写 SacrificeTotal = 1）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>基础属性：46×32、伤害 1008、击退 8、40 帧、月后 14 档、1 铂金 80 金、弹速 8</summary>
        public override void SetDefaults()
        {
            Item.width = 46;
            Item.height = 32;
            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item103;
            Item.value = Item.buyPrice(1, 80, 0, 0);
            Item.rare = ItemRarityID.Red;   // 基础稀有度红色，名称颜色由 postMoonLordRarity 覆盖
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;   // 月后 14：蓝（＝灾厄 DarkBlue）
            Item.damage = 1008;
            Item.useAnimation = Item.useTime = 40;
            Item.knockBack = 8f;
            Item.shoot = ModContent.ProjectileType<PenumbraBomb>();
            Item.shootSpeed = ShootSpeed;
            Item.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>+16% 暴击（照源走 <c>ModifyWeaponCrit</c>，不写进 SetDefaults）</summary>
        public override void ModifyWeaponCrit(Player player, ref float crit) => crit += CritBonus;
        /// <summary>
        /// 潜行打击就绪时，炸弹**直接在鼠标位置显现**（速度 (0, -0.5) 缓慢上飘、<c>ai[1] = 1</c>）并打上潜行标记；
        /// 否则走默认投掷。鼠标坐标照源算法，含重力翻转（gravDir = -1）的 Y 轴修正。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (!CDUtil.CanStealthStrike(player))
                return true;
            Vector2 mountCenter = player.RotatedRelativePoint(player.MountedCenter, true);
            float toMouseX = Main.mouseX + Main.screenPosition.X - mountCenter.X;
            float toMouseY = Main.mouseY + Main.screenPosition.Y - mountCenter.Y;
            if (player.gravDir == -1f)
                toMouseY = Main.screenPosition.Y + Main.screenHeight - Main.mouseY - mountCenter.Y;
            if ((float.IsNaN(toMouseX) && float.IsNaN(toMouseY)) || (toMouseX == 0f && toMouseY == 0f))
            {
                toMouseX = player.direction;
                toMouseY = 0f;
            }
            Vector2 spawnPosition = mountCenter + new Vector2(toMouseX, toMouseY);
            int bomb = Projectile.NewProjectile(source, spawnPosition, new Vector2(0f, -0.5f), type, damage, knockback, player.whoAmI, 0f, 1f);
            if (bomb >= 0 && bomb < Main.maxProjectiles)
                CDUtil.SetStealthStrike(Main.projectile[bomb]);
            return false;
        }
        /// <summary>
        /// 配方照 2.0：灾厄魂×6 + 夜魇锭×8 + 夜魇燃料×20；现代 @ 宇宙砧、经典 @ 嘉登熔炉
        /// （经典版没有宇宙砧），两分支各注册一条。
        /// </summary>
        public override void AddRecipes()
        {
            if (TryAddRecipeFrom("CalamityMod", "CosmicAnvil") ||
                TryAddRecipeFrom("CalamityModClassicPreTrailer", "DraedonsForge"))
                return;
            Mod.Logger.Warn("半影：找不到 灾厄魂 / 夜魇锭 / 夜魇燃料 或对应站台（宇宙砧 / 嘉登熔炉），配方未注册。");
        }
        /// <summary>从指定灾厄版本取三味材料与站台注册一条配方；缺任何一件即返回 false（不落半条配方）</summary>
        private bool TryAddRecipeFrom(string calamityModName, string stationName)
        {
            if (!ModLoader.TryGetMod(calamityModName, out Mod calamity))
                return false;
            if (!calamity.TryFind<ModItem>("RuinousSoul", out ModItem ruinousSoul) ||
                !calamity.TryFind<ModItem>("CosmiliteBar", out ModItem cosmiliteBar) ||
                !calamity.TryFind<ModItem>("NightmareFuel", out ModItem nightmareFuel) ||
                !calamity.TryFind<ModTile>(stationName, out ModTile station))
                return false;
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ruinousSoul.Type, 6);
            recipe.AddIngredient(cosmiliteBar.Type, 8);
            recipe.AddIngredient(nightmareFuel.Type, 20);
            recipe.AddTile(station.Type);
            recipe.Register();
            return true;
        }
    }
}
