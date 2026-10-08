using CalamityDemutation.Content.Items.Materials;
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
    /// 天狼星（Sirius）—— 「太阳神杖 / 天狼星」召唤链的**链尾**，吃链上刚落地的太阳神杖。
    /// 老规矩取灾厄 2.0：贴图 68×70 / 判定 62×62、伤害 **160**、击退 3、使用/动画 **10 帧**、魔力 10、
    /// **红底 + 月后 13 档（荧光绿，＝灾厄 `PureGreen`）**、价值 **1 铂金 40 金**、音 `SoundID.Item44`、弹速 10。
    /// </summary>
    /// <remarks>
    /// 效果（2.0 口径）：出手时**消耗全部剩余召唤栏**——`HoldItem` 每帧数出"除自己以外已占用的栏位"，
    /// 剩余栏位写进弹幕的 <c>ai[0]</c>；星灵每帧把 <c>minionSlots</c> 刷成这个数（吃光栏位），
    /// 且**栏位越多越强**：光束伤害 ×<c>(ln(栏位数) + 1)</c>、穿透数 = 栏位数。
    /// 星灵超远距离（7000 像素、不查视线）索敌、30 帧一发；光束命中施加**夜凋**并炸出星爆
    /// （见 <see cref="SiriusExplosion"/>）。
    /// <para>
    /// 与源的差异：㈠ 源把"剩余栏位数"存在 ModItem 的实例字段 `siriusSlots` 上——ModItem 是全类型共享单例，
    /// 联机会让两名玩家互相串数值，工程约定（见工程记忆第 5 节）这类武器状态一律放 ModPlayer，
    /// 本件改用 <c>CalamityDemutationPlayer.siriusSlots</c>；
    /// ㈡ 源用灾厄 `CalamityUtils.KillShootProjectiles` 清场，本工程是软依赖、不引用灾厄类型，
    /// 照经典版写等价的"先杀掉自己在场的同类再召唤"循环。
    /// </para>
    /// <para>
    /// 配方（用户 2026-10-08 拍板照 2.0）：**太阳神杖 + 流明石×5 + 灾厄魂×2 + 起源之簇×12 @ 远古操纵机**。
    /// 两分支各注册一条、缺件即退化成单分支；**材料类名两版不同**（已逐条核过 `.tmod`）：
    /// 现代 `Lumenyl` / `ExodiumCluster`，经典 `Lumenite` / `ExodiumClusterOre`（灾厄魂两边都叫 `RuinousSoul`）。
    /// </para>
    /// </remarks>
    internal class Sirius:ModItem
    {
        /// <summary>研究解锁一份（源 2.0 写 SacrificeTotal = 1）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>
        /// 基础属性：照源 2.0（62×62 判定、伤害 160、击退 3、使用 10 帧、魔力 10、弹速 10、月后 13 档、1 铂金 40 金）。
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 62;
            Item.height = 62;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.UseSound = SoundID.Item44;
            Item.DamageType = DamageClass.Summon;
            Item.mana = 10;
            Item.damage = 160;
            Item.knockBack = 3f;
            Item.useTime = Item.useAnimation = 10;
            Item.shoot = ModContent.ProjectileType<SiriusMinion>();
            Item.shootSpeed = 10f;
            Item.value = Item.buyPrice(1, 40, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 13;   // 月后 13：荧光绿（＝灾厄 PureGreen）
        }
        /// <summary>
        /// 持在手上时每帧重算一次"还能给天狼星吃的召唤栏"：总栏位减去**自己以外**所有在场仆从占用的栏位。
        /// 照源写法（源存在 ModItem 字段上，本工程改存 ModPlayer 以适配联机）。
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
            player.GetModPlayer<CalamityDemutationPlayer>().siriusSlots = (int)(player.maxMinions - minionCount);
        }
        /// <summary>至少还剩 1 个空召唤栏才能召唤（源判据）</summary>
        public override bool CanUseItem(Player player) => player.GetModPlayer<CalamityDemutationPlayer>().siriusSlots >= 1;
        /// <summary>
        /// 召唤星灵：先清掉自己在场的同类（源靠 CalamityUtils，这里照经典版写等价循环），
        /// 把本次能吃下的栏位数写进 <c>ai[0]</c>、把开火冷却初值 30 写进 <c>ai[1]</c>，
        /// 并把原始面板伤害写进 <c>originalDamage</c>。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 清场：自己名下的同类召唤物全部移除，保证"只有一颗星"
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.type == type)
                {
                    p.Kill();
                }
            }
            int slots = player.GetModPlayer<CalamityDemutationPlayer>().siriusSlots;
            int idx = Projectile.NewProjectile(source, position, Vector2.Zero, type, damage, knockback, player.whoAmI, slots, 30f);
            if (Main.projectile.IndexInRange(idx))
            {
                Main.projectile[idx].originalDamage = Item.damage;
            }
            return false;
        }
        /// <summary>
        /// 配方（照源 2.0，两分支各一条、缺件即退化成单分支）：
        /// 太阳神杖 + 流明石×5 + 灾厄魂×2 + 起源之簇×12 @ 远古操纵机（`TileID.LunarCraftingStation`，原版站台）。
        /// </summary>
        public override void AddRecipes()
        {
            bool modern = TryAddRecipeFrom("CalamityMod", "Lumenyl", "ExodiumCluster");
            bool classic = TryAddRecipeFrom("CalamityModClassicPreTrailer", "Lumenite", "ExodiumClusterOre");
            if (!modern && !classic)
                Mod.Logger.Warn("天狼星：两版灾厄都找不到 流明石 / 起源之簇 / 灾厄魂，配方未注册。");
        }
        /// <summary>
        /// 从指定灾厄版本取三味材料注册一条配方（流明石与起源之簇的类名两版不同，由调用方传入）；
        /// 缺任何一件或缺模组即返回 false，不落半条配方。
        /// </summary>
        private bool TryAddRecipeFrom(string calamityModName, string lumenName, string exodiumName)
        {
            if (!ModLoader.TryGetMod(calamityModName, out Mod calamity))
                return false;
            if (!calamity.TryFind<ModItem>(lumenName, out ModItem lumen) ||
                !calamity.TryFind<ModItem>("RuinousSoul", out ModItem soul) ||
                !calamity.TryFind<ModItem>(exodiumName, out ModItem exodium))
                return false;
            CreateRecipe().
                AddIngredient<SunGodStaff>().
                AddIngredient(lumen.Type, 5).
                AddIngredient(soul.Type, 2).
                AddIngredient(exodium.Type, 12).
                AddTile(TileID.LunarCraftingStation).
                Register();
            return true;
        }
    }
}
