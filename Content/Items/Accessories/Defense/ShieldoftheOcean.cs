using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Defense
{
    /// <summary>
    /// 海洋之盾（ShieldoftheOcean） - 肉前海洋系防御饰品。
    /// 装备时置位 shieldoftheOcean 标记，玩家身处水中（<c>Collision.DrownCollision</c> 判为溺水环境）时
    /// 额外 +5 防御，结算在玩家文件的 <c>if(shieldoftheOcean)</c> 块；本体另给 2 点常驻防御。
    /// </summary>
    internal class ShieldoftheOcean:ModItem
    {
        /// <summary>
        /// 基础属性：24x28 贴图、价值 3 金、稀有度绿、防御 2、作为饰品装备
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 24;                          // 贴图宽（像素）
            Item.height = 28;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 3, 0, 0);   // 价值 3 金
            Item.rare = ItemRarityID.Green;           // 稀有度：绿色
            Item.defense = 2;                         // 装备时 +2 防御
            Item.accessory = true;                    // 作为饰品装备
        }
        /// <summary>
        /// 装备时置位 shieldoftheOcean 标记，"入水额外 +5 防御"的判定在玩家文件里统一结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().shieldoftheOcean = true;
        }
        /// <summary>
        /// 配方（分版本）：灾厄海洋系材料 ×5 + 珊瑚 ×5，铁砧（两版材料同名不同物，分别注册）
        /// </summary>
        public override void AddRecipes()
        {
            // 现代版灾厄：海之遗骸（SeaRemains）×5 + 珊瑚×5，铁砧
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if(calamity.TryFind<ModItem>("SeaRemains", out ModItem seaRemains))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(seaRemains.Type, 5);     // 灾厄材料：海之遗骸 ×5
                    recipe.AddIngredient(ItemID.Coral, 5);        // 珊瑚 ×5
                    recipe.AddTile(TileID.Anvils);                // 铁砧
                    recipe.Register();
                }
            }
            // 经典版灾厄：同名材料两版叫法不同，这里是维斯蒂德锭（VictideBar）×5 + 珊瑚×5，铁砧
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if(calamity1.TryFind<ModItem>("VictideBar", out ModItem victideBar))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(victideBar.Type, 5);   // 经典版对应材料 ×5
                    recipe1.AddIngredient(ItemID.Coral, 5);
                    recipe1.AddTile(TileID.Anvils);
                    recipe1.Register();
                }
            }
        }
    }
}
