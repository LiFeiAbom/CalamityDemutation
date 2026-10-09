using CalamityDemutation.Content.Projectiles.Summon;
using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Summon
{
    /// <summary>
    /// 星律之握览（SarosPossession）—— 自持件，老规矩取灾厄 **2.0**（用户 2026-10-09 点名移植：
    /// CI 的「光阴流时伞」与 `MountedScannerLegacy` 的配方都吃本体那件 `SarosPossession`，故整件搬进来）。
    /// 2.0 口径：贴图 56×56、伤害 **171**、击退 4、魔力 10、使用/动画 **10 帧**、弹速 10、
    /// **红底 + 月后 14 档（蓝，＝灾厄 `DarkBlue`）**、价值 **1 铂金 40 金**、音 `SoundID.DD2_BetsyFlameBreath`；
    /// 在鼠标处召唤一道**辐光光环**（<see cref="SarosAura"/>）。
    /// </summary>
    /// <remarks>
    /// 效果（2.0 口径，与天狼星同一套"吃满栏位"设计）：出手时**消耗全部剩余召唤栏**——
    /// `HoldItem` 每帧数出"除自己以外已占用的栏位"，剩余栏位写进光环的 <c>ai[0]</c>；
    /// 光环每帧把 <c>minionSlots</c> 刷成这个数（吃光栏位），且**栏位越多越强**：
    /// 弹幕伤害 ×<c>(log₃(栏位数) + 1)</c>（超过 3 倍走软上限）、生成率 <c>130 × 0.9^栏位数</c>（下限 7 帧）。
    /// <para>
    /// 与源的差异（照天狼星那套工程约定）：㈠ 源把剩余栏位存在 ModItem 的实例字段 `radianceSlots` 上——
    /// ModItem 是全类型共享单例，联机时两名玩家会互相串数值，本件改存
    /// <c>CalamityDemutationPlayer.sarosSlots</c>（工程记忆第 5 节）；
    /// ㈡ 源用灾厄 `CalamityUtils.KillShootProjectiles` 清场，本工程是软依赖、不引用灾厄类型，
    /// 照既有写法写等价的"先杀掉自己在场的同类再召唤"循环。
    /// </para>
    /// <para>
    /// 配方（用户 2026-10-09 拍板）：**天狼星（本工程自持件）+ 夜魇锭 `CosmiliteBar`×8 +
    /// 暗阳碎片 `DarksunFragment`×8 @ 宇宙砧 `CosmicAnvil`**——照源 2.0 的配方，只把第一味换成我们自持的天狼星，
    /// 链条就此闭合：我们的天狼星 → 本件 → 我们的光阴流时伞。本件没有经典版，故只注册现代分支一条。
    /// </para>
    /// <para>数值膨胀：用户 2026-10-09 明确"后续一并处理"，本件暂不挂 `StatInflation`。</para>
    /// </remarks>
    internal class SarosPossession:ModItem
    {
        /// <summary>研究解锁一份（源 2.0 写 `SacrificeTotal = 1`）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>
        /// 基础属性：照源 2.0（56×56、伤害 171、击退 4、魔力 10、使用 10 帧、弹速 10、月后 14 档、1 铂金 40 金）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 56;
            Item.height = 56;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.UseSound = SoundID.DD2_BetsyFlameBreath;
            Item.DamageType = DamageClass.Summon;
            Item.mana = 10;
            Item.damage = 171;
            Item.knockBack = 4f;
            Item.useTime = Item.useAnimation = 10;
            Item.shoot = ModContent.ProjectileType<SarosAura>();
            Item.shootSpeed = 10f;
            Item.value = Item.buyPrice(1, 40, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;
        }
        /// <summary>
        /// 持在手上时每帧重算一次"还能给光环吃的召唤栏"：总栏位减去**自己以外**所有在场仆从占用的栏位
        ///（照源写法；源存在 ModItem 字段上，本工程改存 ModPlayer 以适配联机）。
        /// </summary>
        public override void HoldItem(Player player)
        {
            double minionCount = 0;
            for (int j = 0; j < Main.projectile.Length; j++)
            {
                Projectile proj = Main.projectile[j];
                if (proj.active && proj.owner == player.whoAmI && proj.minion && proj.type != Item.shoot)
                {
                    minionCount += proj.minionSlots;
                }
            }
            player.GetModPlayer<CalamityDemutationPlayer>().sarosSlots = (int)(player.maxMinions - minionCount);
        }
        /// <summary>至少还剩 1 个空召唤栏才能召唤（源判据）</summary>
        public override bool CanUseItem(Player player) => player.GetModPlayer<CalamityDemutationPlayer>().sarosSlots >= 1;
        /// <summary>
        /// 召唤光环：先清掉自己在场的同类（源靠 CalamityUtils，这里照工程既有写法写等价循环），
        /// 把本次能吃下的栏位数写进 <c>ai[0]</c>，并把面板伤害写进 <c>originalDamage</c>。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.type == type)
                {
                    p.Kill();
                }
            }
            int slots = player.GetModPlayer<CalamityDemutationPlayer>().sarosSlots;
            int idx = Projectile.NewProjectile(source, position, Vector2.Zero, type, damage, knockback, player.whoAmI, slots, 0f);
            if (Main.projectile.IndexInRange(idx))
            {
                Main.projectile[idx].originalDamage = Item.damage;
            }
            return false;
        }
        /// <summary>
        /// 配方（照源 2.0：天狼星 + 夜魇锭×8 + 暗阳碎片×8 @ 宇宙砧；第一味取本工程自持的天狼星）。
        /// 两味材料与站台走软依赖，缺件即不落配方。
        /// </summary>
        public override void AddRecipes()
        {
            if (!ModLoader.TryGetMod("CalamityMod", out Mod calamity))
                return;
            if (!calamity.TryFind<ModItem>("CosmiliteBar", out ModItem cosmiliteBar)
                || !calamity.TryFind<ModItem>("DarksunFragment", out ModItem darksunFragment)
                || !calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
            {
                Mod.Logger.Warn("星律之握览：现代版灾厄里找不到 夜魇锭 / 暗阳碎片 / 宇宙砧，配方未注册。");
                return;
            }
            CreateRecipe().
                AddIngredient<Sirius>().
                AddIngredient(cosmiliteBar.Type, 8).
                AddIngredient(darksunFragment.Type, 8).
                AddTile(cosmicAnvil.Type).
                Register();
        }
    }
}
