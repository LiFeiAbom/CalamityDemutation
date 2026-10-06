using System.Collections.Generic;
using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using CalamityDemutation.Systems.Graphic;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Demonshade
{
    /// <summary>
    /// 魔影面罩（DemonshadeHelmRogue，英文名 Demonshade Helm Rogue） - 恶魔之影套（Demonshade）的**盗贼**职业头部件。
    /// 源 = 灾厄大修（CWR）0.4.0.1.3 的 <c>Content/Items/Armor/DemonshadeExter/DemonshadeHelmRogue</c>：
    /// 魔影套的职业头盔变体本来就在工程记忆第 1 节的「保留的 CWR 重制件」清单里（Magic/Ranged/Summon 已移植），
    /// 本件是那批的第四颗、也是最后一颗；注意**该件在 CWR 里被 <c>IsLoadingEnabled => false</c> 停用**（未完工件），
    /// 本工程按用户 2026-10-06 的要求补全并落地。
    /// 与近战头（<see cref="DemonshadeHelm"/>）同构，只把职业换成盗贼：
    /// 单件的伤害 / 暴击数值与近战头**完全一致**（用户 2026-10-06 指定），套装同样是 +100% 伤害；
    /// **不给攻速加成**——用户同日明确要求删去盗贼攻速（近战头自身仍有 +30% 攻速，与本件无关）；
    /// 套装另外做两件盗贼专属的事——把潜行上限抬到 **200**（源写 <c>rogueStealthMax += 2f</c>，
    /// 内部值 1f = 显示 100 点）并把玩家标成「算作盗贼甲」。潜行的落地见
    /// <see cref="CalamityDemutation.Utilities.CDUtil.GrantRogueStealth"/>：
    /// 现代版灾厄走官方 <c>Mod.Call</c>（AddMaxStealth / SetWearingRogueArmor），经典版没有对应 Call、走反射。
    /// 另实现 IExtendedHat：头盔造型的附加层画在 _Extension 贴图上，由 HatExtensionLayer 叠加绘制。
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class DemonshadeHelmRogue:ModItem, IExtendedHat
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;          // 贴图宽（像素）
            Item.height = 18;         // 贴图高（像素）
            Item.value = Item.buyPrice(5, 0, 0, 0);  // 价值 5 铂金，与其余职业件一致
            Item.defense = 35;        // 防御 35（照 CWR 源；近战头 55 / 远程 43 / 法师 37 / 召唤 24 是本工程各自的口径）
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 16;  // 月后稀有度 16 级，名称颜色为品红
        }
        /// <summary>
        /// 附加层贴图路径：造型附加层画在这张 48×1280（20 帧 × 48×64）的贴图上
        /// </summary>
        public string ExtensionTexture => "CalamityDemutation/Content/Items/Armors/Demonshade/DemonshadeHelmRogue_Extension";
        /// <summary>
        /// 附加层相对头部的偏移。该值与贴图同源（取自灾厄大修 <c>CWRPlayerDraw</c> 的 Rogue 分支：
        /// <c>headDrawPosition += new Vector2(-4, -14)</c>），是画师按这套贴图规格定死的，改动会让附加层错位。
        /// </summary>
        public Vector2 ExtensionSpriteOffset(PlayerDrawSet drawInfo) => new Vector2(-4f, -14f);
        /// <summary>
        /// 判定是否集齐恶魔之影套三件（胸甲 + 护腿）
        /// </summary>
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<DemonshadeBreastplate>() && legs.type == ModContent.ItemType<DemonshadeGreaves>();
        }
        /// <summary>
        /// 套装激活时的视觉表现：绘制残影与外描边
        /// </summary>
        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadow = true;
            player.armorEffectDrawOutlines = true;
        }
        /// <summary>
        /// 套装激活：与近战头盔同构，职业换成盗贼，+100% 伤害；
        /// 再置位 demonshadeSetBonus / redDevil / demonshadeClass / demonshadeRogue 标记，
        /// 补上红魔 buff 并召唤红魔，最后每帧把当前伤害同步给场上红魔。
        /// 必须先加再取——否则红魔与三叉戟会漏掉这一档。标记的消费位置见近战头盔的同名方法注释；
        /// demonshadeRogue 由 CalamityDemutationPlayer.PostUpdateEquips 消费（调用潜行桥）。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            // 必须先加再取：player.GetDamage<X>() += 是就地修改玩家身上那份共享加成数据，
            // 同一方法内更早的读取看得到、更晚的读取看不到。
            DamageClass rogue = CDUtil.GetRogueDamageClass();  // 现代版 = CalamityMod/RogueDamageClass；拿不到才退到 tML 的 Throwing
            player.GetDamage(rogue) += 1f;                     // 盗贼伤害 +100%（与近战头的套装档一致）
            CDUtil.AddClassicThrowingStats(player, 1f, 0);     // 经典版没有盗贼 DamageClass，套装这一档也写进它的自定义投掷字段
            int redDevilDamage = CDUtil.GetRogueScaledDamage(player, 10000f);  // 红魔弹幕伤害：以玩家盗贼面板对 10000 基准换算（已含上方这 +100%）
            player.setBonus = this.GetLocalizedValue("SetBonus");
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.demonshadeSetBonus = true;              // 置位套装总标记
            modPlayer.redDevil = true;                        // 置位红魔标记，供红魔弹幕判断去留
            modPlayer.demonshadeClass = rogue;                // 红魔三叉戟按盗贼结算
            modPlayer.demonshadeRogue = true;                 // 置位盗贼标记：PostUpdateEquips 据此给灾厄侧补潜行
            if (player.FindBuffIndex(ModContent.BuffType<Buffs.SummonBuffs.RedDevil>()) == -1)
            {
                player.AddBuff(ModContent.BuffType<Buffs.SummonBuffs.RedDevil>(), 3600, true);  // 无红魔 buff 时补上（3600 帧 = 60 秒）
            }
            int devilType = ModContent.ProjectileType<Projectiles.Summon.RedDevil>();
            // 召唤只在主人本机做：UpdateArmorSet 对每名玩家、每一端都会跑（Player.Update → UpdateArmorSets），
            // 少了这层判定会让客户端替别的玩家生成一只 owner 记成本机玩家的红魔，服务端更会以 255 当 owner。
            if (player.whoAmI == Main.myPlayer && player.ownedProjectileCounts[devilType] < 1)
            {
                Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center.X, player.Center.Y, 0f, -1f, devilType, redDevilDamage, 0f, Main.myPlayer, 0f, 0f);  // 场上无红魔时召唤一只
            }
            // Projectile.damage 在生成那一刻就冻结、此后不随玩家属性变化，而红魔召唤后常驻不重召，
            // 导致换装备或切换职业变体后伤害会陈旧，故每帧把当前算得的伤害同步过去。
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile devil = Main.projectile[i];
                if (devil.active && devil.type == devilType && devil.owner == player.whoAmI)
                {
                    devil.damage = redDevilDamage;
                }
            }
        }
        /// <summary>
        /// 单件装备加成：盗贼（投掷）伤害与暴击——两项数值与近战头一致（用户 2026-10-06 指定，
        /// 并明确要求**不加攻速**）；
        /// 并置位职业标记供红魔三叉戟取值。
        /// 说明：这里加的是**现代版灾厄的真·盗贼类**（CalamityMod/RogueDamageClass，经
        /// <see cref="CalamityDemutation.Utilities.CDUtil.GetRogueDamageClass"/> 按 ModType 全名取，无需反射），
        /// 只有拿不到时才退到 tML 的 Throwing——原因见该方法的注释（继承是"子类吃父类"，
        /// 给 Throwing 加成能作用到盗贼武器，但 DamageType 挂 Throwing 的弹幕吃不到玩家"盗贼专属"的加成）。
        /// 经典版**没有盗贼 DamageClass**（它的盗贼数值是 CalamityCustomThrowingDamagePlayer 的自定义字段），
        /// 故另走 <see cref="CalamityDemutation.Utilities.CDUtil.AddClassicThrowingStats"/> 的反射桥。
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            DamageClass rogue = CDUtil.GetRogueDamageClass();  // 现代版 = CalamityMod/RogueDamageClass；拿不到才退到 tML 的 Throwing
            player.GetModPlayer<CalamityDemutationPlayer>().demonshadeClass = rogue;  // 置位职业标记：本件为盗贼变体
            int critAdd = ConfigSystem.StatInflationEnabled ? LegacyCritChance : 25;  // 盗贼暴击率：常态 +25% / 膨胀 +50%（与近战头一致）
            player.GetDamage(rogue) += 0.5f;        // 盗贼伤害 +50%（与近战头一致）
            player.GetCritChance(rogue) += critAdd; // 同上
            CDUtil.AddClassicThrowingStats(player, 0.5f, critAdd);  // 经典版没有盗贼 DamageClass，改走它的自定义投掷字段（反射）
        }
        /// <summary>数值膨胀开关开启时恢复的旧版盗贼暴击率（沿用近战头 2026-09-27「50 → 25」那次的旧值）</summary>
        private const int LegacyCritChance = 50;
        /// <summary>
        /// 数值膨胀开启时把 tooltip 正文换成膨胀文案（本地化键 Items.DemonshadeHelmRogue.TooltipInflated），
        /// 盗贼暴击率由 25% 显示为 50%。
        /// </summary>
        public override void ModifyTooltips(List<TooltipLine> tooltips) => tooltips.ApplyInflatedTooltip(this);
        /// <summary>
        /// 注册配方：与其余魔影头一致（ShadowspecBar×40 @ 德雷顿熔炉）；
        /// CWR 源写的是 ×12，本工程按既有四颗魔影头的统一口径取 ×40。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("ShadowspecBar", out ModItem shadowspecBar) && calamity.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(shadowspecBar.Type, 40);  // 现代版灾厄：ShadowspecBar×40
                    recipe.AddTile(draedonsForge.Type);            // 现代版灾厄：德雷顿熔炉
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if (classic.TryFind<ModItem>("ShadowspecBar", out ModItem classicShadowspecBar) && classic.TryFind<ModTile>("DraedonsForge", out ModTile classicDraedonsForge))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient(classicShadowspecBar.Type, 40);  // 经典版灾厄：ShadowspecBar×40
                    recipeClassic.AddTile(classicDraedonsForge.Type);            // 经典版灾厄：德雷顿熔炉
                    recipeClassic.Register();
                }
            }
        }
    }
}
