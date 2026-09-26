using CalamityDemutation.Content.Items.Accessories.Comprehensive;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Summon
{
    /// <summary>
    /// 真菌团块 - 召唤职业专家饰品（克鲁布龙宝藏袋掉落）
    /// 装备后召唤一只真菌团块仆从自动攻击敌人，最多同时存在 1 只。
    /// </summary>
    internal class FungalClump:ModItem
    {
        /// <summary>
        /// 基础属性：小尺寸贴图、专家限定、价值 9 金、作为饰品装备
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.value = Item.buyPrice(0, 9, 0, 0);
            Item.expert = true;
            Item.accessory = true;
        }
        /// <summary>
        /// 装备时：置位真菌团块标记并（仅本地玩家侧）维持真菌团块仆从存在
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.fungalClump = true;
            // 仅在本地玩家侧生成，避免多人模式下重复生成
            if (player.whoAmI == Main.myPlayer)
            {
                // buff 仅作存活状态标记，真正的仆从由下方弹幕实现
                if (player.FindBuffIndex(ModContent.BuffType<Buffs.SummonBuffs.FungalClump>()) == -1)
                {
                    player.AddBuff(ModContent.BuffType<Buffs.SummonBuffs.FungalClump>(), 3600, true);
                }
                // 数量不足 1 时补生成，防止每帧重复叠加
                if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.FungalClump>()] < 1)
                {
                    Projectile.NewProjectile(player.GetSource_Accessory(Item), player.Center.X, player.Center.Y, 0f, -1f, ModContent.ProjectileType<Projectiles.Summon.FungalClump>(), (int)(10f * player.GetDamage<SummonDamageClass>().Multiplicative), 1f, Main.myPlayer, 0f, 0f);
                }
            }
        }
        /// <summary>
        /// 互斥判定：已装备真菌团块，或已装备大杂烩（其已包含真菌团块）时禁止重复装备。
        /// 改用 CanAccessoryBeEquippedWith，双向互斥且不再自检自己的标记
        /// </summary>
        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player) => incomingItem.type != ModContent.ItemType<FungalClump>() && incomingItem.type != ModContent.ItemType<TheAmalgam>();
    }
}
