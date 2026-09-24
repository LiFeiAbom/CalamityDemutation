using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 熵之飞刃（行为完全移植自灾厄大修 **0.4.0.3.5** 的 <c>EntropicClaymoreProj</c>）
    /// —— 熵之舞挥砍途中射出的高速飞刃，命中后自身伤害递减 15（同一枚越打越弱）、
    /// 对蠕虫体节伤害额外 ×0.6（避免高速弹幕把长条蠕虫一路洞穿）。
    /// 额外更新 10（每帧 11 次更新，故实际速度约为 velocity 的 11 倍，见 tML 对 <c>extraUpdates</c> 的说明）。
    /// <para>
    /// ⚠️ **刻意偏离源码的两处**（其余逻辑逐行照抄）：
    /// ① 大修这里用 1×1 全透明占位贴图，本体不可见、外观全靠粒子尾迹；用户 2026-09-24 点名要给飞刃
    /// 加上可见贴图，故改用本工程早先移植灾厄 2.0.3.9 时的**大号熵之飞刃贴图（76×18）**
    /// （与灾厄 `EntropicFlechetteLarge.png` 字节一致），并在 <see cref="AI"/> 开头按速度摆正刀身。
    /// 源码另有中号 48×10 / 小号 38×6 两张，未采用。
    /// ② 淡绿丝线粒子的颜色乘了 0.25 —— 源码的满亮度加法混合会饱和成刺眼白线（大修本体同样如此），
    /// 用户 2026-09-24 点名要求压暗，详见 <see cref="AI"/> 内注释。
    /// </para>
    /// </summary>
    internal class EntropicClaymoreProj : ModProjectile
    {
        /// <summary>
        /// 蠕虫体节判定集合：大修 <c>CWRLoad.WormBodys</c> 那份硬编码表的等价物，
        /// 原版两项 + 按类名软依赖解析灾厄的 15 项（解析不到就丢弃，不报错）
        /// </summary>
        private static readonly HashSet<int> WormBodyTypes = [];
        /// <summary>
        /// 灾厄那 15 个蠕虫体节是否已解析到（至少命中一个）。
        /// 未加载灾厄时为 false，此时退回"realLife / 蠕虫 AI"的语义近似判定
        /// </summary>
        private static bool calamityWormBodiesResolved;
        /// <summary>大修 WormBodys 里的灾厄蠕虫体节类名（原版两项在 <see cref="SetStaticDefaults"/> 里直接写 ID）</summary>
        private static readonly string[] CalamityWormBodyNames =
        [
            "AquaticScourgeBody", "StormWeaverBody", "ArmoredDiggerBody", "DesertScourgeBody", "DesertNuisanceBody",
            "DesertNuisanceBodyYoung", "CosmicGuardianBody", "PrimordialWyrmBody", "ThanatosBody1", "ThanatosBody2",
            "DevourerofGodsBody", "AstrumDeusBody", "SepulcherBody", "PerforatorBodyLarge", "PerforatorBodyMedium",
        ];
        /// <summary>本帧的存活计数（每次更新 +1），用于控制粒子起始延迟</summary>
        public ref float Time => ref Projectile.ai[0];
        /// <summary>解析蠕虫体节集合：原版两项固定写入，灾厄的 15 项按类名软依赖查找</summary>
        public override void SetStaticDefaults()
        {
            WormBodyTypes.Clear();
            WormBodyTypes.Add(NPCID.TheDestroyerBody);
            WormBodyTypes.Add(NPCID.EaterofWorldsBody);
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                int resolved = 0;
                foreach (string name in CalamityWormBodyNames)
                {
                    if (calamity.TryFind<ModNPC>(name, out ModNPC npc))
                    {
                        WormBodyTypes.Add(npc.Type);
                        resolved++;
                    }
                }
                calamityWormBodiesResolved = resolved > 0;
            }
        }
        /// <summary>
        /// 基础属性：16×16 碰撞箱、友方近战、穿透无限、额外更新 10、存活 67×10 tick、
        /// 不碰撞物块、启用本地无敌帧（同一敌人 30 tick 内只吃一次）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 10;
            Projectile.timeLeft = 67 * Projectile.extraUpdates;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }
        /// <summary>
        /// 每帧：先按速度方向摆正刀身（源码本体不可见、没这一步，是随可见贴图一并加的）；
        /// 给自身点位补一点淡绿光；出膛 12 tick 后、玩家 1400 像素内、每 3 tick 撒一对粒子
        /// （黑色 <see cref="DRK_Spark"/> 火花 + 淡绿 <see cref="DRK_LineFormPlayer"/> 丝线）；
        /// 每满 11 次更新（即一个真实帧）把玩家本帧的位移增量补给自身，让飞刃跟着玩家走。
        /// </summary>
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Time++;
            Lighting.AddLight(Projectile.Center, Color.LightGreen.ToVector3() * 0.2f);
            Player Owner = Main.player[Projectile.owner];
            float targetDist = Vector2.Distance(Owner.Center, Projectile.Center);
            if (Projectile.timeLeft % 3 == 0 && Time > 12f && targetDist < 1400f)
            {
                DRKLoader.AddParticle(new DRK_Spark(Projectile.Center, -Projectile.velocity * 0.05f, false, 6, 1.6f, Color.Black, Owner));
            }
            if (Projectile.timeLeft % 3 == 0 && Time > 12f && targetDist < 1400f)
            {
                // 刻意偏离源码：源码这里给的是满亮度 Color.LightGreen，而该粒子贴图 alpha 全为 255、
                // 内部还要叠画两层，加法混合下单颗粒子就能把 R/B 顶到 255（大修本体同样是刺眼白线）。
                // 乘 0.25 后每层贡献按系数的平方衰减（rgb 与 alpha 各乘一次、混合再按 alpha 计入），
                // 落地成一条淡绿丝线；嫌亮/暗只改这一个系数即可。
                DRKLoader.AddParticle(new DRK_LineFormPlayer(Projectile.Center, -Projectile.velocity * 0.05f, false, 6, 0.9f, Color.LightGreen * 0.25f, Owner));
            }
            if (Time % (Projectile.extraUpdates + 1) == 0)
            {
                Projectile.position += Owner.position - Owner.oldPosition;
            }
        }
        /// <summary>命中递减：同一枚飞刃每命中一次自身伤害就少 15</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.damage -= 15;
        }
        /// <summary>命中修正：打蠕虫体节时终伤 ×0.6</summary>
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (IsWormBody(target))
            {
                modifiers.FinalDamage *= 0.6f;
            }
        }
        /// <summary>
        /// 是否蠕虫体节：优先用显式集合；灾厄的 15 项一个都没解析到时，
        /// 退回"挂在蠕虫链条上（realLife≥0）或走蠕虫 AI"的语义近似
        /// </summary>
        private static bool IsWormBody(NPC target) => WormBodyTypes.Contains(target.type)
            || (!calamityWormBodiesResolved && (target.realLife >= 0 || target.aiStyle == NPCAIStyleID.Worm));
    }
}
