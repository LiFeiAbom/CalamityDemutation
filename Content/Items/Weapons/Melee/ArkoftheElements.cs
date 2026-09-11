using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Content.Projectiles.Melee;
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
    /// 元素方舟 - 四元素近战剑
    /// 挥砍发射永恒光束，并散射 4 颗自动追踪的元素球（ElementBall）；
    /// 命中时施加多种元素 debuff。
    /// </summary>
    internal class ArkoftheElements:ModItem
    {
        /// <summary>
        /// 图鉴研究解锁数量显式设为 1（该武器研究后即可解锁）
        /// </summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>
        /// 物品基础属性：伤害 180、使用时间 18 帧、击退 8.5、红色稀有度；
        /// 主弹幕为永恒光束（EonBeam），每次挥砍发射一次，月后稀有度 12。
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 84;
            Item.damage = 180;
            Item.DamageType = DamageClass.Melee/* tModPorter Suggestion: Consider MeleeNoSpeed for no attack speed scaling */;
            Item.useAnimation = 18;
            Item.useTime = 18;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 8.5f;
            Item.UseSound = SoundID.Item60;
            Item.autoReuse = true;
            Item.height = 84;
            Item.value = Item.buyPrice(1, 20, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.shoot = ModContent.ProjectileType<EonBeam>();
            Item.shootSpeed = 10f;
            Item.shootsEveryUse = true;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 12;
        }
        /// <summary>
        /// 射击逻辑：主弹幕为永恒光束，再朝鼠标方向散射 4 颗自动追踪元素球。
        /// 注意：鼠标相对玩家的方向向量应为 (鼠标世界坐标 - 玩家锚点)，
        /// 此处用 "-" 修正了原实现误用 "+" 导致的方向计算错误。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile proj = Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, Main.myPlayer);
            proj.timeLeft = 160;
            proj.tileCollide = false;
            proj.ai[1] = Main.rand.Next(1, 5);
            float num72 = Main.rand.Next(22, 30);
            Vector2 vector2 = player.RotatedRelativePoint(player.MountedCenter, true);
            float num78 = Main.mouseX + Main.screenPosition.X - vector2.X;
            float num79 = Main.mouseY + Main.screenPosition.Y - vector2.Y;
            if (player.gravDir == -1f)
            {
                num79 = Main.screenPosition.Y + Main.screenHeight - Main.mouseY - vector2.Y;
            }
            float num80 = (float)Math.Sqrt((double)(num78 * num78 + num79 * num79));
            if (float.IsNaN(num78) && float.IsNaN(num79) || num78 == 0f && num79 == 0f)
            {
                num78 = player.direction;
                num79 = 0f;
                num80 = num72;
            }
            else
            {
                num80 = num72 / num80;
            }
            int num107 = 4;
            for (int num108 = 0; num108 < num107; num108++)
            {
                vector2 = new Vector2(player.position.X + player.width * 0.5f + Main.rand.Next(201) * -player.direction + (Main.mouseX + Main.screenPosition.X - player.position.X), player.MountedCenter.Y - 600f);
                vector2.X = (vector2.X + player.Center.X) / 2f;
                vector2.Y -= 100 * num108;
                num78 = Main.mouseX + Main.screenPosition.X - vector2.X;
                num79 = Main.mouseY + Main.screenPosition.Y - vector2.Y;
                num80 = (float)Math.Sqrt((double)(num78 * num78 + num79 * num79));
                num80 = num72 / num80;
                num78 *= num80;
                num79 *= num80;
                float speedX4 = num78 + Main.rand.Next(-360, 361) * 0.02f;
                float speedY5 = num79 + Main.rand.Next(-360, 361) * 0.02f;
                Projectile.NewProjectile(source, vector2, new Vector2(speedX4, speedY5), ModContent.ProjectileType<ElementBall>(), damage / 2, knockback, player.whoAmI, 0f, Main.rand.Next(3));
            }
            return false;
        }
        /// <summary>
        /// 挥砍特效：修正挥舞位置，偶尔生成彩虹色无重力粉尘
        /// </summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.NextBool(5))
            {
                int num250 = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.RainbowTorch, player.direction * 2, 0f, 150, new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB), 1.3f);
                Main.dust[num250].velocity *= 0.2f;
                Main.dust[num250].noGravity = true;
            }
        }
        /// <summary>
        /// 命中敌人：施加霜冻以及灾厄的圣焰/硫磺火/瘟疫 debuff
        /// </summary>
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn, 120);
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
            {
                target.AddBuff(calamity0.Find<ModBuff>("HolyFlames").Type, 120);
                target.AddBuff(calamity0.Find<ModBuff>("BrimstoneFlames").Type, 120);
                target.AddBuff(calamity0.Find<ModBuff>("Plague").Type, 120);
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                target.AddBuff(calamity1.Find<ModBuff>("HolyLight").Type, 120);
                target.AddBuff(calamity1.Find<ModBuff>("BrimstoneFlames").Type, 120);
                target.AddBuff(calamity1.Find<ModBuff>("Plague").Type, 120);
            }
        }
        /// <summary>
        /// 命中玩家（PvP）：同样施加霜冻与三种灾厄 debuff
        /// </summary>
        public override void OnHitPvp(Player player, Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.Frostburn, 120);
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity0))
            {
                target.AddBuff(calamity0.Find<ModBuff>("HolyFlames").Type, 120);
                target.AddBuff(calamity0.Find<ModBuff>("BrimstoneFlames").Type, 120);
                target.AddBuff(calamity0.Find<ModBuff>("Plague").Type, 120);
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                target.AddBuff(calamity1.Find<ModBuff>("HolyLight").Type, 120);
                target.AddBuff(calamity1.Find<ModBuff>("BrimstoneFlames").Type, 120);
                target.AddBuff(calamity1.Find<ModBuff>("Plague").Type, 120);
            }
        }
        /// <summary>
        /// 武器暴击率 +10%（放在此回调而非 SetDefaults，避免原版对高暴击值的异常处理）
        /// </summary>
        public override void ModifyWeaponCrit(Player player, ref float crit) => crit += 10;
        /// <summary>
        /// 配方（分版本）：真·远古方舟 + 银河奇点 + 灾厄核心 + 生命合金/生命锭 + 月亮锭
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient<TrueArkoftheAncients>();
                recipe.AddIngredient<GalacticaSingularity>(5);
                recipe.AddIngredient(calamity.Find<ModItem>("CoreofCalamity").Type, 5);
                recipe.AddIngredient(calamity.Find<ModItem>("LifeAlloy").Type, 5);
                recipe.AddIngredient(ItemID.LunarBar, 5);
                recipe.AddTile(TileID.LunarCraftingStation);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient<TrueArkoftheAncients>();
                recipe1.AddIngredient(calamity1.Find<ModItem>("GalacticaSingularity").Type, 5);
                recipe1.AddIngredient(calamity1.Find<ModItem>("CoreofCalamity").Type, 5);
                recipe1.AddIngredient(calamity1.Find<ModItem>("BarofLife").Type, 10);
                recipe1.AddIngredient(ItemID.LunarBar, 15);
                recipe1.AddTile(TileID.LunarCraftingStation);
                recipe1.Register();
            }
        }
    }
}
