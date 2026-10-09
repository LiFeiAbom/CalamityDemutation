using CalamityDemutation.Content.Projectiles.Summon;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Summon
{
    /// <summary>
    /// 古冰晶（Ancient Ice Chunk）—— 「归虚之灵」链（CI 口径）的自持件之一。
    /// 老规矩取灾厄 **2.0.3.9**：38×50、伤害 **25**、魔力 10、使用/动画 **25 帧**、击退 2、
    /// **浅红档**、价值 **12 金**（`Rarity4BuyPrice`）、音 `SoundID.Item30`、`autoReuse`，
    /// 整张 6 帧竖直动画贴图（`DrawAnimationVertical(6, 6)` + 灵魂式渲染），
    /// 在鼠标处召唤一头冰灵（<see cref="IceClasperMinion"/>）。
    /// </summary>
    /// <remarks>
    /// 为什么要自持：本件在现代版里**仍在**，但被砍过（1.4.4-release 把使用帧从 25 改成 36 等）——
    /// 按用户 2026-10-09 的口径「1.4.4-release 大砍、需要回调」自持 2.0.3.9 版，供归虚之灵当材料。
    /// <para>
    /// **无配方**——来源是**冰灵 `IceClasper` 掉落**（照源：1/3 概率掉 1 个，现代与经典两条分支各挂一次，
    /// 见 `NPCs/CalamityDemutationGlobalNPC.cs` 的 `ModifyNPCLoot`）。
    /// </para>
    /// <para>
    /// 与源的一处刻意差异：源 2.0.3.9 的 `Shoot` **没写** `originalDamage`（上游到 1.4.4-release 才补上），
    /// 本工程照 1.4.4-release 的写法补 `originalDamage = Item.damage`，免得召唤物伤害二次缩放；
    /// 同批落地的元素之斧也是这么写的。
    /// </para>
    /// </remarks>
    internal class AncientIceChunk:ModItem
    {
        // ── 源里写在物品上的可调参数（冰灵仆从直接读这几个静态字段，照源保留）──
        /// <summary>逐敌独立冷却（帧，源值 20；实际使用时再乘 MaxUpdates）</summary>
        public static int IFrames = 20;
        /// <summary>跟随态允许离主人多远（像素，源值 400）</summary>
        public static float MaxDistanceFromOwner = 400f;
        /// <summary>玩家离目标进到这么近就让冰灵改冲撞（源值 250）</summary>
        public static float DistanceToDash = 250f;
        /// <summary>主人离目标远过这个值就退出冲撞（源值 800）</summary>
        public static float DistanceToStopDash = 800f;
        /// <summary>冲撞基础速度（源值 12）</summary>
        public static float MinVelocity = 12f;
        /// <summary>朝远处目标喷冰锥的间隔（帧，源值 80）</summary>
        public static float TimeToShoot = 80f;
        /// <summary>冰锥伤害倍率（源值 1.5；源注释吐槽"它们有点弱"）</summary>
        public static float ProjectileDMGMultiplier = 1.5f;

        /// <summary>研究解锁一份（照源 `SacrificeTotal = 1`）、注册 6 帧竖直动画与灵魂式渲染</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(6, 6));
            ItemID.Sets.AnimatesAsSoul[Type] = true;
        }
        /// <summary>基础属性：照源 2.0.3.9（38×50、伤害 25、击退 2、使用·动画 25 帧、浅红档、12 金）</summary>
        public override void SetDefaults()
        {
            Item.width = 38;
            Item.height = 50;
            Item.damage = 25;
            Item.DamageType = DamageClass.Summon;
            Item.shoot = ModContent.ProjectileType<IceClasperMinion>();
            Item.knockBack = 2f;
            Item.useTime = Item.useAnimation = 25;
            Item.mana = 10;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(0, 12, 0, 0);   // 源用 Rarity4BuyPrice = buyPrice(0, 12, 0, 0)
            Item.rare = ItemRarityID.LightRed;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item30;
        }
        /// <summary>在鼠标处召唤冰灵、带一点点随机初速（照源 2.0.3.9），并补写 originalDamage</summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var minion = Projectile.NewProjectileDirect(source, Main.MouseWorld, Main.rand.NextVector2Circular(1f, 1f), type, damage, knockback, player.whoAmI);
            if (minion is not null && Main.projectile.IndexInRange(minion.whoAmI))
                minion.originalDamage = Item.damage;
            return false;
        }
    }
}
