using CalamityDemutation.Content.Items.Accessories.Movement;
using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Summon
{
    /// <summary>
    /// 诅咒凝滞腰带（Statis' Belt of Curses）- 顶级**盗贼 / 召唤双职业**饰品（数值结算见
    /// <c>CalamityDemutationPlayer.PostUpdateMiscEffects</c> 的 statisBeltOfCurses 块）。
    /// 盗贼侧：盗贼伤害 +20%、盗贼暴击 +20%（现代版加灾厄的 RogueDamageClass，经典版经 CDUtil
    /// 反射写它的自定义投掷字段）；召唤侧：召唤伤害 +20%、仆从上限 +4、鞭范围/鞭速各 +20%，
    /// 仆从命中附带暗影焰与"哭泣"（TemporalSadness）；另附带自动跳跃、跳跃速度、额外坠落速度、
    /// 闪避、冲刺与尖刺靴等机动性效果。
    /// 凝滞系列最终形态：凝滞祝福 → 凝滞诅咒 → 本件，由凝滞诅咒进阶而来。
    /// <para>
    /// 经典版 tooltip 里的「仆从攻击有概率秒杀普通敌人」**刻意不移植**：源实现（经典版
    /// <c>CalamityGlobalNPC.OnHitByProjectile</c>）靠一张写死的 NPC 类型排除表 + "场上无任何 Boss"
    /// 判据把弹幕伤害改成 <c>npc.lifeMax * 3</c>，多模组环境下既不可靠也无从维护，故效果与文案都不写。
    /// </para>
    /// </summary>
    internal class StatisBeltOfCurses:ModItem
    {
        /// <summary>
        /// 物品基础属性：贴图尺寸、价值、饰品标记，并指定模组自定义的月后稀有度等级
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 28;                         // 贴图宽 28 像素
            Item.height = 32;                        // 贴图高 32 像素
            Item.value = Item.buyPrice(0, 90, 0, 0); // 价值 90 金
            Item.accessory = true;                   // 标记为饰品，可装备于饰品栏
            // 月后物品：稀有度颜色交由全局物品统一渲染
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;
        }
        /// <summary>
        /// 装备时置位 statisBeltOfCurses 标记，数值结算、debuff 施加与机动性效果在 CalamityDemutationPlayer 中统一完成
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 装备时仅置位标记，数值结算统一在 CalamityDemutationPlayer 中完成
            player.GetModPlayer<CalamityDemutationPlayer>().statisBeltOfCurses = true;
        }
        /// <summary>
        /// 配方：以凝滞诅咒为核心材料，兼容灾厄现代版与经典版（两版材料名/合成站不同），分别注册配方
        /// </summary>
        public override void AddRecipes()
        {
            // 兼容灾厄现代版与经典版：两者材料名/合成站不同，需分别注册配方
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("Necroplasm", out ModItem necroplasm1) && calamity.TryFind<ModItem>("NightmareFuel", out ModItem nightmareFuel1) && calamity.TryFind<ModItem>("EndothermicEnergy", out ModItem endothermicEnergy1) && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil1))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<StatisCurse>();
                    recipe.AddIngredient<StatisNinjaBelt>();
                    recipe.AddIngredient(necroplasm1.Type, 20);
                    recipe.AddIngredient(nightmareFuel1.Type, 20);
                    recipe.AddIngredient(endothermicEnergy1.Type, 20);
                    recipe.AddTile(cosmicAnvil1.Type);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if (classic.TryFind<ModItem>("Phantoplasm", out ModItem phantoplasm1) && classic.TryFind<ModItem>("NightmareFuel", out ModItem nightmareFuel2) && classic.TryFind<ModItem>("EndothermicEnergy", out ModItem endothermicEnergy2) && classic.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge1))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient<StatisCurse>();
                    recipeClassic.AddIngredient<StatisNinjaBelt>();
                    recipeClassic.AddIngredient(phantoplasm1.Type, 20);
                    recipeClassic.AddIngredient(nightmareFuel2.Type, 20);
                    recipeClassic.AddIngredient(endothermicEnergy2.Type, 20);
                    recipeClassic.AddTile(draedonsForge1.Type);
                    recipeClassic.Register();
                }
            }
        }
    }
}
