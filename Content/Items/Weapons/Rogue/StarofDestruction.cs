using CalamityDemutation.Content.Items.Materials;
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
    /// 毁灭之星（Star of Destruction）—— 「超新星」下位链的第五件。
    /// 老规矩取灾厄 **2.0**：94×94、伤害 150、击退 10、**使用/动画各 32 帧**（2.0 源值 38，
    /// 用户 2026-10-08 指定改为 32）、**青档（Cyan）**、
    /// 价值 95 金、弹速 5、使用音 <c>SoundID.Item1</c>
    /// （2.0.3.9 起使用帧变 40 并加潜行倍率 0.8×、2.0.4 改红档、1.4.4-release 整件重做——本件都不取）。
    /// <para>
    /// 效果：射出一枚巨大**毁灭地雷**，命中/消散时炸成毁灭弹；**弹数随命中数增长（最多 16）**，
    /// **潜行打击**必定以最大弹数（16）炸开（源写法：潜行那一发伤害 ×0.8 并以 <c>ai[1] = 1</c> 生成）。
    /// </para>
    /// <para>
    /// 配方照 2.0：**本模组的熵构体 <c>MeldConstruct</c>×10 @ 远古操纵机**。
    /// 熵构体是本工程自持件、站台是原版，所以这一条**两分支通用**，不写版本判断。
    /// （源 2.0 的熵构体在灾厄 1.4.4 起已被删除——这正是本批次把熵构体一起搬进来的原因。）
    /// </para>
    /// </summary>
    internal class StarofDestruction : ModItem
    {
        /// <summary>潜行打击那一发的伤害倍率（照 2.0 源：<c>damage * 0.8f</c>）</summary>
        private const float StealthDamageMultiplier = 0.8f;
        /// <summary>数值膨胀后的面板伤害（用户 2026-10-08 指定：150 → 438）</summary>
        private const float InflatedDamage = 438f;
        /// <summary>当前生效的面板基础伤害：膨胀开关开启时用 <see cref="InflatedDamage"/>，否则维持 <c>Item.damage</c> 的源值 150</summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;

        /// <summary>研究解锁一份（源 2.0 写 SacrificeTotal = 1）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>
        /// 数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>。
        /// 本件所有派生伤害都从 <c>Shoot</c> 的 <c>damage</c> 参数往下传（毁灭地雷 → 毁灭弹 ×0.5），会自动跟随。
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base = BaseDamage;
        }
        /// <summary>基础属性：94×94、伤害 150、击退 10、32 帧（用户 2026-10-08 指定，源 38）、青档 95 金、弹速 5</summary>
        public override void SetDefaults()
        {
            Item.width = Item.height = 94;
            Item.damage = 150;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.useAnimation = Item.useTime = 32;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 10f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(0, 95, 0, 0);
            Item.rare = ItemRarityID.Cyan;
            Item.shoot = ModContent.ProjectileType<DestructionStar>();
            Item.shootSpeed = 5f;
            Item.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>
        /// 允许本件进「任意武器」前缀池（掷出类武器可附魔的关键）：照灾厄 <c>RogueWeapon</c> 的做法，
        /// 把 <c>WeaponPrefix()</c> 置真——tML 默认对自定义伤害类返回 false，详见震爆手雷里的长注释。
        /// </summary>
        public override bool WeaponPrefix() => true;
        /// <summary>照灾厄 <c>RogueWeapon</c> 显式关掉远程前缀</summary>
        public override bool RangedPrefix() => false;
        /// <summary>潜行打击就绪时射出"必爆 16 弹"的那一发（伤害 ×0.8、ai[1] = 1）并打上潜行标记</summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (!CDUtil.CanStealthStrike(player))
                return true;
            int star = Projectile.NewProjectile(source, position, velocity, type, (int)(damage * StealthDamageMultiplier), knockback, player.whoAmI, 0f, 1f);
            if (star >= 0 && star < Main.maxProjectiles)
                CDUtil.SetStealthStrike(Main.projectile[star]);
            return false;
        }
        /// <summary>配方照 2.0：本模组熵构体×10 @ 远古操纵机（自持件 + 原版站台 → 两分支通用）</summary>
        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<MeldConstruct>(10).
                AddTile(TileID.LunarCraftingStation).
                Register();
        }
    }
}
