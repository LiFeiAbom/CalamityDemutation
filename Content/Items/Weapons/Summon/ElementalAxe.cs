using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Content.Projectiles.Summon;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Summon
{
    /// <summary>
    /// 元素之斧（Elemental Axe）—— 「归虚之灵」链的**中间件**，
    /// 链下位是苍华之庭，链上位是归虚之灵。
    /// 老规矩取灾厄 2.0.3.9：36×36、伤害 **57**、魔力 10、使用/动画 **15 帧**、击退 5、
    /// **紫档**、价值 **1 铂金 10 金**（`Rarity11BuyPrice`）、音 `SoundID.Item44`、`autoReuse`、弹速 10，
    /// 在鼠标处召唤一把会冲锋的彩虹元素斧（<see cref="ElementalAxeMinion"/>）。
    /// </summary>
    /// <remarks>
    /// 为什么要自持：本件在现代版里**已被删除**（连它自己的弹幕一起），只剩经典世系还有同名件，
    /// 所以按"旧版回归"的口径自持一份 2.0.3.9 版，供归虚之灵当材料。
    /// <para>
    /// 配方（照源 2.0.3.9）：泰拉棱镜 `ItemID.EmpressBlade` + 苍华之庭 + 夜明锭 `ItemID.LunarBar`×5
    /// + 生命合金 `LifeAlloy`×5 + 银河奇点 `GalacticaSingularity`×5 @ 远古操纵机。
    /// **经典分支（`CalamityModClassicPreTrailer`）里没有生命合金**，按用户 2026-10-08 的口径
    /// **直接去掉那一味**，其余四味照旧、同站台。
    /// </para>
    /// </remarks>
    internal class ElementalAxe:ModItem
    {
        /// <summary>研究解锁一份（源 2.0.3.9 由 tML 自动按稀有度决定，这里显式给 1）</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>基础属性：照源 2.0.3.9（36×36、伤害 57、击退 5、使用 15 帧、紫档、1 铂金 10 金）</summary>
        public override void SetDefaults()
        {
            Item.width = 36;
            Item.height = 36;
            Item.damage = 57;
            Item.DamageType = DamageClass.Summon;
            Item.mana = 10;
            Item.useTime = Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.knockBack = 5f;
            Item.value = Item.buyPrice(1, 10, 0, 0);   // 源用 Rarity11BuyPrice = buyPrice(1, 10, 0, 0)
            Item.rare = ItemRarityID.Purple;
            Item.UseSound = SoundID.Item44;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<ElementalAxeMinion>();
            Item.shootSpeed = 10f;
        }
        /// <summary>
        /// 在鼠标处召唤斧头（照源：左右键分支里只有左键生效），
        /// 并把面板伤害写进仆从的 <c>originalDamage</c>（召唤物伤害按它回溯计算）。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse != 2)
            {
                position = Main.MouseWorld;
                velocity.X = 0;
                velocity.Y = 0;
                int p = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
                if (Main.projectile.IndexInRange(p))
                    Main.projectile[p].originalDamage = Item.damage;
            }
            return false;
        }
        /// <summary>
        /// 配方（照源 2.0.3.9）：泰拉棱镜 + 苍华之庭 + 夜明锭×5 + 生命合金×5 + 银河奇点×5 @ 远古操纵机。
        /// 经典分支按用户 2026-10-08 的口径去掉生命合金那一味（经典版没有这件），其余照旧；缺件即退化成单分支。
        /// </summary>
        public override void AddRecipes()
        {
            // 现代分支：照源 2.0.3.9 五味全写
            bool modern = false;
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) &&
                calamity.TryFind<ModItem>("LifeAlloy", out ModItem lifeAlloy))
            {
                CreateRecipe().
                    AddIngredient(ItemID.EmpressBlade).
                    AddIngredient<PlantationStaff>().
                    AddIngredient(ItemID.LunarBar, 5).
                    AddIngredient(lifeAlloy.Type, 5).
                    AddIngredient<GalacticaSingularity>(5).
                    AddTile(TileID.LunarCraftingStation).
                    Register();
                modern = true;
            }
            // 经典分支：去掉生命合金，只留泰拉棱镜 + 苍华之庭 + 夜明锭×5 + 银河奇点×5（用户 2026-10-08 口径）
            bool classic = false;
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod _))
            {
                CreateRecipe().
                    AddIngredient(ItemID.EmpressBlade).
                    AddIngredient<PlantationStaff>().
                    AddIngredient(ItemID.LunarBar, 5).
                    AddIngredient<GalacticaSingularity>(5).
                    AddTile(TileID.LunarCraftingStation).
                    Register();
                classic = true;
            }
            if (!modern && !classic)
                Mod.Logger.Warn("元素之斧：两版灾厄都找不到 生命合金，配方未注册。");
        }
    }
}
