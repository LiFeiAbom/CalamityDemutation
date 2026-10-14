using CalamityDemutation.Content.Projectiles.Ranged;
using CalamityDemutation.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Ranged
{
    /// <summary>
    /// 巨龙之息（Dragon's Breath） - 射手武器，**犽戎宝藏袋掉落、无配方**（源 1.3-release/2.0 的
    /// <c>YharonBag.cs</c> 里 `DropHelper.WeightStack&lt;DragonsBreath&gt;`；本工程挂在既有犽戎袋钩子上）。
    /// <para>
    /// **口径（用户 2026-10-14 逐条拍板）**：走 **CI 支线**（`CalamityInheritance-Beta1.12` 的 `DragonsBreathold`）、
    /// 爆炸弹幕自持一枚与灾厄 <c>FuckYou</c> 同款的件、
    /// 灾厄的"贴脸射击"**不做**、数值膨胀 **220 → 478**、中文名就用「巨龙之息」。
    /// </para>
    /// <para>
    /// 与源（CI）的差别逐条：① 类名 `DragonsBreathold` → **`DragonsBreath`**（工程命名口径：去掉 CI 式 `old` 后缀）；
    /// ② 贴图 124×78 → **58×36**（用户 2026-10-14 指定换成 **CI「远古巨龙之息」`AncientDragonsBreath`** 那张图，
    /// 同日已把原先 cal-1.4.2.101 的 64×28 旧图替换掉；持握偏移随之用该图自带的 (-15, 10)）；
    /// ③ `Item.Calamity().canFirePointBlankShots`（灾厄贴脸射击）
    /// ——本工程没有这套机制，不做；④ 微光与新版灾厄 `DragonsBreath` 互换（CI 的 `CIServerConfig.CustomShimmer`）
    /// ——不移植；⑤ 弹幕与爆炸弹幕改成本工程自持件（`DragonsBreathRound` / `FuckYou`），
    /// CI 自己直接调灾厄那两件。
    /// </para>
    /// <para>
    /// 数值（CI）：伤害 **220**、使用/动画 **9 / 18** 帧、两发间隔 `reuseDelay` **12**、击退 **6.5**、
    /// 弹速 **12**、弹药 = **子弹**、**66% 不消耗弹药**、月后稀有度 **15 档**（= CI 的 `CatalystViolet`，
    /// 价值 **2 铂金 40 金**）、禁用「Murderous」前缀（CI 原样）。
    /// 攻击方式（CI，与 1.3-release/2.0 同构，只是弹数与倍率不同）：**左键两连发**——第一发 **8 颗普通子弹**
    /// （每颗间隔 `Spread = 0.018` 弧度），第二发 **8 颗龙息弹**（散布 ×0.5 更紧，另有额外音效与 30 颗烟尘）；
    /// **右键全自动**——伤害 ×**0.9**、8 颗里随机混 4 颗龙息弹、散布 ×**1.22**、`reuseDelay` 归零。
    /// **点射散布无随机**（照源注释）。
    /// </para>
    /// </summary>
    internal class DragonsBreath:ModItem
    {
        /// <summary>左键两发之间的间隔帧（照 CI：12；1.3-release/2.0 是 15）</summary>
        private const int BetweenShotsPause = 12;
        /// <summary>每发的弹丸数（照 CI：8；1.3-release/2.0 是 6）</summary>
        private const int PelletsPerShot = 8;
        /// <summary>右键全自动时的使用时间倍率（照 CI：0.8 = 更快）</summary>
        private const float FullAutoFireRateMult = 0.8f;
        /// <summary>右键全自动时的伤害倍率（照 CI：0.9；1.3-release/2.0 是 0.8）</summary>
        private const float FullAutoDamageMult = 0.9f;
        /// <summary>每颗弹丸之间的角度间隔（照源注释：这是"每颗"的量，所以很小）</summary>
        private const float Spread = 0.018f;
        /// <summary>左键第二发（龙息弹）的散布倍率（照 CI：0.5，比第一发紧一倍）</summary>
        private const float SecondShotSpreadFactor = 0.5f;
        /// <summary>右键全自动的散布倍率（照 CI：1.22；1.3-release/2.0 是 1.52）</summary>
        private const float FullAutoSpreadFactor = 1.22f;
        /// <summary>不打散弹的固定省弹率（照源：66% 不消耗弹药）</summary>
        private const float AmmoSaveChance = 0.66f;
        /// <summary>数值膨胀后的面板伤害（用户 2026-10-14 指定：220 → 478）</summary>
        private const float InflatedDamage = 478f;
        /// <summary>当前生效的面板基础伤害：开则用膨胀值，否则维持 <c>Item.damage</c> 的源值 220</summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;

        /// <summary>
        /// 研究解锁一份；并允许右键重复触发（右键全自动需要每次点击都重新施放）——照 CI
        /// </summary>
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }
        /// <summary>
        /// 物品基础属性：64×28 贴图、伤害 220、使用/动画 9/18 帧、两发间隔 12、击退 6.5、弹速 12、
        /// 子弹弹药、月后稀有度 15 档、价值 2 铂金 40 金（全部照 CI）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 58;                          // 贴图宽（像素，取 CI「远古巨龙之息」那张图）
            Item.height = 36;                         // 贴图高（像素）
            Item.damage = 220;                        // 面板伤害（照 CI；1.3-release/2.0 是 198）
            Item.DamageType = DamageClass.Ranged;     // 射手伤害类型
            Item.useTime = 9;
            Item.useAnimation = 18;
            Item.reuseDelay = BetweenShotsPause;      // 左键两发之间的间隔
            Item.autoReuse = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;                      // 挥砍动画不造成伤害
            Item.knockBack = 6.5f;
            Item.shoot = ModContent.ProjectileType<DragonsBreathRound>();
            Item.shootSpeed = 12f;
            Item.useAmmo = AmmoID.Bullet;             // 吃子弹类弹药
            Item.value = Item.buyPrice(2, 40, 0, 0);  // 价值 2 铂金 40 金（CI 的 CatalystViolet / 灾厄 Rarity15）
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15;  // 月后稀有度 15（紫）
        }
        /// <summary>
        /// 数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>。
        /// 本件所有派生伤害都从 <c>Shoot</c> 的 <c>damage</c> 参数往下传（8 颗弹丸各自吃满面板），会自动跟随。
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage) => damage.Base = BaseDamage;
        /// <summary>
        /// 66% 不消耗弹药（照源：<c>Main.rand.NextFloat() &gt; 0.66f</c> 时才消耗）
        /// </summary>
        public override bool CanConsumeAmmo(Item ammo, Player player) => Main.rand.NextFloat() > AmmoSaveChance;
        /// <summary>
        /// 持握偏移：用这张图（CI「远古巨龙之息」）自带的 (-15, 10)；换图后如看着别扭直接改这里
        /// </summary>
        public override Vector2? HoldoutOffset() => new Vector2(-15, 10);
        /// <summary>
        /// 允许右键（右键 = 全自动模式）
        /// </summary>
        public override bool AltFunctionUse(Player player) => true;
        /// <summary>
        /// 右键时两发间隔归零（全自动），左键时保持 <see cref="BetweenShotsPause"/>（照 CI）
        /// </summary>
        public override bool CanUseItem(Player player)
        {
            Item.reuseDelay = player.altFunctionUse == 2 ? 0 : BetweenShotsPause;
            return base.CanUseItem(player);
        }
        /// <summary>
        /// 右键全自动时使用时间 ×0.8（照 CI；1.4.4 的钩子名是 UseSpeedMultiplier）
        /// </summary>
        public override float UseSpeedMultiplier(Player player) => player.altFunctionUse == 2 ? FullAutoFireRateMult : 1f;
        /// <summary>
        /// 禁用「Murderous」前缀（照 CI：该前缀会把这把多弹丸武器的收益放大得过头）
        /// </summary>
        public override bool AllowPrefix(int pre) => pre != PrefixID.Murderous;
        /// <summary>
        /// 开火：左键第一发 8 颗普通子弹、第二发 8 颗龙息弹（更紧 + 音效 + 烟尘）；
        /// 右键 8 颗随机混 4 颗龙息弹、伤害 ×0.9、散布 ×1.22。逐行照 CI 的 <c>Shoot</c>。
        /// </summary>
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            SoundEngine.PlaySound(SoundID.Item38, position);
            bool doDust = false;
            Vector2 vel = velocity;
            int[] bulletIDs = new int[PelletsPerShot];
            float spreadFactor = 1f;

            // 右键全自动：8 颗里随机混 4 颗龙息弹，散布更大、伤害更低
            if (player.altFunctionUse == 2)
            {
                damage = (int)(damage * FullAutoDamageMult);

                for (int i = 0; i < PelletsPerShot; ++i)
                    bulletIDs[i] = type;
                int dragonsBreathAdded = 0;
                while (dragonsBreathAdded < PelletsPerShot / 2)
                {
                    int i = Main.rand.Next(PelletsPerShot);
                    if (bulletIDs[i] == Item.shoot)
                        continue;
                    bulletIDs[i] = Item.shoot;
                    ++dragonsBreathAdded;
                }

                spreadFactor = FullAutoSpreadFactor;
            }
            // 左键第一发：8 颗普通子弹
            else if (player.itemAnimation == player.itemAnimationMax - 1)
            {
                for (int i = 0; i < PelletsPerShot; ++i)
                    bulletIDs[i] = type;
            }
            // 左键第二发：8 颗龙息弹，散布很紧，另有额外音效与烟尘
            else
            {
                SoundEngine.PlaySound(SoundID.Item74, position);
                for (int i = 0; i < PelletsPerShot; ++i)
                    bulletIDs[i] = Item.shoot;

                spreadFactor = SecondShotSpreadFactor;
                doDust = true;
            }

            // 实际发射：以中央为轴左右对称铺开（点射散布无随机）
            float angleOffset = Spread * -0.5f * (PelletsPerShot - 1) * spreadFactor;
            for (int i = 0; i < PelletsPerShot; ++i)
            {
                Vector2 rotatedVel = vel.RotatedBy(angleOffset);
                Projectile.NewProjectile(source, position, rotatedVel, bulletIDs[i], damage, knockback, player.whoAmI);
                angleOffset += Spread * spreadFactor;

                if (doDust)
                    SpawnDragonsBreathDust(position, rotatedVel);
            }
            return false;
        }
        /// <summary>
        /// 第二发开火时的烟尘爆发（照 CI）：30 颗，多为烟（53/54）+ 少量火星（262），
        /// 有 1/4 概率改往上飘，尺寸随机 0.5~1.6
        /// </summary>
        private void SpawnDragonsBreathDust(Vector2 pos, Vector2 velocity)
        {
            pos += velocity.SafeNormalize(Vector2.Zero) * Item.width * Item.scale * 0.71f;
            for (int i = 0; i < 30; ++i)
            {
                // 随机挑一种烟（掺一点火星）
                int dustID;
                switch (Main.rand.Next(6))
                {
                    case 0:
                        dustID = 262;
                        break;
                    case 1:
                    case 2:
                        dustID = 54;
                        break;
                    default:
                        dustID = 53;
                        break;
                }

                // 随机速度与角度，把烟"喷"出去
                float dustSpeed = Main.rand.NextFloat(3.0f, 13.0f);
                float angleRandom = 0.06f;
                Vector2 dustVel = new Vector2(dustSpeed, 0.0f).RotatedBy(velocity.ToRotation());
                dustVel = dustVel.RotatedBy(-angleRandom);
                dustVel = dustVel.RotatedByRandom(2.0f * angleRandom);

                // 有 1/4 概率改成往上飘
                if (Main.rand.NextBool(4))
                    dustVel = Vector2.Lerp(dustVel, -Vector2.UnitY * dustVel.Length(), Main.rand.NextFloat(0.6f, 0.85f)) * 0.6f;

                // 随机大小
                float scale = Main.rand.NextFloat(0.5f, 1.6f);

                // 真正生成
                int idx = Dust.NewDust(pos, 1, 1, dustID, dustVel.X, dustVel.Y, 0, default, scale);
                Main.dust[idx].noGravity = true;
                Main.dust[idx].position = pos;
            }
        }
    }
}
