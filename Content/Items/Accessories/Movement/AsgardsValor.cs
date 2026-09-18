using CalamityDemutation.Content.Items.Accessories.Attack;
using CalamityDemutation.Content.Items.Accessories.Defense;
using CalamityDemutation.Enums;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityDemutation.Content.Items.Accessories.Movement
{
    /// <summary>
    /// 阿斯加德之英勇（Asgard's Valor） - 盾牌冲刺饰品链的中段，由原版十字章护盾 + 华丽盾 +
    /// 海之盾 + 阿巴顿 进阶合成。尺寸 38x44、价值 45 金、防御 16、稀有度青。
    /// 装备即向冲刺系统注册 ShieldSlamDash.AsgardsValor —— 基础档盾牌冲撞（撞击伤害 200、无附加弹幕/减益，
    /// 见 CalamityDemutationPlayer.ShieldSlamDash.cs 的数值表）；
    /// 被动（生命上限 +50、免击退/免火块、大量冰霜与烈焰类减益免疫、液体中 +12% 减伤）
    /// 统一在 CalamityDemutationPlayer 的 if(asgardsValor) 块结算。
    /// </summary>
    [AutoloadEquip(EquipType.Shield)]
    internal class AsgardsValor:ModItem
    {
        /// <summary>
        /// 基础属性：38x44 贴图、价值 45 金、防御 16、稀有度青、作为饰品装备
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 38;
            Item.height = 44;
            Item.value = Item.buyPrice(0, 45, 0, 0);
            Item.rare = ItemRarityID.Cyan;
            Item.defense = 16;
            Item.accessory = true;
        }
        /// <summary>
        /// 装备时置位 asgardsValor 标记（被动结算的闸门），并把当前盾牌冲刺设为 ShieldSlamDash.AsgardsValor
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().asgardsValor = true;
            player.GetModPlayer<CalamityDemutationPlayer>().shieldSlamDash = ShieldSlamDash.AsgardsValor;
        }
        /// <summary>
        /// 配方（分版本），两版都以十字章护盾 + 华丽盾 + 海之盾 + 阿巴顿 + 生命果×5 为坯料，于秘银砧合成：
        /// 现代版另加 CoreofCalamity×3；经典版改加 CoreofEleum / CoreofCinder / CoreofChaos 各×3
        /// （三个核心在原版灾厄是同一件"灾厄核心"的分支，经典版按世界邪恶/生态拆成三件）。
        /// 两分支都用 TryGetMod + TryFind 串联：灾厄两版材料名改动频繁，缺材料时静默跳过该条配方，
        /// 而不是让 Find 抛异常把整个模组卡在加载阶段。
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if(calamity.TryFind<ModItem>("CoreofCalamity", out ModItem coreofCalamity))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.AnkhShield);
                    recipe.AddIngredient<OrnateShield>();
                    recipe.AddIngredient<ShieldoftheOcean>();
                    recipe.AddIngredient<Abaddon>();
                    recipe.AddIngredient(coreofCalamity.Type, 3);
                    recipe.AddIngredient(ItemID.LifeFruit, 5);
                    recipe.AddTile(TileID.MythrilAnvil);
                    recipe.Register();
                }
            }
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if(calamity1.TryFind<ModItem>("CoreofEleum", out ModItem coreofEleum)
                    && calamity1.TryFind<ModItem>("CoreofCinder", out ModItem coreofCinder)
                    && calamity1.TryFind<ModItem>("CoreofChaos", out ModItem coreofChaos))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(ItemID.AnkhShield);
                    recipe1.AddIngredient<OrnateShield>();
                    recipe1.AddIngredient<ShieldoftheOcean>();
                    recipe1.AddIngredient<Abaddon>();
                    recipe1.AddIngredient(coreofEleum.Type, 3);
                    recipe1.AddIngredient(coreofCinder.Type, 3);
                    recipe1.AddIngredient(coreofChaos.Type, 3);
                    recipe1.AddIngredient(ItemID.LifeFruit, 5);
                    recipe1.AddTile(TileID.MythrilAnvil);
                    recipe1.Register();
                }
            }
        }
    }
}
