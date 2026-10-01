using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 凤凰之刃（Phoenix Blade）—— 秘银砧档近战（行为照搬灾厄经典版 <c>Items/Weapons/PhoenixBlade.cs</c>）。
    /// 大修与 2.0.3.9 都没有这把武器，经典版是唯一来源。
    /// 效果：近战击杀敌人时在原处炸出一团日耀喷发剑式的爆炸（<c>ProjectileID.SolarWhipSwordExplosion</c>，
    /// 经典版源码里写的是数字 612），并朝斜上方甩出两枚追踪玩家的治疗火焰（灾厄经典版 <c>PhoenixHeal</c>）。
    /// 配方：毁灭刃 + 狱石锭×10 + 炽热精华 + 力量/视野/恐惧之魂各×3 @ 秘银砧。
    /// 注意：炽热精华（<c>EssenceofCinder</c>）与治疗火焰都是经典版灾厄专有物，走软依赖，
    /// 缺任意一项时本条配方不注册（本工程的 CoreofCinder 是另一件东西，不能顶替）。
    /// </summary>
    internal class PhoenixBlade : ModItem
    {
        /// <summary>
        /// 物品基础属性：106×106、伤害 95（源值；数值膨胀开关开启时面板回调到 160）、
        /// 29 帧挥砍、击退 8、浅紫名（48金）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 106;
            Item.height = 106;
            Item.damage = 95;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 29;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 29;
            Item.useTurn = true;
            Item.knockBack = 8f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(0, 48, 0, 0);
            Item.rare = ItemRarityID.LightPurple;
            Item.shootSpeed = 12f;
        }
        /// <summary>
        /// 数值膨胀后的面板伤害（用户 2026-10-01 指定：凤凰之刃 95 → 160）。
        /// </summary>
        private const float InflatedDamage = 160f;
        /// <summary>
        /// 当前生效的面板基础伤害：膨胀开关开启时用 <see cref="InflatedDamage"/>，否则维持 <c>Item.damage</c> 的源值 95。
        /// 面板回调与下面两处击杀表现（日耀爆炸 + 治疗火焰）统一读这里，避免只膨胀一半。
        /// </summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>（运行时读配置，游戏内切换即时生效）</summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base = BaseDamage;
        }
        /// <summary>运行时解析经典版灾厄的治疗火焰弹幕 <c>PhoenixHeal</c> 的类型；未加载时返回 0</summary>
        private static int PhoenixHealType() => ModContent.TryFind("CalamityModClassicPreTrailer", "PhoenixHeal", out ModProjectile phoenixHeal) ? phoenixHeal.Type : 0;
        /// <summary>近战击杀敌人：原位炸出日耀爆炸，并朝左右两侧斜上方各甩出一枚治疗火焰（速度被后面覆写成随机值）</summary>
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (target.life > 0)
                return;
            IEntitySource source = player.GetSource_ItemUse(Item);
            Projectile.NewProjectile(source, target.Center, Vector2.Zero, ProjectileID.SolarWhipSwordExplosion, (int)BaseDamage, Item.knockBack, player.whoAmI);
            int phoenixHeal = PhoenixHealType();
            if (phoenixHeal <= 0)
                return;
            float spread = 180f * 0.0174f;
            double startAngle = Math.Atan2(Item.shootSpeed, Item.shootSpeed) - spread / 2;
            double deltaAngle = spread / 8f;
            float randomSpeedX = Main.rand.Next(5);
            float randomSpeedY = Main.rand.Next(3, 7);
            double offsetAngle = startAngle;
            int left = Projectile.NewProjectile(source, target.Center, new Vector2((float)(Math.Sin(offsetAngle) * 5f), (float)(Math.Cos(offsetAngle) * 5f)), phoenixHeal, (int)BaseDamage, Item.knockBack, player.whoAmI);
            int right = Projectile.NewProjectile(source, target.Center, new Vector2((float)(-Math.Sin(offsetAngle) * 5f), (float)(-Math.Cos(offsetAngle) * 5f)), phoenixHeal, (int)BaseDamage, Item.knockBack, player.whoAmI);
            if (left >= 0 && left < Main.maxProjectiles)
            {
                Main.projectile[left].velocity.X = -randomSpeedX;
                Main.projectile[left].velocity.Y = -randomSpeedY;
            }
            if (right >= 0 && right < Main.maxProjectiles)
            {
                Main.projectile[right].velocity.X = randomSpeedX;
                Main.projectile[right].velocity.Y = -randomSpeedY;
            }
        }
        /// <summary>击杀玩家（PvP）：与 OnHitNPC 同构——原位炸出日耀爆炸，再朝左右斜上方各甩一枚治疗火焰</summary>
        public override void OnHitPvp(Player player, Player target, Player.HurtInfo hurtInfo)
        {
            if (target.statLife > 0)
                return;
            IEntitySource source = player.GetSource_ItemUse(Item);
            Projectile.NewProjectile(source, target.Center, Vector2.Zero, ProjectileID.SolarWhipSwordExplosion, (int)BaseDamage, Item.knockBack, player.whoAmI);
            int phoenixHeal = PhoenixHealType();
            if (phoenixHeal <= 0)
                return;
            float spread = 180f * 0.0174f;
            double startAngle = Math.Atan2(Item.shootSpeed, Item.shootSpeed) - spread / 2;
            double deltaAngle = spread / 8f;
            float randomSpeedX = Main.rand.Next(5);
            float randomSpeedY = Main.rand.Next(3, 7);
            double offsetAngle = startAngle;
            int left = Projectile.NewProjectile(source, target.Center, new Vector2((float)(Math.Sin(offsetAngle) * 5f), (float)(Math.Cos(offsetAngle) * 5f)), phoenixHeal, (int)BaseDamage, Item.knockBack, player.whoAmI);
            int right = Projectile.NewProjectile(source, target.Center, new Vector2((float)(-Math.Sin(offsetAngle) * 5f), (float)(-Math.Cos(offsetAngle) * 5f)), phoenixHeal, (int)BaseDamage, Item.knockBack, player.whoAmI);
            if (left >= 0 && left < Main.maxProjectiles)
            {
                Main.projectile[left].velocity.X = -randomSpeedX;
                Main.projectile[left].velocity.Y = -randomSpeedY;
            }
            if (right >= 0 && right < Main.maxProjectiles)
            {
                Main.projectile[right].velocity.X = randomSpeedX;
                Main.projectile[right].velocity.Y = -randomSpeedY;
            }
        }
        /// <summary>挥舞表现：先用 BetterSwing 修正挥舞位置，约 1/4 概率洒落金色尘（<c>DustID.CopperCoin</c>，源码写 244）</summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.Next(4) == 0)
                Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.CopperCoin);
        }
        /// <summary>配方：毁灭刃 + 狱石锭×10 + 炽热精华 + 三魂各×3 @ 秘银砧（炽热精华走经典版软依赖）</summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("EssenceofCinder", out ModItem essenceofCinder))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient(ItemID.BreakerBlade);
                    recipeClassic.AddIngredient(ItemID.HellstoneBar, 10);
                    recipeClassic.AddIngredient(essenceofCinder.Type);
                    recipeClassic.AddIngredient(ItemID.SoulofMight, 3);
                    recipeClassic.AddIngredient(ItemID.SoulofSight, 3);
                    recipeClassic.AddIngredient(ItemID.SoulofFright, 3);
                    recipeClassic.AddTile(TileID.MythrilAnvil);
                    recipeClassic.Register();
                }
            }
        }
    }
}
