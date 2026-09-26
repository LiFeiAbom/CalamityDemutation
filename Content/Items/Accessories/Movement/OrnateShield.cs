using CalamityDemutation.Enums;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Movement
{
    /// <summary>
    /// 华丽盾（OrnateShield） - 盾牌冲刺饰品链的最下位，肉前中期即可获得。
    /// 装备后向冲刺系统注册 <see cref="ShieldSlamDash.OrnateShield"/>，获得一次盾牌冲撞（双击方向键触发，
    /// 具体冲撞数值与表现见 CalamityDemutationPlayer.ShieldSlamDash.cs），并禁用原版冲刺（dashType = 0）。
    /// <para>
    /// 被动（生命上限 +20、生命再生 +1 HP/s、生命低于 25% 时额外 +4 防御）不在本文件结算，
    /// 而是在玩家文件的 <c>if(ornateShield)</c> 块里统一处理。
    /// </para>
    /// </summary>
    internal class OrnateShield:ModItem
    {
        /// <summary>
        /// 基础属性：36x32 贴图、价值 12 金、稀有度粉、防御 8、作为饰品装备
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 36;                          // 贴图宽（像素）
            Item.height = 32;                         // 贴图高（像素）
            Item.value = Item.buyPrice(0, 12, 0, 0);  // 价值 12 金
            Item.rare = ItemRarityID.Pink;            // 稀有度：粉色
            Item.defense = 8;                         // 装备时 +8 防御
            Item.accessory = true;                    // 作为饰品装备
        }
        /// <summary>
        /// 装备时置位 ornateShield（玩家侧被动块的开启条件），并把当前盾牌冲刺设为 <see cref="ShieldSlamDash.OrnateShield"/>
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().ornateShield = true;
            player.GetModPlayer<CalamityDemutationPlayer>().shieldSlamDash = ShieldSlamDash.OrnateShield;
        }
        /// <summary>
        /// 配方（分版本）：灾厄寒元系矿锭 ×5 + 水晶碎片 ×10，秘银砧（两版材料同名不同物，分别注册）
        /// </summary>
        public override void AddRecipes()
        {
            // 现代版灾厄：寒元锭（CryonicBar）×5 + 水晶碎片×10，秘银砧
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if(calamity.TryFind<ModItem>("CryonicBar", out ModItem cryonicBar))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(cryonicBar.Type, 5);        // 灾厄材料：寒元锭 ×5
                    recipe.AddIngredient(ItemID.CrystalShard, 10);   // 水晶碎片 ×10
                    recipe.AddTile(TileID.MythrilAnvil);             // 秘银砧
                    recipe.Register();
                }
            }
            // 经典版灾厄：同名材料两版叫法不同，这里是 VerstaltiteBar ×5 + 水晶碎片×10，秘银砧
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if(calamity1.TryFind<ModItem>("VerstaltiteBar", out ModItem verstaltiteBar))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(verstaltiteBar.Type, 5);      // 经典版对应材料 ×5
                    recipe1.AddIngredient(ItemID.CrystalShard, 10);
                    recipe1.AddTile(TileID.MythrilAnvil);
                    recipe1.Register();
                }
            }
        }
    }
}
