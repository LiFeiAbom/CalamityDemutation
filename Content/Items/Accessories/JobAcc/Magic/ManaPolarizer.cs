using CalamityDemutation.Content.Projectiles.Healing;
using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Magic
{
    /// <summary>
    /// 魔能谐振仪（Mana Polarizer）—— 移植自灾厄 2.0.3.9 的 <c>Items/Accessories/ManaPolarizer</c>
    /// （源的旧名是 <c>ManaOverloader</c>，故玩家侧标记沿用源的 <c>manaOverloader</c>）。
    /// 法师饰品：魔力上限 +100、魔法伤害 +12%、魔法暴击率 +12%；魔力高于上限一半时生命再生 −2 HP/s；
    /// 手持魔法武器时，魔法弹幕命中敌人会按"伤害 × 比例 × 当前魔力比例"生成
    /// <see cref="ManaPolarizerHealOrb"/> 治疗球（比例随该弹幕已命中次数递减，单次封顶 10 点）。
    /// <para>
    /// 与源的差异（其余照搬 2.0.3.9）：
    /// ① 数值按用户 2026-10-06 点名改写：魔力上限 50 → <b>100</b>、生命再生 −1.5 → <b>−2 HP/s</b>、
    ///    魔法伤害 6% → <b>12%</b>，并新增<b>魔法暴击率 +12%</b>（源没有暴击项）；
    /// ② 来源改为合成（源是史莱姆之神掉落 / 宝藏袋开出），配方与站台由用户指定，见 <see cref="AddRecipes"/>；
    /// ③ 稀有度与价格按用户 2026-10-06 指定改成与虚无箭袋（QuiverofNihility）同档：源的浅红 + 12 金
    ///    → 本工程月后稀有度 <b>12</b>（青绿）+ <b>1 铂金 50 金</b>；
    /// ④ 源的 <c>[LegacyName("ManaOverloader")]</c> 与本工程无关，不保留。
    /// </para>
    /// </summary>
    internal class ManaPolarizer : ModItem
    {
        /// <summary>
        /// 物品基础属性：30×30、价值 1 铂金 50 金、饰品标记，并指定模组自定义的月后稀有度 12 级（青绿）。
        /// 稀有度与价格按用户 2026-10-06 指定与虚无箭袋对齐
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 30;                          // 贴图宽 30 像素
            Item.height = 30;                         // 贴图高 30 像素
            Item.value = Item.buyPrice(1, 50, 0, 0); // 价值 1 铂金 50 金（用户指定：与虚无箭袋一致）
            Item.accessory = true;                    // 标记为饰品，可装备于饰品栏
            // 月后物品：基础稀有度填红，名称颜色由 postMoonLordRarity 统一渲染
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 12;   // 月后自定义稀有度 12 级：青绿（与虚无箭袋一致）
        }
        /// <summary>
        /// 装备时置位 <c>manaOverloader</c> 标记（+50→100 魔力上限在此直接加）；
        /// 魔法增伤/暴击的结算、扣再生、以及魔法命中的吸血分别见
        /// CalamityDemutationPlayer.PostUpdateMiscEffects / UpdateLifeRegen 与 CalamityDemutationGlobalProjectile.OnHitNPC
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().manaOverloader = true;
            player.statManaMax2 += 100;   // 魔力上限 +100（用户 2026-10-06 指定；源为 50）
        }
        /// <summary>
        /// 配方（用户 2026-10-06 指定，源无配方——原版是史莱姆之神掉落）：
        /// 枯萎凝胶 ×140 + 纯净凝胶 ×140 + 死灵质 ×140 + 夜明锭 ×15 + 起源之簇 ×15，在远古操纵机处合成。
        /// 按本工程口径分现代版 / 经典版各注册一条，材料名按各自版本取：
        /// 枯萎凝胶现代版叫 BlightedGel、经典版仍叫旧名 EbonianGel；
        /// 死灵质现代版叫 Necroplasm（1.4.4-release / 2.0.4+），2.0.3.9 叫 Polterplasm、更早/经典版叫 Phantoplasm；
        /// 起源之簇现代版叫 ExodiumCluster、经典版叫 ExodiumClusterOre；
        /// 夜明锭（LunarBar）与远古操纵机（LunarCraftingStation）是原版物品/站台，两版通用。
        /// </summary>
        public override void AddRecipes()
        {
            // 多名字材料查找：同一个材料在不同灾厄版本里改过名，逐个试到命中为止
            static bool TryFindAny(Mod mod, out ModItem item, params string[] names)
            {
                foreach (string name in names)
                {
                    if (mod.TryFind<ModItem>(name, out item))
                    {
                        return true;
                    }
                }
                item = null;
                return false;
            }
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (TryFindAny(calamity, out ModItem blightedGel, "BlightedGel", "EbonianGel")
                    && calamity.TryFind<ModItem>("PurifiedGel", out ModItem purifiedGel)
                    && TryFindAny(calamity, out ModItem necroplasm, "Necroplasm", "Polterplasm", "Phantoplasm")
                    && calamity.TryFind<ModItem>("ExodiumCluster", out ModItem exodiumCluster))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(blightedGel.Type, 140);
                    recipe.AddIngredient(purifiedGel.Type, 140);
                    recipe.AddIngredient(necroplasm.Type, 140);
                    recipe.AddIngredient(ItemID.LunarBar, 15);      // 夜明锭（原版）
                    recipe.AddIngredient(exodiumCluster.Type, 15);
                    recipe.AddTile(TileID.LunarCraftingStation);    // 远古操纵机（原版）
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if (classic.TryFind<ModItem>("EbonianGel", out ModItem ebonianGel)
                    && classic.TryFind<ModItem>("PurifiedGel", out ModItem purifiedGelClassic)
                    && classic.TryFind<ModItem>("Phantoplasm", out ModItem phantoplasm)
                    && classic.TryFind<ModItem>("ExodiumClusterOre", out ModItem exodiumClusterOre))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient(ebonianGel.Type, 140);
                    recipeClassic.AddIngredient(purifiedGelClassic.Type, 140);
                    recipeClassic.AddIngredient(phantoplasm.Type, 140);
                    recipeClassic.AddIngredient(ItemID.LunarBar, 15);
                    recipeClassic.AddIngredient(exodiumClusterOre.Type, 15);
                    recipeClassic.AddTile(TileID.LunarCraftingStation);
                    recipeClassic.Register();
                }
            }
        }
    }
}
