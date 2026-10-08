using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Materials
{
    /// <summary>
    /// 沙漠羽毛（Desert Feather）—— 「太阳神杖 / 天狼星」那条链的**链底材料**。
    /// 老规矩取灾厄 **2.0**：24×24 判定（贴图 24×32）、堆叠 999、售出价 **20 铜**、**蓝档**、研究解锁 5。
    /// </summary>
    /// <remarks>
    /// 为什么要自持：它在灾厄 **1.4.4 世系里已被删除**（本机实装 2.2.2 的 `.tmod` 里 0 命中），
    /// 而 1.3 / 2.0 / 经典三条线都还在，且是太阳之灵法杖（→ 太阳神杖）的配方材料。
    /// <para>
    /// **无配方**——来源是**秃鹫掉落**，照 2.0 的 `CalamityGlobalNPCLoot` 写：
    /// 100% 掉 1~2 片（见 `NPCs/CalamityDemutationGlobalNPC.cs` 的 `ModifyNPCLoot`；
    /// 经典版 cal-1.4.2.101 的掉率是"普通 100% 掉 1 片、专家 1~3 片"，本件按 2.0 口径取 1~2）。
    /// </para>
    /// </remarks>
    internal class DesertFeather : ModItem
    {
        /// <summary>研究解锁 5 个（照源 2.0 的 SacrificeTotal）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 5;
        }
        /// <summary>基础属性：24×24、堆叠 999、售出价 20 铜、蓝档（照源 2.0）</summary>
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.maxStack = 999;
            Item.value = Item.sellPrice(copper: 20);
            Item.rare = ItemRarityID.Blue;
        }
    }
}
