using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Content.Projectiles.Summon;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Summon
{
    /// <summary>
    /// 太阳之灵法杖（Sun Spirit Staff）—— 「太阳神杖 / 天狼星」那条链的中间件，
    /// 也是链顶太阳神杖（SunGodStaff）的配方前件。
    /// 老规矩取灾厄 2.0：44x48 判定、伤害 12、魔力 10、击退 1.15、使用 35 帧、蓝档、
    /// 价值 1 金（2.0 的 Rarity1BuyPrice）、音效 SoundID.Item44，召唤太阳之灵（SolarPixie），
    /// 且全场只能同时存在一只（源用 CanUseItem 卡住）。
    /// </summary>
    /// <remarks>
    /// 为什么要自持：本件在实装 2.2.2 里仍然在，但现行版本是"可叠层强化"的重制版（22 伤，
    /// 配方换成琥珀 / 蚁狮颚 / 棕榈木）；本工程按"旧版回归 + 同名不同物"的老做法自持一份 2.0 版，
    /// 供链顶的太阳神杖当材料（与本体同名件互不干扰，可同时持有）。
    /// <para>
    /// 与源的差异：源调用灾厄的 CalamityUtils.KillShootProjectiles 清场，本工程是软依赖、
    /// 不引用灾厄类型，改为照经典版写等价的"先杀掉自己在场的太阳之灵再召唤"循环。
    /// </para>
    /// </remarks>
    internal class SunSpiritStaff:ModItem
    {
        /// <summary>研究解锁 1 个（照源 2.0 的 SacrificeTotal）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>基础属性：照源 2.0（伤害 12 / 魔力 10 / 击退 1.15 / 使用 35 帧 / 蓝档 / 1 金）</summary>
        public override void SetDefaults()
        {
            Item.damage = 12;
            Item.mana = 10;
            Item.width = 44;
            Item.height = 48;
            Item.useTime = Item.useAnimation = 35;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.knockBack = 1.15f;
            Item.value = Item.buyPrice(0, 1, 0, 0);   // 2.0 的 Rarity1BuyPrice = buyPrice(0, 1, 0, 0)
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = SoundID.Item44;
            Item.shoot = ModContent.ProjectileType<SolarPixie>();
            Item.DamageType = DamageClass.Summon;
        }
        /// <summary>场上已有自己的太阳之灵时不可再召唤（源用 ownedProjectileCounts 卡单一灵体）</summary>
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
        /// <summary>
        /// 召唤太阳之灵：先清掉自己在场的同类（源靠 CalamityUtils，这里照经典版写等价循环），
        /// 在玩家的使用位置生成弹幕并把原始面板伤害写进 originalDamage（供召唤加成结算）。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 清场：自己名下的同类召唤物全部移除，保证"只有一个灵体"
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.type == type)
                {
                    p.Kill();
                }
            }
            int idx = Projectile.NewProjectile(source, position, Vector2.Zero, type, damage, knockback, player.whoAmI);
            if (Main.projectile.IndexInRange(idx))
            {
                Main.projectile[idx].originalDamage = Item.damage;
            }
            return false;
        }
        /// <summary>
        /// 配方（照源 2.0，经典 cal-1.1~1.4.2.101 逐字相同）：砂岩砖×20 + 沙漠羽毛×2 @ 铁砧。
        /// 现代与经典两条线连配方都一字不差，且两味材料都不依赖灾厄
        ///（砂岩砖是原版物品、沙漠羽毛是本模组自持件），故只注册这一条，两分支通用。
        /// </summary>
        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.SandstoneBrick, 20).
                AddIngredient<DesertFeather>(2).
                AddTile(TileID.Anvils).
                Register();
        }
    }
}
