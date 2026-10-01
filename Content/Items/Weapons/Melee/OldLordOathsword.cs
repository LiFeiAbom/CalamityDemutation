using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 旧主誓约之剑 - 早期近战剑，挥砍发射誓约火焰
    /// </summary>
    internal class OldLordOathsword:ModItem
    {
        /// <summary>
        /// 物品基础属性：伤害 31（源值；数值膨胀开关开启时面板回调到 144）、
        /// 使用时间 21 帧、击退 4.5、橙色稀有度，可转向（useTurn=true）；
        /// 主弹幕为誓约火焰（OathswordFlame），每次挥砍发射一次。
        /// </summary>
        public override void SetDefaults()
        {
            Item.damage = 31;
            Item.width = 78;
            Item.height = 78;
            Item.DamageType = DamageClass.Melee/* tModPorter Suggestion: Consider MeleeNoSpeed for no attack speed scaling */;
            Item.useAnimation = 21;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 21;
            Item.useTurn = true;
            Item.knockBack = 4.5f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(0, 4, 0, 0);
            Item.rare = ItemRarityID.Orange;
            Item.shoot = ModContent.ProjectileType<OathswordFlame>();
            Item.shootSpeed = 8f;
        }
        /// <summary>
        /// 数值膨胀后的面板伤害（用户 2026-10-01 指定：旧日领主誓约剑 31 → 144）。
        /// </summary>
        private const float InflatedDamage = 144f;
        /// <summary>
        /// 当前生效的面板基础伤害：膨胀开关开启时用 <see cref="InflatedDamage"/>，否则维持 <c>Item.damage</c> 的源值 31。
        /// </summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>
        /// 数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>（运行时读配置，游戏内切换即时生效）。
        /// 誓约火焰走默认发射路径，取的就是本次修正后的面板值，会自动跟随。
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base = BaseDamage;
        }
        /// <summary>
        /// 挥砍特效：修正武器挥舞位置
        /// </summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
        }
    }
}
