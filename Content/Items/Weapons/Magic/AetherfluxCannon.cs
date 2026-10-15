using CalamityDemutation.Content.Projectiles.Magic;
using CalamityDemutation.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Magic
{
    /// <summary>
    /// 以太流光炮（Aetherflux Cannon） - 法师枪械，**这条链的顶层**（链：纳米净化 → 【本体·净化豪饮者】 → 本件）。
    /// <para>
    /// **口径（用户 2026-10-15 定）**：本件走"**拆开拼**"——**手持弹幕取 2.0.4 的自实现版**
    /// （灾厄那边 2.0.4 起已改用 `GetWeaponDamage` 取伤害，故数值膨胀天然跟随，不必像纳米净化那样改一行）、
    /// **光弹 `PhasedGodRay` 取 2.1 的重做版整套**（粒子化、20×20 判定、无视无敌帧、命中回满寿命并递减）。
    /// <para>
    /// **配方（用户 2026-10-15 定稿，分两次收紧）**：**本工程自持的 <see cref="NanoPurge"/>** +
    /// 金源锭×5 + 幽花锭×12 + 神圣晶簇×8 @**宇宙砧**；材料与站台**一律软依赖本体件**。
    /// **净化豪饮者那一味已按用户 2026-10-15 的指示删掉**（原话"直接删掉这个净化豪饮者这个配方，
    /// 没有啥子用"）——于是这条配方与现代版灾厄**自己那条一模一样**（本体 2.1 起也把 `PurgeGuzzler`
    /// 从配方里删了）；净化豪饮者在本工程里**既不进配方、也不自持**，只作为本体自己的亵神者掉落存在。
    /// </para>
    /// <para>
    /// 面板：94×54 / 魔力 7 / 使用·动画 36 帧 / 击退 4 / 弹速 24 /
    /// **月后 15 档（紫，＝灾厄 `Violet`；基础稀有度 `Red`、名称色由 `postMoonLordRarity` 覆盖）** /
    /// 价值 **2 铂金 40 金**（源 `Rarity15BuyPrice`）/ `channel` + 不显武器贴图
    /// ——**除伤害外全部照 2.0.4**。
    /// **伤害由用户 2026-10-15 点名为 370**（2.0.4 原值 285；2.1 是 355 + `BurnishedAuric`、
    /// 2.2.2 是 315，均不取）；按工程既有先例（元素之斧 / 归墟之灵 / 纳米净化 等），
    /// 用户点名的只是"面板基础值"，稀有度与价值仍取所选源版本。
    /// </para>
    /// <para>
    /// 出手（照 2.0.4）：按玩家朝向归一化后生成手持弹幕（真正的瞄准由手持弹幕每帧朝鼠标修正）。
    /// 同一时刻只允许存在一具手持弹幕。
    /// </para>
    /// <para>
    /// **数值膨胀**（同一开关）：面板 **370 → 910**（用户 2026-10-15 指定）。
    /// 本件的伤害由手持弹幕每帧用 `Owner.GetWeaponDamage(weaponItem)` 取（2.0.4 源里就是这一句，
    /// 不是手写职业加成），所以挂上 <c>ModifyWeaponDamage</c> 之后，手持弹幕与它派生的双螺旋光弹
    /// **整条自动跟随**，无需再改任何派生点。
    /// </para>
    /// </summary>
    internal class AetherfluxCannon : ModItem
    {
        /// <summary>使用·动画帧数（照 2.0.4；手持弹幕的预热与出手间隔都以它为准）</summary>
        public const int UseTime = 36;
        /// <summary>主体色（照 2.1/2.2.2 的 `mainColor`，光弹与手持喷尘共用）</summary>
        public static Color mainColor = Color.Goldenrod;
        /// <summary>点缀色（照 2.1/2.2.2 的 `accentColor`）</summary>
        public static Color accentColor = Color.LightGreen;
        /// <summary>配方里的金源锭数量（照源）</summary>
        private const int AuricBarCount = 5;
        /// <summary>配方里的幽花锭数量（照源）</summary>
        private const int UelibloomBarCount = 12;
        /// <summary>配方里的神圣晶簇数量（照源）</summary>
        private const int DivineGeodeCount = 8;
        /// <summary>数值膨胀后的面板伤害（用户 2026-10-15 指定：370 → 910）</summary>
        private const float InflatedDamage = 910f;
        /// <summary>当前生效的面板基础伤害：开则用膨胀值，否则维持 <c>Item.damage</c> 的 370</summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>研究解锁一份</summary>
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        /// <summary>
        /// 基础属性：94×54 / 伤害 285 / 魔力 7 / 使用·动画 36 帧 / 击退 4 / 弹速 24 /
        /// 月后 15 档 / 2 铂金 40 金（全照 2.0.4）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 94;                          // 贴图宽（像素）
            Item.height = 54;                         // 贴图高（像素）
            Item.damage = 370;                        // 面板伤害（用户 2026-10-15 点名；2.0.4 原值 285）
            Item.DamageType = DamageClass.Magic;
            Item.mana = 7;
            Item.useTime = Item.useAnimation = UseTime;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;                      // 出手动画本体不造成伤害
            Item.noUseGraphic = true;                 // 贴图由手持弹幕负责画
            Item.channel = true;                      // 按住持续开火
            Item.knockBack = 4f;
            Item.rare = ItemRarityID.Red;             // 基础稀有度红色，名称颜色由 postMoonLordRarity 覆盖
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15;   // 月后 15：紫（＝灾厄 Violet）
            Item.value = Item.buyPrice(2, 40, 0, 0);  // 价值 2 铂金 40 金（源 Rarity15BuyPrice）
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<AetherfluxCannonHoldout>();
            Item.shootSpeed = 24f;
        }
        /// <summary>数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>（运行时读开关，不必重载）</summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage) => damage.Base = BaseDamage;
        /// <summary>同一时刻只允许存在一具手持弹幕（照源）</summary>
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
        /// <summary>
        /// 出手：把速度按玩家朝向归一化后生成手持弹幕（照 2.0.4）。
        /// 2.0.7.2 起这里改成"从玩家旋转中心朝鼠标方向生成"，差别只在手持弹幕的第一帧朝向，
        /// 之后两者都由手持弹幕每帧朝鼠标修正，故按所选手持弹幕的源写法保留 2.0.4 这一版。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 shootDirection = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            Projectile.NewProjectile(source, position, shootDirection, type, damage, knockback, player.whoAmI);
            return false;
        }
        /// <summary>
        /// 配方（用户 2026-10-15 定稿，@**宇宙砧**）：**本工程的纳米净化** + 金源锭×5 + 幽花锭×12 +
        /// 神圣晶簇×8（**净化豪饮者已按用户指示删掉**）。
        /// 除本工程自持的纳米净化外**全部软依赖现代版灾厄**（缺任一件就整条不注册并写警告）；
        /// 本件**没有经典版**（经典版灾厄连 `T1000` / 宇宙砧 / 幽花锭 都没有），故只注册这一条。
        /// </summary>
        public override void AddRecipes()
        {
            if (!ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Mod.Logger.Warn("以太流光炮：没找到现代版灾厄（CalamityMod），本条配方未注册。");
                return;
            }
            if (!calamity.TryFind<ModItem>("AuricBar", out ModItem auricBar) ||
                !calamity.TryFind<ModItem>("UelibloomBar", out ModItem uelibloomBar) ||
                !calamity.TryFind<ModItem>("DivineGeode", out ModItem divineGeode) ||
                !calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
            {
                Mod.Logger.Warn("以太流光炮：现代版灾厄里缺 AuricBar / UelibloomBar / DivineGeode / CosmicAnvil 其中一件，本条配方未注册。");
                return;
            }
            CreateRecipe().
                AddIngredient<NanoPurge>().
                AddIngredient(auricBar.Type, AuricBarCount).
                AddIngredient(uelibloomBar.Type, UelibloomBarCount).
                AddIngredient(divineGeode.Type, DivineGeodeCount).
                AddTile(cosmicAnvil.Type).
                Register();
        }
    }
}
