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
        /// <summary>村正命中无机物（InorganicHit）</summary>
        public static readonly SoundStyle MurasamaHitInorganic = new("CalamityDemutation/Sounds/Item/MurasamaHitInorganic") { Volume = 0.55f };
        /// <summary>村正命中有机物（OrganicHit）</summary>
        public static readonly SoundStyle MurasamaHitOrganic = new("CalamityDemutation/Sounds/Item/MurasamaHitOrganic") { Volume = 0.45f };
    }
}
