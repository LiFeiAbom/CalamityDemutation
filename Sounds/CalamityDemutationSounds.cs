using Terraria.Audio;
namespace CalamityDemutation.Sounds
{
    /// <summary>
    /// 音效静态引用（移植自灾厄的对应音效文件）
    /// </summary>
    public static class CalamityDemutationSounds
    {
        /// <summary>至尊灾厄大挥砍（CatastropheSwing）</summary>
        public static readonly SoundStyle CatastropheSwing = new("CalamityDemutation/Sounds/Custom/SCalSounds/CatastropheResonanceSlash");
        /// <summary>弑神者冲刺起手：吞噬者死亡动画音效（DevourerDeath）</summary>
        public static readonly SoundStyle DevourerDeath = new("CalamityDemutation/Sounds/Custom/SCalSounds/DevourerDeath");
        /// <summary>弑神者冲刺命中：吞噬者死亡冲击音效（DevourerDeathImpact）</summary>
        public static readonly SoundStyle DevourerDeathImpact = new("CalamityDemutation/Sounds/Custom/SCalSounds/DevourerDeathImpact");
        /// <summary>吞噬者体节碎裂（DevourerSegmentBreak1）：阿斯加德之庇护冲刺撞击的宇宙爆炸音</summary>
        public static readonly SoundStyle DevourerSegmentBreak1 = new("CalamityDemutation/Sounds/Custom/SCalSounds/DevourerSegmentBreak1") { Volume = 0.3f };
        /// <summary>亵渎天神神圣爆破冲击（ProvidenceHolyBlastImpact）：极乐之庇护冲刺撞击的神圣爆炸音</summary>
        public static readonly SoundStyle ProvidenceHolyBlastImpact = new("CalamityDemutation/Sounds/Custom/SCalSounds/ProvidenceHolyBlastImpact") { Volume = 0.6f };
        /// <summary>亵渎守护者护盾关闭（GuardianShieldDeactivate）：亵渎之魂护盾在位时的受击音（灾厄原用在 ProfanedSoulCrystal.HurtSound）</summary>
        public static readonly SoundStyle GuardianShieldDeactivate = new("CalamityDemutation/Sounds/Custom/ProfanedGuardians/GuardianShieldDeactivate");
        /// <summary>亵渎天神受击（ProvidenceHurt）：亵渎之魂护盾已破时的受击音（同上，灾厄原为 Providence.HurtSound）</summary>
        public static readonly SoundStyle ProvidenceHurt = new("CalamityDemutation/Sounds/NPCHit/ProvidenceHurt");
        /// <summary>肉感斩击（MeatySlash）</summary>
        public static readonly SoundStyle MeatySlashSound = new("CalamityDemutation/Sounds/Custom/MeatySlash");
        /// <summary>村正大挥砍（BigSwing）</summary>
        public static readonly SoundStyle MurasamaBigSwing = new("CalamityDemutation/Sounds/Item/MurasamaBigSwing") { Volume = 0.25f };
        /// <summary>村正命中有机物（OrganicHit）</summary>
        public static readonly SoundStyle MurasamaHitOrganic = new("CalamityDemutation/Sounds/Item/MurasamaHitOrganic") { Volume = 0.45f };
        /// <summary>水晶破碎者蓄力（CrystylCharge）：棱镜破碎者蓄力到 200 帧时播放（灾厄原为 CrystylCrusher.ChargeSound）</summary>
        public static readonly SoundStyle CrystylCharge = new("CalamityDemutation/Sounds/Item/CrystylCharge");
        /// <summary>特斯拉炮开火（TeslaCannonFire）：棱镜魔力阵生成时播放（灾厄原为 TeslaCannon.FireSound）</summary>
        public static readonly SoundStyle TeslaCannonFire = new("CalamityDemutation/Sounds/Item/TeslaCannonFire");
        /// <summary>永世之刃冲刺撞击（ExobladeDashImpact）：元素王者激光命中时播放（灾厄原为 PrismaticRay.HitSound）</summary>
        public static readonly SoundStyle ExobladeDashImpact = new("CalamityDemutation/Sounds/Item/ExobladeDashImpact") { Volume = 0.8f };
        /// <summary>破碎剑柄挥砍（HiltAttack）：分形系列 BrokenHilt 每次挥砍播放（CE 原为 CEUtils.PlaySound("HiltAttack")）</summary>
        public static readonly SoundStyle HiltAttack = new("CalamityDemutation/Sounds/Item/HiltAttack");
        /// <summary>破碎分形挥砍（CE 原名 sf_use）：ShatteredFractal 的普通挥砍式（ai[0] 0/1）起手播放</summary>
        public static readonly SoundStyle FractalSwing = new("CalamityDemutation/Sounds/Item/FractalSwing");
        /// <summary>破碎分形刺出起手（CE 原名 powerwhip）：ShatteredFractal 的刺出式（ai[0] 2）起手播放</summary>
        public static readonly SoundStyle FractalThrust = new("CalamityDemutation/Sounds/Item/FractalThrust");
        /// <summary>破碎分形刺出（CE 原名 sf_shoot）：刺出式射出 FractalShoot 时播放</summary>
        public static readonly SoundStyle FractalShoot = new("CalamityDemutation/Sounds/Item/FractalShoot");
        /// <summary>破碎分形挥砍命中（CE 原名 sf_hit）：普通挥砍式的命中音</summary>
        public static readonly SoundStyle FractalSwingHit = new("CalamityDemutation/Sounds/Item/FractalSwingHit");
        /// <summary>破碎分形刺出命中（CE 原名 sf_hit1）：刺出式的命中音</summary>
        public static readonly SoundStyle FractalThrustHit = new("CalamityDemutation/Sounds/Item/FractalThrustHit");
        /// <summary>破碎分形命中附加音（CE 原名 FractalHit）：普通挥砍式命中时额外叠加播放</summary>
        public static readonly SoundStyle FractalImpact = new("CalamityDemutation/Sounds/Item/FractalImpact");
        /// <summary>分形深渊刃冲刺启动（CE 原名 AbyssalBladeLaunch）：深渊分形的 FractalAbyssalBlade 锁定目标冲出时播放</summary>
        public static readonly SoundStyle AbyssalBladeLaunch = new("CalamityDemutation/Sounds/Item/AbyssalBladeLaunch");
        /// <summary>分形之星裂开（CE 原名 bne_hit，名字不可读故按用途命名）：星熠分形的 FractalStar 寿命将尽、放出四颗渊星时播放</summary>
        public static readonly SoundStyle FractalStarSplit = new("CalamityDemutation/Sounds/Item/bne_hit");
        /// <summary>分形渊星消散（CE 原名 metalhit，名字不可读故按用途命名）：FractalStarblight 消失时播放</summary>
        public static readonly SoundStyle FractalBlightFade = new("CalamityDemutation/Sounds/Item/metalhit");
        /// <summary>元素分形掷出剑影（CE 原名 zypshot2，名字不可读故按用途命名）：ElementalFractalHeld 刺出式射出 ElementalFractalThrown 时播放</summary>
        public static readonly SoundStyle FractalThrow = new("CalamityDemutation/Sounds/Item/zypshot2");
        /// <summary>无星之夜命中（CE 原名 he1，名字不可读故按用途命名）：StarlessNightProj 命中敌人时随机播这一种或其变体</summary>
        public static readonly SoundStyle StarlessNightHit1 = new("CalamityDemutation/Sounds/Item/he1");
        /// <summary>无星之夜命中变体（CE 原名 he3）：同上，与 he1 随机二选一</summary>
        public static readonly SoundStyle StarlessNightHit3 = new("CalamityDemutation/Sounds/Item/he3");
        /// <summary>无星之夜挥砍（CE 原名 sn_swing）：StarlessNightProj 蓄势起手的两段挥砍音</summary>
        public static readonly SoundStyle StarlessNightSwing = new("CalamityDemutation/Sounds/Item/sn_swing");
        /// <summary>符文之歌蓄力完毕（CE 原名 runesong3）：RuneSongHeld 第一段蓄势结束时播放</summary>
        public static readonly SoundStyle RuneSongCharge = new("CalamityDemutation/Sounds/Item/runesong3");
        /// <summary>符文之歌二段斩击（CE 原名 HellkiteSwing1）：命中后弹开、转入大范围多段斩时播放</summary>
        public static readonly SoundStyle RuneSongSwing1 = new("CalamityDemutation/Sounds/Item/HellkiteSwing1");
        /// <summary>符文之歌二段斩击变体（CE 原名 HellkiteSwing2）：与 HellkiteSwing1 随机二选一</summary>
        public static readonly SoundStyle RuneSongSwing2 = new("CalamityDemutation/Sounds/Item/HellkiteSwing2");
        /// <summary>符文脉冲束射出（CE 原名 scholarStaffImpact）：RuneSongHeld 未命中时的收招、发出 RuneBolt 时播放</summary>
        public static readonly SoundStyle RuneSongBoltImpact = new("CalamityDemutation/Sounds/Item/scholarStaffImpact");
        /// <summary>符文之歌命中（CE 原名 runesonghit）：RuneSongHeld 命中敌人时播放</summary>
        public static readonly SoundStyle RuneSongHit = new("CalamityDemutation/Sounds/Item/runesonghit");
        /// <summary>符文脉冲束命中（CE 原名 beast_lavaball_rise1，名字不可读故按用途命名）：RuneBolt 命中敌人时播放</summary>
        public static readonly SoundStyle RuneBoltHit = new("CalamityDemutation/Sounds/Item/beast_lavaball_rise1");
        /// <summary>虚空斩起手（CE 原名 VoidAnticipation）：虚空分形右键发动虚空斩时播放</summary>
        public static readonly SoundStyle VoidSlashCharge = new("CalamityDemutation/Sounds/Item/VoidAnticipation");
        /// <summary>虚空分形掷剑（CE 原名 CastTriangles）：投掷本剑式（ai[0] = 2）起手时与挥砍音一起播放</summary>
        public static readonly SoundStyle VoidFractalThrow = new("CalamityDemutation/Sounds/Item/CastTriangles");
        /// <summary>虚空全屏斩命中（CE 原名 VoidAttack）：第七下全屏斩（ai[0] = 3）命中敌人时播放</summary>
        public static readonly SoundStyle VoidStrikeHit = new("CalamityDemutation/Sounds/Item/VoidAttack");
        /// <summary>虚影薄锋右键突刺起手（CE 原名 AntivoidDashSlash）：Voidshade 物品右键出手时播放，音高 CE 值减 1</summary>
        public static readonly SoundStyle VoidshadeDash = new("CalamityDemutation/Sounds/Item/AntivoidDashSlash");
        /// <summary>虚影薄锋强化期挥砍（CE 原名 rswave）：Voidshade 左键出手且玩家正处于突刺强化期时叠加播放</summary>
        public static readonly SoundStyle VoidshadeBoostSwing = new("CalamityDemutation/Sounds/Item/rswave");
        /// <summary>虚影薄锋命中（CE 原名 antivoidhit，名字不可读故按用途命名）：VoidshadeHeld 普通挥砍命中时播放</summary>
        public static readonly SoundStyle VoidshadeHit = new("CalamityDemutation/Sounds/Item/antivoidhit");
        /// <summary>虚影剑气命中（CE 原名 flashback）：VoidImpact 命中敌人时播放</summary>
        public static readonly SoundStyle VoidImpactHit = new("CalamityDemutation/Sounds/Item/flashback");
    }
}
