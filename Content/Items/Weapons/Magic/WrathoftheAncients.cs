using CalamityDemutation.Content.Projectiles.Magic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Magic
{
    /// <summary>
    /// 远古之怒（Wrath of the Ancients）—— 移植自灾厄 2.0 的
    /// <c>Items/Weapons/Magic/WrathoftheAncients.cs</c>。
    /// <para>
    /// 面板照 2.0：28×30 判定（贴图 38×44）、伤害 47、魔力 20、使用/动画 38 帧、
    /// 击退 6、弹速 9、黄档、价值 80 金；无配方，由地牢死灵法师 / 装甲死灵法师
    /// 以 1/25（普通）/ 1/15（专家）掉落（见 <c>CalamityDemutationGlobalNPC.ModifyNPCLoot</c>）。
    /// </para>
    /// <para>
    /// 出手在鼠标位置生成 <see cref="GranitePulse"/>；脉冲本身不造成伤害，
    /// 每 50 帧向四周各放出 4 对 <see cref="GraniteEnergy"/> 追踪弹。
    /// </para>
    /// </summary>
    internal class WrathoftheAncients : ModItem
    {
        /// <summary>研究解锁一份</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>基础属性全部照灾厄 2.0</summary>
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.damage = 47;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 20;
            Item.useTime = 38;
            Item.useAnimation = 38;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 6f;
            Item.value = Item.buyPrice(0, 80, 0, 0);
            Item.rare = ItemRarityID.Yellow;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<GranitePulse>();
            Item.shootSpeed = 9f;
        }
        /// <summary>在鼠标位置生成脉冲（照 2.0，生成源换成本工程的物品使用源）</summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero, type, damage, knockback, player.whoAmI, 0f, 0f);
            return false;
        }
    }
}
