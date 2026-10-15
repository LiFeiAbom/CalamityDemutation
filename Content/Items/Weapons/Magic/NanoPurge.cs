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
    /// 纳米净化（Nano Purge） - 法师枪械，**以太流光炮整条合成链的最底一件**（入料全是原版件）。
    /// <para>
    /// **口径（用户 2026-10-15 定）**：这一件走"**拆开拼**"——**手持弹幕取 2.0.4 的自实现版**、
    /// **激光弹幕取 2.1 的重做版（整套，含撞墙预测反弹与粒子表现）**。理由：2.0.4 的手持弹幕是灾厄
    /// 自实现的普通弹幕，不必移植 `BaseGunHoldoutProjectile` 基类；而 2.1 起那颗激光的观感才是用户印象里的样子。
    /// 中文名「纳米净化」（灾厄官方 zh-Hans 里这件是 `//` 占位、没有译文）。
    /// </para>
    /// <para>
    /// 面板：62×34 / 魔力 8 / 使用·动画 20 帧 / 击退 3 / 弹速 16 / 原版**红档**（不涉月后档）/
    /// 价值 1 铂金 / 不显武器贴图 + `channel`（手持弹幕自己画）——**除伤害外全部照 2.0.4**。
    /// **伤害由用户 2026-10-15 点名为 135**（＝2.1 重做版的面板值，2.0.4 原值 60）；
    /// 按工程既有先例（元素之斧 / 归墟之灵 等），用户点名的只是"面板基础值"，
    /// 稀有度与价值仍取所选源版本（2.0.4 的红档 / 1 铂金）。
    /// </para>
    /// <para>
    /// **数值膨胀**（同一开关）：面板 135 → **275**（用户 2026-10-15 指定）。
    /// 本件的伤害是"手持弹幕每帧重算"的（见 <see cref="NanoPurgeHoldout"/>），
    /// 故手持弹幕那处必须走引擎入口 `GetWeaponDamage` 才能吃到这里挂的 `ModifyWeaponDamage`
    /// （详见 §7「最容易漏的一环」与手持弹幕里的注释）。
    /// </para>
    /// <para>
    /// 出手：照 2.0.4——按玩家朝向归一化后生成手持弹幕（真正的瞄准由手持弹幕每帧朝鼠标修正，
    /// 见 <see cref="NanoPurgeHoldout"/>）。同一时刻只允许存在一具手持弹幕。
    /// </para>
    /// </summary>
    internal class NanoPurge : ModItem
    {
        /// <summary>使用·动画帧数（照 2.0.4；手持弹幕的射速斜坡也以它为准）</summary>
        public const int UseTime = 20;
        /// <summary>配方里的漩涡碎片数量（照所选源 2.0.4 的 6）</summary>
        private const int VortexFragmentCount = 6;
        /// <summary>配方里的纳米机器人数量（照源）</summary>
        private const int NanitesCount = 100;
        /// <summary>数值膨胀后的面板伤害（用户 2026-10-15 指定：135 → 275）</summary>
        private const float InflatedDamage = 275f;
        /// <summary>当前生效的面板基础伤害：开则用膨胀值，否则维持 <c>Item.damage</c> 的 135</summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>研究解锁一份</summary>
        public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
        /// <summary>
        /// 基础属性：62×34 / 伤害 60 / 魔力 8 / 使用·动画 20 帧 / 击退 3 / 弹速 16 /
        /// 红档 / 1 铂金（全照 2.0.4）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 62;                          // 贴图宽（像素）
            Item.height = 34;                         // 贴图高（像素）
            Item.damage = 135;                        // 面板伤害（用户 2026-10-15 点名；2.0.4 原值 60）
            Item.DamageType = DamageClass.Magic;
            Item.mana = 8;
            Item.useTime = Item.useAnimation = UseTime;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;                      // 出手动画本体不造成伤害
            Item.noUseGraphic = true;                 // 贴图由手持弹幕负责画
            Item.channel = true;                      // 按住持续开火
            Item.knockBack = 3f;
            Item.rare = ItemRarityID.Red;             // 原版红档（照 2.0.4，不挂 postMoonLordRarity）
            Item.value = Item.buyPrice(1, 0, 0, 0);   // 价值 1 铂金（源 Rarity10BuyPrice）
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<NanoPurgeHoldout>();
            Item.shootSpeed = 16f;
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
        /// 配方（照源，@远古操纵机）：激光机枪 + 漩涡碎片×6 + 纳米机器人×100。
        /// **三味全是原版件**，故单条无条件注册（同"弩炮"那种零软依赖的写法）。
        /// 注意经典版灾厄里同一件叫 `Purge`、配方是同一套料的**漩涡碎片×20**；按工程"数值与配方取所选源版本"
        /// 的既有口径，本件只注册所选 2.0.4 的 ×6 那条（若日后要两版并存，这里加一条即可）。
        /// </summary>
        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.LaserMachinegun).
                AddIngredient(ItemID.FragmentVortex, VortexFragmentCount).
                AddIngredient(ItemID.Nanites, NanitesCount).
                AddTile(TileID.LunarCraftingStation).
                Register();
        }
    }
}
