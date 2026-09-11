using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Typeless
{
    /// <summary>
    /// 奥米加蓝触手（OmegaBlueTentacle） - 奥米加蓝（OmegaBlue）套装的自动触手。
    /// 归入 Typeless 目录：本弹幕未设置 Projectile.DamageType（使用默认无类别伤害），不吃任何特定伤害加成。
    /// 由 CalamityDemutationPlayer 在穿着套装时维持 6 条（ai[1] = 0~5 为触手序号，按 60° 间隔环绕玩家），
    /// 基础伤害为通用伤害 ApplyTo(1500)、击退 8；触手平时贴玩家环绕，会主动扑向附近敌人，命中时吸血治疗玩家。
    /// 按 Y 触发「深渊疯狂」（omegaBlueHentai）后移速、索敌范围提升且攻击必定暴击。
    /// </summary>
    internal class OmegaBlueTentacle:ModProjectile
    {
        // ── 实例字段 ──
        /// <summary>
        /// 是否已把分段位置初始化到出生点
        /// </summary>
        public bool initSegments = false;
        /// <summary>
        /// 触手 6 个骨骼节点（0 锚在玩家、5 为头部）
        /// </summary>
        public Vector2[] segment = new Vector2[6];
        // ── 属性 ──
        /// <summary>
        /// 弹幕归属玩家
        /// </summary>
        private Player Owner => Main.player[Projectile.owner];
        // ── 生命周期方法 ──
        /// <summary>
        /// 弹幕基础属性：小判定箱、无限穿透、高频局部无敌以支持持续吸血
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 24;                       // 判定箱宽（像素）
            Projectile.height = 24;                      // 判定箱高（像素）
            Projectile.timeLeft = 8;                     // 仅 8 帧；穿套装时由 AI 每帧续期续命
            Projectile.friendly = true;                  // 友方弹幕
            Projectile.tileCollide = false;              // 穿墙追踪，不被地形阻挡
            Projectile.ignoreWater = true;               // 水中不减速
            Projectile.penetrate = -1;                   // 无限穿透
            Projectile.usesLocalNPCImmunity = true;      // 使用弹幕独立的命中无敌计时
            Projectile.localNPCHitCooldown = 10;         // 同一目标 10 帧内不重复命中
        }
        /// <summary>
        /// 首次生成时把 6 个骨骼节点都落在弹幕中心，避免第一帧从原点拉扯出长线
        /// </summary>
        public override bool PreAI()
        {
            if (!initSegments)
            {
                initSegments = true;
                for (int i = 0; i < 6; i++)
                {
                    segment[i] = Projectile.Center;
                }
            }
            return true;
        }
        /// <summary>
        /// 主 AI：维持存活（穿套装时每帧续期）→ 头部状态机（跟随环绕点/扑击附近敌人）→
        /// 更新 6 段骨骼节点的中点松弛 → 深渊疯狂时追加位移与平滑尘埃拖尾。
        /// ai[0] 状态机：>0 正常跟随；==0 归位（回到范围内时重掷初速）；-1 快速归位模式；
        ///                >120 触发扑击，选定目标后取负表示「正在冲刺」。
        /// ai[1] 为触手序号（0~5），决定其环绕方向（序号 ×60°）。
        /// </summary>
        public override void AI()
        {
            bool madness = Owner.GetModPlayer<CalamityDemutationPlayer>().omegaBlueHentai;   // 是否处于深渊疯狂（按 Y 触发）
            // 玩家存活且仍穿着套装时每帧把 timeLeft 回满，触手靠这一行维持不消失
            if (Owner.active && Owner.GetModPlayer<CalamityDemutationPlayer>().omegaBlueSet)
                Projectile.timeLeft = 8;
            // Tentacle head movement (homing)
            // 头部跟随玩家本帧位移，避免玩家移动时触手被落下
            Vector2 playerVel = Owner.position - Owner.oldPosition;
            Projectile.position += playerVel;
            Projectile.ai[0]++;
            if (Projectile.ai[0] >= 0f)
            {
                // 环绕目标点：玩家中心右侧 50 像素，按触手序号 ai[1]×60° 旋转，6 条均分一圈
                Vector2 home = Owner.Center + new Vector2(50, 0).RotatedBy(MathHelper.ToRadians(60) * Projectile.ai[1]);
                Vector2 distance = home - Projectile.Center;
                float range = distance.Length();
                distance.Normalize();
                if (Projectile.ai[0] == 0f)
                {
                    // 计时归 0（通常是从快速归位模式回到范围内）：按距离决定加速或重掷初速
                    if (range > 13f)
                    {
                        Projectile.ai[0] = -1f; // If in fast mode, stay fast until back in range
                        if (range > 1300f)
                        {
                            Projectile.Kill();   // 离玩家过远：直接销毁，下一帧由套装效果补生成
                            return;
                        }
                    }
                    else
                    {
                        if (madness)
                            Projectile.ai[0] = 120f;   // 疯狂模式：归位即立刻获得一次扑击机会
                        Projectile.velocity.Normalize();
                        Projectile.velocity *= 3f + Main.rand.NextFloat(3f);   // 归位时重掷 3~6 的初速
                        Projectile.netUpdate = true;
                    }
                }
                else
                {
                    distance /= 8f;   // 正常跟随：向环绕点的牵引力取 1/8，运动更柔和
                }
                if (range > 120f) //switch to fast return mode
                {
                    Projectile.ai[0] = -1f;   // 离环绕点过远：切快速归位模式
                    Projectile.netUpdate = true;
                }
                Projectile.velocity += distance;
                if (range > 30f)
                    Projectile.velocity *= 0.96f;   // 距离越远阻尼越强，防止速度失控
                if (Projectile.ai[0] > 120f) //attack nearby enemy
                {
                    // 计时到达阈值：开始一次扑击，随机化下一次的攻击间隔（10~19）
                    Projectile.ai[0] = 10 + Main.rand.Next(10);
                    float maxDistance = madness ? 900f : 600f;   // 疯狂模式索敌范围扩大到 900
                    int target = -1;
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        if (npc.CanBeChasedBy(Projectile))
                        {
                            float npcDistance = Projectile.Distance(npc.Center);
                            if (npcDistance < maxDistance)
                            {
                                maxDistance = npcDistance;
                                target = npc.whoAmI;
                            }
                        }
                    }
                    if (target != -1)
                    {
                        // 以 13 的速度冲向目标，并叠加目标速度、抵消玩家速度，让扑击更贴脸
                        Projectile.velocity = Vector2.Normalize(Main.npc[target].Center - Projectile.Center) * 13f + (Main.npc[target].velocity / 2f) - (playerVel / 2f);
                        Projectile.ai[0] *= -1f;   // 取负标记「正在冲刺」，冲刺期间不再累加计时
                    }
                    Projectile.netUpdate = true;
                }
            }
            //tentacle segment updates
            // 骨骼节点更新：0 号锚在玩家，1~4 用中点松弛互相牵拉，5 号跟随弹幕头部
            segment[0] = Owner.Center;
            for (int i = 1; i < 5; i++)
            {
                MoveSegment(segment[i - 1], ref segment[i], segment[i + 1]);
            }
            MoveSegment(segment[4], ref segment[5], Projectile.Center + Projectile.velocity);
            if (madness)
            {
                if (Projectile.ai[0] != -1f)
                    Projectile.ai[0]++;   // 疯狂模式：计时加快，扑击更频繁
                Projectile.position += Projectile.velocity;   // 额外叠加一次位移，动作更狂乱
                //SMOOTH ASS DUST
                // 平滑尘埃拖尾：按本帧位移方向每 3 像素补一颗尘埃，快速移动时也不断线
                Vector2 dustPos = Projectile.position + Projectile.velocity;
                Vector2 tickVel = dustPos - Projectile.oldPosition; //playerVel + projectile.velocity * 2f;
                dustPos += new Vector2(Projectile.width / 2, 0).RotatedBy(Projectile.rotation);
                dustPos += new Vector2(Projectile.width / 2 - 4, Projectile.height / 2 - 4);
                const float factor = 3f;   // 尘埃间距（像素）
                int limit = (int)(tickVel.Length() / factor);
                if (limit == 0)
                {
                    // 本帧位移不足一个间距：只补 1 颗
                    Dust d = Dust.NewDustPerfect(dustPos, DustID.PurificationPowder, Vector2.Zero, 100, Color.Transparent, 0.9f);
                    d.noGravity = true;
                    d.noLight = true;
                    d.fadeIn = 1f;
                }
                else
                {
                    tickVel.Normalize();
                    tickVel *= factor;
                    for (int i = 0; i <= limit; i++)
                    {
                        Dust d = Dust.NewDustPerfect(dustPos, DustID.PurificationPowder, Vector2.Zero, 100, Color.Transparent, 0.9f);
                        d.noGravity = true;
                        d.noLight = true;
                        d.fadeIn = 1f;
                        d.position -= tickVel * i;   // 沿位移反方向回铺，填补两帧之间的空隙
                    }
                }
            }
        }
        /// <summary>
        /// 伤害修正：深渊疯狂状态下强制本次命中暴击（modifiers.SetCrit 置满暴击）
        /// </summary>
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Main.player[Projectile.owner].GetModPlayer<CalamityDemutationPlayer>().omegaBlueHentai)
                modifiers.SetCrit();
        }
        /// <summary>
        /// 命中 NPC：本地端且玩家还有吸血额度时，按「敌人伤害 / 弹幕伤害」换算治疗量，
        /// 扣减玩家 lifeSteal 预算并生成原版 SpiritHeal（治疗光球）把血量补给玩家。
        /// 深渊疯狂下必定暴击、治疗更频繁，故返还一半预算以保持同样的吸血间隔。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 排除训练假人；lifeSteal 为玩家的每秒吸血预算
            if (Projectile.owner == Main.myPlayer && Main.player[Main.myPlayer].lifeSteal > 0f && target.type != NPCID.TargetDummy)
            {
                int healAmount = 10 * target.damage / Projectile.damage; //should always be around max, less if enemy has defense/DR
                if (healAmount > 0)
                {
                    Main.LocalPlayer.lifeSteal -= healAmount;
                    if (Main.LocalPlayer.GetModPlayer<CalamityDemutationPlayer>().omegaBlueHentai) //hentai always crits, this makes it have same lifesteal delay
                        Main.LocalPlayer.lifeSteal += healAmount / 2;
                    // 生成 SpiritHeal：ai[0]=玩家索引、ai[1]=治疗量
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ProjectileID.SpiritHeal, 0, 0f, Projectile.owner, Projectile.owner, healAmount);
                }
            }
        }
        /// <summary>
        /// 命中玩家（PvP / 被反弹等情形）：与命中 NPC 相同，按双方伤害比例换算治疗量并生成 SpiritHeal。
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Projectile.owner == Main.myPlayer && Main.player[Main.myPlayer].lifeSteal > 0f)
            {
                int healAmount = 10 * info.Damage / Projectile.damage; //should always be around max, less if enemy has defense/DR
                if (healAmount > 0)
                {
                    Main.LocalPlayer.lifeSteal -= healAmount;
                    if (Main.LocalPlayer.GetModPlayer<CalamityDemutationPlayer>().omegaBlueHentai) //hentai always crits, this makes it have same lifesteal delay
                        Main.LocalPlayer.lifeSteal += healAmount / 2;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ProjectileID.SpiritHeal, 0, 0f, Projectile.owner, Projectile.owner, healAmount);
                }
            }
        }
        /// <summary>
        /// 自定义绘制：中断当前批次改用 Immediate 模式，套用玩家胸甲染料（GameShaders.Armor.ApplySecondary）
        /// 使触手跟随装备染色，然后依次画 5 段身体贴图（Segment1~5，各自朝向弹幕头部）
        /// 与头部贴图，最后恢复默认批次并返回 false（本体由本方法负责，不再走引擎绘制）。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            GameShaders.Armor.ApplySecondary(Owner.cBody, Owner, new DrawData?());   // 套用玩家胸甲染料着色器
            Texture2D texture2D13 = Terraria.GameContent.TextureAssets.Projectile[Type].Value;   // 头部贴图
            Texture2D segmentSprite = ModContent.Request<Texture2D>("CalamityDemutation/Content/Projectiles/Typeless/OmegaBlueTentacleSegment1").Value;
            for (int i = 0; i < 5; i++)
            {
                Projectile.rotation = (Projectile.Center - segment[i]).ToRotation();   // 每节朝向头部
                // 逐节换成对应段落贴图（i=0 用 Segment1，依次递增）
                switch (i)
                {
                    case 0:
                        break;
                    case 1:
                        segmentSprite = ModContent.Request<Texture2D>("CalamityDemutation/Content/Projectiles/Typeless/OmegaBlueTentacleSegment2").Value;
                        break;
                    case 2:
                        segmentSprite = ModContent.Request<Texture2D>("CalamityDemutation/Content/Projectiles/Typeless/OmegaBlueTentacleSegment3").Value;
                        break;
                    case 3:
                        segmentSprite = ModContent.Request<Texture2D>("CalamityDemutation/Content/Projectiles/Typeless/OmegaBlueTentacleSegment4").Value;
                        break;
                    case 4:
                        segmentSprite = ModContent.Request<Texture2D>("CalamityDemutation/Content/Projectiles/Typeless/OmegaBlueTentacleSegment5").Value;
                        break;
                    default:
                        break;
                }
                Main.spriteBatch.Draw(segmentSprite, segment[i] - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), segmentSprite.Bounds, Projectile.GetAlpha(lightColor), Projectile.rotation, segmentSprite.Bounds.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            }
            Projectile.rotation = (Projectile.Center - segment[5]).ToRotation();   // 头部朝向自身中心
            Main.spriteBatch.Draw(texture2D13, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), texture2D13.Bounds, Projectile.GetAlpha(lightColor), Projectile.rotation, texture2D13.Bounds.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        // ── 私有工具 ──
        /// <summary>
        /// 单次骨骼松弛：把节点 current 移向相邻两节点的中点，使触手呈平滑弧线而非硬直折线
        /// </summary>
        private static void MoveSegment(Vector2 previous, ref Vector2 current, Vector2 next)
        {
            current = previous + next;
            current /= 2;   // 取前后节点的中点
        }
    }
}
