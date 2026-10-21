using System;
using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using CalamityDemutation.Content.Projectiles.Magic;
using CalamityDemutation.Sounds;
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
    /// （`useTime = useAnimation = 7`、击退 3.25、**弹速 30**、**左键等离子球、右键能量束，按键照原版不交换**）；
    /// **贴图用用户提供的 98×34 老版图**（源文件 `SHPC_(1.1.7.701).png`，即经典灾厄那张），
    /// 故 `Item.width/height` 与持枪偏移按该图取（照经典版口径），与 2.0 的 124×52 不同；
    /// ③ 稀有度走月后自定义 **17 档（传奇）**；④ 价值 **36 金**；⑤ 名字中英同为 `SHPC`；
    /// ⑥ 音效照 2.0（等离子球＝原版 `Item92`、能量束＝灾厄 `LaserCannon`）；⑦ **去掉合成配方**；
    /// ⑧ 增幅走 <see cref="DefenseBlade"/> 那套传奇加算表。
    /// </para>
    /// <para>
    /// **两种出手方式**（照 2.0：同一把枪左右键各一套，各自的间隔/蓝耗/音效互不影响）：
    /// 左键＝**等离子球** 5 发扇形、球伤害 ×1.1、魔力 20、间隔 7 × 7.14 ≈ 50 帧（球本身见 <see cref="SHPCPlasma"/>，
    /// 会停滞并引发大范围爆炸）；
    /// 右键＝**能量束** 5 道、击退 ×0.5、魔力 ×0.3（＝6）、间隔 7 帧（见 <see cref="SHPCLaser"/>）。
    /// </para>
    /// <para>
    /// **旧版支线三阶升级线的取舍（用户 2026-10-16 定）**：**T3 不移植**；
    /// **T1 / T2 常态化**（不做微光湖强化道具、不做葡萄汁解锁，效果直接常驻，实测后再定去留）：
    /// ① T2 → 两种招式一律 **5 发**（＝源版 `Tier2 ? 5 : 3` 的满档）；
    /// ② T1 → 能量束**每 60 帧**额外附带一颗**会追踪**的相位球（＝源版 `fireCD = 60` 的节奏）。
    /// 源里 T1 那条**实际是空转**（额外球不判 Tier、追踪判定又是死代码），本工程按"本该有的样子"写对，
    /// 追踪开关走弹幕的 `ai[2] = 1`（见 <see cref="SHPCPlasma"/>），冷却放玩家侧（见 <c>CalamityDemutationPlayer.SHPC.cs</c>）。
    /// </para>
    /// <para>
    /// **增幅**：照 <see cref="DefenseBlade"/> 的传奇进度表——石巨人 → 至尊灾厄共 18 档**加算**，
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
        /// <summary>
        /// 每次出手打出的弹幕数量：**5 发**（照 2.0 的 3 发，再按用户 2026-10-16 的口径把源版 旧版支线的 **T2 常态化**：两种招式都 +2）
        /// </summary>
        private const int ShotsPerUse = 5;
        /// <summary>右键能量束附带的那颗追踪相位球的冷却（帧）：照源 旧版支线的 <c>fireCD = 60</c></summary>
        private const int PhaseOrbCooldown = 60;
        /// <summary>
        /// 传奇武器增伤进度表：石巨人 → 至尊灾厄（主线 18 档）——**与 <see cref="DefenseBlade"/> 同一张表**。
        /// 每项为 (是否已击败, 固定增伤)；加算累加合计 +5.00（满配 6.00×）。
        /// 前中期增量小、终局增量大：反映难度递增，同时弥补伤害基数变大后固定增量的相对收益下降。
        /// </summary>
        private static readonly (Func<bool> Downed, float Increment)[] LegendaryBosses =
        [
            // —— 石巨人 → 月球领主 ——
            (() => NPC.downedGolemBoss, 0.06f),                          // 石巨人 Golem
            (() => BossSystem.Plaguebringer, 0.09f),                     // 瘟疫使者歌莉娅 Plaguebringer Goliath
            (() => BossSystem.Ravager, 0.09f),                           // 毁灭魔像（掠夺者）Ravager
            (() => NPC.downedAncientCultist, 0.06f),                     // 拜月教邪教徒 Lunatic Cultist
            (() => BossSystem.AstrumDeus, 0.12f),                        // 星神游龙 Astrum Deus
            (() => NPC.downedMoonlord, 0.12f),                           // 月球领主 Moon Lord
            // —— 月后 · 亵渎前 ——
            (() => BossSystem.Guardians, 0.12f),                         // 亵渎守卫 Profaned Guardians
            (() => BossSystem.Dragonfolly, 0.12f),                       // 丛林龙 Dragonfolly
            // —— 亵渎 → 噬神 ——
            (() => BossSystem.Providence, 0.25f),                        // 亵渎天神 Providence
            (() => BossSystem.CeaselessVoid || ClassicSentinelsDowned, 0.12f), // 无尽虚空 Ceaseless Void
            (() => BossSystem.StormWeaver || ClassicSentinelsDowned, 0.12f),   // 风暴编织者 Storm Weaver
            (() => BossSystem.Signus || ClassicSentinelsDowned, 0.12f),        // 西格纳斯 Signus
            (() => BossSystem.Polterghast, 0.31f),                       // 噬魂幽花 Polterghast
            (() => BossSystem.OldDuke, 0.31f),                           // 老公爵 Old Duke
            (() => BossSystem.DevourerOfGods, 0.49f),                    // 噬神者 Devourer of Gods
            // —— 终局 ——
            (() => BossSystem.Yharon, 1.26f),                            // 犽戎 Yharon
            (() => BossSystem.ExoMechs, 0.62f),                          // 星流巨械 Exo Mechs
            (() => BossSystem.SupremeCalamitas, 0.62f),                  // 至尊灾厄 Supreme Calamitas
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
            Item.shootSpeed = 30f;                    // 弹速 30（用户 2026-10-16 指定；本体 2.0 原值 20）
            Item.rare = ItemRarityID.Red;             // 基础稀有度：红（名称色由 postMoonLordRarity 覆盖）
            Item.value = Item.buyPrice(0, 36, 0, 0);  // 价值 36 金（用户 2026-10-16 指定）
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 17;   // 月后自定义稀有度 17 档（传奇）
        }
        /// <summary>
        /// 持握偏移：<c>HoldoutOffset().X</c> 直接就是贴图原点 X（不是增量；模组物品的默认值是 **10**）。
        /// 本件源口径为经典灾厄的 <c>(-25, 0)</c>（配 98×34 的小枪身，2.0 的 <c>(-35, -10)</c> 是配 124×52 大枪身），
        /// 末次调整由用户 2026-10-16 点名取 **-15**（先要"右移 10 点"、给过 -35，随即改口定 -15——以用户点名为准）。
        /// 若日后觉得还不够或过头，**改这一个数就行**。
        /// </summary>
        public override Vector2? HoldoutOffset() => new Vector2(-15, 0);
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
        /// 出手（照 2.0，按键照原版不交换）：右键＝能量束 5 道、击退 ×0.5（另每 60 帧附带一颗追踪相位球）；
        /// 左键＝等离子球 5 发扇形、球伤害 ×1.1。两处都返回 false（自己生成弹幕）。
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
                // T1 常态化：能量束每 60 帧额外补一颗"追踪相位球"（伤害与击退同本次射击的裸值，照源）
                CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
                if (modPlayer.shpcPhaseOrbCooldown <= 0)
                {
                    Projectile.NewProjectile(source, position.X, position.Y, velocity.X, velocity.Y, ModContent.ProjectileType<SHPCPlasma>(), damage, knockback, player.whoAmI, 0f, 0f, 1f);
                    modPlayer.shpcPhaseOrbCooldown = PhaseOrbCooldown;
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
        /// 常态基础 1.00×、满配 6.00×（合计 +5.00）。
        /// </summary>
        public static float LegendaryDamage()
        {
            float damageBuff = 1f;
            foreach (var (downed, increment) in LegendaryBosses)
            {
                if (downed())
                    damageBuff += increment;
            }
            return damageBuff;
        }
    }
}
