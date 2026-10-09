using CalamityDemutation.Content.Projectiles.Summon;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Summon
{
    /// <summary>
    /// 圣化火花（Sanctified Spark）—— 「归虚之灵」链（CI 口径）的自持件之一。
    /// 老规矩取灾厄 **2.0.3.9**（它在 2.0 里叫**能量法杖 `EnergyStaff`**，后来改名成本件，
    /// 源里就标着 `[LegacyName("EnergyStaff")]`）：66×68、伤害 **128**、魔力 10、使用/动画 **14 帧**、
    /// 击退 5、**紫档**、价值 **1 铂金 10 金**（`Rarity11BuyPrice`）、`autoReuse`、法杖姿态；
    /// 它是**哨兵**武器（`Item.sentry`）——在鼠标处插一座亵渎能量炮台（<see cref="ProfanedEnergy"/>）。
    /// </summary>
    /// <remarks>
    /// 为什么要自持：1.4.4-release 把它大改过（伤害 128→100、**魔力 10→100**、使用帧 14→30，
    /// 哨兵本体 `ProfanedEnergy` 更是从 159 行整段重写成 222 行的新写法），
    /// 按用户 2026-10-09「1.4.4-release 大砍、需要回调」的口径自持 2.0.3.9 版。
    /// <para>
    /// **无配方**——来源是**不动明王 `ImpiousImmolator` 掉落 1/15**（照源 2.0.3.9 与经典 1.4.2.101 的同一条，
    /// 现代与经典两条分支各挂一次，见 `NPCs/CalamityDemutationGlobalNPC.cs`）；
    /// 源里另有一条"天顶世界下 装甲掘墓者头 1/10"的彩蛋掉落，本工程未移植。
    /// </para>
    /// <para>
    /// 与源的一处差异：源的 `[LegacyName("EnergyStaff")]` 是灾厄自己的跨 mod 旧名兼容特性，
    /// 本工程用不到，故未写。
    /// </para>
    /// </remarks>
    internal class SanctifiedSpark:ModItem
    {
        /// <summary>法杖姿态（照源：物品按"持杖"方式绘制与挥动）</summary>
        public override void SetStaticDefaults()
        {
            Item.staff[Item.type] = true;
        }
        /// <summary>基础属性：照源 2.0.3.9（66×68、伤害 128、击退 5、使用·动画 14 帧、紫档、1 铂金 10 金）</summary>
        public override void SetDefaults()
        {
            Item.width = 66;
            Item.height = 68;
            Item.damage = 128;
            Item.DamageType = DamageClass.Summon;
            Item.sentry = true;              // 哨兵武器：吃哨兵栏位而非召唤栏
            Item.mana = 10;
            Item.useTime = Item.useAnimation = 14;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 5f;
            Item.value = Item.buyPrice(1, 10, 0, 0);   // 源用 Rarity11BuyPrice = buyPrice(1, 10, 0, 0)
            Item.rare = ItemRarityID.Purple;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<ProfanedEnergy>();
        }
        /// <summary>
        /// 在鼠标处插一座炮台（<c>ai[0] = 16</c> 是"登场后等 16 帧才开火"，照源），
        /// 补写 <c>originalDamage</c>，并刷新哨兵栏位（照源的 <c>player.UpdateMaxTurrets()</c>）。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int p = Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero, type, damage, knockback, player.whoAmI, 16f);
            if (Main.projectile.IndexInRange(p))
                Main.projectile[p].originalDamage = Item.damage;
            player.UpdateMaxTurrets();
            return false;
        }
    }
}
