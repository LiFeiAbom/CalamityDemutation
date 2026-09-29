using CalamityDemutation.Content.Items.Materials;
using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Players;
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
    /// 星河之刃（GalactusBlade）—— 照搬灾厄 2.0.4 的 <c>Items/Weapons/Melee/GalactusBlade.cs</c>：
    /// 60×58、伤害 84、17 帧使用与挥舞、击退 6、射速 23，每次挥砍从玩家上方 600 像素处洒下 <b>5 颗</b>星河彗星
    /// （逐颗再向上错开 100 像素、横向 ±200 抖动），命中挂 300 帧神圣火焰。
    /// <para>
    /// 与源的差异：① 稀有度按本工程口径写 <c>ItemRarityID.Red</c> + <c>postMoonLordRarity = 12</c>
    /// （源为 Turquoise 稀有度，正是 12 档）；② 价值照抄源的 <c>RarityTurquoiseBuyPrice</c>（2.0.4 实值 1 铂 50 金）；
    /// ③ 神圣火焰走软依赖施加；④ 补一份 PvP 命中（本工程约定）；⑤ 挥砍加 <c>CDUtil.BetterSwing</c>（本工程近战惯例）。
    /// </para>
    /// </summary>
    internal class GalactusBlade:ModItem
    {
        /// <summary>图鉴研究解锁数量：研究一次即可解锁</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>
        /// 物品基础属性：伤害 84、17 帧使用与挥舞、击退 6、射速 23、红名（月后稀有度 12）；
        /// 主弹幕为星河彗星（GalacticaComet），命中音效照抄源（SoundID.Item105）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 60;
            Item.height = 58;
            Item.damage = 84;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 17;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.knockBack = 6f;
            Item.UseSound = SoundID.Item105;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(1, 50, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.shoot = ModContent.ProjectileType<GalacticaComet>();
            Item.shootSpeed = 23f;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 12;
        }
        /// <summary>
        /// 射击逻辑（照搬源）：一次挥砍生成 5 颗彗星，每颗的出生点都在玩家上方 600 像素、按序号逐颗再抬高 100 像素，
        /// 速率朝鼠标方向归一化后带 <c>±100 * 0.02</c> 的随机抖动；每颗的 <c>ai[1]</c> 传 <c>0~9</c> 的随机数
        /// （弹幕侧据此决定是否追加发光效果）。返回 false 表示不走默认的单发发射。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            float cometSpeed = Item.shootSpeed;
            Vector2 realPlayerPos = player.RotatedRelativePoint(player.MountedCenter, true);
            float mouseXDist = (float)Main.mouseX + Main.screenPosition.X - realPlayerPos.X;
            float mouseYDist = (float)Main.mouseY + Main.screenPosition.Y - realPlayerPos.Y;
            if (player.gravDir == -1f)
            {
                mouseYDist = Main.screenPosition.Y + (float)Main.screenHeight - (float)Main.mouseY - realPlayerPos.Y;
            }
            float mouseDistance = (float)Math.Sqrt((double)(mouseXDist * mouseXDist + mouseYDist * mouseYDist));
            if ((float.IsNaN(mouseXDist) && float.IsNaN(mouseYDist)) || (mouseXDist == 0f && mouseYDist == 0f))
            {
                mouseXDist = (float)player.direction;
                mouseYDist = 0f;
                mouseDistance = cometSpeed;
            }
            else
            {
                mouseDistance = cometSpeed / mouseDistance;
            }
            for (int i = 0; i < 5; i++)
            {
                realPlayerPos = new Vector2(player.position.X + (float)player.width * 0.5f + (float)(Main.rand.Next(201) * -(float)player.direction) + ((float)Main.mouseX + Main.screenPosition.X - player.position.X), player.MountedCenter.Y - 600f);
                realPlayerPos.X = (realPlayerPos.X + player.Center.X) / 2f + (float)Main.rand.Next(-200, 201);
                realPlayerPos.Y -= (float)(100 * i);
                mouseXDist = (float)Main.mouseX + Main.screenPosition.X - realPlayerPos.X;
                mouseYDist = (float)Main.mouseY + Main.screenPosition.Y - realPlayerPos.Y;
                if (mouseYDist < 0f)
                {
                    mouseYDist *= -1f;
                }
                if (mouseYDist < 20f)
                {
                    mouseYDist = 20f;
                }
                mouseDistance = (float)Math.Sqrt((double)(mouseXDist * mouseXDist + mouseYDist * mouseYDist));
                mouseDistance = cometSpeed / mouseDistance;
                mouseXDist *= mouseDistance;
                mouseYDist *= mouseDistance;
                float speedX4 = mouseXDist + (float)Main.rand.Next(-100, 101) * 0.02f;
                float speedY5 = mouseYDist + (float)Main.rand.Next(-100, 101) * 0.02f;
                Projectile.NewProjectile(source, realPlayerPos.X, realPlayerPos.Y, speedX4, speedY5, ModContent.ProjectileType<GalacticaComet>(), damage, knockback, player.whoAmI, 0f, (float)Main.rand.Next(10));
            }
            return false;
        }
        /// <summary>
        /// 挥砍表现：先用 BetterSwing 修正挥舞位置（本工程近战挥舞武器的必备写法），
        /// 再约 1/4 概率在命中框内扬起一颗传送药水色或天柱色的尘（照搬源的粉尘）
        /// </summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.NextBool(4))
            {
                Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, Main.rand.NextBool() ? DustID.TeleportationPotion : DustID.Vortex);
            }
        }
        /// <summary>命中敌人：挂 300 帧神圣火焰（灾厄本家 debuff，缺该 buff 时静默跳过）</summary>
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone) => CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "HolyFlames", 300);
        /// <summary>命中玩家（PvP）：与 OnHitNPC 同构，挂同样的神圣火焰</summary>
        public override void OnHitPvp(Player player, Player target, Player.HurtInfo hurtInfo) => CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "HolyFlames", 300);
        /// <summary>
        /// 配方（分版本）：现代版照抄 2.0.4 源——狂星之怒 + 灾厄的 DivineGeode×10 + 银河奇点×5，在远古操纵台；
        /// 经典版镜像经典灾厄原配方——银河奇点取经典版灾厄的，另外多出灵气/力量之魂/夜明锭/暗黑碎块/光明碎块。
        /// 现代版的银河奇点沿用本工程口径，取本模组自有的 <c>GalacticaSingularity</c>。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("DivineGeode", out ModItem divineGeode))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(ItemID.StarWrath);
                    recipe.AddIngredient(divineGeode.Type, 10);
                    recipe.AddIngredient<GalacticaSingularity>(5);
                    recipe.AddTile(TileID.LunarCraftingStation);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic))
            {
                if (classic.TryFind<ModItem>("GalacticaSingularity", out ModItem galacticaSingularity))
                {
                    Recipe recipeClassic = CreateRecipe();
                    recipeClassic.AddIngredient(galacticaSingularity.Type, 5);
                    recipeClassic.AddIngredient(ItemID.StarWrath);
                    recipeClassic.AddIngredient(ItemID.Ectoplasm, 10);
                    recipeClassic.AddIngredient(ItemID.SoulofMight, 20);
                    recipeClassic.AddIngredient(ItemID.LunarBar, 5);
                    recipeClassic.AddIngredient(ItemID.DarkShard);
                    recipeClassic.AddIngredient(ItemID.LightShard);
                    recipeClassic.AddTile(TileID.LunarCraftingStation);
                    recipeClassic.Register();
                }
            }
        }
    }
}
