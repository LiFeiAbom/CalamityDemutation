using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 幻影喷出的**幽焰**（照灾厄 2.0.3.9 <c>Projectiles/Summon/GhostFire.cs</c> 移植）：
    /// 16×16 判定、`extraUpdates = 3`、穿透 200、寿命 600、逐敌 50 帧独立冷却；
    /// 出生后先减速 1 秒（这期间不追踪），之后**无视地形**追最近的敌人（射程 3000）；
    /// 一旦打到人，就转为余势衰减（寿命压到 60、速度 ×0.88）；快消失时缩到几乎看不见并停止伤害。
    /// 贴图引工程共用隐形图，画面由 <see cref="PreDraw"/> 用灰度圆点自绘（源用灾厄的
    /// `SmallGreyscaleCircle`，本工程把它原样拷进 `Assets/ExtraTextures/`）。
    /// </summary>
    internal class GhostFire:ModProjectile
    {
        /// <summary>贴图引工程共用的隐形占位图（源也走隐形图，画面全靠自绘）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/InvisibleProj";
        /// <summary>快消失时关掉命中（照源）</summary>
        public bool ableToHit = true;
        /// <summary>当前追踪的目标（照源的公开字段）</summary>
        public NPC target;

        /// <summary>属于仆从弹药（MinionShot）；残影缓存 20 格、模式 0（照源）</summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 20;
        }
        /// <summary>基础属性：照源（16×16、不撞地形、穿透 200、extraUpdates 3、寿命 600、逐敌 50 帧）</summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 200;
            Projectile.extraUpdates = 3;
            Projectile.timeLeft = 600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 50;
            Projectile.DamageType = DamageClass.Summon;
        }
        /// <summary>快消失（≤20 帧）时不再造成伤害（照源）</summary>
        public override bool? CanDamage() => ableToHit ? (bool?)null : false;
        /// <summary>命中过就转余势衰减；出生 1 秒内不追踪，之后追最近的敌人；将死时停止伤害</summary>
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.localAI[0] += 1f / (Projectile.extraUpdates + 1);

            // 打过人（穿透数被扣过）：把寿命压到 60 并持续减速，仍能在近处蹭到多个目标
            if (Projectile.penetrate < 200)
            {
                if (Projectile.timeLeft > 60)
                {
                    Projectile.timeLeft = 60;
                }
                Projectile.velocity *= 0.88f;
            }
            // 出生 1 秒内：持续减速，且不追踪
            else if (Projectile.localAI[0] < 60f)
            {
                Projectile.velocity *= 0.93f;
            }
            else
            {
                FindTarget(player);
            }

            if (Projectile.timeLeft <= 20)
            {
                ableToHit = false;
            }
        }
        /// <summary>找最近的敌人（射程 3000，优先玩家右键标记的目标）；找不到就慢慢减速，找到就扑上去</summary>
        public void FindTarget(Player player)
        {
            float maxDistance = 3000f;
            bool foundTarget = false;
            if (player.HasMinionAttackTargetNPC)
            {
                NPC npc = Main.npc[player.MinionAttackTargetNPC];
                if (npc.CanBeChasedBy(Projectile, false))
                {
                    float targetDist = Vector2.Distance(npc.Center, Projectile.Center);
                    if (targetDist < maxDistance)
                    {
                        maxDistance = targetDist;
                        foundTarget = true;
                        target = npc;
                    }
                }
            }
            if (!foundTarget)
            {
                for (int npcIndex = 0; npcIndex < Main.maxNPCs; npcIndex++)
                {
                    NPC npc = Main.npc[npcIndex];
                    if (npc.CanBeChasedBy(Projectile, false))
                    {
                        float targetDist = Vector2.Distance(npc.Center, Projectile.Center);
                        if (targetDist < maxDistance)
                        {
                            maxDistance = targetDist;
                            foundTarget = true;
                            target = npc;
                        }
                    }
                }
            }
            if (!foundTarget)
            {
                Projectile.velocity *= 0.98f;
            }
            else
            {
                KillTheThing(target);
            }
        }
        /// <summary>
        /// 超级追踪（照源：追踪速度与预判强度都按 `extraUpdates + 1` 摊薄，
        /// 免得高频率更新的弹幕 1:1 复制目标的瞬时移动）
        /// </summary>
        public void KillTheThing(NPC npc)
        {
            Projectile.velocity = Projectile.SuperhomeTowardsTarget(npc, 50f / (Projectile.extraUpdates + 1), 60f / (Projectile.extraUpdates + 1), 1f / (Projectile.extraUpdates + 1));
        }
        /// <summary>
        /// 自绘：沿残影位置叠两层青蓝灰度圆点（外层大、内层小），越靠后的残影越淡越小，
        /// 将死时整体缩小到看不见（照源的光点画法）
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D lightTexture = ModContent.Request<Texture2D>("CalamityDemutation/Assets/ExtraTextures/SmallGreyscaleCircle").Value;
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float colorInterpolation = (float)Math.Cos(Projectile.timeLeft / 32f + Main.GlobalTimeWrappedHourly / 20f + i / (float)Projectile.oldPos.Length * MathHelper.Pi) * 0.5f + 0.5f;
                Color color = Color.Lerp(Color.Cyan, Color.LightBlue, colorInterpolation) * 0.4f;
                color.A = 0;
                Vector2 drawPosition = Projectile.oldPos[i] + lightTexture.Size() * 0.5f - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY) + new Vector2(-28f, -28f);
                Color outerColor = color;
                Color innerColor = color * 0.5f;
                float intensity = 0.9f + 0.15f * (float)Math.Cos(Main.GlobalTimeWrappedHourly % 60f * MathHelper.TwoPi);
                intensity *= MathHelper.Lerp(0.15f, 1f, 1f - i / (float)Projectile.oldPos.Length);
                if (Projectile.timeLeft <= 60)
                {
                    intensity *= Projectile.timeLeft / 60f;
                }
                Vector2 outerScale = new Vector2(1f) * intensity;
                Vector2 innerScale = new Vector2(1f) * intensity * 0.7f;
                outerColor *= intensity;
                innerColor *= intensity;
                Main.EntitySpriteDraw(lightTexture, drawPosition, null, outerColor, 0f, lightTexture.Size() * 0.5f, outerScale * 0.6f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(lightTexture, drawPosition, null, innerColor, 0f, lightTexture.Size() * 0.5f, innerScale * 0.6f, SpriteEffects.None, 0);
            }
            return false;
        }
    }
}
