using CalamityDemutation.Content.Projectiles.Summon;
using CalamityDemutation.Sounds;
using CalamityDemutation.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Summon
{
    /// <summary>
    /// 巨龙七星灯（YharonSonStaff，移植自 CalamityInheritance 的 Content/Items/Weapons/Summon/YharonSonStaff.cs）：
    /// 召唤一条「犽戎之子」为你作战的召唤杖，每个召唤物占 4 个仆从栏位
    /// （数值膨胀开关开启时降到 2 个；命中无敌帧始终维持 CI 原样，见 <see cref="SonYharon"/>）。
    /// <para>
    /// 与 CI 原版的差异：① 显示名去掉 CI 的 <c>[Legacy]</c> 后缀——那是 CI 用来标记"灾厄旧版内容回归"的后缀，本模组本就是灾厄扩展，不需要；
    /// ② CI 的稀有度是 <c>CIConfig.SpecialRarityColor ? YharonFire : DeepBlue</c>（配置可切的金黄特殊色 / 深蓝），
    /// 本模组按 CI 自己注释的口径（DeepBlue = 灾厄 Rarity14）统一映射成 <c>postMoonLordRarity = 14</c>，不做配置分支；
    /// ③ CI 的物品基类 <c>CISummon</c> 只覆写了 <c>ModifyResearchSorting</c>（把物品塞进召唤武器分类），
    /// 本模组按伤害类型自动归类，故不移植该类；④ 音效改用从灾厄拷进本模组的 <c>YharonInfernado</c>（= CI 的 CommonCalamitySounds.FlareSound）。
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
            Item.damage = 120;
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
        /// <summary>
        /// 数值膨胀后的面板伤害（用户 2026-10-01 指定：巨龙七星灯 120 → 750）。
        /// </summary>
        private const float InflatedDamage = 750f;
        /// <summary>
        /// 当前生效的面板基础伤害：膨胀开关开启时用 <see cref="InflatedDamage"/>，否则维持 <c>Item.damage</c> 的源值 120。
        /// </summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>
        /// 数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>（运行时读配置，游戏内切换即时生效）。
        /// 仆从弹幕的 <c>originalDamage</c> 取自 <c>Player.ItemCheck</c> 经 <c>GetWeaponDamage</c> 算出的本次面板值，
        /// 因此召唤物伤害会跟随；同一开关还把仆从栏位从 4 降到 2（见 <see cref="SonYharon"/>）。
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base = BaseDamage;
        }
    }
}
