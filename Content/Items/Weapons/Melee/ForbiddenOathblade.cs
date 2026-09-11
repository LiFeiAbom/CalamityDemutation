using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 禁忌誓言之刃 - 困难模式近战剑，命中时造成追加伤害并生成大量粉尘
    /// </summary>
    internal class ForbiddenOathblade:ModItem
    {
        /// <summary>
        /// 物品基础属性：伤害 110、使用时间与使用动画同为 25 帧、击退 6.5、粉色稀有度；
        /// 主弹幕为崇高誓言之刃弹幕（ExaltedOathBladeProj），弹速 10。
        /// 与同系列其他剑不同，这里未设置 shootsEveryUse。
        /// </summary>
        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Melee;
            Item.width = 74;
            Item.height = 74;
            Item.damage = 110;
            Item.useAnimation = Item.useTime = 25;
            Item.useTurn = false;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6.5f;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(0, 36, 0, 0);
            Item.rare = ItemRarityID.Pink;
            Item.shoot = ModContent.ProjectileType<ExaltedOathBladeProj>();
            Item.shootSpeed = 10f;
        }
        /// <summary>
        /// 挥砍特效：修正挥舞位置并扬起暗影束法杖粉尘
        /// </summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            player.BetterSwing();
            if (Main.rand.NextBool(3))
                Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.ShadowbeamStaff);
        }
        /// <summary>
        /// 命中敌人：施加暗影烈焰与着火并造成追加伤害与大量粉尘
        /// </summary>
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.ShadowFlame, 360);
            target.AddBuff(BuffID.OnFire, 720);
            int onHitDamage = (int)player.GetTotalDamage<MeleeDamageClass>().ApplyTo(2 * Item.damage);
            // 用显式 StrikeNPC 施加，与 PvP 分支的 Player.Hurt 数值同源对齐（口径一致）
            target.StrikeNPC(target.CalculateHitInfo(onHitDamage, hit.HitDirection));
            float firstDustScale = 1.7f;
            float secondDustScale = 0.8f;
            float thirdDustScale = 2f;
            Vector2 dustRotation = (target.rotation - MathHelper.Pi).ToRotationVector2();
            Vector2 dustVelocity = dustRotation * target.velocity.Length();
            SoundEngine.PlaySound(SoundID.Item14, target.Center);
            // 步进写在循环头的规范写法，避免中途 continue 时走步失效导致死循环
            for (int i = 0; i < 40; i++)
            {
                int swingDust = Dust.NewDust(new Vector2(target.position.X, target.position.Y), target.width, target.height, DustID.ShadowbeamStaff, 0f, 0f, 200, default, firstDustScale);
                Dust dust = Main.dust[swingDust];
                dust.position = target.Center + Vector2.UnitY.RotatedByRandom(MathHelper.TwoPi) * (float)Main.rand.NextDouble() * target.width / 2f;
                dust.noGravity = true;
                dust.velocity.Y -= 4.5f;
                dust.velocity *= 3f;
                dust.velocity += dustVelocity * Main.rand.NextFloat();
                swingDust = Dust.NewDust(new Vector2(target.position.X, target.position.Y), target.width, target.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default, secondDustScale);
                dust.position = target.Center + Vector2.UnitY.RotatedByRandom(MathHelper.TwoPi) * (float)Main.rand.NextDouble() * target.width / 2f;
                dust.velocity.Y -= 3f;
                dust.velocity *= 2f;
                dust.noGravity = true;
                dust.fadeIn = 1f;
                dust.color = Color.Crimson * 0.5f;
                dust.velocity += dustVelocity * Main.rand.NextFloat();
            }
            for (int j = 0; j < 20; j++)
            {
                int swingDust2 = Dust.NewDust(new Vector2(target.position.X, target.position.Y), target.width, target.height, DustID.ShadowbeamStaff, 0f, 0f, 0, default, thirdDustScale);
                Dust dust = Main.dust[swingDust2];
                dust.position = target.Center + Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi).RotatedBy((double)target.velocity.ToRotation(), default) * target.width / 3f;
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
            target.AddBuff(BuffID.ShadowFlame, 360);
            target.AddBuff(BuffID.OnFire, 720);
            // PvP 追加伤害与 NPC 版同口径（2 倍武器伤害，经职业加成换算）；
            // PvP 目标为玩家，须清除其免疫帧后以 Player.Hurt 施加，追加伤害才能稳定生效
            int onHitDamage = (int)player.GetTotalDamage<MeleeDamageClass>().ApplyTo(2 * Item.damage);
            target.immuneTime = 0;
            target.Hurt(new PlayerDeathReason { SourcePlayerIndex = player.whoAmI }, onHitDamage, player.direction, true);
            float firstDustScale = 1.7f;
            float secondDustScale = 0.8f;
            float thirdDustScale = 2f;
            // 玩家无 rotation 属性，用面朝方向替代 NPC 版的朝向向量
            Vector2 dustRotation = Vector2.UnitX * target.direction;
            Vector2 dustVelocity = dustRotation * target.velocity.Length();
            SoundEngine.PlaySound(SoundID.Item14, target.Center);
            // 步进写在循环头的规范写法，避免中途 continue 时走步失效导致死循环
            for (int i = 0; i < 40; i++)
            {
                int swingDust = Dust.NewDust(new Vector2(target.position.X, target.position.Y), target.width, target.height, DustID.ShadowbeamStaff, 0f, 0f, 200, default, firstDustScale);
                Dust dust = Main.dust[swingDust];
                dust.position = target.Center + Vector2.UnitY.RotatedByRandom(MathHelper.TwoPi) * (float)Main.rand.NextDouble() * target.width / 2f;
                dust.noGravity = true;
                dust.velocity.Y -= 4.5f;
                dust.velocity *= 3f;
                dust.velocity += dustVelocity * Main.rand.NextFloat();
                swingDust = Dust.NewDust(new Vector2(target.position.X, target.position.Y), target.width, target.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default, secondDustScale);
                dust.position = target.Center + Vector2.UnitY.RotatedByRandom(MathHelper.TwoPi) * (float)Main.rand.NextDouble() * target.width / 2f;
                dust.velocity.Y -= 3f;
                dust.velocity *= 2f;
                dust.noGravity = true;
                dust.fadeIn = 1f;
                dust.color = Color.Crimson * 0.5f;
                dust.velocity += dustVelocity * Main.rand.NextFloat();
            }
            for (int j = 0; j < 20; j++)
            {
                int swingDust2 = Dust.NewDust(new Vector2(target.position.X, target.position.Y), target.width, target.height, DustID.ShadowbeamStaff, 0f, 0f, 0, default, thirdDustScale);
                Dust dust = Main.dust[swingDust2];
                dust.position = target.Center + Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi).RotatedBy((double)target.velocity.ToRotation(), default) * target.width / 3f;
                dust.noGravity = true;
                dust.velocity.Y -= 1.5f;
                dust.velocity *= 0.5f;
                dust.velocity += dustVelocity * (0.6f + 0.6f * Main.rand.NextFloat());
            }
        }
        /// <summary>
        /// 配方：刃冠誓约之剑 + 旧主誓约之剑 + 5 恐惧之魂，在秘银砧合成（两版灾厄共用）
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) || ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
               Recipe recipe = CreateRecipe();
               recipe.AddIngredient<BladecrestOathsword>();
               recipe.AddIngredient<OldLordOathsword>();
               recipe.AddIngredient(ItemID.SoulofFright, 5);
               recipe.AddTile(TileID.MythrilAnvil);
               recipe.Register();
            }
        }
    }
}
