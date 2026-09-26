using CalamityDemutation.Content.Projectiles.Melee;
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
        /// 物品基础属性：伤害 31、使用时间 21 帧、击退 4.5、橙色稀有度，可转向（useTurn=true）；
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
        /// 挥砍特效：修正武器挥舞位置
        /// </summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
        }
    }
}
