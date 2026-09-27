using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 元素圣剑（ElementalExcalibur） - 本模组的终盘近战剑。
    /// 普通挥砍并发射彩虹光束（<see cref="ElementalExcaliburBeam"/>），光束效果取决于颜色。
    /// <para>
    /// 获取：真·断钢剑 + 审判之剑（GreatswordofJudgement）+ 暗影焰锭×5，在德雷顿熔炉合成（两版灾厄均同配方）。
    /// </para>
    /// </summary>
    internal class ElementalExcalibur : ModItem
    {
        /// <summary>左键彩虹光束的颜色编号：每次发射递增、0~11 循环，作为 ai[0] 传给光束弹幕选色</summary>
        private int BeamType = 0;
        /// <summary>挥砍粉尘的颜色透明度（NewDust 的 alpha 参数，越大越淡）</summary>
        private const int alpha = 50;
        /// <summary>
        /// 静态属性：显式指定图鉴研究解锁数量为 1
        /// </summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }
        /// <summary>
        /// 基础属性：112×112 贴图、伤害 4000、14 帧挥砍、击退 8、暴击额外 +10%（见 <see cref="ModifyWeaponCrit"/>）、
        /// 主弹幕为彩虹光束（<see cref="ElementalExcaliburBeam"/>）、月后稀有度 16。
        /// </summary>
        public override void SetDefaults()
        {
            Item.damage = 4000;                       // 终盘档伤害
            Item.useAnimation = 14;                   // 动画时长 14 帧
            Item.useStyle = ItemUseStyleID.Swing;     // 挥砍姿势
            Item.useTurn = false;                     // 挥砍时不随鼠标转向
            Item.useTime = 14;                        // 使用间隔 14 帧
            Item.DamageType = DamageClass.Melee;      // 近战伤害
            Item.knockBack = 8f;                      // 击退 8
            Item.UseSound = SoundID.Item1;            // 挥砍音
            Item.autoReuse = true;                    // 自动挥舞
            Item.width = 112;                         // 贴图宽（像素）
            Item.height = 112;                        // 贴图高（像素）
            Item.value = Item.buyPrice(1, 50, 0, 0);  // 价值 1 铂金 50 金
            Item.shoot = ModContent.ProjectileType<ElementalExcaliburBeam>(); // 左键主弹幕：彩虹光束
            Item.shootSpeed = 12f;                    // 弹幕初速
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 16;
        }
        /// <summary>武器暴击率额外 +10%（原版对 SetDefaults 里的高暴击值处理异常，故放到此回调）</summary>
        public override void ModifyWeaponCrit(Player player, ref float crit) => crit += 10;
        /// <summary>
        /// 射击逻辑：发射彩虹光束（<see cref="BeamType"/> 每发递增、0~11 循环，作为 ai[0] 传给光束弹幕选色）
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, position.X, position.Y, velocity.X, velocity.Y, type, damage, knockback, player.whoAmI, BeamType, 0f);
            BeamType++;
            if (BeamType > 11)
                BeamType = 0;
            return false;
        }
        /// <summary>
        /// 挥砍特效：修正武器挥舞位置，偶尔按当前 <see cref="BeamType"/> 颜色生成一颗无重力的彩虹粉尘
        /// </summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            CDUtil.BetterSwing(player);
            if (Main.rand.NextBool(4))
            {
                Color color = new Color(255, 0, 0, alpha);  // 默认红（case 0 直接用此值）
                switch (BeamType)
                {
                    case 0: // 红
                        break;
                    case 1: // 橙
                        color = new Color(255, 128, 0, alpha);
                        break;
                    case 2: // 黄
                        color = new Color(255, 255, 0, alpha);
                        break;
                    case 3: // 青柠
                        color = new Color(128, 255, 0, alpha);
                        break;
                    case 4: // 绿
                        color = new Color(0, 255, 0, alpha);
                        break;
                    case 5: // 蓝绿
                        color = new Color(0, 255, 128, alpha);
                        break;
                    case 6: // 青
                        color = new Color(0, 255, 255, alpha);
                        break;
                    case 7: // 浅蓝
                        color = new Color(0, 128, 255, alpha);
                        break;
                    case 8: // 蓝
                        color = new Color(0, 0, 255, alpha);
                        break;
                    case 9: // 紫
                        color = new Color(128, 0, 255, alpha);
                        break;
                    case 10: // 品红
                        color = new Color(255, 0, 255, alpha);
                        break;
                    case 11: // 亮粉
                        color = new Color(255, 0, 128, alpha);
                        break;
                    default:
                        break;
                }
                Dust dust24 = Main.dust[Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, DustID.RainbowMk2, 0f, 0f, alpha, color, 1.2f)];
                dust24.noGravity = true;  // 彩虹粉尘不受重力，避免下落
            }
        }
        /// <summary>掉落在地上时绘制 Glow 发光层</summary>
        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Item.DrawItemGlowmaskSingleFrame(spriteBatch, rotation, ModContent.Request<Texture2D>("CalamityDemutation/Content/Items/Weapons/Melee/ElementalExcaliburGlow").Value);
        }
        /// <summary>
        /// 命中敌人：真近战回血 10~12，并按版本施加灾厄减益——现代版上 VulnerabilityHex / MiracleBlight / Dragonfire，
        /// 经典版上 DemonFlames / GodSlayerInferno / HolyLight，各 600 帧（10 秒）
        /// </summary>
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 真近战命中回血：超上限的部分夹回最大值，否则血条会先冲高再被原版夹回，出现血量跳变
            int healAmount = Main.rand.Next(3) + 10;
            player.statLife += healAmount;
            player.HealEffect(healAmount);
            if (player.statLife > player.statLifeMax2)
                player.statLife = player.statLifeMax2;
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModBuff>("VulnerabilityHex", out ModBuff vulnerabilityHex))
                    target.AddBuff(vulnerabilityHex.Type, 600);
                if (calamity.TryFind<ModBuff>("MiracleBlight", out ModBuff miracleBlight))
                    target.AddBuff(miracleBlight.Type, 600);
                if (calamity.TryFind<ModBuff>("Dragonfire", out ModBuff dragonfire))
                    target.AddBuff(dragonfire.Type, 600);
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModBuff>("DemonFlames", out ModBuff demonFlames))
                    target.AddBuff(demonFlames.Type, 600);
                if (calamity1.TryFind<ModBuff>("GodSlayerInferno", out ModBuff godSlayerInferno))
                    target.AddBuff(godSlayerInferno.Type, 600);
                if (calamity1.TryFind<ModBuff>("HolyLight", out ModBuff holyLight))
                    target.AddBuff(holyLight.Type, 600);
            }
        }
        /// <summary>命中玩家（PvP）：与 NPC 版逻辑一致——回血 10~12 并施加对应版本的灾厄减益，各 600 帧</summary>
        public override void OnHitPvp(Player player, Player target, Player.HurtInfo hurtInfo)
        {
            // 同 OnHitNPC：真近战命中回血，超上限则夹回最大值
            int healAmount = Main.rand.Next(3) + 10;
            player.statLife += healAmount;
            player.HealEffect(healAmount);
            if (player.statLife > player.statLifeMax2)
                player.statLife = player.statLifeMax2;
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModBuff>("VulnerabilityHex", out ModBuff vulnerabilityHex))
                    target.AddBuff(vulnerabilityHex.Type, 600);
                if (calamity.TryFind<ModBuff>("MiracleBlight", out ModBuff miracleBlight))
                    target.AddBuff(miracleBlight.Type, 600);
                if (calamity.TryFind<ModBuff>("Dragonfire", out ModBuff dragonfire))
                    target.AddBuff(dragonfire.Type, 600);
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModBuff>("DemonFlames", out ModBuff demonFlames))
                    target.AddBuff(demonFlames.Type, 600);
                if (calamity1.TryFind<ModBuff>("GodSlayerInferno", out ModBuff godSlayerInferno))
                    target.AddBuff(godSlayerInferno.Type, 600);
                if (calamity1.TryFind<ModBuff>("HolyLight", out ModBuff holyLight))
                    target.AddBuff(holyLight.Type, 600);
            }
        }
        /// <summary>配方（两版同配方）：真·断钢剑 + 审判之剑 + 暗影焰锭×5，在德雷顿熔炉合成</summary>
        public override void AddRecipes()
        {
            // 现代版灾厄：德雷顿熔炉（DraedonsForge）
            if(ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.TrueExcalibur);                    // 真·断钢剑
                recipe.AddIngredient<GreatswordofJudgement>();                 // 审判之剑（本模组移植物）
                recipe.AddIngredient(calamity.Find<ModItem>("ShadowspecBar").Type, 5); // 灾厄材料：暗影焰锭 ×5
                recipe.AddTile(calamity.Find<ModTile>("DraedonsForge").Type);  // 德雷顿熔炉
                recipe.Register();
            }
            // 经典版灾厄：同名熔炉不同物，分开注册
            if(ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                Recipe recipe1 = CreateRecipe();
                recipe1.AddIngredient(ItemID.TrueExcalibur);
                recipe1.AddIngredient<GreatswordofJudgement>();
                recipe1.AddIngredient(calamity1.Find<ModItem>("ShadowspecBar").Type, 5);
                recipe1.AddTile(calamity1.Find<ModTile>("DraedonsForge").Type);
                recipe1.Register();
            }
        }
    }
}
