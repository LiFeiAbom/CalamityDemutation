using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Ranged
{
    /// <summary>
    /// 速射机枪（Minigun） - 射手武器。**2.0.3.9 起被灾厄删除**（直读实装 2.2.2 的整包文件表确认没有此件），
    /// 属"旧版回归"件。
    /// <para>
    /// **口径（用户 2026-10-14 逐条拍板）**：实现取 **2.0**（与 旧版灾厄 逐项一致）、
    /// 贴图用**现代那张**（92×44 / `1A2E97E6`，旧版灾厄 / 2.0 / 旧版支线三处同款）、
    /// 灾厄"贴脸射击"**不做**。
    /// </para>
    /// <para>
    /// 面板（2.0 底 + 用户改值）：92×44 / 使用·动画 3 帧 / 击退 2.5 / 弹速 22 / 子弹弹药 /
    /// **80% 不消耗弹药** / 每次开火打一颗弹药（±15×0.05 抖动）/ 月后稀有度 15 档（源的自定义 Violet）/
    /// 价值 2 铂金 40 金（源 `Rarity15BuyPrice`）；持握偏移 (-10, 0)。
    /// </para>
    /// <para>
    /// 配方（用户指定）：**现代 = P90（本工程自持件）+ 原版链式机枪 + 古圣金源锭 `AuricBar`×5
    /// @ 宇宙砧 `CosmicAnvil`** ——源配方那味 `ClockGatlignum`（发条加特林）已随 1.4.4 被灾厄删除，
    /// 故按用户口径换成 P90，并按用户 2026-10-14 追加要求再补一味**原版链式机枪**（`ItemID.ChainGun`）；
    /// **经典 = 链式机枪 + 宇宙锭×5 + 幻魂×5 @ 嘉登熔炉**（照经典灾厄经典灾厄，与工程其它件同口径）。
    /// </para>
    /// </summary>
    internal class Minigun:ModItem
    {
        /// <summary>不打散弹的固定省弹率（照源：80% 不消耗弹药）</summary>
        private const float AmmoSaveChance = 0.8f;
        /// <summary>单发弹丸的速度抖动幅度（照源：±15 × 0.05 = ±0.75）</summary>
        private const float SpreadJitter = 0.05f;
        /// <summary>当前生效的面板基础伤害：开则用膨胀值，否则维持 <c>Item.damage</c> 的 375</summary>

        /// <summary>
        /// 研究解锁一份
        /// </summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>
        /// 物品基础属性：92×44、伤害 375（用户指定，源 275）、使用/动画 3 帧、击退 2.5、弹速 22、
        /// 子弹弹药、使用音 `Item41`、月后稀有度 15 档、价值 2 铂金 40 金
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 92;                          // 贴图宽（像素）
            Item.height = 44;                         // 贴图高（像素）
            Item.damage = 375;                        // 面板伤害（用户 2026-10-14 指定；源 2.0 是 275）
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 3;                         // 使用/动画各 3 帧（照 2.0，这就是"速射"）
            Item.useAnimation = 3;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;                      // 挥砍动画不造成伤害
            Item.knockBack = 2.5f;
            Item.value = Item.buyPrice(2, 40, 0, 0);  // 价值 2 铂金 40 金（源 Rarity15BuyPrice）
            Item.UseSound = SoundID.Item41;
            Item.autoReuse = true;
            Item.shoot = ProjectileID.PurificationPowder;  // 射的是弹药本体（照源的占位写法）
            Item.shootSpeed = 22f;
            Item.useAmmo = AmmoID.Bullet;             // 吃子弹类弹药
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15;  // 月后稀有度 15（紫）
        }
        /// <summary>
        /// 本件派生伤害走 <c>Shoot</c> 的 <c>damage</c> 参数（射的是弹药本体），会自动跟随。
        /// </summary>
        /// <summary>
        /// 持握偏移：照 2.0 的 (-10, 0)
        /// </summary>
        public override Vector2? HoldoutOffset() => new Vector2(-10, 0);
        /// <summary>
        /// 80% 不消耗弹药（照源：<c>rand &gt; 0.8</c> 时才消耗）
        /// </summary>
        public override bool CanConsumeAmmo(Item ammo, Player player) => Main.rand.NextFloat() > AmmoSaveChance;
        /// <summary>
        /// 开火：每次一颗弹药本体，加 ±0.75 速度抖动（照 2.0）
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            float SpeedX = velocity.X + Main.rand.Next(-15, 16) * SpreadJitter;
            float SpeedY = velocity.Y + Main.rand.Next(-15, 16) * SpreadJitter;
            Projectile.NewProjectile(source, position.X, position.Y, SpeedX, SpeedY, type, damage, knockback, player.whoAmI);
            return false;
        }
        /// <summary>
        /// 配方（分版本，缺件即退化成单分支）：现代 = 本工程自持的 P90 + 金源锭×5 @ 宇宙砧（用户指定）；
        /// 经典 = 链式机枪 + 宇宙锭×5 + 幻魂×5 @ 嘉登熔炉（照 经典灾厄）。
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if(calamity.TryFind<ModItem>("AuricBar", out ModItem auricBar)
                    && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<P90>();
                    recipe.AddIngredient(ItemID.ChainGun);      // 原版链式机枪（用户 2026-10-14 追加要求）
                    recipe.AddIngredient(auricBar.Type, 5);
                    recipe.AddTile(cosmicAnvil.Type);
                    recipe.Register();
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if(classic.TryFind<ModItem>("CosmiliteBar", out ModItem cosmiliteBar)
                    && classic.TryFind<ModItem>("Phantoplasm", out ModItem phantoplasm)
                    && classic.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient(ItemID.ChainGun);
                    recipeClassic.AddIngredient(cosmiliteBar.Type, 5);
                    recipeClassic.AddIngredient(phantoplasm.Type, 5);
                    recipeClassic.AddTile(draedonsForge.Type);
                    recipeClassic.Register();
                }
            }
        }
    }
}
