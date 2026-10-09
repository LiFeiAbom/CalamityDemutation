using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Magic
{
    /// <summary>
    /// 始源林海爆裂（SilvaBurst） - 始源林海法师头（SilvaMaskedCap）套装效果
    /// 「魔法弹幕引发巨型爆炸」生成的**独立**爆裂判定
    ///（按 CalamityMod 1.4.4-release / 2.0.4 的 <c>Projectiles/Magic/SilvaBurst.cs</c> 1:1 移植；
    /// CI 的 <c>SilvaMagicSetLegacy</c> 引用的就是它，用户 2026-10-09 指定该套装效果换成 CI 模式后启用）。
    /// 96×96 的隐形判定框（穿地形、穿透 -1），开启"每名敌人只结算一次"的局部无敌；
    /// 存活仅 2 帧，生成的那一帧在自身范围内喷出叶绿粉尘并加一团偏绿的光。
    /// 伤害由生成方给定（CI：800 + 0.6 × 触发弹幕的伤害），本件自身不带任何减益，与源一致。
    /// </summary>
    public class SilvaBurst : ModProjectile
    {
        /// <summary>贴图指工程共用的隐形占位图（源同样指 CalamityMod/Projectiles/InvisibleProj）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>
        /// 基础属性：96×96 判定框、友方、穿透 -1、每名敌人只结算一次、穿地形、
        /// 全透明、存活 2 帧、伤害类型 = 魔法（源原样）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 96;                                // 判定框宽（像素）
            Projectile.height = 96;                               // 判定框高（像素）
            Projectile.friendly = true;                           // 友方弹幕
            Projectile.penetrate = -1;                            // 不因命中而消失
            Projectile.usesLocalNPCImmunity = true;               // 使用局部无敌 → 每名敌人只吃一次
            Projectile.localNPCHitCooldown = -1;                  // -1 = 只结算一次
            Projectile.ignoreWater = true;                         // 不受水影响
            Projectile.tileCollide = false;                        // 穿地形
            Projectile.DamageType = DamageClass.Magic;             // 伤害类型：魔法
            Projectile.alpha = 255;                                // 全透明（本身就是隐形判定）
            Projectile.timeLeft = 2;                               // 存活 2 帧
        }
        /// <summary>
        /// AI：加一团偏绿的强光，并在整个判定框内喷叶绿粉尘（源里尘号 157 = ChlorophyteWeapon，
        /// 颜色恒为"迪斯科彩虹 R + 固定绿 (203,103)"，与其余林海系弹幕同一套配色）
        /// </summary>
        public override void AI()
        {
            // 亮绿色的爆发光
            float brightness = 1.6f;
            Lighting.AddLight(Projectile.Center, 0.27f * brightness, 0.82f * brightness, 0.157f * brightness);
            // 先薄薄三粒，再 30 粒高速扩散的粉尘
            for (int i = 0; i < 3; i++)
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.ChlorophyteWeapon, 0f, 0f, 100, new Color(Main.DiscoR, 203, 103), 1.5f);
            for (int i = 0; i < 30; i++)
            {
                int blastDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.ChlorophyteWeapon, 0f, 0f, 0, new Color(Main.DiscoR, 203, 103), 2.5f);
                Main.dust[blastDust].noGravity = true;
                Main.dust[blastDust].velocity *= 3f;
                blastDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.ChlorophyteWeapon, 0f, 0f, 100, new Color(Main.DiscoR, 203, 103), 1.5f);
                Main.dust[blastDust].velocity *= 2f;
                Main.dust[blastDust].noGravity = true;
            }
        }
    }
}
