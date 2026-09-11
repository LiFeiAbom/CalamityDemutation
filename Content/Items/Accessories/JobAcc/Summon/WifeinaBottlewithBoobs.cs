using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Content.Projectiles.Summon;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Summon
{
    /// <summary>
    /// 瓶中妻（大胸版） - 召唤职业专家饰品
    /// 装备后召唤沙之妻（DrewsSandyWaifu）仆从，与元素之心互斥。
    /// </summary>
    internal class WifeinaBottlewithBoobs:ModItem
    {
        /// <summary>
        /// 物品基础属性：贴图尺寸、售价、专家专属与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 20;                         // 贴图宽 20 像素
            Item.height = 26;                        // 贴图高 26 像素
            Item.value = Item.buyPrice(0, 15, 0, 0); // 售价 15 金
            Item.expert = true;                      // 专家模式专属物品
            Item.accessory = true;                   // 标记为饰品，可装备于饰品栏
        }
        /// <summary>
        /// 装备时置位 wifeinaBottlewithBoobs 标记，并在本地玩家侧补挂 DrewsSandyWaifu buff 与仆从弹幕
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 装备时仅置位标记，仆从生成逻辑见下方
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.wifeinaBottlewithBoobs = true;
            // 仅在本地玩家侧生成仆从，避免多人下重复
            if (player.whoAmI == Main.myPlayer)
            {
                // buff 仅作为状态标记，仆从本体由下方弹幕实现
                if (player.FindBuffIndex(ModContent.BuffType<Buffs.SummonBuffs.DrewsSandyWaifu>()) == -1)
                {
                    player.AddBuff(ModContent.BuffType<Buffs.SummonBuffs.DrewsSandyWaifu>(), 3600, true);
                }
                // 数量不足 1 时补生成，防止每帧叠加多个仆从
                if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.DrewsSandyWaifu>()] < 1)
                {
                    Projectile.NewProjectile(player.GetSource_Accessory(Item), player.Center.X, player.Center.Y, 0f, -1f, ModContent.ProjectileType<Projectiles.Summon.DrewsSandyWaifu>(), (int)(45f * player.GetDamage(DamageClass.Summon).Multiplicative), 2f, Main.myPlayer, 0f, 0f);
                }
            }
        }
        /// <summary>
        /// 装备互斥判定：已装备元素之心（heartoftheElements）时禁止再装备本饰品，避免仆从/效果叠加
        /// </summary>
        public override bool CanEquipAccessory(Player player, int slot, bool modded)/* tModPorter Suggestion: Consider using new hook CanAccessoryBeEquippedWith */
        {
            // 与元素之心互斥：已装备元素之心时禁止再装备本饰品，避免效果叠加
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (modPlayer.heartoftheElements)
            {
                return false;
            }
            return true;
        }
    }
}
