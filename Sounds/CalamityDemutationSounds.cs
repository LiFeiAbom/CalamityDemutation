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
    }
}
