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
    /// 崇高誓言之刃 - 困难模式近战剑，挥砍散射誓言之刃弹幕并造成爆发伤害
    /// </summary>
    internal class ExaltedOathblade:ModItem
    {
        internal const float ShootSpeed = 3f;
        /// <summary>
        /// 物品基础属性：伤害 175、使用时间 20 帧、击退 7.5、黄色稀有度；
        /// 主弹幕为崇高誓言之刃弹幕（ExaltedOathBladeProj），弹速取常量 ShootSpeed=3，
        /// 每次挥砍发射一次。
        /// </summary>
        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Melee;
            Item.width = 88;
            Item.height = 88;
            Item.damage = 175;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.useTurn = false;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 7.5f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(0, 80, 0, 0);
            Item.rare = ItemRarityID.Yellow;
            Item.shoot = ModContent.ProjectileType<ExaltedOathBladeProj>();
            Item.shootSpeed = ShootSpeed;
            Item.shootsEveryUse = true;
        }
        /// <summary>
        /// 射击逻辑：向两侧各散射一发半伤害的誓言之刃弹幕
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int index = 8;
            for (int i = -index; i <= index; i += index)
            {
                Vector2 perturbedSpeed = velocity.RotatedBy(MathHelper.ToRadians(i));
                Projectile.NewProjectile(source, position, perturbedSpeed, type, damage / 2, knockback, player.whoAmI);
            }
            return false;
        }
        /// <summary>
        /// 挥砍特效：修正挥舞位置并扬起暗影束法杖粉尘
        /// </summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.NextBool(3))
                Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.ShadowbeamStaff);
        }
        /// <summary>
        /// 命中敌人：施加暗影烈焰与着火，造成追加伤害并生成大量粉尘
        /// </summary>
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.ShadowFlame, 450);
            target.AddBuff(BuffID.OnFire, 900);
            // 追加伤害与 PvP 分支同源：同为 GetWeaponDamage*2，用显式 StrikeNPC 施加，
            // 避免 ApplyDamageToNPC 可能二次乘职业加成导致 NPC 侧偏高，两侧数值对齐
            int onHitDamage = player.GetWeaponDamage(player.HeldItem) * 2;
            target.StrikeNPC(target.CalculateHitInfo(onHitDamage, hit.HitDirection));
            float firstDustScale = 1.7f;
            float secondDustScale = 0.8f;
            float thirdDustScale = 2f;
            Vector2 dustRotation = (target.rotation - MathHelper.PiOver2).ToRotationVector2();
            Vector2 dustVelocity = dustRotation * target.velocity.Length();
            SoundEngine.PlaySound(SoundID.Item14, target.Center);
            for (int i = 0; i < 40; i++)
            {
                int swingDust = Dust.NewDust(target.position, target.width, target.height, DustID.ShadowbeamStaff, 0f, 0f, 200, default, firstDustScale);
                Dust dust = Main.dust[swingDust];
                dust.position = target.Center + Vector2.UnitY.RotatedByRandom(Math.PI) * (float)Main.rand.NextDouble() * target.width / 2f;
                dust.noGravity = true;
                dust.velocity.Y -= 4.5f;
                dust.velocity *= 3f;
                dust.velocity += dustVelocity * Main.rand.NextFloat();
                swingDust = Dust.NewDust(target.position, target.width, target.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default, secondDustScale);
                dust.position = target.Center + Vector2.UnitY.RotatedByRandom(Math.PI) * (float)Main.rand.NextDouble() * target.width / 2f;
                dust.velocity.Y -= 3f;
                dust.velocity *= 2f;
                dust.noGravity = true;
                dust.fadeIn = 1f;
                dust.color = Color.Crimson * 0.5f;
                dust.velocity += dustVelocity * Main.rand.NextFloat();
            }
            for (int j = 0; j < 20; j++)
            {
                int swingDust2 = Dust.NewDust(target.position, target.width, target.height, DustID.ShadowbeamStaff, 0f, 0f, 0, default, thirdDustScale);
                Dust dust = Main.dust[swingDust2];
                dust.position = target.Center + Vector2.UnitX.RotatedByRandom(Math.PI).RotatedBy((double)target.velocity.ToRotation(), default) * target.width / 3f;
                dust.noGravity = true;
                dust.velocity.Y -= 1.5f;
                dust.velocity *= 0.5f;
                dust.velocity += dustVelocity * (0.6f + 0.6f * Main.rand.NextFloat());
            }
        }
        /// <summary>
        /// 命中玩家（PvP）：与 NPC 版一致——施加暗影烈焰与着火、2 倍武器伤害追加与大量粉尘
        /// </summary>
        public override void OnHitPvp(Player player, Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.ShadowFlame, 450);
            target.AddBuff(BuffID.OnFire, 900);
            // PvP 追加伤害与 NPC 版同口径（2 倍武器伤害）；
            // PvP 目标为玩家，须清除其免疫帧后以 Player.Hurt 施加，追加伤害才能稳定生效
            int onHitDamage = player.GetWeaponDamage(player.HeldItem) * 2;
            target.immuneTime = 0;
            target.Hurt(new PlayerDeathReason { SourcePlayerIndex = player.whoAmI }, onHitDamage, player.direction, true);
            float firstDustScale = 1.7f;
            float secondDustScale = 0.8f;
            float thirdDustScale = 2f;
            // 玩家无 rotation 属性，用面朝方向替代 NPC 版的朝向向量
            Vector2 dustRotation = Vector2.UnitX * target.direction;
            Vector2 dustVelocity = dustRotation * target.velocity.Length();
            SoundEngine.PlaySound(SoundID.Item14, target.Center);
            for (int i = 0; i < 40; i++)
            {
                int swingDust = Dust.NewDust(target.position, target.width, target.height, DustID.ShadowbeamStaff, 0f, 0f, 200, default, firstDustScale);
                Dust dust = Main.dust[swingDust];
                dust.position = target.Center + Vector2.UnitY.RotatedByRandom(Math.PI) * (float)Main.rand.NextDouble() * target.width / 2f;
                dust.noGravity = true;
                dust.velocity.Y -= 4.5f;
                dust.velocity *= 3f;
                dust.velocity += dustVelocity * Main.rand.NextFloat();
                swingDust = Dust.NewDust(target.position, target.width, target.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default, secondDustScale);
                dust.position = target.Center + Vector2.UnitY.RotatedByRandom(Math.PI) * (float)Main.rand.NextDouble() * target.width / 2f;
                dust.velocity.Y -= 3f;
                dust.velocity *= 2f;
                dust.noGravity = true;
                dust.fadeIn = 1f;
                dust.color = Color.Crimson * 0.5f;
                dust.velocity += dustVelocity * Main.rand.NextFloat();
            }
            for (int j = 0; j < 20; j++)
            {
                int swingDust2 = Dust.NewDust(target.position, target.width, target.height, DustID.ShadowbeamStaff, 0f, 0f, 0, default, thirdDustScale);
                Dust dust = Main.dust[swingDust2];
                dust.position = target.Center + Vector2.UnitX.RotatedByRandom(Math.PI).RotatedBy((double)target.velocity.ToRotation(), default) * target.width / 3f;
                dust.noGravity = true;
                dust.velocity.Y -= 1.5f;
                dust.velocity *= 0.5f;
                dust.velocity += dustVelocity * (0.6f + 0.6f * Main.rand.NextFloat());
            }
        }
        /// <summary>
        /// 配方：禁忌誓言之刃 + 断裂英雄剑，在秘银砧合成（两版灾厄共用）
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) || ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient<ForbiddenOathblade>();
                recipe.AddIngredient(ItemID.BrokenHeroSword);
                recipe.AddTile(TileID.MythrilAnvil);
                recipe.Register();
            }
        }
    }
}
