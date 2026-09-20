using CalamityDemutation.Content.Projectiles.Melee;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 泰拉巨刃（Terratomere）—— 月后近战巨剑（移植自灾厄大修 0.4.0.1.3 的 TerratomereEcType 重置版）。
    /// 手持挥砍体 TerratomereHoldout（83 帧四段弧线挥砍），挥砍途中发射 3 发泰拉闪电（TerratomereBolts）、
    /// 收招时放出一道大光束剑气（TerratomereBeams）；真近战命中回血并施加冰川状态。
    /// 配方沿用现代版灾厄 1.4.4：原版泰拉刃 + 灾厄夜明锭（UelibloomBar）×18 于秘银砧；经典版退回五剑合成。
    /// 伤害 833 = 大修 EcType 185 × 4.5（本模组 CE 分形膨胀口径，832.5 上取整）。
    /// </summary>
    internal class Terratomere : ModItem
    {
        // ── 灾厄 Terratomere 系列常量（SwingTime 取大修 EcType 的 83，其余同灾厄原版）──
        /// <summary>挥砍周期（帧）</summary>
        public const int SwingTime = 83;
        /// <summary>小刀光生成间隔（帧）</summary>
        public const int SmallSlashCreationRate = 9;
        /// <summary>真近战命中回血量</summary>
        public const int TrueMeleeHitHeal = 4;
        /// <summary>冰川状态持续帧数</summary>
        public const int TrueMeleeGlacialStateTime = 30;
        /// <summary>小刀光伤害倍率</summary>
        public const float SmallSlashDamageFactor = 0.4f;
        /// <summary>爆炸膨胀系数</summary>
        public const float ExplosionExpandFactor = 1.013f;
        /// <summary>拖尾偏移完成度</summary>
        public const float TrailOffsetCompletionRatio = 0.2f;
        /// <summary>主题色 1（灾厄 TerraColor1）</summary>
        public static readonly Color TerraColor1 = new Color(141, 203, 50);
        /// <summary>主题色 2（灾厄 TerraColor2）</summary>
        public static readonly Color TerraColor2 = new Color(83, 163, 136);
        /// <summary>物品基础属性：60×66、伤害 833、21 帧挥砍、击退 7、无贴图且 noMelee，主弹幕为手持挥砍体；月后稀有度 12（青绿）</summary>
        public override void SetDefaults()
        {
            Item.width = 60;
            Item.height = 66;
            Item.damage = 833;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 21;
            Item.useTime = 21;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTurn = true;
            Item.knockBack = 7f;
            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = Item.buyPrice(1, 50, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.shoot = ModContent.ProjectileType<TerratomereHoldout>();
            Item.shootSpeed = 60f;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 12;
        }
        /// <summary>一场挥砍尚未结束时不能再次挥砍</summary>
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
        /// <summary>
        /// 配方分版本：现代版 = 原版泰拉刃 + 灾厄 UelibloomBar×18 @ 秘银砧；
        /// 经典版 = 灾厄五剑（XerocsGreatsword/Floodtide/Hellkite/TemporalFloeSword）+ 泰拉刃（或泰拉刃刃）@ 远古操纵机
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("UelibloomBar", out ModItem uelibloomBar))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.TerraBlade);
                    recipe.AddIngredient(uelibloomBar.Type, 18);
                    recipe.AddTile(TileID.MythrilAnvil);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModItem>("XerocsGreatsword", out ModItem xerocsGreatsword)
                    && calamity1.TryFind<ModItem>("Floodtide", out ModItem floodtide)
                    && calamity1.TryFind<ModItem>("Hellkite", out ModItem hellkite)
                    && calamity1.TryFind<ModItem>("TemporalFloeSword", out ModItem temporalFloeSword))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(xerocsGreatsword.Type);
                    recipe1.AddIngredient(floodtide.Type);
                    recipe1.AddIngredient(hellkite.Type);
                    recipe1.AddIngredient(temporalFloeSword.Type);
                    recipe1.AddIngredient(ItemID.TerraBlade);
                    recipe1.AddTile(TileID.LunarCraftingStation);
                    recipe1.Register();
                    if (calamity1.TryFind<ModItem>("TerraEdge", out ModItem terraEdge))
                    {
                        Recipe recipe2 = CreateRecipe();
                        recipe2.AddIngredient(xerocsGreatsword.Type);
                        recipe2.AddIngredient(floodtide.Type);
                        recipe2.AddIngredient(hellkite.Type);
                        recipe2.AddIngredient(temporalFloeSword.Type);
                        recipe2.AddIngredient(terraEdge.Type);
                        recipe2.AddTile(TileID.LunarCraftingStation);
                        recipe2.Register();
                    }
                }
            }
        }
    }
}
