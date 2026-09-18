using CalamityDemutation.Content.Projectiles.Melee;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 制裁大剑（Greatsword of Judgement） - 月后近战巨剑（移植自灾厄大修 0.4.0.1.3 的 GreatswordofJudgementEcType 重置版）。
    /// 与灾厄原版的区别只有三处：弹幕由"白色追踪球 JudgementProj"换成"审判光束 JudgementBeam"、
    /// 弹速 10 → 15、售价档改用 Rarity10（本工程 postMoonLordRarity = 10，名称保持红色，与原版观感一致）。
    /// </summary>
    internal class GreatswordofJudgement : ModItem
    {
        /// <summary>
        /// 物品基础属性：78×78、伤害 40、18 帧挥砍、击退 7、自动挥舞且可转向，
        /// 主弹幕为审判光束（弹速 15）；shootsEveryUse 使每次使用都发弹，而非每轮挥砍只发一次
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 78;                              // 贴图宽（像素）
            Item.height = 78;                             // 贴图高（像素）
            Item.damage = 40;                             // 基础伤害
            Item.DamageType = DamageClass.Melee;          // 近战伤害
            Item.useAnimation = 18;                       // 挥砍动画时长（帧）
            Item.useTime = 18;                            // 使用冷却（帧）
            Item.useStyle = ItemUseStyleID.Swing;         // 使用姿势：挥砍
            Item.useTurn = true;                          // 挥砍时可转向
            Item.knockBack = 7f;                          // 击退
            Item.UseSound = SoundID.Item1;                // 使用音效（原版挥砍）
            Item.autoReuse = true;                        // 自动挥舞
            Item.value = Item.buyPrice(1, 0, 0, 0);       // 价值：对齐灾厄 Rarity10BuyPrice（1 铂金买入 / 20 金卖出）
            Item.rare = ItemRarityID.Red;                 // 基础稀有度红色（postMoonLordRarity 未覆盖 10 档，故名称保持红色）
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 10;   // 月后稀有度 10 级（对应灾厄 Rarity10）
            Item.shoot = ModContent.ProjectileType<JudgementBeam>();   // 主弹幕：审判光束
            Item.shootSpeed = 15f;                        // 弹速
            Item.shootsEveryUse = true;                   // 每次使用都触发 Shoot（与工程内方舟系剑同款口径）
        }
        /// <summary>
        /// 配方：夜明锭×7 于远古操纵机（沿用灾厄原配方，不涉及灾厄材料）。
        /// 与工程既有口径一致——仅在加载了某一版灾厄时注册，避免同时装两版时重复注册。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) || ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.LunarBar, 7);
                recipe.AddTile(TileID.LunarCraftingStation);
                recipe.Register();
            }
        }
    }
}
