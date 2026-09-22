using CalamityDemutation.Content.Projectiles.Summon;
using CalamityDemutation.Sounds;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Summon
{
    /// <summary>
    /// 巨龙七星灯（YharonSonStaff，移植自 CalamityInheritance 的 Content/Items/Weapons/Summon/YharonSonStaff.cs）：
    /// 召唤一条「犽戎之子」为你作战的召唤杖，每个召唤物占 2 个仆从栏位
    /// （CI 原样是 4；栏位与其弹幕的命中无敌帧都是 2026-09-22 用户拍板下调的，见 <see cref="SonYharon"/>）。
    /// <para>
    /// 与 CI 原版的差异：① 伤害按 2026-09-22 用户拍板的既有档位口径做 ×4.5 膨胀（CI 原值 120 → 540）；
    /// ② 显示名去掉 CI 的 <c>[Legacy]</c> 后缀——那是 CI 用来标记"灾厄旧版内容回归"的后缀，本模组本就是灾厄扩展，不需要；
    /// ③ CI 的稀有度是 <c>CIConfig.SpecialRarityColor ? YharonFire : DeepBlue</c>（配置可切的金黄特殊色 / 深蓝），
    /// 本模组按 CI 自己注释的口径（DeepBlue = 灾厄 Rarity14）统一映射成 <c>postMoonLordRarity = 14</c>，不做配置分支；
    /// ④ CI 的物品基类 <c>CISummon</c> 只覆写了 <c>ModifyResearchSorting</c>（把物品塞进召唤武器分类），
    /// 本模组按伤害类型自动归类，故不移植该类；⑤ 音效改用从灾厄拷进本模组的 <c>YharonInfernado</c>（= CI 的 CommonCalamitySounds.FlareSound）。
    /// </para>
    /// </summary>
    internal class YharonSonStaff:ModItem
    {
        /// <summary>法杖形态的持握绘制，并在旅途模式登记为 1 次研究（CI 原样）</summary>
        public override void SetStaticDefaults()
        {
            Item.staff[Item.type] = true;
            Item.ResearchUnlockCount = 1;
        }
        public override void SetDefaults()
        {
            Item.height = 48;
            Item.width = 56;
            Item.mana = 50;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.DamageType = DamageClass.Summon;
            Item.damage = 540;              // CI 原值 120，按既有档位口径 ×4.5
            Item.noMelee = true;
            Item.knockBack = 7f;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.UseSound = CalamityDemutationSounds.YharonInfernado;
            Item.rare = ItemRarityID.Red;   // 基础稀有度红，真正的名称颜色由 postMoonLordRarity 覆盖
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;
            Item.value = Item.buyPrice(platinum: 2);    // CI 的 RarityPriceDeepBlue = 2 铂金
            Item.shoot = ModContent.ProjectileType<SonYharon>();
            Item.shootSpeed = 10f;
        }
    }
}
