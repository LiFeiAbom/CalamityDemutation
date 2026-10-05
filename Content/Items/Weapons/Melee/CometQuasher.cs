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
    /// 彗星陨刃（Comet Quasher）—— 秘银砧档近战（行为移植自灾厄大修 0.4.0.1.3 的 <c>CometQuasherEcType</c>）。
    /// 挥舞时朝准心甩出一颗陨石；近战命中敌人时从目标上方 500~600 像素处再砸下一颗陨石。
    /// 暴击伤害减半（<c>CritDamage *= 0.5</c>），这是大修原样保留的手感设定。
    /// 配方：陨石锭×25 + 灵质×5 @ 秘银砧（全原版材料）。
    /// 弹幕用的是灾厄本体 <c>CometQuasherMeteor</c>，走软依赖（经典版灾厄没有这个弹幕，纯经典环境下本武器不会发射东西）。
    /// </summary>
    internal class CometQuasher : ModItem
    {
        /// <summary>
        /// 物品基础属性：46×62 碰撞箱、缩放 1.5、伤害 80（源值；数值膨胀开关开启时面板回调到 160）、
        /// 15 帧挥砍、击退 2.75、黄名（60金）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 46;
            Item.height = 62;
            Item.scale = 1.5f;
            Item.damage = 80;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 15;
            Item.useTurn = true;
            Item.knockBack = 2.75f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(0, 60, 0, 0);
            Item.rare = ItemRarityID.Yellow;
            Item.shootSpeed = 9f;
            // 本模组对灾厄是弱引用，加载顺序不保证，SetDefaults 阶段可能查不到灾厄的弹幕；
            // 查不到时先挂一个原版占位弹幕，保证 Shoot 一定会被调用，真正的弹幕在 Shoot 里运行时解析。
            Item.shoot = ModContent.TryFind("CalamityMod", "CometQuasherMeteor", out ModProjectile meteor) ? meteor.Type : ProjectileID.WoodenArrowFriendly;
        }
        /// <summary>
        /// 数值膨胀后的面板伤害（用户 2026-10-01 指定：彗星陨刃 80 → 160）。
        /// </summary>
        private const float InflatedDamage = 160f;
        /// <summary>
        /// 当前生效的面板基础伤害：膨胀开关开启时用 <see cref="InflatedDamage"/>，否则维持 <c>Item.damage</c> 的源值 80。
        /// 挥舞甩出的陨石（半面板）与命中砸下的陨石（满面板）都按"裸面板"派生，统一读这里，避免只膨胀一半。
        /// </summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>（运行时读配置，游戏内切换即时生效）</summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base = BaseDamage;
        }
        /// <summary>运行时解析灾厄本体弹幕 <c>CometQuasherMeteor</c> 的类型；灾厄未加载时返回 0</summary>
        private static int MeteorType() => ModContent.TryFind("CalamityMod", "CometQuasherMeteor", out ModProjectile meteor) ? meteor.Type : 0;
        /// <summary>挥舞时朝准心方向甩出一颗陨石（弹幕伤害为面板的一半，<c>ai[1]</c> 给 0.5~0.8 的随机值）</summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int meteor = MeteorType();
            if (meteor <= 0)
                return false;
            Projectile.NewProjectile(source, position, velocity, meteor, (int)(BaseDamage * 0.5f), Item.knockBack, player.whoAmI, 0f, 0.5f + (float)Main.rand.NextDouble() * 0.3f);
            return false;
        }
        /// <summary>暴击伤害减半（大修原设定）</summary>
        public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers) => modifiers.CritDamage *= 0.5f;
        /// <summary>
        /// PvP 近似：与 <see cref="AnarchyBlade"/> 同款——PvE 侧暴击伤害减半，玩家侧无暴击信息，
        /// 故按期望值折算 <c>(1 + 0.5c) / (1 + c)</c>（c = 本武器总暴击率）。
        /// </summary>
        public override void ModifyHitPvp(Player player, Player target, ref Player.HurtModifiers modifiers)
        {
            float crit = Math.Clamp(player.GetWeaponCrit(Item), 0, 100) / 100f;
            modifiers.FinalDamage *= (1f + 0.5f * crit) / (1f + crit);
        }
        /// <summary>近战命中：在目标上方 500~600 像素处再生成一颗陨石砸下（伤害取满面板）</summary>
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            int meteor = MeteorType();
            if (meteor <= 0)
                return;
            Vector2 offsetVr = GetRandomVector(-75f, -105f, Main.rand.Next(500, 600));
            Vector2 spanPos = target.Center + offsetVr;
            Vector2 vr = offsetVr.SafeNormalize(Vector2.Zero) * -17;
            Projectile.NewProjectile(new EntitySource_Parent(player), spanPos, vr, meteor, (int)BaseDamage, Item.knockBack, player.whoAmI, 0f, 0.5f + (float)Main.rand.NextDouble() * 0.3f);
        }
        /// <summary>PvP 命中：同近战命中，从目标上方砸下陨石</summary>
        public override void OnHitPvp(Player player, Player target, Player.HurtInfo hurtInfo)
        {
            int meteor = MeteorType();
            if (meteor <= 0)
                return;
            Vector2 offsetVr = GetRandomVector(-75f, -105f, Main.rand.Next(500, 600));
            Vector2 spanPos = target.Center + offsetVr;
            Vector2 vr = offsetVr.SafeNormalize(Vector2.Zero) * -17;
            Projectile.NewProjectile(new EntitySource_Parent(player), spanPos, vr, meteor, (int)BaseDamage, Item.knockBack, player.whoAmI, 0f, 0.5f + (float)Main.rand.NextDouble() * 0.3f);
        }
        /// <summary>挥舞表现：先用 BetterSwing 修正巨剑挥舞位置，约 1/3 概率洒落火星尘（<c>DustID.Torch</c>，大修源码里写的是数字 6）</summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.NextBool(3))
                Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.Torch);
        }
        /// <summary>
        /// 等价于大修的 <c>CWRUtils.GetRandomVevtor(startAngle, targetAngle, length)</c>：
        /// 在 [startAngle, targetAngle] 度之间取随机角度，返回该角度上的单位向量乘以 length。
        /// </summary>
        private static Vector2 GetRandomVector(float startAngle, float targetAngle, float length)
        {
            float randomAngle = (targetAngle - startAngle) * Main.rand.NextFloat() + startAngle;
            float radian = MathHelper.ToRadians(randomAngle);
            return new Vector2((float)Math.Cos(radian), (float)Math.Sin(radian)) * length;
        }
        /// <summary>配方：陨石锭×25 + 灵质×5 @ 秘银砧（全原版材料）</summary>
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MeteoriteBar, 25);
            recipe.AddIngredient(ItemID.Ectoplasm, 5);
            recipe.AddTile(TileID.MythrilAnvil);
            recipe.Register();
        }
    }
}
