using CalamityDemutation.Content.Projectiles.Summon.Umbrella;
using CalamityDemutation.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Summon
{
    /// <summary>
    /// 光阴时流伞（TemporalUmbrella）—— **CI 口径**的自持件（用户 2026-10-09 点名移植）。
    /// CI 对应件是 `TemporalUmbrellaOld`（CI 官方中文写作「光阴时流伞」，英文 Temporal Umbrella）；
    /// 类名按本工程命名口径去掉 `Old` 后缀（见工程记忆第 4 节）。
    /// </summary>
    /// <remarks>
    /// 属性照 CI：判定 74×72、伤害 **963**（CI 源 1000，按用户 2026-10-10 点名调整）、魔力 **99**、
    /// 使用·动画 **10 帧**、击退 1、
    /// **红底 + 月后 16 档（品红＝灾厄 `HotPink`，即"魔影档"）**、价值 **2 铂金 80 金**、
    /// 音 `SoundID.Item68`、弹速 10、召唤伤害类型；在鼠标处召唤一顶**魔法礼帽**
    /// （<see cref="MagicHat"/>）。
    /// <para>
    /// **用户 2026-10-09 的两条口径**：① 配方取 **CI 的第①条**（主配方）；
    /// ② **天顶世界的全部内容都删掉** —— 故源里的 `Main.zenithWorld ? 150 : …` 伤害分支、
    /// 天顶世界专用配方、以及 `Item.rare` 之外的捐赠品稀有度都按普通世界处理。
    /// 源里另一个 `CIServerConfig.ShadowspecBuff ? 4000 : 1000` 分支（CI 自己的服务器配置）本工程没有对应开关，
    /// 改为挂在 `StatInflation` 上。
    /// </para>
    /// <para>
    /// 数值膨胀（用户 2026-10-10 点名）：关态 <c>Item.damage</c> = **963**（CI 源 1000，点名调整）、
    /// 开态 **4000**（＝CI 那个 `ShadowspecBuff` 档）。召唤件特有一环——`Shoot` 里写进礼帽的
    /// `originalDamage` 也必须读 `BaseDamage`（本件原先靠引擎按 `Item.damage` 自动写入，加膨胀后改自己写）；
    /// 礼帽抛出的七件工具按 `Projectile.damage` 派生，会自动跟随。
    /// </para>
    /// <para>
    /// 配方（CI 第①条，逐味照抄）：**尖刺岩杖 `SpikecragStaff` + 星律之握览（本工程自持的
    /// `SarosPossession`）+ 原版雨伞 `ItemID.Umbrella` + 原版高顶礼帽 `ItemID.TopHat` +
    /// 魔影锭 `ShadowspecBar`×4 @ 嘉登熔炉 `DraedonsForge`**。
    /// CI 另有一条"灾厄精华换购"配方与一条天顶世界配方，按用户口径**都不做**；本件也没有经典版，
    /// 故只注册现代分支一条。
    /// </para>
    /// </remarks>
    internal class TemporalUmbrella:ModItem
    {
        /// <summary>研究解锁一份（照 CI 写 `ResearchUnlockCount = 1`）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>
        /// 基础属性：照 CI（74×72、伤害 963、魔力 99、使用 10 帧、击退 1、月后 16 档、2 铂金 80 金、弹速 10）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 74;
            Item.height = 72;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.UseSound = SoundID.Item68;
            Item.DamageType = DamageClass.Summon;
            Item.mana = 99;
            Item.damage = 963;
            Item.knockBack = 1f;
            Item.useTime = Item.useAnimation = 10;
            Item.shoot = ModContent.ProjectileType<MagicHat>();
            Item.shootSpeed = 10f;
            Item.value = Item.buyPrice(2, 80, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 16;
        }
        /// <summary>
        /// 数值膨胀后的面板伤害（用户 2026-10-10 指定：963 → 4000，即 CI 的 ShadowspecBuff 档）。
        /// </summary>
        private const float InflatedDamage = 4000f;
        /// <summary>
        /// 当前生效的面板基础伤害：膨胀开关开启时用 <see cref="InflatedDamage"/>，否则维持 <c>Item.damage</c> 的 963。
        /// </summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>
        /// 数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>（运行时读配置，游戏内切换即时生效）。
        /// 召唤件特有一环：<c>Shoot</c> 里写进礼帽的 <c>originalDamage</c> 也必须读 <see cref="BaseDamage"/>。
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base = BaseDamage;
        }
        /// <summary>
        /// 召唤条件（照 CI）：自己名下的礼帽只能存在一顶，且至少要有 5 个召唤栏位。
        /// </summary>
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0 && player.maxMinions >= 5;
        /// <summary>
        /// 出手：先清掉自己在场的同类礼帽（源靠灾厄 `CalamityUtils.KillShootProjectiles`，本工程软依赖、
        /// 照既有写法写等价循环），随后在**鼠标位置**生成一顶新礼帽（CI 的写法），
        /// 并把面板伤害写进礼帽的 <c>originalDamage</c>（召唤件加膨胀后必须自己写，见上）。
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
            int idx = Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero, type, damage, knockback, player.whoAmI);
            if (Main.projectile.IndexInRange(idx))
            {
                Main.projectile[idx].originalDamage = (int)BaseDamage;
            }
            return false;
        }
        /// <summary>
        /// 配方（照 CI 第①条）：尖刺岩杖 + 星律之握览 + 原版雨伞 + 原版高顶礼帽 + 魔影锭×4 @ 嘉登熔炉。
        /// 尖刺岩杖、魔影锭与嘉登熔炉走软依赖按名取；缺件即不落配方。
        /// </summary>
        public override void AddRecipes()
        {
            if (!ModLoader.TryGetMod("CalamityMod", out Mod calamity))
                return;
            if (!calamity.TryFind<ModItem>("SpikecragStaff", out ModItem spikecragStaff)
                || !calamity.TryFind<ModItem>("ShadowspecBar", out ModItem shadowspecBar)
                || !calamity.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
            {
                Mod.Logger.Warn("光阴时流伞：现代版灾厄里找不到 尖刺岩杖 / 魔影锭 / 嘉登熔炉，配方未注册。");
                return;
            }
            CreateRecipe().
                AddIngredient(spikecragStaff.Type).
                AddIngredient<SarosPossession>().
                AddIngredient(ItemID.Umbrella).
                AddIngredient(ItemID.TopHat).
                AddIngredient(shadowspecBar.Type, 4).
                AddTile(draedonsForge.Type).
                Register();
        }
    }
}
