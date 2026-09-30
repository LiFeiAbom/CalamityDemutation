using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 天罚（Nemesis，移植自 CalamityEntropy 的 <c>Nemesis/Nemesis.cs</c>）——
    /// 本体既不显示也不判定（<c>noMelee</c> + <c>noUseGraphic</c>），招式全交给手持弹幕 <see cref="NemesisHeld"/>。
    /// 360 伤害、18 帧、击退 5.5、射速 18，允许右键。
    /// 每次左键按 <b>0~6 循环</b>计数：满 6 次后的第 7 次由 <c>ai[0] = 1</c> 触发放天罚；
    /// 右键（<c>altFunctionUse == 2</c>）一律走 <c>ai[0] = 2</c> 的蓄力式。
    /// <para>
    /// 与 CE 的差异：① 去掉 <c>IDevItem</c> 开发者物品标记（本工程无此系统）；
    /// ② <c>Item.SetKnifeHeld&lt;NemesisHeld&gt;()</c> 展开为本工程的等价写法
    /// （<c>noMelee</c> + <c>noUseGraphic</c> + <c>shoot</c> 指向手持弹幕）；
    /// ③ 配方按本工程口径重做（CE 原配方含捐赠者专属的 <c>FlowingLight</c> 翅膀，普通玩家拿不到）；
    /// ④ 稀有度：CE 只有基础红，本工程另加 <c>postMoonLordRarity = 16</c>（品红，与中子之刃 / 最终分形同档）。
    /// </para>
    /// </summary>
    internal class Nemesis:ModItem
    {
            public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>
        /// 物品基础属性：154×154、伤害 360、18 帧使用与挥舞、击退 5.5、红名、3 铂 20 金；
        /// 不显示本体、由手持弹幕 <see cref="NemesisHeld"/> 负责挥舞与判定；允许反复右键
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 154;
            Item.height = 154;
            Item.damage = 360;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 18;
            Item.useTime = 18;
            Item.scale = 1f;
            Item.useTurn = true;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.knockBack = 5.5f;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(platinum: 3, gold: 20);
            Item.rare = ItemRarityID.Red;                   // 基础稀有度红，真正的名称颜色由 postMoonLordRarity 覆盖
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 16;  // 月后稀有度 16：与中子之刃 / 最终分形同档（品红）
            Item.shoot = ModContent.ProjectileType<NemesisHeld>();
            Item.shootSpeed = 18f;
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }
        public override bool AltFunctionUse(Player player) => true;
        /// <summary>
        /// 出手：按 0~6 循环推进 <see cref="CalamityDemutationPlayer.nemesisFireIndex"/>，满 6 后本次传 <c>ai[0] = 1</c>（天罚）；
        /// 右键则覆盖成 <c>ai[0] = 2</c>（蓄力）。生成的挥舞体由 <see cref="NemesisHeld"/> 分派三种招式。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position
            , Vector2 velocity, int type, int damage, float knockback)
        {
                int newLevel = 0;
                // 计数存在玩家身上（源为 ModItem 实例字段，联机下队友的挥砍会替你推进天罚计数）
                CalamityDemutationPlayer mp = player.GetModPlayer<CalamityDemutationPlayer>();
                if (++mp.nemesisFireIndex > 6)
                {
                    newLevel = 1;
                    mp.nemesisFireIndex = 0;
                }
            if (player.altFunctionUse == 2)
            {
                newLevel = 2;
            }
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, newLevel);
            return false;
        }
        /// <summary>
        /// 配方（分版本）：焚灭天惩 + 星河之刃 + 魔影锭×10，站在灾厄的月后站台合成——
        /// 现代版用宇宙砧（<c>CosmicAnvil</c>）、经典版用嘉登熔炉（<c>DraedonsForge</c>）。
        /// CE 原配方里的 <c>FlowingLight</c>（捐赠者翅膀）、<c>FadingRunestone</c> 与 <c>VoidWell</c> 站台均不适用，故整体改按本工程口径。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("ShadowspecBar", out ModItem shadowspecBar)
                    && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    CreateRecipe()
                        .AddIngredient<TheBurningSky>()                 // 焚灭天惩
                        .AddIngredient<GalactusBlade>()                 // 星河之刃
                        .AddIngredient(shadowspecBar.Type, 10)          // 现代版灾厄：魔影锭×10
                        .AddTile(cosmicAnvil.Type)                      // 现代版灾厄：宇宙砧
                        .Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if (classic.TryFind<ModItem>("ShadowspecBar", out ModItem classicShadowspecBar)
                    && classic.TryFind<ModTile>("DraedonsForge", out ModTile classicDraedonsForge))
                {
                    CreateRecipe()
                        .AddIngredient<TheBurningSky>()                 // 焚灭天惩
                        .AddIngredient<GalactusBlade>()                 // 星河之刃
                        .AddIngredient(classicShadowspecBar.Type, 10)   // 经典版灾厄：魔影锭×10
                        .AddTile(classicDraedonsForge.Type)             // 经典版灾厄：嘉登熔炉
                        .Register();
                }
            }
        }
    }
}
