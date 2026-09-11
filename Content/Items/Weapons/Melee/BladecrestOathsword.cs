using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 刃冠誓约之剑 - 早期近战剑，挥砍发射鲜血镰刀并点燃命中目标
    /// </summary>
    internal class BladecrestOathsword : ModItem
    {
        /// <summary>
        /// 物品基础属性：伤害 25、使用时间 23 帧、击退 4、橙色稀有度，不自动连击（autoReuse=false）；
        /// 主弹幕为鲜血镰刀（BloodScythe），每次挥砍发射一次。
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 58;
            Item.damage = 25;
            Item.DamageType = DamageClass.Melee/* tModPorter Suggestion: Consider MeleeNoSpeed for no attack speed scaling */;
            Item.useAnimation = 23;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 23;
            Item.knockBack = 4f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = false;
            Item.height = 58;
            Item.value = Item.buyPrice(0, 4, 0, 0);
            Item.rare = ItemRarityID.Orange;
            Item.shoot = ModContent.ProjectileType<BloodScythe>();
            Item.shootSpeed = 6f;
            Item.shootsEveryUse = true;
        }
        /// <summary>
        /// 挥砍特效：修正武器挥舞位置
        /// </summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
        }
        /// <summary>
        /// 命中敌人：施加着火 debuff
        /// </summary>
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, 200);
        }
        /// <summary>
        /// 命中玩家（PvP）：施加着火 debuff
        /// </summary>
        public override void OnHitPvp(Player player, Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.OnFire, 200);
        }
    }
}
