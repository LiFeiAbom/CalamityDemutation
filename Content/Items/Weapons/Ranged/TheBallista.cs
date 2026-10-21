using CalamityDemutation.Content.Projectiles.Ranged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Ranged
{
    /// <summary>
    /// 弩炮（The Ballista） - 射手弓，天堂之风链**批 4 的第一件**（T1 六把武器之一），
    /// ＝灾厄 2.0 的 <c>Items/Weapons/Ranged/TheBallista.cs</c>（逐行照搬）。
    /// <para>
    /// 面板 / 机制**全取 2.0**；经典分支照经典配方——那份配方**与 2.0 逐字相同、且三味料全是原版件**，
    /// 故只注册一条、数值共用 2.0（经典版的 145 伤 / 20 帧 **不取**）。
    /// 源 tooltip：<c>Converts wooden arrows into greatarrows that crush enemy armor and break into shards on death</c>。
    /// </para>
    /// <para>
    /// 面板（照 2.0）：判定 40×70（贴图 40×78）/ 伤害 99 / 使用·动画 22 帧 / 击退 8 / 弹速 20 /
    /// 箭类弹药 / 使用音 `Item5` / 黄档（原版档，不涉月后档）/ 80 金。
    /// </para>
    /// <para>
    /// **任何弹药一律换成 <see cref="BallistaGreatArrow"/>**（弹药全转化，用户 2026-10-15 对天堂之风
    /// **及其整条下位链**统一的口径）。源既不归一化出手速度、也不打 `noDropItem`、更没有贴墙收箭——**照源保留**。
    /// </para>
    /// </summary>
    internal class TheBallista : ModItem
    {
        /// <summary>配方里的灵气数量（照源 10）</summary>
        private const int EctoplasmCount = 10;
        /// <summary>当前生效的面板基础伤害：开则用膨胀值，否则维持 <c>Item.damage</c> 的 99</summary>
        /// <summary>研究解锁一份</summary>
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        /// <summary>
        /// 基础属性：判定 40×70（照源；贴图 40×78，判定比贴图矮）/ 伤害 99 / 使用·动画 22 帧 /
        /// 击退 8 / 弹速 20 / 箭类弹药 / 使用音 `Item5` / 黄档 / 80 金
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 40;                          // 照源
            Item.height = 70;                         // 照源（贴图 40×78）
            Item.damage = 99;                         // 面板伤害（照 2.0；经典版是 145）
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;                      // 拉弓动画不造成伤害
            Item.knockBack = 8f;
            Item.value = Item.buyPrice(0, 80, 0, 0);  // 价值 80 金（照源）
            Item.rare = ItemRarityID.Yellow;          // 黄档（原版档）
            Item.UseSound = SoundID.Item5;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<BallistaGreatArrow>();
            Item.shootSpeed = 20f;
            Item.useAmmo = AmmoID.Arrow;              // 吃箭类弹药
        }
        /// <summary>
        /// 本件派生伤害走 <c>Shoot</c> 的 <c>damage</c> 参数（弩箭、以及弩箭迸出的碎片都由它派生），会自动跟随。
        /// </summary>
        /// <summary>
        /// **任何弹药都换成 <see cref="BallistaGreatArrow"/>**（弹药全转化，用户 2026-10-15 定；
        /// 源 2.0 是"木箭才转化、非木箭照原弹种射出"，本工程按新口径不取）
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<BallistaGreatArrow>(), damage, knockback, player.whoAmI);
            return false;
        }
        /// <summary>
        /// 持握偏移（用户 2026-10-15 对整条链指定的"贴图整体右移"口径）；
        /// <c>HoldoutOffset().X</c> 直接是贴图原点 X（模组默认 10，越小越往右），故取负值。
        /// </summary>
        public override Vector2? HoldoutOffset() => new Vector2(-10, 0);
        /// <summary>
        /// 配方（照源，@秘银砧）：骨髓弓 + 远古战斗护甲材料 + 灵气×10。
        /// 2.0 与经典版**逐字相同、且三味全是原版件** ⇒ 不分公司版本、只注册一条
        /// （这正是它与链上其他件"两版各注册一条"的差别所在）。
        /// </summary>
        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.Marrow).
                AddIngredient(ItemID.AncientBattleArmorMaterial).
                AddIngredient(ItemID.Ectoplasm, EctoplasmCount).
                AddTile(TileID.MythrilAnvil).
                Register();
        }
    }
}
