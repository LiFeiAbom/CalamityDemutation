using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 远古叶棱晶（SilvaCrystal） - 始源林海召唤头（SilvaHelmet）套装召唤的漂浮棱晶
    ///（按经典版灾厄 Projectiles/Summon/SilvaCrystal.cs 1:1 移植）。
    /// 钉在主人头顶上方 60 像素（重力翻转时镜到下方），锁定 1500 像素内可追击的敌人后，
    /// 每 25 帧朝目标随机方向射出 3 枚 <see cref="SilvaCrystalExplosion"/>（伤害继承自身）；
    /// 本体不造成接触伤害（<see cref="CanDamage"/> 返回 false），也不占仆从栏。
    /// 存续不靠 timeLeft，而是每帧由玩家侧 <c>sCrystal</c> 标志（由同名增益维护）压到 2 帧续命。
    /// 与源的差异：源里那段依赖 CalamityGlobalProjectile 的"仆从伤害变化时重算 Projectile.damage"本工程没有该全局，
    /// 改由 tML 原生的 <c>originalDamage</c> 机制实现（见 SilvaHelmet 生成处，等效）。
    /// </summary>
    public class SilvaCrystal : ModProjectile
    {
        /// <summary>
        /// 注册为可牺牲的召唤物，并接受玩家用"目标锁定"键指定的攻击目标
        /// </summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
        }
        /// <summary>
        /// 基础属性：32×32 碰撞箱；召唤物、不占仆从栏、入水不减速、穿地形、无限穿透、
        /// 存活 18000 帧（×5 后为 90000）、初始全透明（淡入）；按召唤职业结算
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.ignoreWater = true;
            Projectile.minion = true;
            Projectile.minionSlots = 0f;
            Projectile.timeLeft = 18000;
            Projectile.alpha = 255;
            Projectile.tileCollide = false;
            Projectile.timeLeft *= 5;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Summon;
        }
        /// <summary>
        /// AI：套装失效即消失；钉在主人头顶、淡入、喷彩尘；
        /// 按经典版的状态机锁定目标（ai[0] 0=搜寻 / &gt;0 计时 / &lt;0 冷却），到点射出三枚叶棱晶爆裂
        /// </summary>
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            if (!modPlayer.silvaSummon)
            {
                Projectile.active = false;
                return;
            }
            if (player.dead)
            {
                modPlayer.sCrystal = false;
            }
            if (modPlayer.sCrystal)
            {
                Projectile.timeLeft = 2;
            }
            // 钉在主人头顶上方 60 像素；位置取整消除抖动，重力翻转时镜到下方
            Projectile.position.X = player.Center.X - Projectile.width / 2;
            Projectile.position.Y = player.Center.Y - Projectile.height / 2 + player.gfxOffY - 60f;
            if (player.gravDir == -1f)
            {
                Projectile.position.Y += 120f;
                Projectile.rotation = MathHelper.Pi;
            }
            else
            {
                Projectile.rotation = 0f;
            }
            Projectile.position.X = (int)Projectile.position.X;
            Projectile.position.Y = (int)Projectile.position.Y;
            Projectile.velocity = Vector2.Zero;
            Projectile.alpha -= 5;
            if (Projectile.alpha < 0)
            {
                Projectile.alpha = 0;
            }
            if (Projectile.direction == 0)
            {
                Projectile.direction = player.direction;
            }
            if (Projectile.alpha == 0 && Main.rand.NextBool(15))
            {
                Dust silvaDust = Main.dust[Dust.NewDust(Projectile.Top, 0, 0, DustID.RainbowMk2, 0f, 0f, 100, new Color(Main.DiscoR, 203, 103), 1f)];
                silvaDust.velocity.X = 0f;
                silvaDust.noGravity = true;
                silvaDust.fadeIn = 1f;
                silvaDust.position = Projectile.Center + Vector2.UnitY.RotatedByRandom(MathHelper.TwoPi) * (4f * Main.rand.NextFloat() + 26f);
                silvaDust.scale = 0.5f;
            }
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] >= 60f)
            {
                Projectile.localAI[0] = 0f;
            }
            if (Projectile.ai[0] < 0f)
            {
                Projectile.ai[0] += 1f;
            }
            if (Projectile.ai[0] == 0f)
            {
                int targetID = -1;
                float attackRange = 1500f;
                if (player.HasMinionAttackTargetNPC)
                {
                    NPC npc = Main.npc[player.MinionAttackTargetNPC];
                    if (npc.CanBeChasedBy(Projectile, false))
                    {
                        float targetDist = Projectile.Distance(npc.Center);
                        if (targetDist < attackRange && Collision.CanHitLine(Projectile.Center, 0, 0, npc.Center, 0, 0))
                        {
                            attackRange = targetDist;
                            targetID = npc.whoAmI;
                        }
                    }
                }
                if (targetID < 0)
                {
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC npc = Main.npc[i];
                        if (npc.CanBeChasedBy(Projectile, false))
                        {
                            float targetDistance = Projectile.Distance(npc.Center);
                            if (targetDistance < attackRange && Collision.CanHitLine(Projectile.Center, 0, 0, npc.Center, 0, 0))
                            {
                                attackRange = targetDistance;
                                targetID = i;
                            }
                        }
                    }
                }
                if (targetID != -1)
                {
                    Projectile.ai[0] = 1f;
                    Projectile.ai[1] = targetID;
                    Projectile.netUpdate = true;
                    return;
                }
            }
            if (Projectile.ai[0] > 0f)
            {
                int npcTrack = (int)Projectile.ai[1];
                if (!Main.npc[npcTrack].CanBeChasedBy(Projectile, false))
                {
                    Projectile.ai[0] = 0f;
                    Projectile.ai[1] = 0f;
                    Projectile.netUpdate = true;
                    return;
                }
                Projectile.ai[0] += 1f;
                if (Projectile.ai[0] >= 5f)
                {
                    // 源用 SafeDirectionTo（灾厄扩展）；tML 只有 DirectionTo，取向量的 X 定朝向，NaN 时兜底成竖直向上
                    Vector2 toTarget = Projectile.DirectionTo(Main.npc[npcTrack].Center);
                    if (toTarget.HasNaNs())
                    {
                        toTarget = Vector2.UnitY;
                    }
                    int projXDirection = (toTarget.X > 0f) ? 1 : -1;
                    Projectile.direction = projXDirection;
                    Projectile.ai[0] = -20f;
                    Projectile.netUpdate = true;
                    if (Projectile.owner == Main.myPlayer)
                    {
                        Vector2 attackPos = Main.npc[npcTrack].position + Main.npc[npcTrack].Size * Utils.RandomVector2(Main.rand, 0f, 1f) - Projectile.Center;
                        for (int j = 0; j < 3; j++)
                        {
                            Vector2 finalAttackPos = Projectile.Center + attackPos;
                            if (j > 0)
                            {
                                finalAttackPos = Projectile.Center + attackPos.RotatedByRandom(MathHelper.PiOver4) * (Main.rand.NextFloat() * 0.5f + 0.75f);
                            }
                            float hue = Main.rgbToHsl(new Color(Main.DiscoR, 203, 103)).X;
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), finalAttackPos, Vector2.Zero,
                                ModContent.ProjectileType<SilvaCrystalExplosion>(), Projectile.damage, Projectile.knockBack,
                                Projectile.owner, hue, Projectile.whoAmI);
                        }
                        return;
                    }
                }
            }
        }
        /// <summary>
        /// 绘制颜色：随淡入进度同步降低不透明度（源原样，最透时 alpha 保留 127）
        /// </summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255 - Projectile.alpha, 255 - Projectile.alpha, 255 - Projectile.alpha, 127 - Projectile.alpha / 2);
        }
        /// <summary>
        /// 自定义绘制：把同一张贴图绕中心按 90° 旋转叠画 4 次，构成缓慢呼吸的十字棱晶光晕
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Color colorArea = Lighting.GetColor((int)(Projectile.position.X + Projectile.width * 0.5) / 16, (int)((Projectile.position.Y + Projectile.height * 0.5) / 16.0));
            Vector2 projPos = Projectile.position + new Vector2(Projectile.width, Projectile.height) / 2f + Vector2.UnitY * Projectile.gfxOffY - Main.screenPosition;
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Color colorAlpha = Projectile.GetAlpha(colorArea);
            Vector2 halfFrame = frame.Size() / 2f;
            float scaleFactor = (float)Math.Cos(MathHelper.TwoPi * (Projectile.localAI[0] / 60f)) + 3f + 3f;
            for (float k = 0f; k < 4f; k += 1f)
            {
                Main.EntitySpriteDraw(texture, projPos + Vector2.UnitY.RotatedBy(k * MathHelper.PiOver2) * scaleFactor,
                    frame, colorAlpha * 0.2f, Projectile.rotation, halfFrame, Projectile.scale, SpriteEffects.None, 0);
            }
            return false;
        }
        /// <summary>
        /// 本体不造成接触伤害（伤害全部由叶棱晶爆裂结算）
        /// </summary>
        public override bool? CanDamage() => false;
    }
}
