using CalamityDemutation.Content.Projectiles.Melee;
using CalamityDemutation.Players;
using CalamityDemutation.Sounds;
using CalamityDemutation.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Weapons.Melee
{
    /// <summary>
    /// 巨龙之怒（移植自 CWR 的 DragonRage）：左键四段挥砍、右键二段重击+一段蓄力。
    /// 从简：EctypeItem 基类砍掉直接继承 ModItem，TrueMeleeDamageClass→MeleeNoSpeed，Violet 稀有度→Red，
    /// 灾厄/Murasama 音效→本模组 CalamityDemutationSounds。
    /// </summary>
    internal class DragonRage : ModItem
    {
        //internal static bool coolWorld => Main.zenithWorld || Main.getGoodWorld || Main.drunkWorld || Main.worldName == "HoCha113";
        /// <summary>
        /// 允许右键重复触发：右键重击/蓄力需要每次点击都重新施放，故开启 ItemsThatAllowRepeatedRightClick
        /// </summary>
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }
        /// <summary>
        /// 物品基础属性：伤害 1275（源值；数值膨胀开关开启时面板回调到 6375）、
        /// 使用时间 32 帧、击退 7.5、暴击 +16；
        /// 无武器贴图且 noMelee，主弹幕为手持挥砍体 DragonRageHeld（channel 持续引导），
        /// 月后稀有度 15。段数计数器存在玩家身上（<see cref="CalamityDemutationPlayer.dragonRageLevel"/> /
        /// <see cref="CalamityDemutationPlayer.dragonRageLevelAlt"/>，源为 ModItem 实例字段，联机下会被队友串档）。
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 74;
            Item.height = 74;
            Item.value = Item.buyPrice(1, 80, 0, 0);
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 32;
            Item.useTime = 32;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.damage = 1275;
            Item.crit = 16;
            Item.knockBack = 7.5f;
            Item.noUseGraphic = true;
            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.noMelee = true;
            Item.channel = true;
            Item.shootSpeed = 10f;
            Item.shoot = ModContent.ProjectileType<DragonRageHeld>();
            Item.rare = ItemRarityID.Red;
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 15;
        }
        /// <summary>
        /// 数值膨胀后的面板伤害（用户 2026-10-01 定稿：以武器现存的 1275 为基数，
        /// 按「犽戎掉落」这条线索套 PostYharon 档的 **×5**，即 1275 × 5 = 6375）。
        /// 备选口径「取灾厄 1.4.4 的 888 再 ×5 = 4440」已被用户否决，勿改。
        /// </summary>
        private const float InflatedDamage = 6375f;
        /// <summary>
        /// 当前生效的面板基础伤害：膨胀开关开启时用 <see cref="InflatedDamage"/>，否则维持 <c>Item.damage</c> 的 1275。
        /// </summary>
        private float BaseDamage => ConfigSystem.StatInflationEnabled ? InflatedDamage : Item.damage;
        /// <summary>
        /// 数值膨胀：把面板基础伤害换成 <see cref="BaseDamage"/>（运行时读配置，游戏内切换即时生效）。
        /// 手持挥砍体、龙怒火球与爆炸弹幕都是按传下去的 <c>damage</c> 比例派生，会自动跟随。
        /// </summary>
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base = BaseDamage;
        }
        /// <summary>
        /// 射击核心（供 Shoot 委托）：段数通过 Projectile.NewProjectile 的 ai 参数写入弹幕。
        /// 左键为四段挥砍（Level 0→3，伤害依次 ×1.0/×1.15/×1.25/×1.55，末段换用 Item71 音效）；
        /// 右键为两段重击（LevelAlt 0/1 → ai 4/5，对应 CatastropheSwing 音效），
        /// 第三次右键放一段 ai=6 的蓄力重击并重置段数。
        /// 注：newLevel = 4 + LevelAlt 在 LevelAlt&lt;2 时只能取 4/5，故 newLevel==6 的 60% 降伤分支实际不可达。
        /// </summary>
        internal static bool ShootFunc(ref int Level, ref int LevelAlt, Item Item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                if (LevelAlt < 2)
                {
                    SoundEngine.PlaySound(CalamityDemutationSounds.CatastropheSwing with { MaxInstances = 6, Volume = 0.6f }, position);
                    int newLevel = 4 + LevelAlt;
                    int newDmg = damage;
                    if (newLevel == 6) //&& coolWorld)
                    {
                        newDmg = (int)(damage * 0.6f);
                    }
                    Projectile.NewProjectile(source, position, velocity, type, newDmg, knockback, player.whoAmI, newLevel);
                    LevelAlt++;
                    return false;
                }
                SoundEngine.PlaySound(CalamityDemutationSounds.MeatySlashSound, player.Center);
                SoundEngine.PlaySound(CalamityDemutationSounds.CatastropheSwing with { MaxInstances = 6, Volume = 1.06f }, position);
                Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 6);
                LevelAlt = 0;
                return false;
            }
            if (!Main.dedServ)
            {
                SoundStyle sound = CalamityDemutationSounds.MurasamaBigSwing with { Pitch = 0.3f + Level * 0.25f };
                if (Level == 3)
                {
                    sound = SoundID.Item71 with { Volume = 1.5f, Pitch = 0.75f };
                }
                SoundEngine.PlaySound(sound, player.position);
            }
            int newdmg = damage;
            if (Level == 1)
            {
                newdmg = (int)(damage * 1.15f);
            }
            else if (Level == 2)
            {
                newdmg = (int)(damage * 1.25f);
            }
            else if (Level == 3)
            {
                newdmg = (int)(damage * 1.55f);
            }
            Projectile.NewProjectile(source, position, velocity, type, newdmg, knockback, player.whoAmI, Level);
            if (++Level > 3)
            {
                Level = 0;
            }
            LevelAlt = 0;
            return false;
        }
            /// <summary>
            /// 重写默认射击：直接委托 ShootFunc，并把玩家身上的段数计数器以 ref 传入
            /// （<see cref="CalamityDemutationPlayer.dragonRageLevel"/> / <see cref="CalamityDemutationPlayer.dragonRageLevelAlt"/>）
            /// </summary>
            public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
            {
                CalamityDemutationPlayer mp = player.GetModPlayer<CalamityDemutationPlayer>();
                return ShootFunc(ref mp.dragonRageLevel, ref mp.dragonRageLevelAlt, Item, player, source, position, velocity, type, damage, knockback);
            }
        public override bool AltFunctionUse(Player player) => true;
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
    }
}
