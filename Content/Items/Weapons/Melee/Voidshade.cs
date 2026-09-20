using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Players;
using CalamityDemutation.Sounds;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 虚影薄锋（Voidshade，移植自 CalamityEntropy）—— 分形系列收官时顺带移植的第十一件武器，
    /// 由破碎剑刃 + 黑曜石×12 在铁砧合成。本体既不显示也不判定（noUseGraphic / noMelee），
    /// 招式全交给手持弹幕 <see cref="VoidshadeHeld"/>：左键挥砍（连段在 0/1 两式之间交替，
    /// 第 16 帧甩出一道 <see cref="VoidImpact"/>），右键为一次虚影突刺（伤害 ×1.5，
    /// 突进途中放出一道 <see cref="WohLaser"/>，刺中目标后短时间内给下一次挥砍双倍伤害）。
    /// <para>
    /// 与 CE 原版的差异：① <c>ModContent.RarityType&lt;VoidPurple&gt;</c> 换成工程的月后稀有度体系
    /// <c>postMoonLordRarity = 15</c>（= 紫色，同第九把虚空分形，见 <c>CalamityDemutationGlobalItem.ModifyTooltips</c>），
    /// 基础稀有度统一填红；② 音效走本模组的 <see cref="CalamityDemutationSounds"/>
    /// （powerwhip→FractalThrust，与破碎分形刺出同一份文件；AntivoidDashSlash→VoidshadeDash；
    /// rswave→VoidshadeBoostSwing），音高按既有口径取 CE 值减 1，<c>CEUtils.WeapSound</c> 按 1.0；
    /// ③ 「突刺命中后强化下一刀」的计时改走 <see cref="CalamityDemutationPlayer.voidshadeBoostTime"/>
    /// （CE 在它自己的 <c>EModPlayer</c> 里）；④ 配方照 CE 原文 —— 它自己已经做过一次去灾厄化
    /// （注释「脱离灾厄:灾厄 Voidstone 按 material-map 换黑曜石」），故不需要再改材料。
    /// </para>
    /// </summary>
    internal class Voidshade:ModItem
    {
        /// <summary>本次左键的连段：0/1 交替决定挥砍方向与旋向（右键时临时置 3，走突刺式）</summary>
        public int attackType = 0;
        /// <summary>连段闲置计时：超过 <see cref="UpdateInventory"/> 里的 120 帧没出手就把连段拨回 0</summary>
        public int comboExpireTimer = 0;
        /// <summary>允许按住右键连续触发（否则右键在松开前只会出手一次）</summary>
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 40;                                // 贴图宽（像素）
            Item.height = 40;                               // 贴图高（像素）
            Item.noMelee = true;                            // 本体不做挥砍判定
            Item.noUseGraphic = true;                       // 本体不画贴图
            Item.useStyle = ItemUseStyleID.Shoot;           // 举械姿势，实际挥砍由手持弹幕表现
            Item.useTime = Item.useAnimation = 32;          // 使用时间/动画时长 32 帧
            Item.autoReuse = true;
            Item.scale = 1f;
            Item.DamageType = DamageClass.Melee;
            Item.damage = 300;                             // 300 点近战伤害
            Item.knockBack = 6;
            Item.UseSound = CalamityDemutationSounds.FractalThrust;
            Item.crit = 6;                                  // 额外暴击率
            Item.shoot = ModContent.ProjectileType<VoidshadeHeld>();
            Item.shootSpeed = 16f;                          // 决定手持弹幕的朝向速度
            Item.value = Item.buyPrice(gold: 20);           // 价值 20 金
            Item.rare = ItemRarityID.Red;                   // 基础稀有度红，真正的名称颜色由 postMoonLordRarity 覆盖
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15; // 月后稀有度 15：名称染紫
        }
        /// <summary>启用右键（发动虚影突刺）</summary>
        public override bool AltFunctionUse(Player player) => true;
        /// <summary>
        /// 右键：把这次出手改成突刺式（<c>attackType = 3</c>，伤害 ×1.5）并播突刺起手音；
        /// 左键：若玩家正处在「突刺命中强化」期，额外叠一记挥砍音，并固定播一遍原版挥剑音。
        /// 随后把本次招式作为 <c>ai[0]</c> 交给手持弹幕，并推进连段（右键因为先置了 3，推进两次后回到 1）。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                attackType = 3;
                damage = (int)(damage * 1.5f);
                SoundEngine.PlaySound(CalamityDemutationSounds.VoidshadeDash with { Pitch = -0.2f, MaxInstances = 4, Volume = 0.65f }, player.Center);
            }
            else
            {
                if (player.GetModPlayer<CalamityDemutationPlayer>().voidshadeBoostTime > 0)
                {
                    SoundEngine.PlaySound(CalamityDemutationSounds.VoidshadeBoostSwing with { Volume = 0.6f }, player.Center);
                }
                SoundEngine.PlaySound(SoundID.Item1, player.Center);
            }
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, Main.myPlayer, attackType);
            attackType = (attackType + 1) % 2;
            if (player.altFunctionUse == 2)
            {
                attackType = (attackType + 1) % 2;
            }
            comboExpireTimer = 0;
            return false;
        }
        /// <summary>连段闲置超过 120 帧就把连段拨回起点（CE 原样）</summary>
        public override void UpdateInventory(Player player)
        {
            if (comboExpireTimer++ >= 120)
            {
                attackType = 0;
            }
        }
        /// <summary>虽用 Shoot 姿势，但伤害类型是近战，允许吃近战前缀的速度加成</summary>
        public override bool MeleePrefix() => true;
        /// <summary>配方（CE 已去灾厄化，照抄）：破碎剑刃 + 黑曜石×12 @ 铁砧</summary>
        public override void AddRecipes()
        {
            CreateRecipe().AddIngredient(ItemID.BreakerBlade)
                .AddIngredient(ItemID.Obsidian, 12)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
