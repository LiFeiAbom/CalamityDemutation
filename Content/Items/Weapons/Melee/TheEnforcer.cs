using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 暴政（The Enforcer）—— 月后近战巨剑，整把移植自灾厄 2.0.3.9 的 <c>Items/Weapons/Melee/TheEnforcer.cs</c>。
    /// <para>
    /// 与源的唯一差异（用户点名）：源把「喷火」挂在 <c>OnHitNPC</c> / <c>OnHitPvp</c> 上——要先打中敌人，
    /// 才在玩家附近随机撒 5 枚 <c>EssenceFlame2</c>（那两段还是逐行重复的同一份代码）；
    /// 本工程改成**发射逻辑**：每次挥砍（<see cref="Shoot"/>）不求命中，直接在鼠标处炸开一圈共
    /// <see cref="FlamesPerBarrage"/> 枚追踪火焰，火焰伤害仍取面板的 25%。
    /// 其余数值照搬源：100×100 贴图、缩放 1.5、伤害 890、击退 9、必击退/可转向、
    /// 使用音 Item20、1 铂 40 金（源的 <c>RarityDarkBlueBuyPrice</c>）、月后稀有度 14
    /// （源的 <c>Rarities/DarkBlue</c> 颜色为 (43,96,222)，与本工程 14 档完全一致，故不换算）；
    /// 世界中的物品同样带 <c>TheEnforcerGlow</c> 发光蒙版。
    /// 使用时间与挥舞动画源为 17 帧，用户 2026-10-05 指定改为 14 帧（两项一起）。
    /// </para>
    /// <para>
    /// 数值膨胀（用户 2026-10-03 点名，同一开关三项联动）：面板 890 → **2200**
    /// （<see cref="InflatedDamage"/>），每次挥砍的火种 6 → **10**，单枚火焰伤害 25% → **75%**
    /// （三项都在运行时读 <c>ConfigSystem.StatInflationEnabled</c>，游戏内切换即时生效，不必重召/重进）；
    /// 膨胀开启时 tooltip 正文换成 <c>Items.TheEnforcer.TooltipInflated</c>，见 <see cref="ModifyTooltips"/>。
    /// </para>
    /// <para>
    /// 配方双版本各一条（两版灾厄都软依赖，缺料即不注册）：
    /// 现代版＝宇宙锭×12 @ 宇宙铁砧；经典版＝宇宙锭×15 @ 嘉登熔炉。
    /// </para>
    /// </summary>
    internal class TheEnforcer : ModItem
    {
        /// <summary>常态每次挥砍在鼠标处释放的火焰枚数（源的 OnHit 一次性撒 5 枚，这里按用户「大量火焰」的口径取 6）</summary>
        private const int BaseFlamesPerBarrage = 6;
        /// <summary>数值膨胀开启时每次挥砍释放的火焰枚数（用户 2026-10-03 指定：6 → 10）</summary>
        private const int InflatedFlamesPerBarrage = 10;
        /// <summary>常态单枚火焰的伤害占面板基础伤害的比例（源为 25%）</summary>
        private const float BaseFlameDamageRatio = 0.25f;
        /// <summary>数值膨胀开启时单枚火焰的伤害占比（用户 2026-10-03 指定：25% → 75%）</summary>
        private const float InflatedFlameDamageRatio = 0.75f;
        /// <summary>数值膨胀后的面板伤害（用户 2026-10-03 指定：暴政 890 → 2200）</summary>
        private const float InflatedDamage = 2200f;
        /// <summary>当前生效的火焰枚数（运行时读配置，游戏内切换即时生效）</summary>
        private static int FlamesPerBarrage => ConfigSystem.StatInflationEnabled ? InflatedFlamesPerBarrage : BaseFlamesPerBarrage;
        /// <summary>当前生效的单枚火焰伤害占比（运行时读配置）</summary>
        private static float FlameDamageRatio => ConfigSystem.StatInflationEnabled ? InflatedFlameDamageRatio : BaseFlameDamageRatio;
        /// <summary>
        /// 当前生效的面板基础伤害：膨胀开关开启时用 <see cref="InflatedDamage"/>，否则维持 <c>Item.damage</c> 的源值 890。
        /// 按面板比例派生的火焰伤害也读这里，避免出现「面板涨了、火焰没涨」。
        /// </summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>火焰迸发时的初始径向速度下限</summary>
        private const float MinFlameSpeed = 2.5f;
        /// <summary>火焰迸发时的初始径向速度上限</summary>
        private const float MaxFlameSpeed = 6f;
        /// <summary>唯一武器，研究解锁 1 个</summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>
        /// 物品基础属性：100×100、缩放 1.5、伤害 890、14 帧使用与挥舞（用户 2026-10-05 指定：源 17 → 14）、击退 9、可转向、
        /// 红色基础稀有度 + 月后稀有度 14（真正的名字颜色由全局物品覆盖）；
        /// 主弹幕挂 <see cref="EssenceFlame2"/>，只为让 <see cref="Shoot"/> 被调用（实际生成数由那里决定）。
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 100;
            Item.height = 100;
            Item.scale = 1.5f;
            Item.damage = 890;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = Item.useTime = 14;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTurn = true;
            Item.knockBack = 9f;
            Item.UseSound = SoundID.Item20;
            Item.autoReuse = true;
            Item.value = Item.buyPrice(1, 40, 0, 0);
            Item.rare = ItemRarityID.Red;
            Item.shoot = ModContent.ProjectileType<EssenceFlame2>();
            Item.shootSpeed = 9f;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 14;
        }
        /// <summary>数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>（运行时读配置，游戏内切换即时生效）</summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base = BaseDamage;
        }
        /// <summary>
        /// 数值膨胀开启时把说明正文整段换成 <c>Items.TheEnforcer.TooltipInflated</c>
        /// （本地化键写的是膨胀口径：十枚火焰、每枚 75%），关掉则维持原正文的 25%。
        /// </summary>
        public override void ModifyTooltips(List<TooltipLine> tooltips) => tooltips.ApplyInflatedTooltip(this);
        /// <summary>
        /// 发射逻辑（替代源的 OnHit 撒火）：每次挥砍直接在<strong>鼠标处</strong>炸开一圈火焰——
        /// 火种沿圆周均匀分布（角度加一点随机抖动）、生成点离准心 12~52 像素、初速沿径向外扩
        /// （2.5~6 像素/帧）并额外叠一点「朝准心方向」的偏置速度，看起来既像在准心处爆开、又整体朝瞄准方向推出去。
        /// 火焰伤害取面板的 25%（数值膨胀开启时改成 75%），走 <c>GetTotalDamage</c> 预乘玩家加成后一次性算进生成包。
        /// 音效把源的 Item73 从「命中时」前移到「出火时」，同时补一圈暗影束尘（<c>DustID.ShadowbeamStaff</c>，即源里写的 173 号尘）。
        /// 返回 false：不让原版按 <c>Item.shoot</c> 再补一发单弹。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 mouseWorld = Main.MouseWorld;
            Vector2 playerCenter = player.RotatedRelativePoint(player.MountedCenter, true);
            Vector2 aimDirection = (mouseWorld - playerCenter).SafeNormalize(Vector2.UnitX * player.direction);
            SoundEngine.PlaySound(SoundID.Item73, mouseWorld);
            int flameDamage = (int)player.GetTotalDamage(DamageClass.Melee).ApplyTo(FlameDamageRatio * BaseDamage);
            int flameType = ModContent.ProjectileType<EssenceFlame2>();
            for (int i = 0; i < FlamesPerBarrage; i++)
            {
                float angle = MathHelper.TwoPi * i / FlamesPerBarrage + Main.rand.NextFloat(-0.35f, 0.35f);
                Vector2 burstDirection = angle.ToRotationVector2();
                Vector2 spawnPosition = mouseWorld + burstDirection * Main.rand.NextFloat(12f, 52f);
                Vector2 flameVelocity = burstDirection * Main.rand.NextFloat(MinFlameSpeed, MaxFlameSpeed) + aimDirection * 1.25f;
                // ai1 照源传 Main.rand.Next(3)（源的弹幕没读这个值，留作绘制多样性）
                Projectile.NewProjectile(source, spawnPosition, flameVelocity, flameType, flameDamage, 0f, player.whoAmI, 0f, Main.rand.Next(3));
            }
            for (int d = 0; d < 14; d++)
            {
                int dust = Dust.NewDust(mouseWorld - Vector2.One * 20f, 40, 40, DustID.ShadowbeamStaff, 0f, 0f, 100, default, 1.8f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 2.5f;
            }
            return false;
        }
        /// <summary>挥砍表现：BetterSwing 修正挥舞位置（本工程近战挥舞武器的惯例），并按源洒暗影束尘（源写的 173 号）</summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.NextBool(3))
            {
                Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.ShadowbeamStaff);
            }
        }
        /// <summary>世界中的物品按源的 <c>TheEnforcerGlow</c> 画发光蒙版</summary>
        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Item.DrawItemGlowmaskSingleFrame(spriteBatch, rotation, ModContent.Request<Texture2D>(Texture + "Glow").Value);
        }
        /// <summary>
        /// 配方双版本各一条：现代版宇宙锭×12 @ 宇宙铁砧（源 2.0.3.9）；
        /// 经典版宇宙锭×15 @ 嘉登熔炉（灾厄经典 1.4.2.101 的同名武器配方）。
        /// 两版都走软依赖，材料或站台查不到就不注册。
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity)
                && calamity.TryFind<ModItem>("CosmiliteBar", out ModItem modernCosmiliteBar)
                && calamity.TryFind<ModTile>("CosmicAnvil", out ModTile cosmicAnvil))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(modernCosmiliteBar.Type, 12);
                recipe.AddTile(cosmicAnvil.Type);
                recipe.Register();
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod classic)
                && classic.TryFind<ModItem>("CosmiliteBar", out ModItem classicCosmiliteBar)
                && classic.TryFind<ModTile>("DraedonsForge", out ModTile draedonsForge))
            {
                Recipe recipeClassic = CreateRecipe();
                recipeClassic.AddIngredient(classicCosmiliteBar.Type, 15);
                recipeClassic.AddTile(draedonsForge.Type);
                recipeClassic.Register();
            }
        }
    }
}
