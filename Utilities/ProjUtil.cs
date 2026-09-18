using CalamityDemutation.Content.Projectiles;
using CalamityDemutation.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
namespace CalamityDemutation.Utilities
{
    /// <summary>
    /// 通用工具类（弹幕部分）：提供弹幕残影绘制、自动追踪与寻敌等扩展方法
    /// </summary>
    internal static partial class CDUtil
    {
        /// <summary>
        /// 缓和的追逐行为（移植自 CWR 的 ChasingBehavior2）：让实体速度方向以
        /// <paramref name="homingStrength"/> 为单帧最大转角平滑转向目标点，
        /// 速度大小按 <paramref name="speedUpdates"/> 系数缩放。
        /// </summary>
        public static Vector2 ChasingBehavior2(this Entity entity, Vector2 targetCenter, float speedUpdates = 1, float homingStrength = 0.1f)
        {
            float targetAngle = entity.AngleTo(targetCenter);
            float f = entity.velocity.ToRotation().RotTowards(targetAngle, homingStrength);
            Vector2 speed = f.ToRotationVector2() * entity.velocity.Length() * speedUpdates;
            entity.velocity = speed;
            return speed;
        }
        /// <summary>
        /// 绘制弹幕残影（拖尾）。
        /// <paramref name="mode"/>：0 = 全部残影按透明度递减绘制；1 = 按残影数量递增步长绘制；
        /// 2 = 使用各帧旋转/翻转角度绘制。当配置开启性能模式（PerformanceMode）时，
        /// 跳过所有残影仅绘制本体，以降低弹幕数量庞大时的绘制开销。
        /// </summary>
        public static void DrawAfterimages(Projectile proj, int mode, Color lightColor, int typeOneIncrement = 1, Texture2D texture = null)
        {
            if (texture is null)
                texture = TextureAssets.Projectile[proj.type].Value;
            int frameHeight = texture.Height / Main.projFrames[proj.type];
            int frameY = frameHeight * proj.frame;
            float scale = proj.scale;
            float rotation = proj.rotation;
            Rectangle rectangle = new Rectangle(0, frameY, texture.Width, frameHeight);
            Vector2 origin = rectangle.Size() / 2f;
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (proj.spriteDirection == -1)
                spriteEffects = SpriteEffects.FlipHorizontally;
            bool failedToDrawAfterimages = false;
            if (ConfigSystem.Instance?.PerformanceMode != true)
            {
                Vector2 centerOffset = proj.Size / 2f;
                Color alphaColor = proj.GetAlpha(lightColor);
                switch (mode)
                {
                    // 模式0：遍历全部历史位置绘制残影，越旧的位置透明度越低（均匀渐隐拖尾）
                    case 0:
                        for (int i = 0; i < proj.oldPos.Length; ++i)
                        {
                            Vector2 drawPos = proj.oldPos[i] + centerOffset - Main.screenPosition + new Vector2(0f, proj.gfxOffY);
                            // 残影透明度随年龄线性递减：i 越大（越旧）alpha 越小
                            Color color = alphaColor * ((float)(proj.oldPos.Length - i) / (float)proj.oldPos.Length);
                            Main.spriteBatch.Draw(texture, drawPos, new Rectangle?(rectangle), color, rotation, origin, scale, spriteEffects, 0f);
                        }
                        break;
                    // 模式1：以 TrailCacheLength 为依据、按 typeOneIncrement 步长隔帧取样绘制残影
                    case 1:
                        int increment = Math.Max(1, typeOneIncrement);
                        Color drawColor = alphaColor;
                        int afterimageCount = ProjectileID.Sets.TrailCacheLength[proj.type];
                        float afterimageColorCount = (float)afterimageCount * 1.5f;
                        int k = 0;
                        while (k < afterimageCount)
                        {
                            Vector2 drawPos = proj.oldPos[k] + centerOffset - Main.screenPosition + new Vector2(0f, proj.gfxOffY);
                            // 越靠后的残影（k 越大）颜色越暗，drawColor 逐帧累乘实现非线性渐隐
                            if (k > 0)
                            {
                                float colorMult = (float)(afterimageCount - k);
                                drawColor *= colorMult / afterimageColorCount;
                            }
                            Main.spriteBatch.Draw(texture, drawPos, new Rectangle?(rectangle), drawColor, rotation, origin, scale, spriteEffects, 0f);
                            k += increment;
                        }
                        break;
                    // 模式2：每个残影使用各自记录过的旋转角/翻转方向绘制（适合自带旋转的弹幕）
                    case 2:
                        for (int i = 0; i < proj.oldPos.Length; ++i)
                        {
                            float afterimageRot = proj.oldRot[i];
                            SpriteEffects sfxForThisAfterimage = proj.oldSpriteDirection[i] == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                            Vector2 drawPos = proj.oldPos[i] + centerOffset - Main.screenPosition + new Vector2(0f, proj.gfxOffY);
                            Color color = alphaColor * ((float)(proj.oldPos.Length - i) / (float)proj.oldPos.Length);
                            Main.spriteBatch.Draw(texture, drawPos, new Rectangle?(rectangle), color, afterimageRot, origin, scale, sfxForThisAfterimage, 0f);
                        }
                        break;
                    default:
                        // 未知的 mode：标记失败，稍后退化为只绘制本体
                        failedToDrawAfterimages = true;
                        break;
                }
            }
            // 性能模式 / 弹幕未登记残影缓存 / 模式非法时：只绘制弹幕本体（无拖尾）
            if (ConfigSystem.Instance?.PerformanceMode == true || ProjectileID.Sets.TrailCacheLength[proj.type] <= 0 || failedToDrawAfterimages)
            {
                Vector2 startPos = proj.Center;
                Main.spriteBatch.Draw(texture, startPos - Main.screenPosition + new Vector2(0f, proj.gfxOffY), rectangle, proj.GetAlpha(lightColor), rotation, origin, scale, spriteEffects, 0f);
            }
        }
        /// <summary>
        /// 扩大弹幕碰撞箱：以弹幕中心为锚点重算 position 与 width/height（视觉贴图不变，
        /// 仅放大命中判定）。共四个重载：(width, height) 直接给定宽高；newSize 指定正方形边长；
        /// Vector2 newSize 取整后转发；expandRatio 按原宽高的倍数缩放。
        /// </summary>
        public static void ExpandHitboxBy(this Projectile projectile, int width, int height)
        {
            projectile.position = projectile.Center;
            projectile.width = width;
            projectile.height = height;
            projectile.position -= projectile.Size * 0.5f;
        }
        /// <summary>
        /// 扩大弹幕碰撞箱（正方形）：宽高都设为 <paramref name="newSize"/>，转发到 (width,height) 重载
        /// </summary>
        public static void ExpandHitboxBy(this Projectile projectile, int newSize)
        {
            projectile.ExpandHitboxBy(newSize, newSize);
        }
        /// <summary>
        /// 扩大弹幕碰撞箱（向量形式）：把 <paramref name="newSize"/> 取整后转发到 (width,height) 重载
        /// </summary>
        public static void ExpandHitboxBy(this Projectile projectile, Vector2 newSize)
        {
            projectile.ExpandHitboxBy((int)newSize.X, (int)newSize.Y);
        }
        /// <summary>
        /// 扩大弹幕碰撞箱（倍率形式）：按原宽高各乘 <paramref name="expandRatio"/> 后取整，转发到 (width,height) 重载
        /// </summary>
        public static void ExpandHitboxBy(this Projectile projectile, float expandRatio)
        {
            projectile.ExpandHitboxBy((int)((float)projectile.width * expandRatio), (int)((float)projectile.height * expandRatio));
        }
        /// <summary>
        /// 弹幕爆炸（移植自 CWR 的 Explode）：播放爆炸声、临时把碰撞箱扩大为
        /// <paramref name="blastRadius"/> 半径并立即结算一次伤害判定，之后还原碰撞箱。
        /// 常用于弹幕 Kill 时扩大命中范围补一次伤害。
        /// </summary>
        public static void Explode(this Projectile projectile, int blastRadius = 120, SoundStyle explosionSound = default, bool spanSound = true)
        {
            Vector2 originalPosition = projectile.position;
            int originalWidth = projectile.width;
            int originalHeight = projectile.height;
            if (spanSound)
            {
                _ = SoundEngine.PlaySound(explosionSound == default ? SoundID.Item14 : explosionSound, projectile.Center);
            }
            projectile.position = projectile.Center;
            projectile.width = projectile.height = blastRadius * 2;
            projectile.position.X -= projectile.width / 2;
            projectile.position.Y -= projectile.height / 2;
            projectile.maxPenetrate = -1;
            projectile.penetrate = -1;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = -1;
            projectile.Damage();
            projectile.position = originalPosition;
            projectile.width = originalWidth;
            projectile.height = originalHeight;
        }
        /// <summary>
        /// 寻找距离指定位置最近的合法敌人（移植自 CWR 的 FindClosestNPC）。
        /// <paramref name="ignoreTiles"/> 为 false 时要求两点间视线无遮挡；
        /// <paramref name="bossPriority"/> 为 true 时优先锁定 Boss（含血肉墙）。
        /// </summary>
        public static NPC FindClosestNPC(this Vector2 origin, float maxDistanceToCheck, bool ignoreTiles = true, bool bossPriority = false)
        {
            NPC closestTarget = null;
            float distance = maxDistanceToCheck;
            if (bossPriority)
            {
                bool bossFound = false;
                for (int index2 = 0; index2 < Main.npc.Length; index2++)
                {
                    if ((bossFound && !Main.npc[index2].boss && Main.npc[index2].type != NPCID.WallofFleshEye) || !Main.npc[index2].CanBeChasedBy())
                    {
                        continue;
                    }
                    float extraDistance2 = (Main.npc[index2].width / 2) + (Main.npc[index2].height / 2);
                    bool canHit2 = true;
                    if (extraDistance2 < distance && !ignoreTiles)
                    {
                        canHit2 = Collision.CanHit(origin, 1, 1, Main.npc[index2].Center, 1, 1);
                    }
                    if (Vector2.Distance(origin, Main.npc[index2].Center) < distance + extraDistance2 && canHit2)
                    {
                        if (Main.npc[index2].boss || Main.npc[index2].type == NPCID.WallofFleshEye)
                        {
                            bossFound = true;
                        }
                        distance = Vector2.Distance(origin, Main.npc[index2].Center);
                        closestTarget = Main.npc[index2];
                    }
                }
            }
            else
            {
                for (int index = 0; index < Main.npc.Length; index++)
                {
                    if (Main.npc[index].CanBeChasedBy())
                    {
                        float extraDistance = (Main.npc[index].width / 2) + (Main.npc[index].height / 2);
                        bool canHit = true;
                        if (extraDistance < distance && !ignoreTiles)
                        {
                            canHit = Collision.CanHit(origin, 1, 1, Main.npc[index].Center, 1, 1);
                        }
                        if (Vector2.Distance(origin, Main.npc[index].Center) < distance + extraDistance && canHit)
                        {
                            distance = Vector2.Distance(origin, Main.npc[index].Center);
                            closestTarget = Main.npc[index];
                        }
                    }
                }
            }
            return closestTarget;
        }
        /// <summary>
        /// 在给定范围内寻找最近的合法敌人（排除友方、生命过小、无法被追踪的目标）
        /// </summary>
        public static NPC FindClosestTarget(Vector2 center, float maxDist, bool ignoreTiles = true)
        {
            float distStored = maxDist;
            NPC acceptableTarget = null;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                // 把敌人体型尺寸计入搜索半径：越大的敌人即使中心距离稍远也算“可达”
                float exDist = npc.width + npc.height;
                // 过滤：非活跃、友方、最大生命过小或不可被追踪的目标一律跳过
                if (!npc.active || npc.friendly || npc.lifeMax < 5 || !npc.CanBeChasedBy(center, false))
                    continue;
                // 粗筛：中心距离超过当前半径 + 体型余量则直接跳过
                if (Vector2.Distance(center, npc.Center) > distStored + exDist)
                    continue;
                float curNpcDist = Vector2.Distance(npc.Center, center);
                // 只接受比当前最近目标更近的敌人，不断收紧搜索半径
                if (curNpcDist < distStored)
                {
                    // ignoreTiles 为 false 时，要求两点间视线无遮挡才视为目标
                    if (!ignoreTiles)
                    {
                        if (!Collision.CanHitLine(center, 1, 1, npc.Center, 1, 1))
                            continue;
                    }
                    distStored = curNpcDist;
                    acceptableTarget = npc;
                }
            }
            return acceptableTarget;
        }
        /// <summary>
        /// 由数值 num 生成固定方向 (1,1) 的单位速度向量（各分量约 0.7071），用作斜向初速度。
        /// </summary>
        public static Vector2 GiveVelocity(float num)
        {
            Vector2 velocity = new(num, num);
            velocity.Normalize();
            return velocity;
        }
        /// <summary>
        /// 弹幕自动追踪：在 <paramref name="distance"/> 像素内寻找最近的可攻击敌人并转向飞向它。
        /// </summary>
        /// <param name="speed">追踪转向速度</param>
        /// <param name="inertia">惯性（越大转向越平滑）</param>
        /// <param name="maxAngleChange">每帧最大转向角度（度），为空则不限制</param>
        /// <param name="ignoreTile">是否忽略地形阻挡</param>
        public static void HomeInNPC(this Projectile proj, float distance, float speed, float inertia, float? maxAngleChange = null, bool ignoreTile = true)
        {
            NPC npc = FindClosestTarget(proj.Center, distance, ignoreTile);
            if (npc is not null)
            {
                proj.HomingTarget(npc.Center, distance, speed, inertia, maxAngleChange);
            }
        }
        /// <summary>
        /// 弹幕自动寻敌追踪（供其它类复用）：仅对友方弹幕生效，
        /// 在限定距离内寻找最近的合法敌人并平滑转向飞向它。
        /// 追踪期间临时提升 extraUpdates 以模拟“更快的更新步数”让转向更顺滑，
        /// 未找到目标时恢复原 extraUpdates。
        /// </summary>
        public static void HomeInOnNPC(Projectile projectile, bool ignoreTiles, float distanceRequired, float homingVelocity, float inertia, float? maxAngleChange = null)
        {
            if (!projectile.friendly)
                return;
            // 首次调用时记录弹幕原始的 extraUpdates，供追踪结束后恢复
            if (projectile.GetGlobalProjectile<CalamityDemutationGlobalProjectile>().defExtraUpdates == -1)
                projectile.GetGlobalProjectile<CalamityDemutationGlobalProjectile>().defExtraUpdates = projectile.extraUpdates;
            Vector2 destination = projectile.Center;
            float maxDistance = distanceRequired;
            int targetIndex = -1;
            // 在锁定范围内寻找最近的合法敌人；extraDistance 把敌人的体型半径也算入距离
            foreach (NPC npc in Main.ActiveNPCs)
            {
                float extraDistance = (npc.width / 2) + (npc.height / 2);
                if (!npc.CanBeChasedBy(projectile, false) || !projectile.WithinRange(npc.Center, maxDistance + extraDistance))
                    continue;
                float currentNPCDist = Vector2.Distance(npc.Center, projectile.Center);
                // 记录当前最近目标；ignoreTiles 为 false 时额外要求视线无阻挡
                if ((currentNPCDist < maxDistance) && (ignoreTiles || Collision.CanHit(projectile.Center, 1, 1, npc.Center, 1, 1)))
                {
                    maxDistance = currentNPCDist;
                    targetIndex = npc.whoAmI;
                }
            }
            if (targetIndex != -1)
            {
                destination = Main.npc[targetIndex].Center;
                // 追踪期间把 extraUpdates +1，让转向以更高频率计算、运动更平滑
                projectile.extraUpdates = projectile.GetGlobalProjectile<CalamityDemutationGlobalProjectile>().defExtraUpdates + 1;
                Vector2 homeDirection = (destination - projectile.Center).SafeNormalize(Vector2.UnitY);
                // 惯性插值：原速度与目标方向速度按 inertia 加权平均，inertia 越大转向越柔和
                Vector2 newVelocity = (projectile.velocity * inertia + homeDirection * homingVelocity) / (inertia + 1f);
                if (maxAngleChange.HasValue)
                {
                    // 若单帧转向角超限，则把新速度方向夹取到允许的最大转角内（仅改方向、保持速率）
                    float currentAngle = (float)Math.Atan2(projectile.velocity.Y, projectile.velocity.X);
                    float targetAngle = (float)Math.Atan2(newVelocity.Y, newVelocity.X);
                    float angleDifference = MathHelper.WrapAngle(targetAngle - currentAngle);
                    float maxChangeRadians = MathHelper.ToRadians(maxAngleChange.Value);
                    if (Math.Abs(angleDifference) > maxChangeRadians)
                    {
                        float clampedAngle = currentAngle + Math.Sign(angleDifference) * maxChangeRadians;
                        float speed = newVelocity.Length();
                        newVelocity = new Vector2((float)Math.Cos(clampedAngle), (float)Math.Sin(clampedAngle)) * speed;
                    }
                }
                projectile.velocity = newVelocity;
            }
            else
            {
                // 未锁定到目标：恢复弹幕原有的更新步数
                projectile.extraUpdates = projectile.GetGlobalProjectile<CalamityDemutationGlobalProjectile>().defExtraUpdates;
            }
        }
        /// <summary>
        /// 让弹幕转向指定目标点：若超过 <paramref name="distRequired"/> 距离则不追踪；
        /// 转向时按惯性平滑插值，可选限制每帧最大转角。
        /// </summary>
        public static void HomingTarget(this Projectile proj, Vector2 target, float distRequired, float speed, float inertia, float? maxAngleChange = null)
        {
            if (distRequired > 0 && Vector2.Distance(proj.Center, target) > distRequired)
                return;
            Vector2 home = (target - proj.Center).SafeNormalize(Vector2.UnitY);
            Vector2 velo = (proj.velocity * inertia + home * speed) / (inertia + 1f);
            if (maxAngleChange.HasValue)
            {
                float curAngle = proj.velocity.ToRotation();
                float tarAngle = velo.ToRotation();
                float angleDiffer = MathHelper.WrapAngle(tarAngle - curAngle);
                float maxRadians = MathHelper.ToRadians(maxAngleChange.Value);
                if (Math.Abs(angleDiffer) > maxRadians)
                {
                    float clampedAngle = curAngle + Math.Sign(angleDiffer) * maxRadians;
                    float setSpeed = velo.Length();
                    velo = new Vector2((float)Math.Cos(clampedAngle), (float)Math.Sin(clampedAngle)) * setSpeed;
                }
            }
            proj.velocity = velo;
        }
        /// <summary>
        /// 判断弹幕是否由本地玩家拥有（owner == Main.myPlayer），
        /// 多人环境下用于只让弹幕主人在本机执行生成/特效，避免重复。
        /// </summary>
        public static bool IsOwnedByLocalPlayer(this Projectile projectile)
        {
            return projectile.owner == Main.myPlayer;
        }
        /// <summary>
        /// 生成随机方向的单位速度向量并乘以随机速率（用于弹幕/粒子散射）。
        /// </summary>
        public static Vector2 RandomVelocity(float directionMult, float speedLowerLimit, float speedCap, float speedMult = 0.1f)
        {
            Vector2 velocity = new(Main.rand.NextFloat(-directionMult, directionMult), Main.rand.NextFloat(-directionMult, directionMult));
            // 重新随机以避免零向量归一化时除零
            while (velocity.X == 0f && velocity.Y == 0f)
            {
                velocity = new Vector2(Main.rand.NextFloat(-directionMult, directionMult), Main.rand.NextFloat(-directionMult, directionMult));
            }
            velocity.Normalize();
            velocity *= Main.rand.NextFloat(speedLowerLimit, speedCap) * speedMult;
            return velocity;
        }
        /// <summary>
        /// 随机角度平滑转向：把当前角度向目标角度最多旋转 <paramref name="maxChange"/> 弧度。
        /// 移植自 CWR 的 RotTowards，供 ChasingBehavior2 等追踪逻辑使用。
        /// </summary>
        public static float RotTowards(this float curAngle, float targetAngle, float maxChange)
        {
            curAngle = MathHelper.WrapAngle(curAngle);
            targetAngle = MathHelper.WrapAngle(targetAngle);
            if (curAngle < targetAngle)
            {
                if (targetAngle - curAngle > (float)Math.PI)
                {
                    curAngle += (float)Math.PI * 2f;
                }
            }
            else if (curAngle - targetAngle > (float)Math.PI)
            {
                curAngle -= (float)Math.PI * 2f;
            }
            curAngle += MathHelper.Clamp(targetAngle - curAngle, 0f - maxChange, maxChange);
            return MathHelper.WrapAngle(curAngle);
        }
        /// <summary>
        /// 取从实体中心指向 <paramref name="destination"/> 的单位向量；两点重合时返回
        /// <paramref name="fallback"/>（未提供则用零向量），避免 SafeNormalize 除零得到 NaN。
        /// </summary>
        public static Vector2 SafeDirectionTo(this Entity entity, Vector2 destination, Vector2? fallback = null)
        {
            if (!fallback.HasValue)
            {
                fallback = Vector2.Zero;
            }
            return (destination - entity.Center).SafeNormalize(fallback.Value);
        }
        /// <summary>
        /// 返回从 <paramref name="vr1"/> 指向 <paramref name="vr2"/> 的位移向量（vr2 - vr1）。
        /// </summary>
        public static Vector2 To(this Vector2 vr1, Vector2 vr2)
        {
            return vr2 - vr1;
        }
        /// <summary>
        /// 向量单位化（长度归一为 1）；零向量返回零向量而非除法结果。
        /// </summary>
        public static Vector2 UnitVector(this Vector2 vr)
        {
            return vr.SafeNormalize(Vector2.Zero);
        }
        /// <summary>
        /// 弹幕齐射（移植自灾厄 CalamityUtils.ProjectileBarrage）：在 originVec 附近的随机点生成一颗弹幕并射向 targetPos。
        /// 生成点：X 偏移取 [xOffsetMin, xOffsetMax] 的随机值、正负由 fromRight 决定（它只是符号，不是真的"从右侧"）；
        /// Y 偏移取 [yOffsetMin, yOffsetMax] 的随机值、正负再随机一次，故上下都会出。
        /// 速度：先按目标方向求出向量、加 inaccuracyOffset 抖动，单位化后乘 projSpeed。
        /// clamped 为 true 时速度先 ×150、再把两个分量各自夹在 ±15 内——形成"只能沿近似斜向飞且打不准"的散射观感
        /// （灾厄的 InfernalBlade / LiquidBlade / AstralPikeProj 都用这个口径）。
        /// </summary>
        /// <returns>生成出的弹幕实例</returns>
        public static Projectile ProjectileBarrage(IEntitySource source, Vector2 originVec, Vector2 targetPos, bool fromRight, float xOffsetMin, float xOffsetMax, float yOffsetMin, float yOffsetMax, float projSpeed, int projType, int damage, float knockback, int owner, bool clamped = false, float inaccuracyOffset = 5f)
        {
            float xPos = originVec.X + Main.rand.NextFloat(xOffsetMin, xOffsetMax) * fromRight.ToDirectionInt();
            float yPos = originVec.Y + Main.rand.NextFloat(yOffsetMin, yOffsetMax) * Main.rand.NextBool().ToDirectionInt();
            Vector2 spawnPosition = new Vector2(xPos, yPos);
            Vector2 velocity = targetPos - spawnPosition;
            velocity.X += Main.rand.NextFloat(-inaccuracyOffset, inaccuracyOffset);
            velocity.Y += Main.rand.NextFloat(-inaccuracyOffset, inaccuracyOffset);
            velocity.Normalize();
            velocity *= projSpeed * (clamped ? 150f : 1f);
            if (clamped)
            {
                velocity.X = MathHelper.Clamp(velocity.X, -15f, 15f);
                velocity.Y = MathHelper.Clamp(velocity.Y, -15f, 15f);
            }
            return Projectile.NewProjectileDirect(source, spawnPosition, velocity, projType, damage, knockback, owner);
        }
    }
}
