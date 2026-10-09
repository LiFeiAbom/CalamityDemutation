using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 元素之斧的仆从（照灾厄 2.0.3.9 <c>Projectiles/Summon/ElementalAxeMinion.cs</c> 移植）：
    /// 52×52、占 **1 格**召唤栏、逐敌 **30 帧**独立冷却、不撞地形、无限穿透，
    /// 自己原地旋转并交给 <see cref="CDUtil.ChargingMinionAI"/> 跑"找目标 → 冲锋 → 冷却"的循环。
    /// 彩虹染色靠 <see cref="GetAlpha"/>（`Main.DiscoRGB`）+ 自绘旋转贴图的 <see cref="PreDraw"/>。
    /// 在场期间由 <see cref="ElementalAxeBuff"/> 维持玩家侧的 <c>eAxe</c> 标志，标志断了就消失。
    /// </summary>
    internal class ElementalAxeMinion:ModProjectile
    {
        /// <summary>召唤者（主人）</summary>
        public Player Owner => Main.player[Projectile.owner];
        /// <summary>主人的本模组玩家实例（读写 <c>eAxe</c> 标志）</summary>
        public CalamityDemutationPlayer ModdedOwner => Owner.GetModPlayer<CalamityDemutationPlayer>();
        /// <summary>
        /// 灾厄召唤物预设：可被"放弃召唤物"取消（MinionSacrificable），
        /// 也可被玩家右键标记目标（MinionTargettingFeature）
        /// </summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
        }
        /// <summary>基础属性：照源 2.0.3.9（52×52、占 1 栏、30 帧逐敌冷却、不撞地形、无限穿透）</summary>
        public override void SetDefaults()
        {
            Projectile.width = 52;
            Projectile.height = 52;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.minionSlots = 1;
            Projectile.timeLeft = 18000;
            Projectile.penetrate = -1;
            Projectile.timeLeft *= 5;
            Projectile.minion = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
            Projectile.DamageType = DamageClass.Summon;
        }
        /// <summary>每帧续召唤增益；增益那头负责点亮 <c>eAxe</c>，标志还在就给自己续命</summary>
        public override void AI()
        {
            Player player = Owner;
            player.AddBuff(ModContent.BuffType<ElementalAxeBuff>(), 3600);
            if (player.dead)
            {
                ModdedOwner.eAxe = false;
            }
            if (ModdedOwner.eAxe)
            {
                Projectile.timeLeft = 2;
            }
            // 斧头本身只是原地打转，位移全交给下面那套冲锋状态机
            Projectile.rotation += 0.075f;

            // 参数照源：索敌 1600 / 回主人 1800、有目标放宽到 2500 / 归位 400 / 冷却 30 帧 /
            // 逼近 24、拉开 12 / 待机点相对主人 (0,-60) / 计数阈值 30 / 冲锋速度 16 /
            // 索敌要求视线、冲锋时无视地形
            Projectile.ChargingMinionAI(1600f, 1800f, 2500f, 400f, 1, 30f, 24f, 12f, new Vector2(0f, -60f), 30f, 16f, true, true);
        }
        /// <summary>彩虹染色（照源：用 `Main.DiscoRGB` 覆盖光照色）</summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB, Projectile.alpha);
        }
        /// <summary>自绘：贴图中心对齐弹幕中心并随 rotation 旋转（照源）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
        /// <summary>
        /// 命中挂"元素混合"（`ElementalMix`）60 帧：现代版灾厄有该减益、**经典版没有**，
        /// 所以走双版本自动 + 原版兜底的写法，缺件时退回燃烧 `BuffID.OnFire`。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyCalamityBuffWithFallback(target, "ElementalMix", 60, BuffID.OnFire);
        }
    }
}
