using CalamityDemutation.Content.Buffs.SummonBuffs;
using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon.Umbrella
{
    /// <summary>
    /// 魔法礼帽（MagicHat，按 CI 的 `Projectiles/Summon/Umbrella/MagicHatOld` 移植；类名去掉 `Old` 后缀）——
    /// 光阴流时伞（<see cref="Content.Items.Weapons.Summon.TemporalUmbrella"/>）召唤的仆从。
    /// 30×30 判定、**占 5 个召唤栏位**、**自身不造成接触伤害**；平时悬在主人头顶上方 60 像素
    /// （重力翻转时翻面），随鼠标文字色微微脉动缩放，登场先喷 50 粒骨火尘。
    /// </summary>
    /// <remarks>
    /// 攻击（照 CI）：在 <see cref="Range"/> = 1500 像素内找敌（优先玩家用召唤武器右键标记的目标），
    /// 找到后**每 5 帧**随机抛出 1 件工具 —— 从
    /// <see cref="MagicUmbrella"/> / <see cref="MagicRifle"/> / <see cref="MagicHammer"/> /
    /// <see cref="MagicAxe"/> / <see cref="MagicBird"/> 里等概率选一件，
    /// 初速 `rand(-10, 10) / rand(-15, -8)`，伤害 = 礼帽自身伤害。
    /// 存续靠每帧由 <c>modPlayer.magicHat</c> 标志把 <c>timeLeft</c> 压成 2 续命。
    /// </remarks>
    internal class MagicHat:ModProjectile
    {
        /// <summary>索敌/归位距离（源里那句 1500.0001f 的浮点误差也照抄）</summary>
        public const float Range = 1500.0001f;
        /// <summary>
        /// 标记为可牺牲的召唤物、允许玩家用召唤武器右键锁定目标，并开 8 格残影缓存（模式 1）
        /// </summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        }
        /// <summary>
        /// 基础属性：30×30、常驻网络同步、友方、不受水影响、**占 5 栏**、超长兜底寿命
        ///（实际靠召唤标志压到 2 帧续命）、无限穿透、不撞地形、仆从（源原样）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.minionSlots = 5f;
            Projectile.timeLeft = 18000;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft *= 5;
            Projectile.minion = true;
        }
        /// <summary>
        /// 礼帽 AI：挂增益与续命 → 贴住主人头顶（重力翻转翻面）→ 按鼠标文字色脉动缩放 →
        /// 首帧喷登场尘 → 主人端在 1500 像素内索敌，每 5 帧抛出一件随机工具。
        /// </summary>
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            player.AddBuff(ModContent.BuffType<MagicHatBuff>(), 3600);
            if (player.dead)
            {
                modPlayer.magicHat = false;
            }
            if (modPlayer.magicHat)
            {
                Projectile.timeLeft = 2;
            }
            // 贴住主人头顶上方 60 像素；重力翻转时落到头顶下方并翻面
            Projectile.position.X = player.Center.X - Projectile.width / 2;
            Projectile.position.Y = player.Center.Y - Projectile.height / 2 + player.gfxOffY - 60f;
            if (player.gravDir == -1f)
            {
                Projectile.position.Y += 150f;
                Projectile.rotation = MathHelper.Pi;
            }
            else
            {
                Projectile.rotation = 0f;
            }
            Projectile.position.X = (int)Projectile.position.X;
            Projectile.position.Y = (int)Projectile.position.Y;
            // 随鼠标文字色微微脉动缩放（源原样）
            float pulse = Main.mouseTextColor / 200f - 0.35f;
            pulse *= 0.2f;
            Projectile.scale = pulse + 0.95f;
            // 登场尘
            if (Projectile.localAI[0] == 0f)
            {
                for (int i = 0; i < 50; i++)
                {
                    int dust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y + 16f), Projectile.width, Projectile.height - 16, DustID.BoneTorch, 0f, 0f, 0, default, 1f);
                    Main.dust[dust].velocity *= 2f;
                    Main.dust[dust].scale *= 1.15f;
                }
                Projectile.localAI[0] += 1f;
            }
            // 索敌与开火（只在主人端）
            if (Projectile.owner != Main.myPlayer)
            {
                return;
            }
            float targetX = Projectile.position.X;
            float targetY = Projectile.position.Y;
            float detectionRange = Range;
            bool enemyDetected = false;
            if (player.HasMinionAttackTargetNPC)
            {
                NPC npc = Main.npc[player.MinionAttackTargetNPC];
                if (npc.CanBeChasedBy(Projectile, false))
                {
                    float npcX = npc.position.X + npc.width / 2;
                    float npcY = npc.position.Y + npc.height / 2;
                    float dist = Math.Abs(targetX + Projectile.width / 2 - npcX) + Math.Abs(targetY + Projectile.height / 2 - npcY);
                    if (dist < detectionRange)
                    {
                        detectionRange = dist;
                        targetX = npcX;
                        targetY = npcY;
                        enemyDetected = true;
                    }
                }
            }
            else
            {
                for (int i = 0; i < Main.npc.Length; i++)
                {
                    NPC npc = Main.npc[i];
                    if (!npc.CanBeChasedBy(Projectile, true))
                    {
                        continue;
                    }
                    float npcX = npc.position.X + npc.width / 2;
                    float npcY = npc.position.Y + npc.height / 2;
                    float dist = Math.Abs(targetX + Projectile.width / 2 - npcX) + Math.Abs(targetY + Projectile.height / 2 - npcY);
                    if (dist < detectionRange)
                    {
                        detectionRange = dist;
                        targetX = npcX;
                        targetY = npcY;
                        enemyDetected = true;
                    }
                }
            }
            if (!enemyDetected)
            {
                return;
            }
            Projectile.ai[1] += 1f;
            if (Projectile.ai[1] % 5f != 0f)
            {
                return;
            }
            // 源这里写的是 Main.rand.Next(1, 2)（恒为 1），照抄成"每 5 帧恰好一件"
            int toolType = Utils.SelectRandom(Main.rand, new int[]
            {
                ModContent.ProjectileType<MagicUmbrella>(),
                ModContent.ProjectileType<MagicRifle>(),
                ModContent.ProjectileType<MagicHammer>(),
                ModContent.ProjectileType<MagicAxe>(),
                ModContent.ProjectileType<MagicBird>()
            });
            float velocityX = Main.rand.NextFloat(-10f, 10f);
            float velocityY = Main.rand.NextFloat(-15f, -8f);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                Projectile.oldPosition.X + Projectile.width / 2, Projectile.oldPosition.Y + Projectile.height / 2,
                velocityX, velocityY, toolType, Projectile.damage, 0f, Projectile.owner, 0f, 0f);
        }
        /// <summary>绘制色固定为半透明灰白（源原样，用于自发光观感）</summary>
        public override Color? GetAlpha(Color lightColor) => new Color(200, 200, 200, 200);
        /// <summary>礼帽本体不造成接触伤害（照源）</summary>
        public override bool? CanDamage() => false;
    }
}
