using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Players;
using CalamityDemutation.Sounds;
using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 女妖之爪（BansheeHook） - 移植自灾厄大修（CalamityOverhaul）0.4.0.1.3 的重制版，
    /// 对应灾厄本体同名武器（现代版 2.0.3.9 / 经典版 cal-1.4.2.101，两者均为波尔提斯掉落）。
    /// <para>
    /// 重制内容：左键挥爪时先散射 4 枚哀怨之镰，再在收钩瞬间甩出惊惧巨镰；右键改为引导式蓄能，
    /// 蓄满后爪子跟手悬停、持续从鼠标位置拉出追杀的惊惧巨镰，蓄能耗尽自动收招（详见 BansheeHookProj）。
    /// </para>
    /// 数值取重制版的 220 伤害（源本体为 250），价值/稀有度沿用本体档（1 铂金 40 金、13 档荧光绿）。
    /// **无配方**：获取途径与源一致，由波尔提斯掉落，见 CalamityDemutationGlobalNPC.ModifyNPCLoot。
    /// </summary>
    internal class BansheeHook : ModItem
    {
        /// <summary>
        /// 物品基础属性：尺寸、伤害、使用方式、弹幕与稀有度
        /// </summary>
        public override void SetStaticDefaults()
        {
            ItemID.Sets.Spears[Type] = true;                              // 长矛类（影响长矛与饰品交互）
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;    // 允许连续右键（引导模式）
        }
        public override void SetDefaults()
        {
            Item.width = 120;                                       // 贴图宽（像素）
            Item.height = 108;                                      // 贴图高（像素）
            Item.damage = 220;                                      // 重制版面板伤害（源本体 250）
            Item.noMelee = true;                                    // 真近战关闭，伤害全由手持弹幕结算
            Item.noUseGraphic = true;                               // 不绘制物品本身的挥动贴图
            Item.channel = true;                                    // 允许按住右键引导
            Item.DamageType = DamageClass.Melee;                    // 近战
            Item.useAnimation = 21;
            Item.useTime = 21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 8.5f;
            Item.UseSound = SoundID.DD2_GhastlyGlaivePierce;        // 原版穿刺音
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<BansheeHookProj>();
            Item.shootSpeed = 42f;
            Item.value = Item.buyPrice(1, 40, 0, 0);                // 价值 1 铂金 40 金（本体档）
            Item.rare = ItemRarityID.Red;                           // 基础稀有度红
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 13;   // 月后稀有度 13（荧光绿）
        }
        /// <summary>
        /// 世界中的物品贴图加一层发光遮罩（本体做法，贴图取自 2.0.3.9）
        /// </summary>
        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            Item.DrawItemGlowmaskSingleFrame(spriteBatch, rotation, ModContent.Request<Texture2D>(Texture + "Glow").Value);
        }
        /// <summary>数值膨胀后的面板伤害（用户 2026-10-03 指定：220 → 250）。</summary>
        private const float InflatedDamage = 250f;
        /// <summary>当前生效的面板基础伤害：开关开则用膨胀值 250，否则维持 Item.damage 的源值 220。</summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>
        /// 面板伤害改走运行时回调（不要写进 SetDefaults，否则游戏内切开关不生效）。
        /// 派生伤害都取自 Projectile.damage / GetWeaponDamage(手持)，天然跟随本面板值。
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage) => damage.Base = BaseDamage;
        /// <summary>
        /// 同一时间只允许一把爪子在场（本体与重制版一致）
        /// </summary>
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
        /// <summary>
        /// 允许右键：右键进入引导模式（由手持弹幕的 ai[1] = 1 分支接管）
        /// </summary>
        public override bool AltFunctionUse(Player player) => true;
        /// <summary>
        /// 出手：先把速度按鼠标方向归一化到 shootSpeed，再带一个随机侧摆量 ai[0]；
        /// 右键额外播放两段特效音、清空蓄能并把 ai[1] 置 1 交给引导分支。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            float num82 = Main.mouseX + Main.screenPosition.X - position.X;
            float num83 = Main.mouseY + Main.screenPosition.Y - position.Y;
            if (player.gravDir == -1f)
            {
                num83 = Main.screenPosition.Y + Main.screenHeight - Main.mouseY - position.Y;
            }
            float num84 = (float)Math.Sqrt(num82 * num82 + num83 * num83);
            if ((float.IsNaN(num82) && float.IsNaN(num83)) || (num82 == 0f && num83 == 0f))
            {
                num82 = player.direction;
                num83 = 0f;
                num84 = Item.shootSpeed;
            }
            else
            {
                num84 = Item.shootSpeed / num84;
            }
            num82 *= num84;
            num83 *= num84;
            float ai4 = Main.rand.NextFloat() * Item.shootSpeed * 0.75f * player.direction;
            velocity = new Vector2(num82, num83);
            int proj = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, ai4);
            if (player.altFunctionUse == 2)
            {
                SoundEngine.PlaySound(CalamityDemutationSounds.MeatySlashSound, player.Center);
                SoundEngine.PlaySound(CalamityDemutationSounds.BloodflareRangerActivation, player.Center);
                player.GetModPlayer<CalamityDemutationPlayer>().bansheeHookCharge = 0f;   // 每次重新引导都从零蓄能
                Main.projectile[proj].ai[1] = 1;
            }
            return false;
        }
    }
}
