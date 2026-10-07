using Terraria.Audio;
namespace CalamityDemutation.Sounds
{
    /// <summary>
    /// 音效静态引用（移植自灾厄的对应音效文件）
    /// </summary>
    public static class CalamityDemutationSounds
    {
        /// <summary>至尊灾厄大挥砍（CatastropheSwing）</summary>
        public static readonly SoundStyle CatastropheSwing = new("CalamityDemutation/Sounds/Custom/CatastropheResonanceSlash");
        /// <summary>弑神者冲刺起手：吞噬者死亡动画音效（DevourerDeath）</summary>
        public static readonly SoundStyle DevourerDeath = new("CalamityDemutation/Sounds/Custom/DevourerDeath");
        /// <summary>弑神者冲刺命中：吞噬者死亡冲击音效（DevourerDeathImpact）</summary>
        public static readonly SoundStyle DevourerDeathImpact = new("CalamityDemutation/Sounds/Custom/DevourerDeathImpact");
        /// <summary>吞噬者体节碎裂（DevourerSegmentBreak1）：阿斯加德之庇护冲刺撞击的宇宙爆炸音</summary>
        public static readonly SoundStyle DevourerSegmentBreak1 = new("CalamityDemutation/Sounds/Custom/DevourerSegmentBreak1") { Volume = 0.3f };
        /// <summary>亵渎天神神圣爆破冲击（ProvidenceHolyBlastImpact）：极乐之庇护冲刺撞击的神圣爆炸音</summary>
        public static readonly SoundStyle ProvidenceHolyBlastImpact = new("CalamityDemutation/Sounds/Custom/ProvidenceHolyBlastImpact") { Volume = 0.6f };
        /// <summary>肉感斩击（MeatySlash）</summary>
        public static readonly SoundStyle MeatySlashSound = new("CalamityDemutation/Sounds/Custom/MeatySlash");
        /// <summary>血炎游侠头激活音（女妖之爪重制版引导/收招时使用，音频取自灾厄 2.0.3.9）</summary>
        public static readonly SoundStyle BloodflareRangerActivation = new("CalamityDemutation/Sounds/Item/BloodflareRangerActivation");
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
        /// <summary>泰拉巨刃挥砍（TerratomereSwing）：TerratomereHoldout 挥砍到发射点时播放（灾厄原为 Terratomere.SwingSound）</summary>
        public static readonly SoundStyle TerratomereSwing = new("CalamityDemutation/Sounds/Item/TerratomereSwing");
        /// <summary>急速斩击（SwiftSlice）：TerratomereSlashCreator 生成小刀光时播放（灾厄原为 CommonCalamitySounds.SwiftSliceSound）</summary>
        public static readonly SoundStyle SwiftSliceSound = new("CalamityDemutation/Sounds/Custom/SwiftSlice");
        /// <summary>吞噬涡流爆炸（SubsumingVortexExplosion）：TerratomereExplosion 首帧播放（灾厄原为 SubsumingVortex.ExplosionSound）</summary>
        public static readonly SoundStyle SubsumingVortexExplosion = new("CalamityDemutation/Sounds/Custom/SubsumingVortexExplosion");
        /// <summary>电浆烈焰（灾厄原名 ELRFire）：熵之舞挥砍中每次射出熵之飞刃时播放，音高按第几发递增、音量 0.45</summary>
        public static readonly SoundStyle ELRFire = new("CalamityDemutation/Sounds/Item/ELRFire");
        /// <summary>掷出咒刃（灾厄原名 CursedDaggerThrow）：禅心剑真近战命中时播放，每次挥砍只响一次（灾厄原为 Ataraxia 的 hitsound 分支）</summary>
        public static readonly SoundStyle CursedDaggerThrow = new("CalamityDemutation/Sounds/Item/CursedDaggerThrow") { Volume = 0.5f, Pitch = 0.9f, PitchVariance = 0.2f, MaxInstances = -1 };
        /// <summary>女巫套激活音（经典版 cal-1.4.2.101 同名 wav，源路径 Sounds/Custom/SilvaActivation）：
        /// 女巫套装免死保命窗口开启时播放（经典版 CalamityPlayerPreTrailer.cs:5639 原样）</summary>
        public static readonly SoundStyle SilvaActivation = new("CalamityDemutation/Sounds/Custom/SilvaActivation");
        /// <summary>女巫套激活音·现代版（2.0 的 Sounds/Custom/AbilitySounds/SilvaActivation.ogg）：
        /// 深渊魔镜的闪避触发时播放（源里借的是灾厄 SilvaHeadSummon.ActivationSound）。
        /// 与上面那条**不是同一段音频**（经典 4.8 秒 / 现代 2.4 秒），故用不同文件名共存——
        /// 同名不同扩展名会让 tML 在加载期抛 Multiple extensions for asset 并禁用整个模组</summary>
        public static readonly SoundStyle SilvaActivationModern = new("CalamityDemutation/Sounds/Custom/SilvaActivationModern");
    }
}
