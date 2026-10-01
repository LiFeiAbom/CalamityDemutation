using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 熵之舞（Entropic Claymore）—— 月球工作台档近战（物品与挥砍行为整体移植自灾厄大修 **0.4.0.3.5** 的
    /// <c>EntropicClaymoreEcType</c> / <c>EntropicClaymoreHeld</c>）。
    /// 每次挥砍把玩家锁进一个 28 帧的挥砍体（<see cref="EntropicClaymoreHeld"/>），
    /// 挥砍途中每 20×攻速 帧朝准心射出一枚熵之飞刃（<see cref="EntropicClaymoreProj"/>，伤害同手持体），
    /// 飞刃随发数张开成扇面、速度逐发递增；命中后自身伤害递减、对蠕虫体节减半。
    /// 配方：熵构体×15 @ 月球工作台（熵构体 MeldConstruct 是灾厄材料，走软依赖）。
    /// </summary>
    internal class EntropicClaymore : ModItem
    {
        /// <summary>
        /// 物品基础属性：130×106、伤害 92（源值；数值膨胀开关开启时面板回调到 113）、
        /// 28 帧使用间隔（挥砍动画 38 帧）、击退 5.25、青名（80金）、
        /// 无贴图且 noMelee，靠挥砍体 <see cref="EntropicClaymoreHeld"/> 出伤
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 130;
            Item.height = 106;
            Item.damage = 92;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 38;
            Item.useTime = 28;
            Item.knockBack = 5.25f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item1;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = Item.buyPrice(0, 80, 0, 0);
            Item.rare = ItemRarityID.Cyan;
            Item.shoot = ModContent.ProjectileType<EntropicClaymoreHeld>();
            Item.shootSpeed = 12f;
        }
        /// <summary>
        /// 数值膨胀后的面板伤害（用户 2026-10-01 指定：熵之舞 92 → 113）。
        /// </summary>
        private const float InflatedDamage = 113f;
        /// <summary>
        /// 当前生效的面板基础伤害：膨胀开关开启时用 <see cref="InflatedDamage"/>，否则维持 <c>Item.damage</c> 的源值 92。
        /// </summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>
        /// 数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>（运行时读配置，游戏内切换即时生效）。
        /// 挥砍体与熵之飞刃取的 <c>Projectile.damage</c> 来自生成时传入的本次面板值，会自动跟随。
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base = BaseDamage;
        }
        /// <summary>配方：熵构体×15 @ 月球工作台（灾厄材料，走软依赖，未加载灾厄时不注册）</summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("MeldConstruct", out ModItem meldConstruct))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(meldConstruct.Type, 15);
                    recipe.AddTile(TileID.LunarCraftingStation);
                    recipe.Register();
                }
            }
        }
    }
}
