using Terraria;
using Terraria.ModLoader;

namespace CalamityDemutation.Utilities
{
    /// <summary>
    /// 现代灾厄材料的经典版等价换算。
    /// </summary>
    internal static class CalamityMaterialEquivalents
    {
        internal const int NightmareFuelPerEssence = 5;
        internal const int NecroplasmPerEssence = 2;
        internal const int EndothermicEnergyPerEssence = 5;
        internal const int DarksunFragmentPerEssence = 5;
        internal const int AuricOrePerBar = 5;
        internal const int DragonSoulFragmentPerBar = 1;

        /// <summary>
        /// 将指定数量的现代化神魂精换算成经典灾厄的完整四味配方材料。
        /// 经典版没有 Necroplasm，使用对应的旧名 Phantoplasm。
        /// </summary>
        internal static bool TryAddClassicAscendantSpiritEssence(Recipe recipe, int essenceCount)
        {
            if (essenceCount <= 0
                || !ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic)
                || !classic.TryFind<ModItem>("NightmareFuel", out ModItem nightmareFuel)
                || !classic.TryFind<ModItem>("Phantoplasm", out ModItem phantoplasm)
                || !classic.TryFind<ModItem>("EndothermicEnergy", out ModItem endothermicEnergy)
                || !classic.TryFind<ModItem>("DarksunFragment", out ModItem darksunFragment))
            {
                return false;
            }

            recipe.AddIngredient(nightmareFuel.Type, NightmareFuelPerEssence * essenceCount);
            recipe.AddIngredient(phantoplasm.Type, NecroplasmPerEssence * essenceCount);
            recipe.AddIngredient(endothermicEnergy.Type, EndothermicEnergyPerEssence * essenceCount);
            recipe.AddIngredient(darksunFragment.Type, DarksunFragmentPerEssence * essenceCount);
            return true;
        }

        /// <summary>
        /// 将指定数量的现代圣金源锭换算成经典灾厄的圣金源矿与龙魂碎片。
        /// 经典龙魂碎片内部名为 HellcasterFragment。
        /// </summary>
        internal static bool TryAddClassicAuricBar(Recipe recipe, int barCount)
        {
            if (barCount <= 0
                || !ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic)
                || !classic.TryFind<ModItem>("AuricOre", out ModItem auricOre)
                || !classic.TryFind<ModItem>("HellcasterFragment", out ModItem dragonSoulFragment))
            {
                return false;
            }

            recipe.AddIngredient(auricOre.Type, AuricOrePerBar * barCount);
            recipe.AddIngredient(dragonSoulFragment.Type, DragonSoulFragmentPerBar * barCount);
            return true;
        }
    }
}
