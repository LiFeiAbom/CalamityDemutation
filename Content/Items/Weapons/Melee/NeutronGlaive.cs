using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 中子长戟（NeutronGlaive） - 月后终局近战挥砍武器，由黑物质棒在灾厄砧/锻造台上打造。
    /// 每次挥击都会射出 NeutronGlaiveBeam 弹幕，弹幕消亡时再生成 NeutronExplode 扭曲爆点，
    /// 形成「挥砍→光束→中子爆裂」的联动伤害链。
    /// </summary>
    internal class NeutronGlaive:ModItem
    {
        /// <summary>
        /// 物品外观动画：设置为灵魂类物品，贴图逐帧竖直滚动（6 帧、每 5 tick 换一帧）。
        /// </summary>
        public override void SetStaticDefaults()
        {
            ItemID.Sets.AnimatesAsSoul[Type] = true;                          // 按灵魂类物品处理（浮动/发光表现）
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(5, 6));// 垂直动画：6 帧、每 5 tick 换一帧
        }
        /// <summary>
        /// 物品基础属性：巨型挥砍武器，14 帧出手、必定发射中子光束弹幕，自定义月后稀有度 15。
        /// </summary>
        public override void SetDefaults()
        {
            Item.height = 154;   // 贴图高（像素）
            Item.width = 154;    // 贴图宽（像素）
            Item.damage = 3420; // 855 * 4 = 3420
            Item.DamageType = DamageClass.Melee;   // 归属近战伤害
            Item.useAnimation = Item.useTime = 14; // 使用动画与冷却同为 14 帧（很快）
            Item.useTurn = true;                   // 挥砍时可转向
            Item.useStyle = ItemUseStyleID.Swing;  // 挥砍式使用
            Item.noMelee = true;                   // 无近战挥砍判定（右键为手持蓄力模式）
            Item.knockBack = 7.5f;                 // 击退力
            Item.UseSound = SoundID.Item60;        // 挥砍音效
            Item.autoReuse = true;                 // 长按可自动连续挥砍
            Item.value = Item.buyPrice(13, 53, 75, 0);   // 价值 13 铂金 53 金 75 银
            Item.rare = ItemRarityID.Red;                // 基础稀有度红色
            Item.crit = 8;                               // 额外暴击率 +8%
            Item.shoot = ModContent.ProjectileType<NeutronGlaiveBeam>();  // 挥砍时发射的弹幕
            Item.shootSpeed = 18f;                       // 弹幕初速度
            Item.shootsEveryUse = true;                  // 每次使用都发射（而非仅第一次）
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 16;  // 月后自定义稀有度等级 16
        }
        /// <summary>
        /// 使用条件：左键正常挥砍；右键切换到手持蓄力模式（改音效与无近战/无使用贴图），
        /// 并限制场上只能同时存在一把 NeutronGlaiveHeld
        /// </summary>
        public override bool CanUseItem(Player player)
        {
            Item.noMelee = false;
            Item.noUseGraphic = false;
            Item.UseSound = SoundID.Item60;
            if (player.altFunctionUse == 2)
            {
                Item.noMelee = true;
                Item.noUseGraphic = true;
                Item.UseSound = SoundID.AbigailAttack;
            }
            return player.ownedProjectileCounts[ModContent.ProjectileType<NeutronGlaiveHeld>()] == 0;
        }
        /// <summary>
        /// 允许右键副功能（altFunctionUse == 2）
        /// </summary>
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
        /// <summary>
        /// 发射逻辑：右键（altFunctionUse == 2）生成手持弹幕 NeutronGlaiveHeld 并拦截默认发射；
        /// 左键走基类默认发射 NeutronGlaiveBeam
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<NeutronGlaiveHeld>(), damage, knockback, player.whoAmI);
                return false;
            }
            return base.Shoot(player, source, position, velocity, type, damage, knockback);
        }
        /// <summary>
        /// 近战挥舞特效：仅调用 CDUtil.BetterSwing 修正武器挥舞位置，使巨剑类挥舞更贴合视觉。
        /// </summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
        }
        /// <summary>
        /// 注册配方：兼容灾厄现代版与经典版，两版所需工作台名字一致但是具体对应不同，故分别注册配方。
        /// 两版均消耗 12 个黑物质棒。
        /// </summary>
        public override void AddRecipes()
        {
            // 现代版灾厄：德雷顿熔炉（DraedonsForge）
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<BlackMatterStick>(12);
                    recipe.AddTile(draedonsForge.Type);
                    recipe.Register();
                }
            }
            // 经典版灾厄：德雷顿熔炉（DraedonsForge）
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModTile>("DraedonsForge", out ModTile classicDraedonsForge))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient<BlackMatterStick>(12);
                    recipe1.AddTile(classicDraedonsForge.Type);
                    recipe1.Register();
                }
            }
        }
    }
}
