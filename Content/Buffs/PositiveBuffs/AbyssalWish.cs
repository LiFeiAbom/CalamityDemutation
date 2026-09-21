using Terraria;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.PositiveBuffs
{
    /// <summary>
    /// 渊洋之愿增益（Abyssal Wish）：由渊洋秘药（AbyssalElixir）赋予的祝福类增益。
    /// 移植自灾厄现代版 Buffs/StatBuffs/AmidiasBlessing.cs —— 三个参照版本（现代 1.4.4 / 现代 2.0.3.9 / 经典）
    /// 的类体逐字相同，故不存在版本分支。
    /// 效果为水下无限呼吸：每帧把剩余呼吸值顶到呼吸上限之上，因此缺氧倒计时不会启动，
    /// 深渊等强制缺氧区域同样有效。
    /// </summary>
    internal class AbyssalWish:ModBuff
    {
        /// <summary>
        /// 注册为正面增益、且随存档保存，与灾厄原版口径一致
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;        // 非减益：按增益图标显示
            Main.buffNoSave[Type] = false;    // 随存档保存
        }
        /// <summary>
        /// 每帧把剩余呼吸值抬到上限之上，实现水下无限呼吸
        /// </summary>
        public override void Update(Player player, ref int buffIndex)
        {
            player.breath = player.breathMax + 91;   // +91 为灾厄原值（其原注释亦未解释该数字），照搬保真
        }
    }
}
