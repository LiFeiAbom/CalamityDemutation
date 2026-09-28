using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Defense
{
    /// <summary>
    /// 无暇粹魂晶（Purity）—— 灾厄 <b>2.0.3.9</b> 口径（照搬 2.0.3.9 的 <c>Purity</c>，
    /// 源里带 <c>[LegacyName("AstralArcanum")]</c>，本工程不涉及跨模组改名故略去）。
    /// 是宝石系三件的顶件（王冠宝石 → 感染宝石 → 无暇粹魂晶，三者**互斥**），也是「（古）」链的终点：
    /// 需要甘露安瓿（古）+ 感染宝石合成。
    /// <para>
    /// 装备后是一整套「再生 + 减益对抗」：+100 最大生命、继承整条蜂蜜系标记（回血/站桩/减益时长减半扩到整张 debuffList）、
    /// 按缺失生命给更高一档的再生（3~7 HP/s）、**带减益时**按节拍直接回血（可被惩罚机制拖慢）、
    /// 随减益数上涨的动态防御（20+(N−1)×8）、以及对一批灾厄持续伤害减益的**免疫**。
    /// <para>
    /// 与源的三处**用户点名偏离**（2026-09-28，源里这三项都没有）：
    /// ① 基础防御 8 → **12**；② 追加 **12% 伤害减免**；③ 追加**免疫击退**。
    /// </para>
    /// </summary>
    internal class Purity:ModItem
    {
        /// <summary>注册 7 帧纵向动画（源的贴图 50×350 就是 7 帧），并标记为「以灵魂形式动画」</summary>
        public override void SetStaticDefaults()
        {
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(5, 7));
            ItemID.Sets.AnimatesAsSoul[Type] = true;
        }
        /// <summary>物品基础属性：18×44、防御 +12（源为 8，用户点名 +4）、月后稀有度 15（源为 Violet 档）、1 铂 50 金</summary>
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 44;
            Item.defense = 12;
            Item.value = Item.buyPrice(1, 50, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.accessory = true;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15;
        }
        /// <summary>
        /// 装备时：+100 最大生命、12% 伤害减免、免疫击退，置位 <c>purity</c> 与蜂蜜系三个标记
        /// （继承整条（古）链的回血与减半），并按源加光（光辉软泥（古）/ 甘露安瓿（古）都没装备时才加）。
        /// 其中伤害减免与免疫击退是用户点名追加的（源无）
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.statLifeMax2 += 100;
            player.endurance += 0.12f;
            player.noKnockback = true;
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.purity = true;
            modPlayer.alwaysHoneyRegen = true;
            modPlayer.honeyDewHalveDebuffs = true;
            modPlayer.livingDewHalveDebuffs = true;
            if (!(modPlayer.rOoze || modPlayer.aAmpoule) && !hideVisual)
            {
                Lighting.AddLight(player.Center, new Vector3(1.32f, 1.32f, 1.82f));
            }
        }
        /// <summary>
        /// 配方（照源）：甘露安瓿（古）+ 感染宝石 + AuricBar×5 + AscendantSpiritEssence×4 @ 宇宙砧。
        /// 经典版没有宇宙砧（改**嘉登熔炉**）、没有 AuricBar（改 **AuricOre**）、没有 AscendantSpiritEssence（改 **EndothermicEnergy**）
        /// —— 三处替代均按用户 2026-09-28 的指定
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod modern)
                && modern.TryFind<ModItem>("AuricBar", out ModItem auricBar)
                && modern.TryFind<ModItem>("AscendantSpiritEssence", out ModItem ascendantSpiritEssence)
                && modern.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
            {
                CreateRecipe().
                    AddIngredient<AmbrosialAmpoule2>().
                    AddIngredient<InfectedJewel>().
                    AddIngredient(auricBar.Type, 5).
                    AddIngredient(ascendantSpiritEssence.Type, 4).
                    AddTile(cosmicAnvil.Type).
                    Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic)
                && classic.TryFind<ModItem>("AuricOre", out ModItem auricOre)
                && classic.TryFind<ModItem>("EndothermicEnergy", out ModItem endothermicEnergy)
                && classic.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
            {
                CreateRecipe().
                    AddIngredient<AmbrosialAmpoule2>().
                    AddIngredient<InfectedJewel>().
                    AddIngredient(auricOre.Type, 5).
                    AddIngredient(endothermicEnergy.Type, 4).
                    AddTile(draedonsForge.Type).
                    Register();
            }
        }
    }
}
