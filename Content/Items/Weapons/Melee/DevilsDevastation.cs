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
    /// 恶魔毁灭（DevilsDevastation） - 终局近战剑，挥砍散射誓言之刃并以对称轴镜像召唤恶魔叉
    /// </summary>
    internal class DevilsDevastation:ModItem
    {
        /// <summary>
        /// 物品基础属性：伤害 520、使用时间 14 帧、击退 6.75、红色稀有度；
        /// 主弹幕为誓言之刃（Oathblade），每次挥砍发射一次，月后稀有度 14。
        /// </summary>
        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Melee;
            Item.width = 118;
            Item.height = 118;
            Item.damage = 520;
            Item.useAnimation = 14;
            Item.useTime = 14;
            Item.useTurn = false;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6.75f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(1, 80, 0, 0);
            Item.shoot = ModContent.ProjectileType<Oathblade>();
            Item.shootSpeed = 28f;
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;
            Item.shootsEveryUse = true;
        }
        /// <summary>
        /// 射击逻辑：主弹幕散射 3 把誓言之刃（Oathblade，按 ±8° 展开）；
        /// 再以「玩家中心→鼠标」连线为对称轴，在玩家上下方各召唤 3 组×3 把恶魔叉（DemonFork），
        /// 上下两组关于对称轴严格镜像。返回 false 表示由本方法自行管理弹幕生成。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source2, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            //Shoot 3 oathblades in a spread
            int index = 8;
            for (int j = -index; j <= index; j += index)
            {
                Vector2 perturbedSpeed = new Vector2(velocity.X, velocity.Y).RotatedBy(MathHelper.ToRadians(j));
                Projectile.NewProjectile(source2, position, perturbedSpeed, type, damage, knockback, player.whoAmI);
            }
            // 对称轴：玩家中心 → 鼠标世界坐标 的连线（鼠标坐标需处理重力翻转）
            float speed = Item.shootSpeed;
            Vector2 mouseWorld = new Vector2(Main.mouseX + Main.screenPosition.X, Main.mouseY + Main.screenPosition.Y);
            if (player.gravDir == -1f)
            {
                mouseWorld.Y = Main.screenPosition.Y + Main.screenHeight - Main.mouseY;
            }
            Vector2 axisDir = mouseWorld - player.Center;
            if (axisDir == Vector2.Zero)
            {
                axisDir = new Vector2(player.direction, 0f);
            }
            axisDir.Normalize();
            // 反射辅助：向量 v 关于单位轴 axis 的镜像
            static Vector2 MirrorAcross(Vector2 v, Vector2 axis) => 2f * Vector2.Dot(v, axis) * axis - v;
            // 叉阵：下方来源发射一组，再以对称轴镜像发出上方一组（各 3 组 × 3 把）
            int projAmt = 3;
            for (int projIndex = 0; projIndex < projAmt; projIndex++)
            {
                // 下方来源点（保留原逻辑）：横向在玩家与鼠标之间随机，纵向在玩家下方 600px 起
                Vector2 source = new Vector2(player.Center.X + Main.rand.Next(201) * -player.direction + (mouseWorld.X - player.position.X), player.MountedCenter.Y + 600f);
                source.X = (source.X + player.Center.X) / 2f + Main.rand.Next(-200, 201);
                source.Y += 100 * projIndex;
                // 下方速度：从来源指向鼠标，强制修正为向上
                Vector2 direction = mouseWorld - source;
                if (direction.Y < 0f)
                {
                    direction.Y *= -1f;
                }
                if (direction.Y < 20f)
                {
                    direction.Y = 20f;
                }
                float aimDist = speed / direction.Length();
                direction.X *= aimDist;
                direction.Y *= aimDist;
                direction.X += Main.rand.NextFloat(-40f, 40f) * 0.02f;
                direction.Y += Main.rand.NextFloat(-40f, 40f) * 0.02f;
                direction.Y *= -1;
                // 上方来源点与速度：关于对称轴镜像（复用下方同一组随机偏移，保证上下严格对称）
                Vector2 mirroredSource = player.Center + MirrorAcross(source - player.Center, axisDir);
                // 防止镜像来源点出世界顶部/底部，clamp 到世界范围内
                mirroredSource.Y = MathHelper.Clamp(mirroredSource.Y, 16f, Main.maxTilesY * 16f - 16f);
                Vector2 mirroredDirection = MirrorAcross(direction, axisDir);
                // 下方 3 把
                Projectile.NewProjectile(source2, source, direction, ModContent.ProjectileType<DemonFork>(), damage, knockback, player.whoAmI);
                Projectile.NewProjectile(source2, source, direction, ModContent.ProjectileType<DemonFork>(), damage, knockback, player.whoAmI);
                Projectile.NewProjectile(source2, source, direction, ModContent.ProjectileType<DemonFork>(), damage, knockback, player.whoAmI);
                // 上方 3 把
                Projectile.NewProjectile(source2, mirroredSource, mirroredDirection, ModContent.ProjectileType<DemonFork>(), damage, knockback, player.whoAmI);
                Projectile.NewProjectile(source2, mirroredSource, mirroredDirection, ModContent.ProjectileType<DemonFork>(), damage, knockback, player.whoAmI);
                Projectile.NewProjectile(source2, mirroredSource, mirroredDirection, ModContent.ProjectileType<DemonFork>(), damage, knockback, player.whoAmI);
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
        /// 命中敌人：施加暗影烈焰（450 帧）与着火（900 帧）debuff，
        /// 追加 4 倍面板伤害（口径与 PvP 分支一致），并生成暗影束粉尘爆发。
        /// </summary>
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 修正死代码：原 150/300 两行会被后面的 450/900 覆盖，属冗余，删除
            target.AddBuff(BuffID.ShadowFlame, 450);
            target.AddBuff(BuffID.OnFire, 900);
            // 追加伤害与 PvP 分支口径一致：同为 Item.damage*4（裸面板，不二次乘职业加成），
            // 用显式 StrikeNPC 施加，与 PvP 的 Player.Hurt 数值完全对齐
            target.StrikeNPC(target.CalculateHitInfo(Item.damage * 4, hit.HitDirection));
            float firstDustScale = 1.7f;
            float secondDustScale = 0.8f;
            float thirdDustScale = 2f;
            Vector2 dustRotation = (target.rotation - MathHelper.PiOver2).ToRotationVector2();
            Vector2 dustVelocity = dustRotation * target.velocity.Length();
            SoundEngine.PlaySound(SoundID.Item14, target.Center);
            for (int i = 0; i < 40; i++)
            {
                int dustInt = Dust.NewDust(target.position, target.width, target.height, DustID.ShadowbeamStaff, 0f, 0f, 200, default, firstDustScale);
                Dust dust = Main.dust[dustInt];
                dust.position = target.Center + Vector2.UnitY.RotatedByRandom(Math.PI) * Main.rand.NextFloat() * target.width / 2f;
                dust.noGravity = true;
                dust.velocity.Y -= 4.5f;
                dust.velocity *= 3f;
                dust.velocity += dustVelocity * Main.rand.NextFloat();
                dustInt = Dust.NewDust(target.position, target.width, target.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default, secondDustScale);
                dust.position = target.Center + Vector2.UnitY.RotatedByRandom(Math.PI) * Main.rand.NextFloat() * target.width / 2f;
                dust.velocity.Y -= 3f;
                dust.velocity *= 2f;
                dust.noGravity = true;
                dust.fadeIn = 1f;
                dust.color = Color.Crimson * 0.5f;
                dust.velocity += dustVelocity * Main.rand.NextFloat();
            }
            for (int j = 0; j < 20; j++)
            {
                int dustInt = Dust.NewDust(target.position, target.width, target.height, DustID.ShadowbeamStaff, 0f, 0f, 0, default, thirdDustScale);
                Dust dust = Main.dust[dustInt];
                dust.position = target.Center + Vector2.UnitX.RotatedByRandom(Math.PI).RotatedBy((double)target.velocity.ToRotation(), default) * target.width / 3f;
                dust.noGravity = true;
                dust.velocity.Y -= 1.5f;
                dust.velocity *= 0.5f;
                dust.velocity += dustVelocity * (0.6f + 0.6f * Main.rand.NextFloat());
            }
        }
        /// <summary>
        /// 命中玩家（PvP）：与 NPC 版逻辑一致——施加暗影烈焰/着火并追加 4 倍面板伤害；
        /// 因 PvP 目标此时带免疫帧，先清零免疫时间再以 Player.Hurt 施加追加伤害。
        /// </summary>
        public override void OnHitPvp(Player player, Player target, Player.HurtInfo hurtInfo)
        {
            // 与 NPC 版一致：debuff（已删冗余）+ 4 倍追加伤害（PvP 目标为玩家，用 Player.Hurt 施加）
            target.AddBuff(BuffID.ShadowFlame, 450);
            target.AddBuff(BuffID.OnFire, 900);
            // PvP 命中后目标处于免疫帧，清除后追加伤害才能稳定生效（与 NPC 版直接 StrikeNPC 语义一致）
            target.immuneTime = 0;
            target.Hurt(new PlayerDeathReason { SourcePlayerIndex = player.whoAmI }, Item.damage * 4, player.direction, true);
            float firstDustScale = 1.7f;
            float secondDustScale = 0.8f;
            float thirdDustScale = 2f;
            // 玩家无 rotation 属性，用面朝方向替代 NPC 版的朝向向量
            Vector2 dustRotation = Vector2.UnitX * target.direction;
            Vector2 dustVelocity = dustRotation * target.velocity.Length();
            SoundEngine.PlaySound(SoundID.Item14, target.Center);
            for (int i = 0; i < 40; i++)
            {
                int dustInt = Dust.NewDust(target.position, target.width, target.height, DustID.ShadowbeamStaff, 0f, 0f, 200, default, firstDustScale);
                Dust dust = Main.dust[dustInt];
                dust.position = target.Center + Vector2.UnitY.RotatedByRandom(Math.PI) * Main.rand.NextFloat() * target.width / 2f;
                dust.noGravity = true;
                dust.velocity.Y -= 4.5f;
                dust.velocity *= 3f;
                dust.velocity += dustVelocity * Main.rand.NextFloat();
                dustInt = Dust.NewDust(target.position, target.width, target.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default, secondDustScale);
                dust.position = target.Center + Vector2.UnitY.RotatedByRandom(Math.PI) * Main.rand.NextFloat() * target.width / 2f;
                dust.velocity.Y -= 3f;
                dust.velocity *= 2f;
                dust.noGravity = true;
                dust.fadeIn = 1f;
                dust.color = Color.Crimson * 0.5f;
                dust.velocity += dustVelocity * Main.rand.NextFloat();
            }
            for (int j = 0; j < 20; j++)
            {
                int dustInt = Dust.NewDust(target.position, target.width, target.height, DustID.ShadowbeamStaff, 0f, 0f, 0, default, thirdDustScale);
                Dust dust = Main.dust[dustInt];
                dust.position = target.Center + Vector2.UnitX.RotatedByRandom(Math.PI).RotatedBy((double)target.velocity.ToRotation(), default) * target.width / 3f;
                dust.noGravity = true;
                dust.velocity.Y -= 1.5f;
                dust.velocity *= 0.5f;
                dust.velocity += dustVelocity * (0.6f + 0.6f * Main.rand.NextFloat());
            }
        }
        /// <summary>
        /// 配方（分版本）：灾厄之刃 + 崇高誓言之刃 + 星辉矿锭/暗黑碎片等月后材料
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient<Devastation>();
                recipe.AddIngredient<ExaltedOathblade>();
                recipe.AddIngredient(calamity.Find<ModItem>("CosmiliteBar").Type, 8);
                recipe.AddIngredient(calamity.Find<ModItem>("DarksunFragment").Type, 20);
                recipe.AddTile(calamity.Find<ModTile>("CosmicAnvil").Type);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient<Devastation>();
                recipe1.AddIngredient<ExaltedOathblade>();
                recipe1.AddIngredient(calamity1.Find<ModItem>("CosmiliteBar").Type, 5);
                recipe1.AddIngredient(calamity1.Find<ModItem>("Phantoplasm").Type, 5);
                recipe1.AddTile(calamity1.Find<ModTile>("DraedonsForge").Type);
                recipe1.Register();
            }
        }
    }
}
