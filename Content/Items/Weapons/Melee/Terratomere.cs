using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Systems;
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
        /// <summary>
        /// 物品基础属性：60×66、伤害 185（源值；数值膨胀开关开启时面板回调到 370）、
        /// 21 帧挥砍、击退 7、无贴图且 noMelee，主弹幕为手持挥砍体；月后稀有度 12（青绿）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 60;
            Item.height = 66;
            Item.damage = 185;
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
        /// <summary>
        /// 数值膨胀后的面板伤害（用户 2026-10-01 指定：泰拉巨刃 185 → 370）。
        /// </summary>
        private const float InflatedDamage = 370f;
        /// <summary>
        /// 当前生效的面板基础伤害：膨胀开关开启时用 <see cref="InflatedDamage"/>，否则维持 <c>Item.damage</c> 的源值 185。
        /// </summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>
        /// 数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>（运行时读配置，游戏内切换即时生效）。
        /// 手持挥砍体拿的是生成时传入的面板值，小刀光（×0.4）与光束剑气都按 <c>Projectile.damage</c> 派生，全部自动跟随。
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base = BaseDamage;
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
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if (classic.TryFind<ModItem>("XerocsGreatsword", out ModItem xerocsGreatsword)
                    && classic.TryFind<ModItem>("Floodtide", out ModItem floodtide)
                    && classic.TryFind<ModItem>("Hellkite", out ModItem hellkite)
                    && classic.TryFind<ModItem>("TemporalFloeSword", out ModItem temporalFloeSword))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient(xerocsGreatsword.Type);
                    recipeClassic.AddIngredient(floodtide.Type);
                    recipeClassic.AddIngredient(hellkite.Type);
                    recipeClassic.AddIngredient(temporalFloeSword.Type);
                    recipeClassic.AddIngredient(ItemID.TerraBlade);
                    recipeClassic.AddTile(TileID.LunarCraftingStation);
                    recipeClassic.Register();
                    if (classic.TryFind<ModItem>("TerraEdge", out ModItem terraEdge))
                    {
                        recipeClassic = CreateRecipe();
                        recipeClassic.AddIngredient(xerocsGreatsword.Type);
                        recipeClassic.AddIngredient(floodtide.Type);
                        recipeClassic.AddIngredient(hellkite.Type);
                        recipeClassic.AddIngredient(temporalFloeSword.Type);
                        recipeClassic.AddIngredient(terraEdge.Type);
                        recipeClassic.AddTile(TileID.LunarCraftingStation);
                        recipeClassic.Register();
                    }
                }
            }
        }
    }
}
