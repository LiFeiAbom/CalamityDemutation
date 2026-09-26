using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Content.Projectiles.Summon;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Comprehensive
{
    /// <summary>
    /// 玫瑰石（RoseStone） - 综合饰品（召唤玫瑰仆从）
    /// 提供 +3% 通用伤害、+2 生命回复、+20 最大生命并发出红光，同时召唤玫瑰仆从。
    /// </summary>
    internal class RoseStone:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、稀有度与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 20;                          // 贴图宽（像素）
            Item.height = 20;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 15, 0, 0);  // 价值 15 金
            Item.rare = ItemRarityID.Pink;            // 稀有度：粉（Pink）
            Item.accessory = true;                    // 标记为饰品，可装备于饰品栏
        }
        /// <summary>
        /// 装备时：置位玫瑰石标记并（仅本地玩家侧）维持一只玫瑰仆从，避免多人下重复生成。
        /// 隐藏外观时只保留属性，不召唤玫瑰娘、粉色照明关闭（场上已有的由 BigBustyRose 据 roseStoneVisible 立刻消散）
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.roseStone = true;
            // 隐藏外观时只保留属性：不召唤玫瑰娘、粉色照明关闭，场上已有的也据此外标记立刻消散
            modPlayer.roseStoneVisible = !hideVisual;
            // 仅本地玩家负责生成并维护召唤物，避免多人下重复生成
            if (!hideVisual && player.whoAmI == Main.myPlayer)
            {
                if (player.FindBuffIndex(ModContent.BuffType<BrimstoneWaifu>()) == -1)
                {
                    player.AddBuff(ModContent.BuffType<BrimstoneWaifu>(), 3600, true);
                }
                if (player.ownedProjectileCounts[ModContent.ProjectileType<BigBustyRose>()] < 1)
                {
                    Projectile.NewProjectile(player.GetSource_Accessory(Item), player.Center.X, player.Center.Y, 0f, -1f, ModContent.ProjectileType<BigBustyRose>(), (int)(45f * player.GetDamage(DamageClass.Summon).Multiplicative), 2f, Main.myPlayer, 0f, 0f);
                }
            }
        }
        /// <summary>
        /// 与元素之心互斥：元素之心已包含玫瑰石的召唤物，避免重复装备。
        /// </summary>
        public override bool CanEquipAccessory(Player player, int slot, bool modded)/* tModPorter Suggestion: Consider using new hook CanAccessoryBeEquippedWith */
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (modPlayer.heartoftheElements)
            {
                return false;
            }
            return true;
        }
    }
}
