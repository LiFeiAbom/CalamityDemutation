using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 灾变阔剑 - 神圣系近战剑，挥砍发射火花并概率施加多种 debuff
    /// </summary>
    internal class CatastropheClaymore:ModItem
    {
        /// <summary>
        /// 物品基础属性：伤害 85、使用时间与使用动画同为 23 帧、击退 6.25、淡紫稀有度；
        /// 主弹幕为灾变火花（CatastropheClaymoreSparkle），每次挥砍发射一次。
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 60;
            Item.height = 60;
            Item.damage = 85;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = Item.useTime = 23;
            Item.useTurn = false;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6.25f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(0, 48, 0, 0);
            Item.rare = ItemRarityID.LightPurple;
            Item.shoot = ModContent.ProjectileType<CatastropheClaymoreSparkle>();
            Item.shootSpeed = 11f;
        }
        /// <summary>
        /// 射击逻辑：发射一颗带随机 ai 值的火花弹幕
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, Main.myPlayer, Main.rand.Next(3));
            return false;
        }
        /// <summary>
        /// 挥砍特效：修正挥舞位置并扬起粉色精灵粉尘
        /// </summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.NextBool(3))
            {
                Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.PinkFairy);
            }
        }
        /// <summary>
        /// 命中敌人：概率施加脓液、三级狱炎、二级霜燃 debuff
        /// </summary>
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextBool(3))
            {
                target.AddBuff(BuffID.Ichor, 60);
                target.AddBuff(BuffID.OnFire3, 180);
                target.AddBuff(BuffID.Frostburn2, 120);
            }
        }
        /// <summary>
        /// 命中玩家（PvP）：概率施加脓液、三级狱炎、二级霜燃 debuff
        /// </summary>
        public override void OnHitPvp(Player player, Player target, Player.HurtInfo hurtInfo)
        {
            if (Main.rand.NextBool(3))
            {
                target.AddBuff(BuffID.Ichor, 60);
                target.AddBuff(BuffID.OnFire3, 180);
                target.AddBuff(BuffID.Frostburn2, 120);
            }
        }
        /// <summary>
        /// 配方：神圣锭 + 水晶碎块 + 暗影之魂 + 诅咒焰/脓液（按世界邪恶类型）+ 机械三王魂
        /// </summary>
        public override void AddRecipes()
        {
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity) || ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.HallowedBar, 10);
                recipe.AddIngredient(ItemID.CrystalShard, 15);
                recipe.AddIngredient(ItemID.SoulofNight, 5);
                recipe.AddIngredient(ItemID.CursedFlame, 5);
                recipe.AddIngredient(ItemID.SoulofMight, 3);
                recipe.AddIngredient(ItemID.SoulofSight, 3);
                recipe.AddIngredient(ItemID.SoulofFright, 3);
                recipe.AddTile(TileID.MythrilAnvil);
                recipe.Register();
                recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.HallowedBar, 10);
                recipe.AddIngredient(ItemID.CrystalShard, 15);
                recipe.AddIngredient(ItemID.SoulofNight, 5);
                recipe.AddIngredient(ItemID.Ichor, 5);
                recipe.AddIngredient(ItemID.SoulofMight, 3);
                recipe.AddIngredient(ItemID.SoulofSight, 3);
                recipe.AddIngredient(ItemID.SoulofFright, 3);
                recipe.AddTile(TileID.MythrilAnvil);
                recipe.Register();
            }
        }
    }
}
