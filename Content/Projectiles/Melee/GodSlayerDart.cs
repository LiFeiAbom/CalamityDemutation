using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 弑神飞镖·改（GodKiller2） - 弑神者（GodSlayer）近战套装的「命中触发」两段式飞镖。
    /// 玩家近战命中（或近战弹幕命中）时由 CalamityDemutationPlayer 生成，伤害 = 500 + 手持武器伤害/2，60 帧冷却。
    /// 与灾厄原版 GodKiller 弹幕（受击反击、单段直飞）的区别：
    /// 1) 触发方式不同，二代由「打人」触发、冷却 60 帧，一代由「被打 80+」触发；
    /// 2) 二代是两段式——ai[0]==0 时贴身挂载 50 帧，按 3 帧间隔向四周甩出 8 枚子飞镖（挂载形态不造成伤害）；
    /// 3) 子飞镖 ai[0]≠0，先减速 40 帧再以不断加速的追踪速度（ai[1] 上限 25）扑向敌人，并带 10 段残影；
    /// 4) 一代靠终点 100×100 判定箱收尾，二代没有该机制，改为持续追踪 + 残影表现。
    /// 贴图使用本目录的 GodSlayerDart.png（未复用灾厄 GodKiller 的贴图），发光描边用 GodslayerDartGlow.png（见 PostDraw）。
    /// </summary>
    internal class GodSlayerDart:ModProjectile
    {
        // ── 实例字段 ──
        /// <summary>
        /// 是否已完成首次初始化（用于只设置一次 timeLeft）
        /// </summary>
        public bool initialized = false;
        /// <summary>
        /// 标记处于挂载形态（仅作记录，实际分支以 ai[0] 判定）
        /// </summary>
        public bool isMount = false;
        /// <summary>
        /// 挂载形态发射倒计时，归 0 时发射并重置为 3
        /// </summary>
        public int firedely = 2;
        /// <summary>
        /// 挂载形态已发射的子飞镖数（上限 8）
        /// </summary>
        public int hasfirecount = 0;
        /// <summary>
        /// 计划中的发射间隔（声明未使用，实际在 MountDark 中硬编码为 3）
        /// </summary>
        public int mountdartfirerelay = 3;
        /// <summary>
        /// 每枚挂载弹幕各自的随机起始扇区偏移（0~8）
        /// </summary>
        public int randomstart = Main.rand.Next(0, 9);
        // ── 属性 ──
        /// <summary>
        /// 弹幕归属玩家
        /// </summary>
        public Player Owner => Main.player[Projectile.owner];
        // ── 嵌套类型 ──
        /// <summary>
        /// 行为枚举（仅作语义标注，实际分支用 ai[0] 判定：0=挂载、非 0=飞行）
        /// </summary>
        public enum Dart
        {
            /// <summary>正常飞行形态（ai[0]≠0）</summary>
            NorDart,
            /// <summary>挂载在玩家身上的形态（ai[0]==0）</summary>
            MountDart,
        }
        // ── 生命周期方法 ──
        /// <summary>
        /// 登记残影缓存：长度 10、模式 1，供 PreDraw 的 CDUtil.DrawAfterimages 使用
        /// </summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        }
        /// <summary>
        /// 弹幕基础属性：挂载形态 22×46 的窄高判定箱、穿透 1；timeLeft 会在 AI 中按形态覆写
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 22;            // 判定箱宽（像素）
            Projectile.height = 46;           // 判定箱高（像素），比一代更高更细
            Projectile.friendly = true;       // 友方弹幕
            Projectile.ignoreWater = true;    // 水中不减速
            Projectile.tileCollide = false;   // 无视地形
            Projectile.penetrate = 1;         // 只命中一次
            Projectile.timeLeft = 140;        // 初值占位，AI 首帧会按形态改成 50（挂载）或 360（飞行）
        }
        /// <summary>
        /// 两段式状态机：ai[0]==0 走挂载形态（贴身 50 帧放镖），ai[0]≠0 走飞行形态（减速后追踪）。
        /// 首次执行时按形态写入 timeLeft。
        /// </summary>
        public override void AI()
        {
            if (Projectile.ai[0] == 0f)
            {
                MountDark(Projectile, ref firedely, ref hasfirecount);
                if (initialized == false)
                {
                    Projectile.timeLeft = 50;   // 挂载形态存活 50 帧，够放完 8 枚飞镖
                    initialized = true;
                    isMount = true;
                }
            }
            else
            {
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;   // 贴图朝速度方向，+90° 校正
                DarkAI(Projectile);
                if (initialized == false)
                {
                    Projectile.timeLeft = 360;   // 飞行形态存活 360 帧
                    initialized = true;
                }
            }
        }
        /// <summary>
        /// 挂载形态（ai[0]==0）不可造成伤害，只有真正的子飞镖（ai[0]≠0）才能命中敌人
        /// </summary>
        public override bool? CanHitNPC(NPC target) => Projectile.ai[0] != 0f;
        /// <summary>
        /// 命中 NPC：对现代版 / 经典版灾厄分别取同名 GodSlayerInferno（神裁狱火）并施加 500 帧。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                target.AddBuff(calamity.Find<ModBuff>("GodSlayerInferno").Type, 500);
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                target.AddBuff(calamity1.Find<ModBuff>("GodSlayerInferno").Type, 500);
            }
        }
        /// <summary>
        /// 命中玩家：与命中 NPC 相同，施加 500 帧 GodSlayerInferno。
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                target.AddBuff(calamity.Find<ModBuff>("GodSlayerInferno").Type, 500);
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                target.AddBuff(calamity1.Find<ModBuff>("GodSlayerInferno").Type, 500);
            }
        }
        /// <summary>
        /// 消亡：仅飞行形态（ai[0]≠0）播放 Item89 音效并爆出 Butterfly + ShadowbeamStaff 两色尘埃；
        /// 挂载形态悄然消失，不产生任何特效。
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            if (Projectile.ai[0] != 0f)
            {
                SoundEngine.PlaySound(SoundID.Item89, Projectile.position);
                for (int j = 0; j < 5; j++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Butterfly, 0f, 0f, 100, default, 1.5f);
                    Main.dust[dust].velocity *= 3f;
                    if (Main.rand.NextBool())
                    {
                        Main.dust[dust].scale = 0.5f;
                        Main.dust[dust].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                    }
                }
                for (int k = 0; k < 10; k++)
                {
                    int dust2 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default, 2f);
                    Main.dust[dust2].noGravity = true;
                    Main.dust[dust2].velocity *= 5f;
                    dust2 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.ShadowbeamStaff, 0f, 0f, 100, default, 1.5f);
                    Main.dust[dust2].velocity *= 2f;
                }
            }
        }
        /// <summary>
        /// 绘制：挂载形态（ai[0]==0）不绘制本体；飞行形态交给 CDUtil.DrawAfterimages 画残影+本体
        /// （模式 1、步长 2，配合 SetStaticDefaults 里登记的 10 段残影缓存）。
        /// 两种情形都返回 false，避免引擎再画一遍本体。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.ai[0] == 0f)
            {
                return false;
            }
            else
                CDUtil.DrawAfterimages(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 2);
            return false;
        }
        /// <summary>
        /// 后置绘制：非挂载形态（真正的子飞镖）额外叠画一层 GodslayerDartGlow 发光描边。
        /// </summary>
        public override void PostDraw(Color lightColor)
        {
            if (!isMount)
            {
                Vector2 origin = new(11f, 23f);
                Main.EntitySpriteDraw(ModContent.Request<Texture2D>("CalamityDemutation/Content/Projectiles/Melee/GodslayerDartGlow").Value, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
            }
        }
        // ── 公开方法 ──
        /// <summary>
        /// 飞行形态 AI：短时减速蓄力，再以逐帧加速的追踪速度扑向 10000 像素内最近的敌人；
        /// 同时喷 ShadowbeamStaff + Butterfly 双色尘埃拖尾。
        /// ai[1] 记录当前追踪速度（0 起步、上限 25），由 CDUtil.HomeInOnNPC 消费。
        /// </summary>
        public static void DarkAI(Projectile projectile)
        {
            // 速度够快（任一轴 ≥2）时喷拖尾尘埃；循环 2 次，第 2 次采样位置偏移半个速度
            if (Math.Abs(projectile.velocity.X) >= 2f || Math.Abs(projectile.velocity.Y) >= 2f)
            {
                for (int i = 0; i < 2; i++)
                {
                    float shortXVel = 0f;
                    float shortYVel = 0f;
                    if (i == 1)
                    {
                        shortXVel = projectile.velocity.X * 0.5f;
                        shortYVel = projectile.velocity.Y * 0.5f;
                    }
                    // 主拖尾：ShadowbeamStaff 尘埃，随机放大、低速外扩、无重力
                    int d = Dust.NewDust(new Vector2(projectile.position.X + 3f + shortXVel, projectile.position.Y + 3f + shortYVel) - projectile.velocity * 0.5f, projectile.width - 8, projectile.height - 8, DustID.ShadowbeamStaff, 0f, 0f, 100, default, 1f);
                    Main.dust[d].scale *= 1f + Main.rand.Next(5) * 0.1f;
                    Main.dust[d].velocity *= 0.2f;
                    Main.dust[d].noGravity = true;
                    // 叠加 Butterfly 尘埃做淡入光点
                    d = Dust.NewDust(new Vector2(projectile.position.X + 3f + shortXVel, projectile.position.Y + 3f + shortYVel) - projectile.velocity * 0.5f, projectile.width - 8, projectile.height - 8, DustID.Butterfly, 0f, 0f, 100, default, 0.1f);
                    Main.dust[d].fadeIn = 1f + Main.rand.Next(5) * 0.1f;
                    Main.dust[d].velocity *= 0.05f;
                }
            }
            // 前 40 帧（timeLeft 360→320）持续减速蓄力
            if (projectile.timeLeft > 320)
            {
                projectile.velocity *= 0.9f;
            }
            // 之后进入追踪：ai[1] 作为追踪速度，每帧 +1.6 并夹取到 25 上限，越追越快
            if (projectile.timeLeft < 320)
            {
                float maxSpeed = 25f;
                float acceleration = 0.08f * 20f;
                float homeInSpeed = MathHelper.Clamp(projectile.ai[1] += acceleration, 0f, maxSpeed);
                // 10000 像素搜索范围、惯性 15、单帧最大转角 8°；ignoreTiles 取反自 tileCollide
                CDUtil.HomeInOnNPC(projectile, !projectile.tileCollide, 10000f, homeInSpeed, 15f, 8f);
            }
        }
        /// <summary>
        /// 挂载形态发射逻辑：以玩家「旋转后的坐骑中心」为出膛点，把 360° 均分成 8 份，
        /// 每 3 帧依次从玩家身上甩出一枚子飞镖（ai[0]=1 的飞行形态）；
        /// 起始角叠加 22.5° + 45°×randomstart 的随机偏移，使每次释放的扇形方向都不相同。
        /// </summary>
        public void MountDark(Projectile projectile, ref int firerelay, ref int hasfirecount)
        {
            Player Owner = Main.player[projectile.owner];
            Vector2 armPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, true);   // 玩家旋转后的出膛点
            var source = projectile.GetSource_FromThis(); ;
            firerelay--;
            float baseAngle = projectile.velocity.ToRotation();   // 以出生方向为整个扇形的基准角
            int numberOfProjectiles = 8;                          // 共 8 枚子飞镖
            float spreadAngle = MathHelper.ToRadians(360);        // 整圈分布
            float angleStep = spreadAngle / numberOfProjectiles;  // 每枚相隔 45°
            float randStartAngle = 22.5f + 45 * randomstart;      // 随机起始相位
            float currentAngle = baseAngle - spreadAngle / 2 + (angleStep * hasfirecount) + MathHelper.ToRadians(randStartAngle);
            Vector2 direction = new((float)Math.Cos(currentAngle), (float)Math.Sin(currentAngle));
            if (firerelay == 0 && hasfirecount < 8)
            {
                // 子飞镖：速度 32、击退 2、ai[0]=1（飞行形态），伤害沿用父弹幕
                Projectile.NewProjectile(source, armPosition, direction * 32f, ModContent.ProjectileType<GodSlayerDart>(), projectile.damage, 2f, projectile.owner, 1, 0f);
                firerelay = 3;
                hasfirecount++;
            }
        }
    }
}
