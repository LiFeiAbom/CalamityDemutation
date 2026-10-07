using CalamityDemutation.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.JobAcc.Rogue
{
    /// <summary>
    /// 纳米技术（Nanotech）—— 顶级盗贼饰品，物品本体口径取灾厄经典版 1.4.2.101 世系
    /// （<c>Items/Accessories/Nanotech.cs</c>：28×32、90 金、月后 20 档）。
    /// 装备时只置位 <c>nanotech</c> 标记，数值与行为统一在别处结算：
    /// ① <c>CalamityDemutationPlayer.PostUpdateMiscEffects</c>：盗贼伤害 +20%、盗贼暴击 +20
    ///    （用户 2026-10-07 点名；经典版源为 +15% / +10%）、盗贼弹速 +15%；手持盗贼武器时额外
    ///    +30 防御与 +10% 伤害减免（照经典版 <c>CalamityPlayer1Point2.cs:917</c> 的口径）；
    /// ② <c>CalamityDemutationGlobalProjectile.AI</c>：盗贼弹幕飞行途中每 30 帧在原地留下一枚纳米刀刃
    ///    （<c>Content/Projectiles/Rogue/Nanotech.cs</c>，伤害 = 弹幕伤害 ×0.15、伤害类型 = 盗贼），
    ///    潜行打击弹幕额外 +20 护甲穿透；
    /// ③ <c>CalamityDemutationGlobalProjectile.OnHitNPC</c>：潜行打击命中（前 3 次）时从画面上方砸下
    ///    6 枚灾厄本体的 <c>NanoFlare</c>。
    /// ②③ 两条机制按用户 2026-10-07 的要求取自 CI 的 <c>NanotechOld</c>（不是 1.4.2 / cal-1.2 的旧写法：
    /// 旧写法是"每 30 次命中"且刀刃射出就能打；CI 是"每 30 帧"且刀刃前 30 帧不能命中、之后才追踪）。
    /// <para>
    /// **刻意不移植**：经典版 tooltip 里的「盗贼武器有概率秒杀普通敌人」——源实现是在
    /// <c>CalamityGlobalNPC1Point2.ModifyHitByProjectile</c> 里靠一张写死的 NPC 类型排除表把
    /// <c>FinalDamage.Base</c> 改成 <c>npc.lifeMax * 5</c>，多模组环境下既不可靠也无从维护，
    /// 用户 2026-10-07 明确要求取消，故效果与文案都不写。
    /// </para>
    /// <para>
    /// 与灾厄本体重名：现代版灾厄 2.2.2 自带一件 <c>Nanotech</c>（46×46、CosmicPurple，配方走
    /// 「盗贼徽章 + 吸血鬼护符 + 电工手套 + 月锭 + 登升者之魂 @ 宇宙砧」的合并流），与本件（经典版
    /// 档位、旧配方）是同名不同物——属本工程"灾厄旧版内容回归"的既定做法，与虚无箭袋同理。
    /// </para>
    /// </summary>
    internal class Nanotech : ModItem
    {
        /// <summary>
        /// 基础属性：28×32、价值 90 金、饰品，并指定月后稀有度 20 档（彩虹，对应经典版的
        /// <c>postMoonLordRarity = 20</c>）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 32;
            Item.value = Item.buyPrice(0, 90, 0, 0);
            Item.accessory = true;
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 20;
        }
        /// <summary>
        /// 装备时置位 <c>nanotech</c> 标记；数值结算、刀刃生成与潜行打击都在
        /// CalamityDemutationPlayer / CalamityDemutationGlobalProjectile 里按标记触发
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().nanotech = true;
        }
        /// <summary>
        /// 配方：照经典版源（火星管道板×250 + 纳米机器人×500 + 死灵质×20 + 梦魇燃料×20 +
        /// 吸热能量×20），现代版把死灵质的旧名 <c>Phantoplasm</c> 换成 <c>Necroplasm</c>、
        /// 站台从嘉登熔炉换成宇宙砧；两版材料对不上时写日志，不静默消失。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("Necroplasm", out ModItem necroplasm) &&
                    calamity.TryFind<ModItem>("NightmareFuel", out ModItem nightmareFuel) &&
                    calamity.TryFind<ModItem>("EndothermicEnergy", out ModItem endothermicEnergy) &&
                    calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.MartianConduitPlating, 250);
                    recipe.AddIngredient(ItemID.Nanites, 500);
                    recipe.AddIngredient(necroplasm.Type, 20);
                    recipe.AddIngredient(nightmareFuel.Type, 20);
                    recipe.AddIngredient(endothermicEnergy.Type, 20);
                    recipe.AddTile(cosmicAnvil.Type);
                    recipe.Register();
                }
                else
                {
                    Mod.Logger.Warn("纳米技术：现代版灾厄里找不到 Necroplasm / NightmareFuel / EndothermicEnergy / CosmicAnvil，本条配方未注册。");
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if (classic.TryFind<ModItem>("Phantoplasm", out ModItem phantoplasm) &&
                    classic.TryFind<ModItem>("NightmareFuel", out ModItem nightmareFuelClassic) &&
                    classic.TryFind<ModItem>("EndothermicEnergy", out ModItem endothermicEnergyClassic) &&
                    classic.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient(ItemID.MartianConduitPlating, 250);
                    recipeClassic.AddIngredient(ItemID.Nanites, 500);
                    recipeClassic.AddIngredient(phantoplasm.Type, 20);
                    recipeClassic.AddIngredient(nightmareFuelClassic.Type, 20);
                    recipeClassic.AddIngredient(endothermicEnergyClassic.Type, 20);
                    recipeClassic.AddTile(draedonsForge.Type);
                    recipeClassic.Register();
                }
                else
                {
                    Mod.Logger.Warn("纳米技术：经典版灾厄里找不到 Phantoplasm / NightmareFuel / EndothermicEnergy / DraedonsForge，本条配方未注册。");
                }
            }
        }
    }
}
