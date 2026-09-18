using System.Collections.Generic;
using System.Linq;
using CalamityDemutation.Enums;
using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityDemutation.Content.Items.Accessories.Movement
{
    /// <summary>
    /// 极乐之庇护（Elysian Aegis） - 盾牌冲刺饰品链的中段，专家限定，掉落自亵渎天神（Providence）。
    /// 尺寸 48x42、价值 60 金、防御 16、稀有度青。
    /// 装备即向冲刺系统注册 ShieldSlamDash.ElysianAegis —— 神圣烈焰强化的盾牌冲撞（撞击伤害 500，
    /// 附带神圣爆炸弹幕与 300 帧神圣烈焰，见 CalamityDemutationPlayer.ShieldSlamDash.cs 的数值表）；
    /// 被动（生命上限 +100、生命回复 +8、火焰类减益免疫）在玩家文件的 if(elysianAegis) 块结算。
    /// 另有主动机制：按主动技能键切换"守护状态"，开启时护盾充能随时间消耗并转化为伤害/暴击/仇恨/防御，
    /// 关闭时回充 —— 结算在玩家文件的 if(elysianAegispower) 块，按键在 ProcessTriggers 中读取。
    /// </summary>
    [AutoloadEquip(EquipType.Shield)]
    internal class ElysianAegis:ModItem
    {
        /// <summary>
        /// 基础属性：48x42 贴图、价值 60 金、专家限定、稀有度青、防御 16、作为饰品装备
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 48;
            Item.height = 42;
            Item.value = Item.buyPrice(0, 60, 0, 0);
            Item.expert = true;
            Item.rare = ItemRarityID.Cyan;
            Item.defense = 16;
            Item.accessory = true;
        }
        /// <summary>
        /// 装备时置位三个标记：elysianAegis（被动块）、elysianAegispower（守护状态块的开启条件）、
        /// 并把当前盾牌冲刺设为 ShieldSlamDash.ElysianAegis
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().elysianAegis = true;
            player.GetModPlayer<CalamityDemutationPlayer>().elysianAegispower = true;
            player.GetModPlayer<CalamityDemutationPlayer>().shieldSlamDash = ShieldSlamDash.ElysianAegis;
        }
        /// <summary>
        /// 把 tooltip 里的 [KEY] 占位替换为守护状态的当前绑定键（玩家改键后随之更新）。
        /// 灾厄原版走它自己的工具条扩展（IntegrateHotkey / FindAndReplace，定义在灾厄 Utilities/ItemUtils.cs），
        /// 本工程没有那两个方法，故在此内联：只替换第一条 Mod 为 "Terraria" 且含 [KEY] 的说明行；
        /// 未绑定任何键时回退显示注册默认键 N。占位符 [KEY] 在 en-US 与 zh-Hans 两份 hjson 里都要保留。
        /// </summary>
        public override void ModifyTooltips(List<TooltipLine> list)
        {
            TooltipLine line = list.FirstOrDefault(x => x.Mod == "Terraria" && x.Text.Contains("[KEY]"));
            if (line != null)
                line.Text = line.Text.Replace("[KEY]", KeybindsSystem.ElysianKeyDisplay);
        }
    }
}
