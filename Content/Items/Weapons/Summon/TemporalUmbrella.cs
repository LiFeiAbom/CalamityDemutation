using CalamityDemutation.Content.Projectiles.Summon.Umbrella;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Summon
{
    /// <summary>
    /// 光阴流时伞（TemporalUmbrella）—— **CI 口径**的自持件（用户 2026-10-09 点名移植）。
    /// CI 对应件是 `TemporalUmbrellaOld`（CI 官方中文写作「光阴时流伞[Legacy]」，英文 Temporal Umbrella）；
    /// 类名按本工程命名口径去掉 `Old` 后缀（见工程记忆第 4 节）。
    /// </summary>
    /// <remarks>
    /// 属性照 CI：判定 74×72、伤害 **1000**、魔力 **99**、使用·动画 **10 帧**、击退 1、
    /// **红底 + 月后 16 档（品红＝灾厄 `HotPink`，即"魔影档"）**、价值 **2 铂金 80 金**、
    /// 音 `SoundID.Item68`、弹速 10、召唤伤害类型；在鼠标处召唤一顶**魔法礼帽**
    /// （<see cref="MagicHat"/>）。
    /// <para>
    /// **用户 2026-10-09 的两条口径**：① 配方取 **CI 的第①条**（主配方）；
    /// ② **天顶世界的全部内容都删掉** —— 故源里的 `Main.zenithWorld ? 150 : …` 伤害分支、
    /// 天顶世界专用配方、以及 `Item.rare` 之外的捐赠品稀有度都按普通世界处理。
    /// 源里另一个 `CIServerConfig.ShadowspecBuff ? 4000 : 1000` 分支（CI 自己的服务器配置）本工程没有对应开关，
    /// 取基础值 **1000**（将来若要挂 `StatInflation`，4000 正好是现成的"开态"候选）。
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
        /// 基础属性：照 CI（74×72、伤害 1000、魔力 99、使用 10 帧、击退 1、月后 16 档、2 铂金 80 金、弹速 10）
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
            Item.damage = 1000;
            Item.knockBack = 1f;
            Item.useTime = Item.useAnimation = 10;
            Item.shoot = ModContent.ProjectileType<MagicHat>();
            Item.shootSpeed = 10f;
            Item.value = Item.buyPrice(2, 80, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 16;
        }
        /// <summary>
        /// 召唤条件（照 CI）：自己名下的礼帽只能存在一顶，且至少要有 5 个召唤栏位。
        /// </summary>
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0 && player.maxMinions >= 5;
        /// <summary>
        /// 出手：先清掉自己在场的同类礼帽（源靠灾厄 `CalamityUtils.KillShootProjectiles`，本工程软依赖、
        /// 照既有写法写等价循环），随后在**鼠标位置**生成一顶新礼帽（CI 的写法）。
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
            Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero, type, damage, knockback, player.whoAmI);
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
                Mod.Logger.Warn("光阴流时伞：现代版灾厄里找不到 尖刺岩杖 / 魔影锭 / 嘉登熔炉，配方未注册。");
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
