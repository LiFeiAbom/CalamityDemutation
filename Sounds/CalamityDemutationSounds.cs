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
    }
}
