using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Demonshade
{
    /// <summary>
    /// 魔影面甲（DemonshadeHelmSummon） - 恶魔之影套（Demonshade）的召唤职业头部件。
    /// 单件给召唤伤害、仆从栏与哨兵栏，以及鞭子的攻速与攻击范围。
    /// 套装与近战头盔 DemonshadeHelm 同构，职业换成召唤，且增伤幅度不同（+130% 而非 +100%）。
    /// 造型取自灾厄大修的 DemonshadeHelmSummon：面甲单独画在 _Extension 贴图上，
    /// 由 HatExtensionLayer 在原版头部层之后叠加绘制。
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class DemonshadeHelmSummon:ModItem, IExtendedHat
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;          // 贴图宽（像素）
            Item.height = 18;         // 贴图高（像素）
            Item.value = Item.buyPrice(5, 0, 0, 0);  // 价值 5 铂金，与其余职业件一致
            Item.defense = 24;        // 防御 24
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 16;  // 月后稀有度 16 级，名称颜色为品红
        }
        /// <summary>
        /// 附加层贴图路径：面甲画在这张 52×1280（20 帧 × 52×64）的贴图上
        /// </summary>
        public string ExtensionTexture => "CalamityDemutation/Content/Items/Armors/Demonshade/DemonshadeHelmSummon_Extension";
        /// <summary>
        /// 附加层相对头部的偏移。该值与贴图同源（取自灾厄大修 CWRPlayerDraw 的 Summon 分支），
        /// 是画师按这套贴图规格定死的，改动会让面甲错位。
        /// </summary>
        public Vector2 ExtensionSpriteOffset(PlayerDrawSet drawInfo) => new Vector2(-6f, -4f);
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
        /// 套装激活：与近战头盔同构，职业换成召唤，且增伤幅度为 +130%（其余职业为 +100%）。
        /// 必须先加再取——否则红魔与三叉戟会漏掉这一档。
        /// 再置位 demonshadeSetBonus / redDevil / demonshadeClass 标记，补上红魔 buff 并召唤红魔，
        /// 最后每帧把当前伤害同步给场上红魔。
        /// 标记的消费位置见近战头盔 DemonshadeHelm 的同名方法注释。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            // 必须先加再取：player.GetDamage<X>() += 是就地修改玩家身上那份共享加成数据，
            // 同一方法内更早的读取看得到、更晚的读取看不到。
            player.GetDamage<SummonDamageClass>() += 1.3f;  // 召唤伤害 +130%（本职业变体独有，其余职业为 +100%）
            int redDevilDamage = (int)player.GetDamage<SummonDamageClass>().ApplyTo(10000);  // 红魔弹幕伤害：以玩家召唤伤害对 10000 基准换算（已含上方这 +130%）
            player.setBonus = this.GetLocalizedValue("SetBonus");
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.demonshadeSetBonus = true;             // 置位套装总标记
            modPlayer.redDevil = true;                       // 置位红魔标记，供红魔弹幕判断去留
            modPlayer.demonshadeClass = DamageClass.Summon;  // 红魔三叉戟按召唤结算（与红魔本体的召唤定位一致）
            if (player.FindBuffIndex(ModContent.BuffType<Buffs.SummonBuffs.RedDevil>()) == -1)
            {
                player.AddBuff(ModContent.BuffType<Buffs.SummonBuffs.RedDevil>(), 3600, true);  // 无红魔 buff 时补上（3600 帧 = 60 秒）
            }
            int devilType = ModContent.ProjectileType<Projectiles.Summon.RedDevil>();
            if (player.ownedProjectileCounts[devilType] < 1)
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
        /// 单件装备加成：召唤伤害、仆从/哨兵栏，以及鞭子的攻速与攻击范围；并置位职业标记供红魔三叉戟取值。
        /// 鞭子走 SummonMeleeSpeed 攻速类与 whipRangeMultiplier（本工程时滞诅咒系列已有同样写法）。
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().demonshadeClass = DamageClass.Summon;  // 置位职业标记：本件为召唤变体
            player.GetDamage<SummonDamageClass>() += 0.8f;                            // 召唤伤害 +80%
            player.maxMinions += 10;                                                  // 仆从栏上限 +10
            player.maxTurrets += 5;                                                   // 哨兵栏上限 +5
            player.GetAttackSpeed<SummonMeleeSpeedDamageClass>() += 0.45f;            // 鞭子攻击速度 +45%
            player.whipRangeMultiplier += 0.45f;                                      // 鞭子攻击范围 +45%
        }
        /// <summary>
        /// 注册配方：现代版灾厄（CalamityMod）与经典预发布版灾厄（CalamityModClassicPreTrailer）
        /// 材料与近战头盔一致（ShadowspecBar×40），但对应各自的暗影合金锭与德雷顿熔炉，故分别注册。
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
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModItem>("ShadowspecBar", out ModItem classicShadowspecBar) && calamity1.TryFind<ModTile>("DraedonsForge", out ModTile classicDraedonsForge))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(classicShadowspecBar.Type, 40);  // 经典版灾厄：ShadowspecBar×40
                    recipe1.AddTile(classicDraedonsForge.Type);            // 经典版灾厄：德雷顿熔炉
                    recipe1.Register();
                }
            }
        }
    }
}
