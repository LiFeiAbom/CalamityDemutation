using CalamityDemutation.Content.Projectiles.Rogue;
using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Rogue
{
    /// <summary>
    /// 超新星（Supernova）—— 「超新星」下位链的**最后一件**（链顶）。
    /// 老规矩取灾厄 **2.0**：34×36、伤害 **675**、击退 8、使用/动画各 24 帧、
    /// **红底 + 月后 15 档（紫，＝灾厄 Violet）**、价值 **1 铂金 50 金**（`Rarity15BuyPrice`）、
    /// 弹速 16、使用音 <c>SoundID.Item15</c>
    /// （2.0.3.9 数值相同、2.0.4 起整件重做成 106×112 的智能炸弹 Holdout——本件都不取）。
    /// </summary>
    /// <remarks>
    /// 效果：投出一枚超新星炸弹，命中/落地时产生**巨大爆炸**，炸开成尖刺（3~4 枚、伤害 ×0.6）
    /// 与**追踪能量**（6 枚、伤害 ×0.5）；**潜行打击**那一发伤害 ×1.08，且飞行途中每 8 帧
    /// 向上喷一枚追踪能量（伤害 ×0.48）。
    /// <para>
    /// 配方＝用户拍板的 **2.0 那条**（本模组下位链全闭环）：
    /// **封存奇点 + 毁灭之星 + 破坏者 + 弹道毒炸弹 + 震爆手雷×200 + 半影**（六件全是本模组自持件）
    /// **+ 奇迹物质 `MiracleMatter`**（灾厄本体材料）@ **嘉登熔炉**。
    /// **经典分支去掉奇迹物质那一味**——经典版灾厄没有这件（本机已核 cal-1.4.2.101），其余七味照旧，
    /// 这样经典分支也能合成（主导决定，见工程记忆第 8 节）。
    /// </para>
    /// </remarks>
    internal class Supernova : ModItem
    {
        /// <summary>潜行打击那一发的伤害倍率（照 2.0 源：<c>damage * 1.08f</c>）</summary>
        private const float StealthDamageMultiplier = 1.08f;
        /// <summary>2.0 配方里震爆手雷的用量（照源）</summary>
        private const int ShockGrenadeCount = 200;
        /// <summary>数值膨胀后的面板伤害（用户 2026-10-08 指定：675 → 2250）</summary>
        private const float InflatedDamage = 2250f;
        /// <summary>当前生效的面板基础伤害：膨胀开关开启时用 <see cref="InflatedDamage"/>，否则维持 <c>Item.damage</c> 的源值 675</summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;

        /// <summary>研究解锁一份（源 2.0 写 SacrificeTotal = 1）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>
        /// 数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>。
        /// 本件派生伤害（炸弹 → 爆炸全额 / 尖刺 ×0.6 / 追踪能量 ×0.5、潜行外溢 ×0.48）
        /// 全部由 <c>Shoot</c> 的 <c>damage</c> 与弹幕伤害往下传，会自动跟随。
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base = BaseDamage;
        }
        /// <summary>基础属性：34×36、伤害 675、击退 8、24 帧、月后 15 档、1 铂金 50 金、弹速 16</summary>
        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 36;
            Item.damage = 675;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 24;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 8f;
            Item.UseSound = SoundID.Item15;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(1, 50, 0, 0);
            Item.rare = ItemRarityID.Red;   // 基础稀有度红色，名称颜色由 postMoonLordRarity 覆盖
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15;   // 月后 15：紫（＝灾厄 Violet）
            Item.shoot = ModContent.ProjectileType<SupernovaBomb>();
            Item.shootSpeed = 16f;
            Item.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>
        /// 允许本件进「任意武器」前缀池（掷出类武器可附魔的关键）：照灾厄 <c>RogueWeapon</c> 的做法，
        /// 把 <c>WeaponPrefix()</c> 置真——tML 默认对自定义伤害类返回 false，详见震爆手雷里的长注释。
        /// </summary>
        public override bool WeaponPrefix() => true;
        /// <summary>照灾厄 <c>RogueWeapon</c> 显式关掉远程前缀</summary>
        public override bool RangedPrefix() => false;
        /// <summary>潜行打击就绪时投出"能量外溢"的那一发（伤害 ×1.08）并打上潜行标记</summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (!CDUtil.CanStealthStrike(player))
                return true;
            int bomb = Projectile.NewProjectile(source, position, velocity, type, (int)(damage * StealthDamageMultiplier), knockback, player.whoAmI);
            if (bomb >= 0 && bomb < Main.maxProjectiles)
                CDUtil.SetStealthStrike(Main.projectile[bomb]);
            return false;
        }
        /// <summary>
        /// 配方照 2.0：六件本模组自持件 + 奇迹物质 @ 嘉登熔炉；经典分支去掉奇迹物质（经典版没有它）。
        /// 两分支各自注册一条，缺件写警告、不静默消失。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) &&
                calamity.TryFind<ModItem>("MiracleMatter", out ModItem miracleMatter) &&
                calamity.TryFind<ModTile>("DraedonsForge", out ModTile forge))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient<SealedSingularity>();
                recipe.AddIngredient<StarofDestruction>();
                recipe.AddIngredient<TotalityBreakers>();
                recipe.AddIngredient<BallisticPoisonBomb>();
                recipe.AddIngredient<ShockGrenade>(ShockGrenadeCount);
                recipe.AddIngredient<Penumbra>();
                recipe.AddIngredient(miracleMatter.Type);
                recipe.AddTile(forge.Type);
                recipe.Register();
            }
            else
            {
                Mod.Logger.Warn("超新星：现代版灾厄里找不到 MiracleMatter / DraedonsForge，本条配方未注册。");
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic) &&
                classic.TryFind<ModTile>("DraedonsForge", out ModTile classicForge))
            {
                Recipe classicRecipe = CreateRecipe();
                classicRecipe.AddIngredient<SealedSingularity>();
                classicRecipe.AddIngredient<StarofDestruction>();
                classicRecipe.AddIngredient<TotalityBreakers>();
                classicRecipe.AddIngredient<BallisticPoisonBomb>();
                classicRecipe.AddIngredient<ShockGrenade>(ShockGrenadeCount);
                classicRecipe.AddIngredient<Penumbra>();
                classicRecipe.AddTile(classicForge.Type);
                classicRecipe.Register();
            }
            else
            {
                Mod.Logger.Warn("超新星：经典版灾厄里找不到 DraedonsForge，本条配方未注册。");
            }
        }
    }
}
