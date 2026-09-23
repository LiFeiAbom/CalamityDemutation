using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 月炎之锋（Stellar Striker）—— 月球工作台档近战（行为移植自灾厄大修 0.4.0.1.3 的 <c>StellarStrikerEcType</c>）。
    /// 左键：15 帧快挥，每次挥砍射出一道 1/3 伤害的星流束（<see cref="StellarStrikerBeam"/>）。
    /// 右键：20 帧慢挥、挥砍体积放大 1.5 倍，命中时在敌人头顶召唤一场月炎陨石雨（半伤害 + 暴击再减半）。
    /// 配方：本模组彗星陨刃 + 月锭×5 @ 月球工作台。
    /// </summary>
    internal class StellarStriker : ModItem
    {
        /// <summary>允许同一件物品连续右键（原版默认右键后要等一轮）</summary>
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }
        /// <summary>
        /// 物品基础属性：90×100、缩放 1.5、伤害 480、20 帧挥砍、击退 7.75、红名（1铂10金）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 90;
            Item.height = 100;
            Item.scale = 1.5f;
            Item.damage = 480;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 20;
            Item.useTurn = true;
            Item.knockBack = 7.75f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(1, 10, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.shoot = ProjectileID.LunarFlare;
            Item.shootSpeed = 12f;
        }
        /// <summary>右键可用</summary>
        public override bool AltFunctionUse(Player player) => true;
        /// <summary>每次使用前按左右键切换挥砍速度与体积：左键 15 帧 / 1 倍，右键 20 帧 / 1.5 倍</summary>
        public override bool? UseItem(Player player)
        {
            Item.useAnimation = Item.useTime = 15;
            Item.scale = 1f;
            if (player.altFunctionUse == 2)
            {
                Item.useAnimation = Item.useTime = 20;
                Item.scale = 1.5f;
            }
            return base.UseItem(player);
        }
        /// <summary>左键挥砍时射出一道 1/3 伤害的星流束；右键不发射（改为命中时召唤陨石雨）</summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
                return false;
            SoundEngine.PlaySound(SoundID.Item88, player.Center);
            Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<StellarStrikerBeam>(), damage / 3, knockback, player.whoAmI, 0f, Main.rand.Next(3));
            return false;
        }
        /// <summary>右键命中：召唤月炎陨石雨</summary>
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (player.altFunctionUse == 2)
                SpawnFlares(Item, player, Item.knockBack, Item.damage, hit.Crit);
        }
        /// <summary>右键 PvP 命中：同上（暴击按 true 处理）</summary>
        public override void OnHitPvp(Player player, Player target, Player.HurtInfo hurtInfo)
        {
            if (player.whoAmI == Main.myPlayer && player.altFunctionUse == 2)
                SpawnFlares(Item, player, Item.knockBack, Item.damage, true);
        }
        /// <summary>
        /// 在鼠标方向上方的随机高度生成两波月炎火球（<c>ProjectileID.LunarFlare</c>），
        /// 生成后再把它们的伤害类型改回近战（原版火球默认是魔法）。
        /// 大修原码里有两处 <c>_ = ...</c> 的丢弃表达式（算完就扔），不产生任何效果，已略去。
        /// </summary>
        public static void SpawnFlares(Item item, Player player, float knockback, int damage, bool crit)
        {
            IEntitySource source = player.GetSource_ItemUse(item);
            SoundEngine.PlaySound(SoundID.Item88, player.Center);
            float cometSpeed = item.shootSpeed;
            Vector2 realPlayerPos = player.RotatedRelativePoint(player.MountedCenter, true);
            if (crit)
                damage /= 2;
            for (int j = 0; j < 2; j++)
            {
                realPlayerPos = new Vector2(player.Center.X + Main.rand.Next(201) * -player.direction + (Main.mouseX + Main.screenPosition.X - player.position.X), player.MountedCenter.Y - 600f);
                realPlayerPos.X = (realPlayerPos.X + player.Center.X) / 2f + Main.rand.Next(-200, 201);
                realPlayerPos.Y -= 100 * j;
                float mouseXDist = Main.mouseX + Main.screenPosition.X - realPlayerPos.X + Main.rand.Next(-40, 41) * 0.03f;
                float mouseYDist = Main.mouseY + Main.screenPosition.Y - realPlayerPos.Y;
                if (mouseYDist < 0f)
                    mouseYDist *= -1f;
                if (mouseYDist < 20f)
                    mouseYDist = 20f;
                float mouseDistance = (float)Math.Sqrt(mouseXDist * mouseXDist + mouseYDist * mouseYDist);
                mouseDistance = cometSpeed / mouseDistance;
                mouseXDist *= mouseDistance;
                mouseYDist *= mouseDistance;
                float speedX = mouseXDist;
                float speedY = mouseYDist + Main.rand.Next(-80, 81) * 0.02f;
                int proj = Projectile.NewProjectile(source, realPlayerPos.X, realPlayerPos.Y, speedX, speedY, ProjectileID.LunarFlare, (int)(damage * 0.5), knockback, player.whoAmI, 0f, Main.rand.Next(3));
                if (proj >= 0 && proj < Main.maxProjectiles)
                    Main.projectile[proj].DamageType = DamageClass.Melee;
            }
        }
        /// <summary>挥舞表现：BetterSwing 修正挥舞位置；约 1/3 概率洒落星旋尘（<c>DustID.Vortex</c>）</summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.NextBool(3))
                Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.Vortex);
        }
        /// <summary>配方：本模组彗星陨刃 + 月锭×5 @ 月球工作台</summary>
        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient<CometQuasher>().
                AddIngredient(ItemID.LunarBar, 5).
                AddTile(TileID.LunarCraftingStation).
                Register();
        }
    }
}
