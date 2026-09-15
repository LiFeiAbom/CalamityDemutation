using System.Collections.Generic;
using CalamityDemutation.Systems.Cooldowns;
using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
namespace CalamityDemutation.Utilities
{
    /// <summary>
    /// 通用工具类（冷却部分）：冷却字典的增删查扩展（对应灾厄 Utilities/PlayerUtils.cs 的 Cooldowns 区），
    /// 以及冷却图标上"八向描边字符串"的绘制工具（对应灾厄 CalamityUtils.DrawBorderStringEightWay）。
    /// </summary>
    internal static partial class CDUtil
    {
        /// <summary>
        /// 玩家身上是否正挂着指定 ID 的冷却
        /// </summary>
        public static bool HasCooldown(this Player p, string id)
        {
            if (p is null)
                return false;
            CalamityDemutationPlayer modPlayer = p.CWR();
            return !(modPlayer is null) && modPlayer.cooldowns.ContainsKey(id);
        }
        /// <summary>
        /// 给玩家挂上指定 ID 的冷却并自动创建实例；默认覆盖同 ID 的已有实例
        /// </summary>
        /// <returns>创建出的冷却实例（注意：实例总会创建，但不一定被挂到玩家身上）</returns>
        public static CooldownInstance AddCooldown(this Player p, string id, int duration, bool overwrite = true)
        {
            var cd = CooldownRegistry.Get(id);
            CooldownInstance instance = new CooldownInstance(p, cd, duration);
            if (!p.HasCooldown(id) || overwrite)
                p.CWR().cooldowns[id] = instance;
            return instance;
        }
        /// <summary>
        /// 给玩家挂上指定 ID 的冷却，并把额外参数经反射透传给处理器构造函数；
        /// 默认覆盖同 ID 的已有实例
        /// </summary>
        /// <returns>创建出的冷却实例（注意：实例总会创建，但不一定被挂到玩家身上）</returns>
        public static CooldownInstance AddCooldown(this Player p, string id, int duration, bool overwrite = true, params object[] handlerArgs)
        {
            var cd = CooldownRegistry.Get(id);
            CooldownInstance instance = new CooldownInstance(p, cd, duration, handlerArgs);
            if (!p.HasCooldown(id) || overwrite)
                p.CWR().cooldowns[id] = instance;
            return instance;
        }
        /// <summary>
        /// 移除玩家身上指定 ID 的冷却（该冷却不存在时不做任何事）
        /// </summary>
        public static void ClearCooldown(this Player p, string id)
        {
            p.CWR().cooldowns.Remove(id);
        }
        /// <summary>
        /// 取出玩家身上所有需要显示的冷却实例（handler.ShouldDisplay 为 true 的项）
        /// </summary>
        public static IList<CooldownInstance> GetDisplayedCooldowns(this Player p)
        {
            List<CooldownInstance> ret = new List<CooldownInstance>(16);
            if (p is null)
                return ret;
            foreach (CooldownInstance instance in p.CWR().cooldowns.Values)
                if (instance.handler.ShouldDisplay)
                    ret.Add(instance);
            return ret;
        }
        /// <summary>
        /// 八向描边绘制字符串（移植自灾厄 CalamityUtils.DrawBorderStringEightWay）：
        /// 先在周围 8 个方向各画一遍描边色，再在正中画主色，得到无死角的描边文字
        /// </summary>
        public static void DrawBorderStringEightWay(SpriteBatch sb, DynamicSpriteFont font, string text, Vector2 baseDrawPosition, Color main, Color border, float scale = 1f)
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector2 drawPosition = baseDrawPosition + new Vector2(x, y);
                    if (x == 0 && y == 0)
                        continue;
                    DynamicSpriteFontExtensionMethods.DrawString(sb, font, text, drawPosition, border, 0f, default, scale, SpriteEffects.None, 0f);
                }
            }
            DynamicSpriteFontExtensionMethods.DrawString(sb, font, text, baseDrawPosition, main, 0f, default, scale, SpriteEffects.None, 0f);
        }
    }
}
