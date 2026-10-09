using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon.Umbrella
{
    /// <summary>
    /// 魔法子弹（MagicBullet，按 CI 的 MagicBulletOld 移植；类名去掉 Old 后缀）——
    /// 淡紫色魔法步枪（<see cref="MagicRifle"/>）射出的子弹。
    /// 4×4、走原版子弹 AI（`aiStyle = 1`，模板取 `ProjectileID.BulletHighVelocity`，即源里的裸数字 242）、
    /// 额外更新 7 次、起始全透明后淡入、穿透 1、穿地形、存活 600 帧。
    /// 命中附加贝茜诅咒 + 灵液（原版）与死亡标记 + 护甲碎裂（灾厄）。
    /// </summary>
    internal class MagicBullet:ModProjectile
    {
        /// <summary>标记为仆从射弹（源原样）</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
        }
        /// <summary>
        /// 基础属性（源原样）：4×4、自发光 0.5、起始全透明、额外 7 次更新、缩放 1.18、
        /// 仆从射弹、穿地形、`aiStyle = 1` + 高速子弹模板（源写裸数字 242，本工程按既有口径写成
        /// `ProjectileID.BulletHighVelocity` 常量）、穿透 1、600 帧
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.light = 0.5f;
            Projectile.alpha = 255;
            Projectile.extraUpdates = 7;
            Projectile.scale = 1.18f;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = ProjAIStyleID.Arrow;   // 源写裸数字 1：原版箭矢 AI
            AIType = ProjectileID.BulletHighVelocity;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.tileCollide = false;
        }
        /// <summary>命中 NPC：贝茜诅咒与灵液（原版）+ 死亡标记与护甲碎裂（灾厄，源里护甲碎裂写了两遍即重复刷新，本工程写一遍）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.BetsysCurse, 180);
            target.AddBuff(BuffID.Ichor, 180);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "MarkedforDeath", 180);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "ArmorCrunch", 180);
        }
        /// <summary>PvP 命中玩家：与 OnHitNPC 同构（原版两味 + 灾厄两味）</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.BetsysCurse, 180);
            target.AddBuff(BuffID.Ichor, 180);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "MarkedforDeath", 180);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "ArmorCrunch", 180);
        }
        /// <summary>绘制色固定为紫罗兰（源原样）</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(189, 51, 164, Projectile.alpha);
    }
}
