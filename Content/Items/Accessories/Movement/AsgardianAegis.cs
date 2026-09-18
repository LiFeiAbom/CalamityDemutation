using CalamityDemutation.Enums;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ModLoader;

namespace CalamityDemutation.Content.Items.Accessories.Movement
{
    /// <summary>
    /// 阿斯加德之庇护（Asgardian Aegis） - 盾牌冲刺饰品链的顶端，由阿斯加德之英勇 + 极乐之庇护 进阶合成。
    /// 尺寸 60x54、价值 90 金、防御 28、月后自定义稀有度 14（蓝名）。
    /// 装备即向冲刺系统注册 ShieldSlamDash.AsgardianAegis —— 宇宙地狱火强化的盾牌冲撞（撞击伤害 1000，
    /// 附带宇宙爆炸弹幕与 300 帧弑神者地狱火，见 CalamityDemutationPlayer.ShieldSlamDash.cs 的数值表）；
    /// 生命上限/生命回复/减伤与大量减益免疫等被动，统一在 CalamityDemutationPlayer 的 if(asgardianAegis) 块结算。
    /// 与同文件夹的阿斯加德之英勇 / 极乐之庇护 一样挂 EquipType.Shield，装备时以盾牌外观渲染（需 AsgardianAegis_Shield.png）。
    /// </summary>
    [AutoloadEquip(EquipType.Shield)]
    internal class AsgardianAegis:ModItem
    {
        /// <summary>
        /// 基础属性：60x54 贴图、价值 90 金、防御 28、作为饰品装备、月后稀有度 14
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 60;
            Item.height = 54;
            Item.value = Item.buyPrice(0, 90, 0, 0); //30 gold reforge
            Item.defense = 28;
            Item.accessory = true;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;
        }
        /// <summary>
        /// 装备时置位 asgardianAegis 标记（被动结算的闸门），并把当前盾牌冲刺设为
        /// ShieldSlamDash.AsgardianAegis —— 冲刺的位移/命中/冷却全部由冲刺系统自持，本饰品不参与
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().asgardianAegis = true;
            player.GetModPlayer<CalamityDemutationPlayer>().shieldSlamDash = ShieldSlamDash.AsgardianAegis;
        }
        /// <summary>
        /// 配方（分版本），两版都以阿斯加德之英勇 + 极乐之庇护 为坯料：
        /// 现代版另加 CosmiliteBar×10 + AscendantSpiritEssence×4，于 CosmicAnvil 合成；
        /// 经典版另加 CosmiliteBar×5 + Phantoplasm×5，于 DraedonsForge 合成。
        /// 两分支各自 TryGetMod + TryFind 串联，缺材料/缺工作台时静默跳过该条配方。
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("CosmiliteBar", out ModItem cosmiliteBar)
                    && calamity.TryFind<ModItem>("AscendantSpiritEssence", out ModItem ascendantSpiritEssence)
                    && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<AsgardsValor>();
                    recipe.AddIngredient<ElysianAegis>();
                    recipe.AddIngredient(cosmiliteBar.Type, 10);
                    recipe.AddIngredient(ascendantSpiritEssence.Type, 4);
                    recipe.AddTile(cosmicAnvil.Type);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModItem>("CosmiliteBar", out ModItem classicCosmiliteBar)
                    && calamity1.TryFind<ModItem>("Phantoplasm", out ModItem phantoplasm)
                    && calamity1.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient<AsgardsValor>();
                    recipe1.AddIngredient<ElysianAegis>();
                    recipe1.AddIngredient(classicCosmiliteBar.Type, 5);
                    recipe1.AddIngredient(phantoplasm.Type, 5);
                    recipe1.AddTile(draedonsForge.Type);
                    recipe1.Register();
                }
            }
        }
    }
}
