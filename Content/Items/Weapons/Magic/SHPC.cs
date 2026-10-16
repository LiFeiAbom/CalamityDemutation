using System;
using CalamityDemutation.Content.Projectiles.Magic;
using CalamityDemutation.Sounds;
using CalamityDemutation.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Magic
{
    /// <summary>
    /// SHPC - 传奇级法师枪械，**无配方、由机械三王各 1% 直接击杀掉落**（见 <c>CalamityDemutationGlobalNPC.OnKill</c>）。
    /// <para>
    /// **口径（用户 2026-10-16 逐条拍板）**：
    /// ① 面板基础伤害 **117**；② 出手模板取本体 **2.0 / 2.0.3.9** 那条线
    /// （`useTime = useAnimation = 7`、击退 3.25、弹速 20、**左键等离子球、右键能量束，按键照原版不交换**）；
    /// **贴图用用户提供的 98×34 老版图**（源文件 `SHPC_(1.1.7.701).png`，即经典世系那张），
    /// 故 `Item.width/height` 与持枪偏移按该图取（照经典版口径），与 2.0 的 124×52 不同；
    /// ③ 稀有度走月后自定义 **17 档（传奇）**；④ 价值 **36 金**；⑤ 名字中英同为 `SHPC`；
    /// ⑥ 音效照 2.0（等离子球＝原版 `Item92`、能量束＝灾厄 `LaserCannon`）；⑦ **去掉合成配方**；
    /// ⑧ 增幅走 <see cref="DefenseBlade"/> 那套传奇加算表。
    /// </para>
    /// <para>
    /// **两种出手方式**（照 2.0：同一把枪左右键各一套，各自的间隔/蓝耗/音效互不影响）：
    /// 左键＝**等离子球** 3 发扇形、球伤害 ×1.1、魔力 20、间隔 7 × 7.14 ≈ 50 帧（球本身见 <see cref="SHPCPlasma"/>，
    /// 会停滞并引发大范围爆炸）；
    /// 右键＝**能量束** 3 道、击退 ×0.5、魔力 ×0.3（＝6）、间隔 7 帧（见 <see cref="SHPCLaser"/>）。
    /// </para>
    /// <para>
    /// **增幅**：照 <see cref="DefenseBlade"/> 的传奇进度表——石巨人 → 至尊灾厄共 18 档**加算**，
    /// 常态满配 6.00×、数值膨胀开关开启时改用旧版增量满配 9.10×。
    /// 面板 117 是**单一基准**（用户只点了这一个数），故本件不设"膨胀面板"双值，膨胀开关只作用于增量列。
    /// </para>
    /// </summary>
    internal class SHPC : ModItem
    {
        // ── 静态字段 ──
        /// <summary>面板基础伤害（用户 2026-10-16 指定：117）</summary>
        private const int PanelDamage = 117;
        /// <summary>右键（能量束）的连发间隔：照 2.0 的 `useTime 7`（倍率 1f）</summary>
        private const int BeamUseTime = 7;
        /// <summary>左键（等离子球）的慢速倍率：照 2.0 的 `7.14`（7 × 7.14 ≈ 50 帧一发）</summary>
        private const float PlasmaSlowFactor = 7.14f;
        /// <summary>每次出手打出的弹幕数量（照 2.0：两种招式各 3 发）</summary>
        private const int ShotsPerUse = 3;
        /// <summary>
        /// 传奇武器增伤进度表：石巨人 → 至尊灾厄（主线 18 档）——**与 <see cref="DefenseBlade"/> 同一张表**。
        /// 每项为 (是否已击败, 当前固定增伤, 数值膨胀开关开启时的旧版固定增伤)：
        /// 当前口径加算累加合计 +5.00（满配 6.00×），旧版口径合计 +8.10（满配 9.10×）。
        /// 前中期增量小、终局增量大：反映难度递增，同时弥补伤害基数变大后固定增量的相对收益下降。
        /// </summary>
        private static readonly (Func<bool> Downed, float Increment, float LegacyIncrement)[] LegendaryBosses =
        [
            // —— 石巨人 → 月球领主 ——
            (() => NPC.downedGolemBoss, 0.06f, 0.10f),                          // 石巨人 Golem
            (() => BossSystem.Plaguebringer, 0.09f, 0.15f),                     // 瘟疫使者歌莉娅 Plaguebringer Goliath
            (() => BossSystem.Ravager, 0.09f, 0.15f),                           // 毁灭魔像（掠夺者）Ravager
            (() => NPC.downedAncientCultist, 0.06f, 0.10f),                     // 拜月教邪教徒 Lunatic Cultist
            (() => BossSystem.AstrumDeus, 0.12f, 0.20f),                        // 星神游龙 Astrum Deus
            (() => NPC.downedMoonlord, 0.12f, 0.20f),                           // 月球领主 Moon Lord
            // —— 月后 · 亵渎前 ——
            (() => BossSystem.Guardians, 0.12f, 0.20f),                         // 亵渎守卫 Profaned Guardians
            (() => BossSystem.Dragonfolly, 0.12f, 0.20f),                       // 丛林龙 Dragonfolly
            // —— 亵渎 → 噬神 ——
            (() => BossSystem.Providence, 0.25f, 0.40f),                        // 亵渎天神 Providence
            (() => BossSystem.CeaselessVoid || ClassicSentinelsDowned, 0.12f, 0.20f), // 无尽虚空 Ceaseless Void
            (() => BossSystem.StormWeaver || ClassicSentinelsDowned, 0.12f, 0.20f),   // 风暴编织者 Storm Weaver
            (() => BossSystem.Signus || ClassicSentinelsDowned, 0.12f, 0.20f),        // 西格纳斯 Signus
            (() => BossSystem.Polterghast, 0.31f, 0.50f),                       // 噬魂幽花 Polterghast
            (() => BossSystem.OldDuke, 0.31f, 0.50f),                           // 老公爵 Old Duke
            (() => BossSystem.DevourerOfGods, 0.49f, 0.80f),                    // 噬神者 Devourer of Gods
            // —— 终局 ——
            (() => BossSystem.Yharon, 1.26f, 2.00f),                            // 犽戎 Yharon
            (() => BossSystem.ExoMechs, 0.62f, 1.00f),                          // 星流巨械 Exo Mechs
            (() => BossSystem.SupremeCalamitas, 0.62f, 1.00f),                  // 至尊灾厄 Supreme Calamitas
        ];
        // ── 属性 ──
        /// <summary>
        /// 经典版三使者合并标记是否全部倒下。
        /// 经典版没有单个使者的独立标记（只有 Sentinel1/2/3），因此在经典版下，
        /// 三个使者条目退化为"三使者皆倒"时同时生效（各 +0.20，合计 +0.60）。口径同 <see cref="DefenseBlade"/>。
        /// </summary>
        private static bool ClassicSentinelsDowned =>
            BossSystem.Sentinel1 && BossSystem.Sentinel2 && BossSystem.Sentinel3;
        // ── 生命周期方法 ──
        /// <summary>静态属性：研究所解锁数量设为 1；允许右键连续触发（右键是独立的能量束招式）。</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }
        /// <summary>
        /// 基础属性：尺寸 98×34（用户提供的贴图）/ 面板 117 / 魔力 20 / 使用·动画 7 帧 / 击退 3.25 / 弹速 20（照本体 2.0 线）；
        /// 基础稀有度填红（名称色由 <c>postMoonLordRarity = 17</c> 在 GlobalItem 里覆盖）；
        /// 价值 36 金（用户指定）；<c>Item.shoot</c> 记等离子球（左键那一套）。
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 98;                          // 贴图宽（像素，＝用户提供贴图的实测宽度）
            Item.height = 34;                         // 贴图高（像素，＝用户提供贴图的实测高度）
            Item.damage = PanelDamage;                // 面板基础伤害 117（用户 2026-10-16 指定）
            Item.DamageType = DamageClass.Magic;      // 伤害类型：魔法
            Item.mana = 20;                           // 魔力消耗（等离子球 20；能量束在 ModifyManaCost 里 ×0.3）
            Item.useTime = Item.useAnimation = BeamUseTime;   // 使用间隔（能量束的 7 帧；等离子球在 UseSpeedMultiplier 里放慢）
            Item.useStyle = ItemUseStyleID.Shoot;     // 使用样式：射击
            Item.noMelee = true;                      // 出手动画本体不造成伤害
            Item.knockBack = 3.25f;                   // 击退（照 2.0）
            Item.UseSound = SoundID.Item92;           // 默认音效＝等离子球（照 2.0；能量束在 CanUseItem 里换成 LaserCannon）
            Item.autoReuse = true;                    // 自动连发
            Item.shoot = ModContent.ProjectileType<SHPCPlasma>();   // 左键弹幕：等离子球
            Item.shootSpeed = 20f;                    // 弹速（照 2.0）
            Item.rare = ItemRarityID.Red;             // 基础稀有度：红（名称色由 postMoonLordRarity 覆盖）
            Item.value = Item.buyPrice(0, 36, 0, 0);  // 价值 36 金（用户 2026-10-16 指定）
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 17;   // 月后自定义稀有度 17 档（传奇）
        }
        /// <summary>枪口位置：照经典世系同图的口径 <c>(-25, 0)</c>（98×34 的小枪身，2.0 的 <c>(-35, -10)</c> 是配 124×52 大枪身的）</summary>
        public override Vector2? HoldoutOffset() => new Vector2(-25, 0);
        /// <summary>右键可用（右键是独立的能量束招式）</summary>
        public override bool AltFunctionUse(Player player) => true;
        /// <summary>
        /// 按左右键切换音效（照 2.0，两键对调）：右键＝能量束用灾厄的激光炮音，左键＝等离子球用原版 <c>Item92</c>。
        /// </summary>
        public override bool CanUseItem(Player player)
        {
            Item.UseSound = player.altFunctionUse == 2 ? CalamityDemutationSounds.LaserCannon : SoundID.Item92;
            return base.CanUseItem(player);
        }
        /// <summary>
        /// 按左右键切换间隔（照 2.0）：右键＝能量束走 7 帧连发（倍率 1f）；
        /// 左键＝等离子球走 7 × 7.14 ≈ 50 帧一发（倍率 1 / 7.14）。
        /// </summary>
        public override float UseSpeedMultiplier(Player player) => player.altFunctionUse == 2 ? 1f : 1f / PlasmaSlowFactor;
        /// <summary>右键（能量束）的魔力消耗 ×0.3（照 2.0；左键等离子球维持 20 点）</summary>
        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
            if (player.altFunctionUse == 2)
                mult *= 0.3f;
        }
        /// <summary>
        /// 出手（照 2.0，按键照原版不交换）：右键＝能量束 3 道、击退 ×0.5；
        /// 左键＝等离子球 3 发扇形、球伤害 ×1.1。两处都返回 false（自己生成弹幕）。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                // 右键：能量束（散布 ±1.0 速度单位，照 2.0 的 ±20 × 0.05）
                for (int i = 0; i < ShotsPerUse; i++)
                {
                    float speedX = velocity.X + Main.rand.Next(-20, 21) * 0.05f;
                    float speedY = velocity.Y + Main.rand.Next(-20, 21) * 0.05f;
                    Projectile.NewProjectile(source, position.X, position.Y, speedX, speedY, ModContent.ProjectileType<SHPCLaser>(), damage, knockback * 0.5f, player.whoAmI, 0f, 0f);
                }
                return false;
            }
            // 左键：等离子球（散布 ±2.0 速度单位，照 2.0 的 ±40 × 0.05；球伤害 ×1.1）
            for (int i = 0; i < ShotsPerUse; i++)
            {
                float speedX = velocity.X + Main.rand.Next(-40, 41) * 0.05f;
                float speedY = velocity.Y + Main.rand.Next(-40, 41) * 0.05f;
                Projectile.NewProjectile(source, position.X, position.Y, speedX, speedY, ModContent.ProjectileType<SHPCPlasma>(), (int)(damage * 1.1f), knockback, player.whoAmI, 0f, 0f);
            }
            return false;
        }
        /// <summary>把传奇进度增伤倍率乘入最终伤害（照 <see cref="DefenseBlade"/> 的写法）</summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage) => damage *= LegendaryDamage();
        // ── 公开方法 ──
        /// <summary>
        /// 按 <see cref="LegendaryBosses"/> 进度表加算累加，返回传奇武器伤害倍率（口径与 <see cref="DefenseBlade"/> 完全一致）：
        /// 常态基础 1.00×、满配 6.00×（合计 +5.00）；数值膨胀开关开启时改用旧版增量，满配 9.10×（合计 +8.10）。
        /// </summary>
        public static float LegendaryDamage()
        {
            float damageBuff = 1f;
            bool legacy = ConfigSystem.StatInflationEnabled;
            foreach (var (downed, increment, legacyIncrement) in LegendaryBosses)
            {
                if (downed())
                    damageBuff += legacy ? legacyIncrement : increment;
            }
            return damageBuff;
        }
    }
}
