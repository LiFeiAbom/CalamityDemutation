using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Function
{
    /// <summary>
    /// 铅芯（Lead Core） - 功能型饰品
    /// 装备后免疫灾厄的"辐照（Irradiated）"debuff。
    /// 与其他标记型饰品不同，此饰品不置位 ModPlayer 字段，而是直接改写 player.buffImmune 即时生效。
    /// </summary>
    internal class LeadCore:ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、稀有度、价值与饰品标记
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 26;                         // 贴图宽（像素）
            Item.height = 26;                        // 贴图高（像素）
            Item.rare = ItemRarityID.Green;          // 稀有度：绿色
            Item.value = Item.buyPrice(0, 3, 0, 0);  // 价值 3 金
            Item.accessory = true;                   // 作为饰品装备
        }
        /// <summary>
        /// 装备时直接把玩家对"辐照（Irradiated）"的免疫置位；
        /// 现代版与经典版灾厄各自查找同名 debuff，两分支互相兼容
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：Irradiated
                if(calamity.TryFind<ModBuff>("Irradiated", out ModBuff irradiated1))
                {
                    player.buffImmune[irradiated1.Type] = true;
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄：同名 Irradiated
                if(calamity1.TryFind<ModBuff>("Irradiated", out ModBuff irradiated2))
                {
                    player.buffImmune[irradiated2.Type] = true;
                }
            }
        }
    }
}
