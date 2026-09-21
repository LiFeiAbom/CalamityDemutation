using CalamityDemutation.Players;
using CalamityDemutation.Systems.Graphic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Armors.Demonshade
{
    /// <summary>
    /// 魔影兜帽（DemonshadeHelmMagic） - 恶魔之影套（Demonshade）的法师职业头部件。
    /// 单件给法术伤害与暴击、最大法力，并把法力消耗减半。
    /// 套装与近战头盔 DemonshadeHelm 逐行同构，只把职业从近战换成法师——连带红魔及其三叉戟的伤害职业。
    /// 造型取自灾厄大修的 DemonshadeHelmMagic：兜帽单独画在 _Extension 贴图上，
    /// 由 HatExtensionLayer 在原版头部层之后叠加绘制。
    /// </summary>
    [AutoloadEquip(EquipType.Head)]
    internal class DemonshadeHelmMagic:ModItem, IExtendedHat
    {
        /// <summary>
        /// 物品基础属性：尺寸、价值、防御与月后自定义稀有度
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 18;          // 贴图宽（像素）
            Item.height = 18;         // 贴图高（像素）
            Item.value = Item.buyPrice(5, 0, 0, 0);  // 价值 5 铂金，与其余职业件一致
            Item.defense = 37;        // 防御 37
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 16;  // 月后稀有度 16 级，名称颜色为品红
        }
        /// <summary>
        /// 附加层贴图路径：兜帽画在这张 48×1280（20 帧 × 48×64）的贴图上
        /// </summary>
        public string ExtensionTexture => "CalamityDemutation/Content/Items/Armors/Demonshade/DemonshadeHelmMagic_Extension";
        /// <summary>
        /// 附加层相对头部的偏移。该值与贴图同源（取自灾厄大修 CWRPlayerDraw 的 Magic 分支），
        /// 是画师按这套贴图规格定死的，改动会让兜帽错位。
        /// </summary>
        public Vector2 ExtensionSpriteOffset(PlayerDrawSet drawInfo) => new Vector2(-4f, -16f);
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
        /// 套装激活：与近战头盔逐行同构，仅职业换成法师。
        /// 先叠加 +100% 法术伤害（必须早于红魔伤害取值，否则红魔与三叉戟会漏掉这一档），
        /// 再置位 demonshadeSetBonus / redDevil / demonshadeClass 标记，补上红魔 buff 并召唤红魔，
        /// 最后每帧把当前伤害同步给场上红魔。
        /// 标记的消费位置见近战头盔 DemonshadeHelm 的同名方法注释。
        /// </summary>
        public override void UpdateArmorSet(Player player)
        {
            // 必须先加再取：player.GetDamage<X>() += 是就地修改玩家身上那份共享加成数据，
            // 同一方法内更早的读取看得到、更晚的读取看不到。
            player.GetDamage<MagicDamageClass>() += 1f;  // 法术伤害 +100%
            int redDevilDamage = (int)player.GetDamage<MagicDamageClass>().ApplyTo(10000);  // 红魔弹幕伤害：以玩家法术伤害对 10000 基准换算（已含上方这 +100%）
            player.setBonus = this.GetLocalizedValue("SetBonus");
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.demonshadeSetBonus = true;            // 置位套装总标记
            modPlayer.redDevil = true;                      // 置位红魔标记，供红魔弹幕判断去留
            modPlayer.demonshadeClass = DamageClass.Magic;  // 红魔三叉戟按法术结算
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
        /// 单件装备加成：法术伤害与暴击、最大法力，以及法力消耗减半；并置位职业标记供红魔三叉戟取值。
        /// 法力消耗走 player.manaCost 乘区（灾厄法师头部件通用写法），每次施法只花一半法力。
        /// </summary>
        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().demonshadeClass = DamageClass.Magic;  // 置位职业标记：本件为法师变体
            player.GetDamage<MagicDamageClass>() += 0.5f;   // 法术伤害 +50%
            player.GetCritChance<MagicDamageClass>() += 50; // 法术暴击率 +50%
            player.statManaMax2 += 300;                     // 最大法力 +300
            player.manaCost *= 0.5f;                        // 法力消耗 ×0.5（减半）
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
