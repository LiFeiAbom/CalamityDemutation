using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 流明流体（Lumenyl Fluid）—— 深渊魔镜闪避时炸出的 10 团墨绿流体，整体照灾厄 **2.0** 的
    /// <c>Projectiles/Rogue/AbyssalMirrorProjectile.cs</c>：12×14、3 帧动画（贴图 10×42）、
    /// 穿透 -1、撞墙、忽略水、存活 50 帧，最后 25 帧每帧淡出 10 点。
    /// <para>
    /// 伤害类型按源码的 <c>forceClassless</c> 语义设成 **Generic**（伤害在闪避那一刻已经按盗贼伤害算过，
    /// 再吃一次职业加成就是双份；CI 与 1.4.4 世系同样把它设成 Generic）。
    /// </para>
    /// <para>
    /// 命中给目标挂富营养化（Eutrophication，120 帧）——这件减益**只有现代版灾厄有**，
    /// 经典版按工程既有惯例用它的深海重压（CrushDepth，120 帧）近似替代（两个都装时两条都会挂，
    /// 与 omega 蓝胸甲的 HadopelagicPressure/CrushDepth 处理同款）。多模组下靠判据兜底，不会报错。
    /// </para>
    /// </summary>
    internal class AbyssalMirrorProjectile : ModProjectile
    {
        /// <summary>三帧动画（贴图纵向 3 格）</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 3;
        }
        /// <summary>
        /// 基础属性：12×14、友好弹幕、穿透 -1、撞墙、忽略水、存活 50 帧；
        /// 局部无敌帧 -1（照源，等于"每帧都可命中"）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.alpha = 0;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 50;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = DamageClass.Generic;
        }
        /// <summary>生命末段淡出：存活 25 帧后每帧 alpha +10（照源）</summary>
        public override void AI()
        {
            if (Projectile.timeLeft < 25)
                Projectile.alpha += 10;
        }
        /// <summary>
        /// 命中非友方目标：现代版挂富营养化 120 帧；经典版灾厄没有这件减益，用它的深海重压 120 帧替代
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (target.friendly)
                return;
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "Eutrophication", 120);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "CrushDepth", 120);
        }
    }
}
