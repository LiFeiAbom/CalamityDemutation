using System.Collections.Generic;
using System.Linq;
using CalamityDemutation.Systems;
using Terraria.Localization;
using Terraria.ModLoader;
namespace CalamityDemutation.Utilities
{
    /// <summary>
    /// 通用工具类（Tooltip 部分）：数值膨胀开启时把装备的说明文字换成膨胀后的文案与数字。
    /// 约定：膨胀文案放在本地化键 <c>Mods.CalamityDemutation.Items.&lt;物品内部名&gt;.TooltipInflated</c>，
    /// 格式与 Tooltip 一致（多行用三引号块），未写该键的物品只做防御数字修正。
    /// 只替换原版正文行（名字以 Tooltip 开头），SetBonus 与其它行原样保留。
    /// </summary>
    internal static partial class CDUtil
    {
        /// <summary>
        /// 数值膨胀开启时，用 TooltipInflated 的整段文案替换原版说明正文，
        /// 并把原版「防御」行的数字改成实际生效的防御值。
        /// 说明：膨胀下的防御只能在 UpdateEquip 里补差值（Item.defense 不能在运行期改），
        /// 所以自动生成的防御行仍写着源值，这里按差值把数字改回来与实现对齐。
        /// </summary>
        /// <param name="tooltips">ModifyTooltips 钩子传入的说明行列表</param>
        /// <param name="item">当前物品（取内部名用于拼本地化键，取 Item.defense 用于算防御）</param>
        /// <param name="defenseBonus">膨胀开启时额外补的防御差值；0 表示本件防御不随膨胀变化</param>
        public static void ApplyInflatedTooltip(this List<TooltipLine> tooltips, ModItem item, int defenseBonus = 0)
        {
            if (!ConfigSystem.StatInflationEnabled)
                return;

            if (defenseBonus > 0)
            {
                TooltipLine defenseLine = tooltips.FirstOrDefault(x => x.Mod == "Terraria" && x.Name == "Defense");
                if (defenseLine != null)
                {
                    defenseLine.Text = defenseLine.Text.Replace(item.Item.defense.ToString(), (item.Item.defense + defenseBonus).ToString());
                }
            }

            string key = $"Mods.CalamityDemutation.Items.{item.Name}.TooltipInflated";
            if (!Language.Exists(key))
                return;

            int first = tooltips.FindIndex(x => x.Mod == "Terraria" && x.Name.StartsWith("Tooltip"));
            if (first < 0)
                return;
            int count = 0;
            while (first + count < tooltips.Count
                && tooltips[first + count].Mod == "Terraria"
                && tooltips[first + count].Name.StartsWith("Tooltip"))
            {
                count++;
            }

            string[] lines = Language.GetTextValue(key).Split('\n');
            tooltips.RemoveRange(first, count);
            for (int i = 0; i < lines.Length; i++)
            {
                tooltips.Insert(first + i, new TooltipLine(item.Mod, "Tooltip" + i, lines[i].TrimEnd('\r')));
            }
        }
    }
}
