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
    /// 配方：熵构体×15 @ 远古操纵机（灾厄材料，走软依赖）。熵构体 <c>MeldConstruct</c> 在 2.2.x 被本体
    /// 删除，故现代版 2.2.x 起按它原本的合成（<c>MeldBlob</c>×6 + <c>StarblightSoot</c>×3 产 3 个）
    /// 把 15 个等价展开为 <c>MeldBlob</c>×30 + <c>StarblightSoot</c>×15（见 <see cref="AddRecipes"/>）。
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
        /// <summary>
        /// 配方：熵构体×15 @ 远古操纵机（灾厄材料，走软依赖，未加载灾厄时不注册）。
        /// <para>
        /// 2.0.x 有熵构体 <c>MeldConstruct</c>（2.0.4 里 = <c>MeldBlob</c>×6 + <c>StarblightSoot</c>×3
        /// @ 远古操纵机，每次产 3 个），照源直接用；2.2.x 起本体把它删了，照 2.0.4 硬写
        /// <c>TryFind("MeldConstruct")</c> 只会返回 false 把整条配方静默吃掉（2026-10-07 已踩过），
        /// 故 2.2.x 走"按原始合成把 15 个等价展开"的分支：5 次合成 = <c>MeldBlob</c>×30 +
        /// <c>StarblightSoot</c>×15，成本与源配方一致、站台不变（这两样 2.0.x / 2.2.x 都在）。
        /// 注：本体同名武器在 2.2.x 改用 <c>MeldBlob</c>×18 直接合成，本工程不跟这条（会砍掉原成本）。
        /// </para>
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 2.0.x：照源配方，直接用熵构体 ×15
                if (calamity.TryFind<ModItem>("MeldConstruct", out ModItem meldConstruct))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(meldConstruct.Type, 15);
                    recipe.AddTile(TileID.LunarCraftingStation);
                    recipe.Register();
                }
                // 2.2.x：熵构体已被删除，按它的原始合成展开（15 个 = 5 次 × (MeldBlob×6 + StarblightSoot×3)）
                else if (calamity.TryFind<ModItem>("MeldBlob", out ModItem meldBlob) &&
                    calamity.TryFind<ModItem>("StarblightSoot", out ModItem starblightSoot))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(meldBlob.Type, 30);
                    recipe.AddIngredient(starblightSoot.Type, 15);
                    recipe.AddTile(TileID.LunarCraftingStation);
                    recipe.Register();
                }
                else
                {
                    Mod.Logger.Warn("熵之舞：现代版灾厄里找不到 MeldConstruct，也找不到 MeldBlob + StarblightSoot，本条配方未注册。");
                }
            }
        }
    }
}
