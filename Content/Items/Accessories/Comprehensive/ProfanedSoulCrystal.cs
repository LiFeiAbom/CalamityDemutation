using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Content.Projectiles.Summon;
using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Comprehensive
{
    /// <summary>
    /// 亵渎之魂水晶（Profaned Soul Crystal） - 亵渎之魂神器的上位召唤饰品（移植自灾厄 2.2.2，本步做骨架数值 + 变身外观视觉层）。
    /// 装备后进入水晶态：三守护者强化（伤害 750、投矛间隔 480）、亵渎护盾耐久提升到 200、
    /// 无条件获得召唤伤害 +15% 与飞行时间 ×1.1；白天档追加仆从击退/移速/召唤攻速，
    /// 夜晚档追加减伤/防御/生命回复，无仆从且无哨兵时进入 Empowered 档（昼夜两档同时生效）。
    /// 与神器的区别：本工程按用户口径去掉了 Boss 门槛与 10 空闲仆从栏门槛，装备即激活四态。
    /// 本步已做：昼夜两套变身装备贴图注册、首次装备的 120 帧变身动画（PscTransformAnimation / PscTransformRocks）、
    /// 水晶鞭增益（ProfanedCrystalWhipBuff）、武器五职业转化（TransformItemUsage，见该方法）与全部转化弹幕、
    /// IsPscProjectile 名单。召唤鞭的 tag 多倍伤害原挂在灾厄内部 SummonTag 体系上，本工程用自建的
    /// ProfanedCrystalWhipDebuff（登记为原版 tag buff）+ CalamityDemutationGlobalProjectile.ModifyHitNPC 等价实现。
    /// </summary>
    internal class ProfanedSoulCrystal:ModItem
    {
        // ── 静态常量 ──
        /// <summary>
        /// 水晶态护盾耐久上限 = 200（用户指定；2.2.2 源码里该常量是 100，本工程按用户口径加倍。
        /// 神器档为 ProfanedSoulArtifact.ShieldDurabilityMax = 25，与 2.2.2 一致）
        /// </summary>
        public const int ShieldDurabilityMax = 200;
        /// <summary>
        /// 水晶态破盾后的回充延迟（帧，5 秒）与总回充时长（帧，4 秒）
        /// （对应 2.2.2 的 ShieldRechargeDelay = 5 秒、TotalShieldRechargeTime = 4 秒）
        /// </summary>
        public const int ShieldRechargeDelay = 300;
        public const int ShieldRechargeTime = 240;
        /// <summary>
        /// 水晶态受击时暂停回充的时长（帧，5 秒，对齐灾厄 HitHurt 里水晶形态写死的 60*5；神器档为 10 秒）
        /// </summary>
        public const int ShieldRechargeDelayOnHit = 300;
        /// <summary>
        /// 变身动画总时长（帧，120；对应灾厄 2.2.2 的 maxPscAnimTime），也是 PscTransformAnimation 的存活时间
        /// </summary>
        public const int maxPscAnimTime = 120;
        /// <summary>
        /// 夜晚套装的三个装备贴图名（对应灾厄 2.2.2 的 PscNightHead/PscNightLegs/PscNightWings）；
        /// 白天一套直接用物品自身 Name（= "ProfanedSoulCrystal"）作为 EquipName，与灾厄一致。
        /// EquipLoader 只接受字符串名（没有按类型取槽位的重载），故此处用常量保存自身的装备名
        /// </summary>
        public const string NightHeadName = "PscNightHead";
        public const string NightLegsName = "PscNightLegs";
        public const string NightWingsName = "PscNightWings";
        // ── 静态装备槽缓存 ──
        /// <summary>
        /// 昼夜两套变身装备贴图的槽位（SetStaticDefaults 里由 EquipLoader 解析后缓存，供玩家类每帧写
        /// player.head/body/legs/wings 用；服务端不注册贴图，这些值保持 0，而服务端也不绘制）
        /// </summary>
        public static int DayHeadSlot, DayBodySlot, DayLegsSlot, DayWingsSlot;
        public static int NightHeadSlot, NightLegsSlot, NightWingsSlot;
        /// <summary>
        /// 水晶四态（照抄灾厄 2.2.2 的 ProfanedSoulCrystalState）：
        /// Vanity = 未装备/仅有外观，Buffs = 白天常驻档，Enraged = 夜晚常驻档，
        /// Empowered = 无仆从且无哨兵的强化档（覆盖昼夜，两档加成兼得）
        /// </summary>
        public enum ProfanedSoulCrystalState
        {
            Vanity,
            Buffs,
            Enraged,
            Empowered
        }
        // ── 生命周期方法 ──
        /// <summary>
        /// 注册昼夜两套变身装备贴图（对应灾厄 2.2.2 的 EquipSlots）：
        /// 白天一套用物品自身 Name 作为 EquipName（头/身/腿/翅），夜晚一套用 PscNightHead/PscNightLegs/PscNightWings
        /// （只有头/腿/翅，身体保持白天的）。服务端不绘制装备贴图，故非服务器端才注册（与 AuricTeslaBodyArmor 写法一致）
        /// </summary>
        public override void Load()
        {
            if (Main.netMode == NetmodeID.Server)
                return;
            const string path = "CalamityDemutation/Content/Items/Accessories/Comprehensive/";
            EquipLoader.AddEquipTexture(Mod, path + "ProfanedSoulTrans_Head", EquipType.Head, this, Name);    // 白天头（EquipName = 物品 Name）
            EquipLoader.AddEquipTexture(Mod, path + "ProfanedSoulTrans_Body", EquipType.Body, this, Name);    // 白天身体（夜晚继续沿用）
            EquipLoader.AddEquipTexture(Mod, path + "ProfanedSoulTrans_Legs", EquipType.Legs, this, Name);    // 白天腿
            EquipLoader.AddEquipTexture(Mod, path + "ProfanedSoulTrans_Wings", EquipType.Wings, this, Name);  // 白天翅
            EquipLoader.AddEquipTexture(Mod, path + "ProfanedSoulTransNight_Head", EquipType.Head, this, NightHeadName);      // 夜晚头
            EquipLoader.AddEquipTexture(Mod, path + "ProfanedSoulTransNight_Legs", EquipType.Legs, this, NightLegsName);      // 夜晚腿
            EquipLoader.AddEquipTexture(Mod, path + "ProfanedSoulTransNight_Wings", EquipType.Wings, this, NightWingsName);   // 夜晚翅
        }
        /// <summary>
        /// 物品外观动画：8 帧竖直滚动、每 4 tick 换一帧，并按灵魂类物品处理（浮动/发光表现）；
        /// 随后解析并缓存昼夜两套装备槽位、设置变身贴图的隐藏/覆盖标记
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(8, 4));   // 8 帧、每 4 tick 换一帧
            ItemID.Sets.AnimatesAsSoul[Type] = true;                            // 按灵魂类物品处理
            if (Main.netMode == NetmodeID.Server)
                return;
            DayHeadSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Head);
            DayBodySlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body);
            DayLegsSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs);
            DayWingsSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Wings);
            NightHeadSlot = EquipLoader.GetEquipSlot(Mod, NightHeadName, EquipType.Head);
            NightLegsSlot = EquipLoader.GetEquipSlot(Mod, NightLegsName, EquipType.Legs);
            NightWingsSlot = EquipLoader.GetEquipSlot(Mod, NightWingsName, EquipType.Wings);
            SetArmorIDSets();
        }
        /// <summary>
        /// 按 2.2.2 的 ArmorIDSets 设置变身贴图的隐藏/覆盖标记：
        /// 白天身体隐藏上身皮肤与手臂；白天/夜晚腿隐藏下半身皮肤并覆盖腿部绘制；头不绘制本体（避免原版头/发与变身头叠加）
        /// </summary>
        private void SetArmorIDSets()
        {
            ArmorIDs.Body.Sets.HidesTopSkin[DayBodySlot] = true;      // 身体：隐藏上身皮肤
            ArmorIDs.Body.Sets.HidesArms[DayBodySlot] = true;         // 身体：隐藏手臂
            ArmorIDs.Legs.Sets.HidesBottomSkin[DayLegsSlot] = true;   // 白天腿：隐藏下半身皮肤
            ArmorIDs.Legs.Sets.OverridesLegs[DayLegsSlot] = true;     // 白天腿：覆盖原版腿部绘制
            ArmorIDs.Legs.Sets.HidesBottomSkin[NightLegsSlot] = true; // 夜晚腿：同上
            ArmorIDs.Legs.Sets.OverridesLegs[NightLegsSlot] = true;
            ArmorIDs.Head.Sets.DrawHead[DayHeadSlot] = false;         // 白天头：不绘制原版头
            ArmorIDs.Head.Sets.DrawHead[NightHeadSlot] = false;       // 夜晚头：同上
        }
        /// <summary>
        /// 物品基础属性：50x50、饰品、价值 2 铂金 50 金、稀有度红色、月后自定义稀有度 22 级（橙）
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 50;                              // 贴图宽（像素）
            Item.height = 50;                             // 贴图高（像素）
            Item.accessory = true;                        // 作为饰品装备
            Item.value = Item.buyPrice(2, 50, 0, 0);      // 价值 2 铂金 50 金
            Item.rare = ItemRarityID.Red;                 // 基础稀有度红色
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 22;   // 月后稀有度 22 级（橙）
        }
        /// <summary>
        /// 互斥：不允许与下位饰品亵渎之魂神器同时装备（对齐灾厄 CanAccessoryBeEquippedWith）
        /// </summary>
        public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player) => incomingItem.type != ModContent.ItemType<ProfanedSoulArtifact>();
        /// <summary>
        /// 装备时置位水晶标记与神器标记（水晶继承神器的守护者/治疗/护盾结算），hideVisual 时隐藏护盾；
        /// 首次装备（上一帧未装备且场上无变身动画弹幕）时把护盾耐久置 1 并生成 120 帧的变身动画弹幕。
        /// 四态属性、守护者强化与护盾常量切换都在 CalamityDemutationPlayer.PostUpdateMiscEffects 中按 pscState 结算
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.profanedSoulArtifact = true;              // 继承神器的守护者/治疗结算
            modPlayer.profanedCrystal = true;                   // 置位水晶标记，供四态与护盾结算读取
            modPlayer.profanedSoulShieldVisible = !hideVisual;  // 隐藏装备时不显示护盾
            modPlayer.profanedCrystalVisible = !hideVisual;     // 变身外观同样跟随饰品可见性（隐藏时不覆写头/身/腿/翅）
            if (player.whoAmI == Main.myPlayer && !modPlayer.profanedCrystalPrevious && player.ownedProjectileCounts[ModContent.ProjectileType<PscTransformAnimation>()] == 0)
            {
                modPlayer.profanedSoulShieldDurability = 1;                 // 首次装备给护盾置初值 1（对齐 2.2.2）
                modPlayer.profanedCrystalAnim = maxPscAnimTime;             // 变身动画计时置满
                Projectile.NewProjectile(player.GetSource_FromThis(), player.Center, Vector2.Zero, ModContent.ProjectileType<PscTransformAnimation>(), 0, 0f, player.whoAmI);
            }
            // 常规属性加成区（本工程新增、装备即生效的通用全能加成，与四态无关；灾厄 2.2.2 无此段）
            player.statLifeMax2 += (int)(player.statLifeMax2 * 0.08f);
            player.statManaMax2 += (int)(player.statManaMax2 * 0.08f);
            player.GetDamage<GenericDamageClass>() += 0.16f;
            player.GetCritChance<GenericDamageClass>() += 16;
            player.endurance += 0.08f;
            player.statDefense += 8;
            player.lifeRegen += 8;
            player.manaRegen += 8;
            player.moveSpeed += 0.08f;
            player.jumpSpeedBoost += 0.8f;
            player.GetArmorPenetration<GenericDamageClass>() += 16;
            player.manaCost *= 0.84f;
            player.pickSpeed -= 0.16f;
            Lighting.AddLight((int)player.Center.X / 16, (int)player.Center.Y / 16, 1.4f, 0.3f, 0.9f);
        }
        /// <summary>
        /// 戴在时装栏时只给外观（不给任何加成）：护盾与变身外观都显示。
        /// 时装栏的饰品没有"可见性开关"，故两者都直接置 true
        /// </summary>
        public override void UpdateVanity(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.profanedSoulShieldVisible = true;
            modPlayer.profanedCrystalVisible = true;   // 时装栏：变身外观照常显示
        }
        // ── 四态判定与配色 ──
        /// <summary>
        /// 取玩家当前的四态（已按用户口径去掉灾厄的 Boss 门槛与 10 空闲仆从栏门槛）：
        /// 未装备 → Vanity；无仆从且无哨兵 → Empowered；否则按昼夜返回 Enraged（夜）或 Buffs（昼）
        /// </summary>
        internal static ProfanedSoulCrystalState GetPscStateFor(Player player)
        {
            if (!player.GetModPlayer<CalamityDemutationPlayer>().profanedCrystal)
                return ProfanedSoulCrystalState.Vanity;   // 未装备水晶：只有外观
            bool noMinions = player.slotsMinions == 0;
            bool noSentries = !Main.projectile.Any(proj => proj.active && proj.owner == player.whoAmI && proj.sentry);
            if (noMinions && noSentries)
                return ProfanedSoulCrystalState.Empowered;   // 无仆从无哨兵：强化档，覆盖昼夜
            return !Main.dayTime ? ProfanedSoulCrystalState.Enraged : ProfanedSoulCrystalState.Buffs;
        }
        /// <summary>
        /// 四态对应颜色（照抄灾厄 2.2.2 的 GetColorForPsc，Empowered 档按昼夜分两色）
        /// </summary>
        internal static Color GetColorForPsc(int pscState, bool day, int alpha = 0)
        {
            return ((ProfanedSoulCrystalState)pscState) switch
            {
                ProfanedSoulCrystalState.Vanity => new Color(231, 160, 56, alpha),
                ProfanedSoulCrystalState.Buffs => new Color(255, 110, 56, alpha),
                ProfanedSoulCrystalState.Enraged => new Color(145, 208, 188, alpha),
                ProfanedSoulCrystalState.Empowered => day ? new Color(255, 75, 13, alpha) : new Color(84, 186, 163, alpha),
                _ => Color.White
            };
        }
        /// <summary>
        /// 四态配色的**昼夜插值版**（照抄灾厄 2.2.2 的 GetLerpedColorForPsc）：
        /// 把该玩家当前四态的白天色与夜晚色按当前时刻在"正午 / 午夜"两个中点之间插值，
        /// 让护罩颜色随昼夜平滑过渡，而不是到点突变。
        /// 原版把结果缓存进 CalamityPlayer.pscLerpColor（同帧复用）；本工程只在护罩绘制里用一次，直接算即可。
        /// </summary>
        internal static Color GetLerpedColorForPsc(Player player)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            bool day = Main.dayTime;
            double totalTime = day ? Main.dayLength : Main.dayLength + Main.nightLength;
            double currentTime = Main.time;
            double midday = Main.dayLength / 2;
            double midnight = Main.nightLength / 2;
            Color dayColor = GetColorForPsc(modPlayer.pscState, day);
            Color nightColor = GetColorForPsc(modPlayer.pscState > (int)ProfanedSoulCrystalState.Enraged
                ? (int)ProfanedSoulCrystalState.Empowered
                : (int)ProfanedSoulCrystalState.Enraged, false);
            Color targetColor = day ? dayColor : nightColor;
            Color nonTargetColor = day ? nightColor : dayColor;
            double targetTime = day ? midday : midnight;
            double interpolant = Utils.GetLerpValue(totalTime, targetTime, currentTime, false);
            Color result = Color.White;
            if (!day && Main.time > midnight)
                result = Color.Lerp(nightColor, dayColor, 2f - (float)interpolant);
            else if (day && Main.time > midday)
                result = Color.Lerp(nightColor, dayColor, (float)interpolant);
            if (result == Color.White)
                result = Color.Lerp(nonTargetColor, targetColor, (Main.time < midday ? 2f : 0f) - (float)interpolant);
            return result;
        }
        /// <summary>
        /// 武器转化主体（移植自灾厄 2.2.2 的 ProfanedSoulCrystal.TransformItemUsage，但**不顶替原武器**）：
        /// 灾厄在 CanUseItem 里调用它并返回 false，取消武器本身的使用、只发射转化弹幕；
        /// 本工程改为由 CalamityDemutationPlayer.PostItemCheck **每帧**调用，额外派发一份转化弹幕
        /// （原武器伤害与转化弹幕伤害都保留）。之所以不能沿用 CanUseItem：我们保留了原武器，
        /// 武器一进入挥砍动画，CanUseItem 就只在"每刀之间"被调一次，密度会掉到原版的 1/useTime（实机验证过）。
        /// 计数器 profanedSoulWeaponUsage 是**按帧**累加的（与武器动画无关），
        /// 换职业或累加到 370 时清零；五职业各自的触发节奏见各分支注释，
        /// 所有弹幕伤害统一按通用伤害折算并回写 originalDamage。
        /// 数值口径（用户 2026-09-15 定，实测通过后调回全值）：「回调削弱前」（= 2.2.2 源码实际值的 ×5）。
        /// 近战霰射 1750 / 主矛 1250、远程陨石 1500 / 火球 1000、魔法 4500、
        /// 盗贼(召唤)环射 880 / 单片 1100（强化档 625）、鞭 500（鞭没参与 ×5，保持 2.2.2 的 250 ×2）。
        /// 守护者伤害不走这套口径，取最高档 1000。
        /// </summary>
        internal static void TransformItemUsage(Item item, Player player)
        {
            if (player.whoAmI != Main.myPlayer)
                return;
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            // 职业判定：鞭（SummonMeleeSpeed）必须排在召唤前面——它在 tModLoader 里也算召唤类，先判鞭才能各归各的计数器
            int weaponType = item.CountsAsClass<MeleeDamageClass>() ? 1 :
                item.CountsAsClass<RangedDamageClass>() ? 2 :
                item.CountsAsClass<MagicDamageClass>() ? 3 :
                item.CountsAsClass<SummonMeleeSpeedDamageClass>() ? 5 :
                item.CountsAsClass<SummonDamageClass>() ? 4 : -1;
            if (weaponType <= 0)
                return;
            if (modPlayer.profanedSoulWeaponType != weaponType || modPlayer.profanedSoulWeaponUsage >= 370)
            {
                modPlayer.profanedSoulWeaponType = weaponType;
                modPlayer.profanedSoulWeaponUsage = 0;
            }
            // 鼠标方向（只用来定初始朝向）：原版直接 Normalize，这里用 SafeNormalize 避免鼠标压在身上时产生 NaN
            Vector2 correctedVelocity = (Main.MouseWorld - player.Center).SafeNormalize(Vector2.UnitX);
            bool empowered = modPlayer.pscState == (int)ProfanedSoulCrystalState.Empowered;
            bool enraged = modPlayer.pscState >= (int)ProfanedSoulCrystalState.Enraged;
            var source = player.GetSource_ItemUse(item);
            if (weaponType == 1)
            {
                // ── 近战：霰射矛 1750 / 主矛 1250（回调削弱前；2.2.2 为 350 / 250）。
                // 每 6 帧一枚圣光长矛（Enraged 及以上 4 帧），
                // 第 30 帧（Enraged 及以上 20 帧）改为一轮 5 枚扇形霰射（+3 度间隔），随后计数器归零
                if (modPlayer.profanedSoulWeaponUsage % (enraged ? 4 : 6) == 0)
                {
                    if (modPlayer.profanedSoulWeaponUsage > 0 && modPlayer.profanedSoulWeaponUsage % (enraged ? 20 : 30) == 0)
                    {
                        int numProj = 5; // 每 5 发来一轮霰射
                        Vector2 shotgunVelocity = correctedVelocity * 20f;
                        float spread = -6;
                        for (int i = 0; i < numProj; i++)
                        {
                            Vector2 perturbedspeed = new Vector2(shotgunVelocity.X, shotgunVelocity.Y).RotatedBy(MathHelper.ToRadians(spread));
                            int separation = (i * 4) - 8;
                            const int spearBaseDamage = 1750;   // 霰射矛（回调削弱前 1750；2.2.2 为 350）
                            int spearDamage = (int)player.GetTotalDamage<GenericDamageClass>().ApplyTo(spearBaseDamage);
                            int proj = Projectile.NewProjectile(source, player.Center.X, player.Center.Y - separation, perturbedspeed.X, perturbedspeed.Y, ModContent.ProjectileType<ProfanedCrystalMeleeSpear>(), spearDamage, 1f, player.whoAmI, Main.rand.NextBool(modPlayer.profanedSoulWeaponUsage == 4 ? 5 : 7) ? 1f : 0f);
                            if (Main.projectile.IndexInRange(proj))
                            {
                                Main.projectile[proj].DamageType = DamageClass.Generic;
                                Main.projectile[proj].originalDamage = spearBaseDamage;
                            }
                            spread += 3;
                            SoundEngine.PlaySound(SoundID.Item20, player.Center);
                        }
                        modPlayer.profanedSoulWeaponUsage = 0;
                    }
                    else
                    {
                        const int spearBaseDamage = 1250;   // 主矛（回调削弱前 1250；2.2.2 为 250）
                        int spearDamage = (int)player.GetTotalDamage<GenericDamageClass>().ApplyTo(spearBaseDamage);
                        int proj = Projectile.NewProjectile(source, player.Center, correctedVelocity * 14f, ModContent.ProjectileType<ProfanedCrystalMeleeSpear>(), spearDamage, 1f, player.whoAmI, Main.rand.NextBool(modPlayer.profanedSoulWeaponUsage == 4 ? 5 : 7) ? 1f : 0f, 1f);
                        if (Main.projectile.IndexInRange(proj))
                        {
                            Main.projectile[proj].DamageType = DamageClass.Generic;
                            Main.projectile[proj].originalDamage = spearBaseDamage;
                        }
                        SoundEngine.PlaySound(SoundID.Item20, player.Center);
                    }
                }
                modPlayer.profanedSoulWeaponUsage++;
            }
            else if (weaponType == 2)
            {
                // ── 远程：陨石 1500 / 小型火球 1000（回调削弱前；2.2.2 里两者已合并成 200 单值，
                // 这里按 Data.png 的两条条目拆回）。Enraged 及以上 100% 触发，否则 50% 触发——本分支**每帧都判**，
                // 所以实际密度很高（原版同样如此）；
                // 30%（Enraged 且非 Empowered 时 20%）走陨石分支，其中再 5% 是"加厚陨石"（命中时炸出一群陨星）
                if (enraged || Main.rand.NextBool())
                {
                    Vector2 perturbedspeed = new Vector2(correctedVelocity.X * 20f + Main.rand.Next(-3, 4), correctedVelocity.Y * 20f + Main.rand.Next(-3, 4)).RotatedBy(MathHelper.ToRadians(3));
                    bool isSmallBoomer = Main.rand.NextDouble() <= (enraged && !empowered ? 0.2 : 0.3);
                    bool isThiccBoomer = isSmallBoomer && Main.rand.NextDouble() <= 0.05; // 5%
                    int projType = isSmallBoomer ? isThiccBoomer ? 1 : 2 : 3;
                    switch (projType)
                    {
                        case 1: // 加厚陨石
                        case 2: // 普通陨石
                            const int boomBaseDamage = 1500;   // 陨石（回调削弱前 1500；2.2.2 为 200）
                            int boomDamage = (int)player.GetTotalDamage<GenericDamageClass>().ApplyTo(boomBaseDamage);
                            int proj = Projectile.NewProjectile(source, player.Center, perturbedspeed, ModContent.ProjectileType<ProfanedCrystalRangedHuges>(), boomDamage, 0f, player.whoAmI, projType == 1 ? 1f : 0f);
                            if (Main.projectile.IndexInRange(proj))
                            {
                                Main.projectile[proj].DamageType = DamageClass.Generic;
                                Main.projectile[proj].originalDamage = boomBaseDamage;
                            }
                            break;
                        case 3: // 小型火球
                            const int smallBaseDamage = 1000;   // 火球（回调削弱前 1000；2.2.2 为 200）
                            int smallDamage = (int)player.GetTotalDamage<GenericDamageClass>().ApplyTo(smallBaseDamage);
                            int proj2 = Projectile.NewProjectile(source, player.Center, perturbedspeed, ModContent.ProjectileType<ProfanedCrystalRangedSmalls>(), smallDamage, 0f, player.whoAmI, 0f);
                            if (Main.projectile.IndexInRange(proj2))
                            {
                                Main.projectile[proj2].DamageType = DamageClass.Generic;
                                Main.projectile[proj2].originalDamage = smallBaseDamage;
                            }
                            break;
                    }
                    if (projType > 1)
                        SoundEngine.PlaySound(SoundID.Item20, player.Center);
                }
            }
            else if (weaponType == 3)
            {
                // ── 魔法：基础伤害 4500（回调削弱前；2.2.2 为 900），每发消耗 100 倍魔力消耗。
                // 冷却按原版：发完把计数器置 20/25 帧，之后每帧递减；场上没有爆弹与裂片时计数器直接清零（原版同款加速）
                if (player.ownedProjectileCounts[ModContent.ProjectileType<ProfanedCrystalMageFireball>()] == 0 && player.ownedProjectileCounts[ModContent.ProjectileType<ProfanedCrystalMageFireballSplit>()] == 0)
                    modPlayer.profanedSoulWeaponUsage = 0;
                int manaCost = (int)(100 * player.manaCost);
                if (modPlayer.profanedSoulWeaponUsage == 0 && !player.silence && player.CheckMana(manaCost, true))
                {
                    player.manaRegenDelay = (int)player.maxRegenDelay;
                    const int magefireBaseDamage = 4500;   // 圣光爆弹（回调削弱前 4500；2.2.2 为 900）
                    int mageFireDamage = (int)player.GetTotalDamage<GenericDamageClass>().ApplyTo(magefireBaseDamage);
                    if (player.HasBuff(BuffID.ManaSickness))
                    {
                        int sickPenalty = (int)(mageFireDamage * (0.05f * ((player.buffTime[player.FindBuffIndex(BuffID.ManaSickness)] + 60) / 60)));
                        mageFireDamage -= sickPenalty;
                    }
                    SoundEngine.PlaySound(SoundID.Item20, player.Center);
                    int proj = Projectile.NewProjectile(source, player.position, correctedVelocity * 25f, ModContent.ProjectileType<ProfanedCrystalMageFireball>(), mageFireDamage, 1f, player.whoAmI, empowered ? 1f : 0f);
                    if (Main.projectile.IndexInRange(proj))
                    {
                        Main.projectile[proj].DamageType = DamageClass.Generic;
                        Main.projectile[proj].originalDamage = magefireBaseDamage;
                    }
                    modPlayer.profanedSoulWeaponUsage = enraged ? 20 : 25;   // 对齐原版：发完进入 20/25 帧冷却
                }
                if (modPlayer.profanedSoulWeaponUsage > 0)
                    modPlayer.profanedSoulWeaponUsage--;
            }
            else if (weaponType == 4)
            {
                // ── 召唤（原版为盗贼槽）：环射 880 / 单片 1100（强化档 625）。
                // 数值 = 回调削弱前；2.2.2 为 176 / 220（强化档 125）。
                // 节奏按原版（计数器是**帧**）：场上没有水晶螺旋碎片时计数器归零；
                // 计数器每帧 +1（Empowered）/+2（否则），整除 5/10 时发射一片，满 120/360 时改为一次性环射 36 片
                if (player.ownedProjectileCounts[ModContent.ProjectileType<ProfanedCrystalRogueShard>()] == 0)
                    modPlayer.profanedSoulWeaponUsage = 0;
                if (modPlayer.profanedSoulWeaponUsage >= (empowered ? 120 : 360))
                {
                    const int shardBaseDamage = 880;   // 环射 36 片（回调削弱前 880；2.2.2 为 176）
                    int shardDamage = (int)player.GetTotalDamage<GenericDamageClass>().ApplyTo(shardBaseDamage);
                    float crystalCount = 36f;
                    for (float i = 0; i < crystalCount; i++)
                    {
                        float angle = MathHelper.TwoPi / crystalCount * i;
                        int proj = Projectile.NewProjectile(source, player.Center, angle.ToRotationVector2() * 12f, ModContent.ProjectileType<ProfanedCrystalRogueShard>(), shardDamage, 1f, player.whoAmI, 0f, 0f);
                        if (Main.projectile.IndexInRange(proj))
                        {
                            Main.projectile[proj].DamageType = DamageClass.Generic;
                            Main.projectile[proj].originalDamage = shardBaseDamage;
                        }
                        SoundEngine.PlaySound(SoundID.Item20, player.Center);
                    }
                    modPlayer.profanedSoulWeaponUsage = 0;
                }
                else if (modPlayer.profanedSoulWeaponUsage % (empowered ? 5 : 10) == 0)
                {
                    // 单片：Empowered 档每次 3 链（链间 120 度、随时间旋转），否则单链；
                    // 偶数个"图案周期"才发射，形成一圈圈错开的水晶螺旋
                    int chains = empowered ? 3 : 1;
                    int totalShardProjectiles = empowered ? 360 / 5 : 360 / 10;
                    int shardBaseDamage = empowered ? 625 : 1100;   // 单片刃：强化档 625、平时 1100（回调削弱前；2.2.2 为 125 / 220）
                    int shardDamage = (int)player.GetTotalDamage<GenericDamageClass>().ApplyTo(shardBaseDamage);
                    float interval = totalShardProjectiles / chains * (empowered ? 5f : 10f);
                    double patternInterval = Math.Floor(modPlayer.profanedSoulWeaponUsage / interval);
                    if (patternInterval % 2 == 0)
                    {
                        double radians = MathHelper.TwoPi / chains;
                        double angleA = radians * 0.5;
                        double angleB = MathHelper.ToRadians(90f) - angleA;
                        float velocityX = (float)(2f * Math.Sin(angleA) / Math.Sin(angleB));
                        Vector2 spinningPoint = new Vector2(velocityX, -2f);
                        for (int i = 0; i < chains; i++)
                        {
                            Vector2 vector2 = spinningPoint.RotatedBy(radians * i + MathHelper.ToRadians(modPlayer.profanedSoulWeaponUsage));
                            vector2.Normalize();
                            int proj = Projectile.NewProjectile(source, player.Center, vector2 * 12f, ModContent.ProjectileType<ProfanedCrystalRogueShard>(), shardDamage, 1f, player.whoAmI, 1f, 0f);
                            if (Main.projectile.IndexInRange(proj))
                            {
                                Main.projectile[proj].DamageType = DamageClass.Generic;
                                Main.projectile[proj].originalDamage = shardBaseDamage;
                            }
                        }
                        SoundEngine.PlaySound(SoundID.Item20, player.Center);
                    }
                }
                modPlayer.profanedSoulWeaponUsage += !empowered ? 2 : 1;
            }
            else if (weaponType == 5)
            {
                // ── 鞭：基础伤害 500（= 2.2.2 的 250 ×2，与其它招式同比例；按字面"40%"应是 100，
                // 但那会低于 2.2.2，用户 2026-09-15 选定 500）。
                // 节奏按原版：场上没有水晶鞭时计数器归零，计数器为 0 才甩一条，甩完置 10 并每帧递减——即 10 帧一条
                if (player.ownedProjectileCounts[ModContent.ProjectileType<ProfanedCrystalWhip>()] == 0)
                    modPlayer.profanedSoulWeaponUsage = 0;
                if (modPlayer.profanedSoulWeaponUsage == 0)
                {
                    const int whipBaseDamage = 500;
                    int whipDamage = (int)player.GetTotalDamage<GenericDamageClass>().ApplyTo(whipBaseDamage);
                    bool buffed = player.HasBuff<ProfanedCrystalWhipBuff>();
                    Vector2 whipVelocity = correctedVelocity * (buffed ? 10f : 8f);
                    int permittedDistance = buffed ? 10 : 8;
                    whipVelocity.X = Math.Clamp(whipVelocity.X, -permittedDistance, permittedDistance);
                    whipVelocity.Y = Math.Clamp(whipVelocity.Y, -permittedDistance, permittedDistance);
                    player.ChangeDir(MathF.Sign(whipVelocity.X));
                    int proj = Projectile.NewProjectile(source, player.Center, whipVelocity, ModContent.ProjectileType<ProfanedCrystalWhip>(), whipDamage, 1f, player.whoAmI);
                    if (Main.projectile.IndexInRange(proj))
                    {
                        Main.projectile[proj].DamageType = DamageClass.Generic;
                        Main.projectile[proj].originalDamage = whipBaseDamage;
                    }
                    modPlayer.profanedSoulWeaponUsage = 10;
                }
                modPlayer.profanedSoulWeaponUsage--;
            }
        }
        /// <summary>
        /// 判断弹幕是否属于亵渎之魂水晶体系（供命中圣焰判定放宽用）。
        /// 收录三只守护者及其从属弹幕（长矛 / 环绕岩石 / 星弹 / 神圣射线 / 圣光爆弹及裂片），
        /// 以及本步新增的全部武器转化弹幕——它们已按通用伤害结算，本名单用于把"召唤类"的门槛也放行。
        /// </summary>
        internal static bool IsPscProjectile(Projectile proj)
        {
            int type = proj.type;
            return type == ModContent.ProjectileType<MiniGuardianAttack>()      // 进攻守护者本体
                || type == ModContent.ProjectileType<MiniGuardianDefense>()     // 防御守护者本体
                || type == ModContent.ProjectileType<MiniGuardianHealer>()      // 治疗守护者本体
                || type == ModContent.ProjectileType<MiniGuardianSpear>()       // 从属：长矛
                || type == ModContent.ProjectileType<MiniGuardianRock>()        // 从属：环绕岩石
                || type == ModContent.ProjectileType<MiniGuardianStars>()       // 从属：星弹
                || type == ModContent.ProjectileType<MiniGuardianHolyRay>()     // 从属：神圣射线
                || type == ModContent.ProjectileType<MiniGuardianFireball>()    // 从属：圣光爆弹
                || type == ModContent.ProjectileType<MiniGuardianFireballSplit>() // 从属：圣光爆弹裂片
                || type == ModContent.ProjectileType<ProfanedCrystalMeleeSpear>()   // 转化：近战圣光长矛
                || type == ModContent.ProjectileType<ProfanedCrystalRangedHuges>()  // 转化：陨石
                || type == ModContent.ProjectileType<ProfanedCrystalRangedSmalls>() // 转化：小型火球
                || type == ModContent.ProjectileType<ProfanedCrystalMageFireball>() // 转化：圣光爆弹
                || type == ModContent.ProjectileType<ProfanedCrystalMageFireballSplit>() // 转化：圣光爆弹裂片
                || type == ModContent.ProjectileType<ProfanedCrystalRogueShard>()   // 转化：水晶螺旋碎片
                || type == ModContent.ProjectileType<ProfanedCrystalWhip>();        // 转化：水晶鞭
        }
        // ── 配方 ──
        /// <summary>
        /// 注册配方（按灾厄 2.2.2 原配方 + 本工程下位饰品）：
        /// 亵渎之魂神器 ×1 + ShadowspecBar×5 + DivineGeode×50 + UnholyEssence×100，站台 ProfanedCrucible。
        /// 灾厄原配方另带 DecraftCondition（击败 SCal / ExoMechs），本工程去掉全部门槛故不注册；
        /// 仅现代版灾厄有 ProfanedCrucible 工作台与水晶本体（经典版两者皆无），故只注册现代分支
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModItem>("ShadowspecBar", out ModItem shadowspecBar)
                    && calamity.TryFind<ModItem>("DivineGeode", out ModItem divineGeode)
                    && calamity.TryFind<ModItem>("UnholyEssence", out ModItem unholyEssence)
                    && calamity.TryFind<ModTile>("ProfanedCrucible", out ModTile profanedCrucible))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient<ProfanedSoulArtifact>();
                    recipe.AddIngredient(shadowspecBar.Type, 5);
                    recipe.AddIngredient(divineGeode.Type, 50);
                    recipe.AddIngredient(unholyEssence.Type, 100);
                    recipe.AddTile(profanedCrucible.Type);
                    recipe.Register();
                }
            }
        }
    }
}
