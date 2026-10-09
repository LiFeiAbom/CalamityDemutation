using CalamityDemutation.Content.Projectiles.Summon;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Summon
{
    /// <summary>
    /// 灾厄挽歌（Calamari's Lament）—— 「归虚之灵」链（CI 口径）**最后一件自持件**。
    /// 老规矩取灾厄 **2.0.3.9**：88×108、伤害 **110**、使用/动画 **10 帧**、魔力 10、`autoReuse`、法杖姿态、
    /// **月后 13 档（荧光绿，即灾厄 `PureGreen`）**、价值 **1 铂金 30 金**（`Rarity13BuyPrice`）、音 `SoundID.Item85`，
    /// 在鼠标处召唤一只**小鱿鱼**（<see cref="CalamarisLamentMinion"/>）。
    /// </summary>
    /// <remarks>
    /// 为什么自持、为什么取 2.0.3.9：1.4.4-release（实装那版）是伤害 120 / 使用 **24 帧**（更慢），
    /// 2.0 那版则**只有物品本体**（仆从、增益、墨汁弹都是 2.0.3.9 才补齐的），故取 2.0.3.9 这套完整实现。
    /// <para>
    /// **与源的两处差异**：① 源里那个 `[GFB]` 彩蛋（天顶世界专用音效 `CalamityMod/Sounds/Item/Inkling` 与
    /// "Woomy!" 提示行）本工程未移植，tooltip 直接写常规文案；
    /// ② 源用 `npcLoot` 的 PostPolter 条件挂**巨型鱿鱼 `ColossalSquid`** 1/3 掉落（见
    /// `NPCs/CalamityDemutationGlobalNPC.cs`，本工程用 `BossSystem.Polterghast` 复刻同一条门槛）。
    /// </para>
    /// </remarks>
    internal class CalamarisLament:ModItem
    {
        // ── 源里写在物品上的可调参数（仆从与墨汁弹直接读，照源保留）──
        /// <summary>索敌半径（源值 8000——写这么大是为了能稳定咬住神明吞噬者那种超长 Boss）</summary>
        public static float EnemyDistanceDetection = 8000f;
        /// <summary>远程态：朝目标贴近的额外速度（源值 10）</summary>
        public static float ShootingExtraTargettingSpeed = 10f;
        /// <summary>远程态允许离主人多远（源值 320）</summary>
        public static float ShootingMinionDistance = 320f;
        /// <summary>远程态的开火间隔（帧，源值 30）</summary>
        public static int ShootingFireRate = 30;
        /// <summary>墨汁弹速度（源值 20）</summary>
        public static float ShootingProjectileSpeed = 20f;

        /// <summary>玩家离目标近到这么近就改成"缠上去"（源值 400）</summary>
        public static float LatchingDistanceRequired = 400f;
        /// <summary>纠缠态：追目标时的额外速度（源值 30）</summary>
        public static float LatchingExtraTargettingSpeed = 30f;
        /// <summary>纠缠态伤害倍率（源值 1.25）</summary>
        public static float LatchingDamageMultiplier = 1.25f;
        /// <summary>逐敌独立冷却（帧，源值 30）</summary>
        public static int LatchingIFrames = 30;

        /// <summary>研究解锁一份（源 2.0.3.9 未显式写，取与本工程其它召唤件同口径）、法杖姿态（照源）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            Item.staff[Type] = true;
        }
        /// <summary>基础属性：照源 2.0.3.9（88×108、伤害 110、使用 10 帧、月后 13 档、1 铂金 30 金）</summary>
        public override void SetDefaults()
        {
            Item.width = 88;
            Item.height = 108;
            Item.damage = 110;
            Item.shoot = ModContent.ProjectileType<CalamarisLamentMinion>();
            Item.DamageType = DamageClass.Summon;

            Item.useTime = Item.useAnimation = 10;
            Item.mana = 10;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item85;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 13;   // 月后 13：荧光绿（＝灾厄 PureGreen）
            Item.value = Item.buyPrice(1, 30, 0, 0);   // 源用 Rarity13BuyPrice = buyPrice(1, 30, 0, 0)

            Item.shootSpeed = 1f;   // 源注释：这个值没用，只是为了让物品表现为法杖
        }
        /// <summary>在鼠标处召唤小鱿鱼（照源）</summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, Main.MouseWorld, velocity, type, damage, knockback, player.whoAmI);
            return false;
        }
    }
}
