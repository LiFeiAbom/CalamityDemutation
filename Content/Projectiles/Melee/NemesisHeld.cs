using CalamityDemutation.Content.Projectiles.Melee.Core;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 天罚手持挥舞体（NemesisHeld，移植自 CalamityEntropy 的 <c>Nemesis/NemesisHeld.cs</c>）：
    /// CE 侧继承 <c>BaseKnife : BaseSwing</c>，本工程改继承等价的 <see cref="BaseSwingCO"/>。
    /// 一条挥舞体同时承载三种招式（由物品传入的 <c>Projectile.ai[0]</c> 决定）：
    /// <list type="bullet">
    /// <item><c>ai[0] == 0</c>（普通左键）：第 10 帧起可在挥砍途中出手，每次朝光标上方甩下 3 颗龙陨星（伤害 ÷4）。</item>
    /// <item><c>ai[0] == 1</c>（每 6 次的「天罚」）：改走一套更快更长的挥砍参数，甩出一枚 <see cref="EXNemesisProj"/>（伤害 ×5）。</item>
    /// <item><c>ai[0] == 2</c>（右键蓄力）：按住右键蓄满 140×更新率 帧，松开后甩出 <see cref="NemesisAlt"/>（伤害 ×10）；
    /// 蓄力期间刀刃旋转、判定关闭，并在玩家下方画一条橙色蓄力条。</item>
    /// </list>
    /// 真近战命中（<c>numHits == 0</c>）时在目标处迸发 6 颗 <c>ai[0] == 1</c> 的龙陨星（可回血）。
    /// <para>
    /// 与 CE 的差异：① 基类由 <c>BaseKnife</c> 换成 <see cref="BaseSwingCO"/>，<c>SetKnifeProperty</c> → <c>SetSwingProperty</c>，
    /// <c>SwingAIType</c> 派发内联（本处只用默认 <c>None</c>）；② <c>OtherMeleeSize</c> 基类没有
    /// （CE 由 <c>MeleeSize</c> 汇总后同时作用于刀身、离心量与判定箱），改为本类自持、只用于 <c>DrawSwing</c> 的剑身缩放；
    /// ③ 蓄力条贴图按本工程惯例绘制时 <c>ModContent.Request</c>（CE 走 <c>VaultLoaden</c> 加载期静态字段）；
    /// ④ <c>CEUtils.randVr</c> → <see cref="CDUtil.randVr"/>，描边 → <see cref="CDUtil.DrawRotatingMarginEffect"/>。
    /// </para>
    /// </summary>
    internal class NemesisHeld:BaseSwingCO
    {
        /// <summary>剑身贴图直接复用物品的 Nemesis 贴图（CE 同）</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/Nemesis";
        /// <summary>刀光底图（CE 的 <c>MotionTrail3</c>）</summary>
        public override string trailTexturePath => "CalamityDemutation/Assets/ExtraTextures/MotionTrail3";
        /// <summary>刀光渐变色带（CE 的 <c>NemesisBar</c>）</summary>
        public override string gradientTexturePath => "CalamityDemutation/Assets/ExtraTextures/NemesisBar";
        /// <summary>蓄力条底图 / 前景图（CE 的 <c>Assets/GenericBarBack</c>、<c>GenericBarFront</c>）</summary>
        private const string BarBackPath = "CalamityDemutation/Assets/ExtraTextures/GenericBarBack";
        private const string BarFrontPath = "CalamityDemutation/Assets/ExtraTextures/GenericBarFront";
        /// <summary>挥舞参数包：CE 放在 <c>BaseKnife</c> 基类上，本工程 BaseSwingCO 没有该字段，故子类自持一份</summary>
        private SwingDataStruct SwingData = new();
        /// <summary>剑身附加缩放（CE 的 <c>OtherMeleeSize</c>，天罚式取 1.24、其余取 1）</summary>
        private float otherMeleeSize = 1f;
        /// <summary>
        /// 挥舞属性初始化（由基类 <c>SetDefaults</c> 回调）：额外更新 4、锁朝向、本地无敌帧 10×更新率、
        /// 判定箱 182×182，其后各项照抄源的 <c>SetKnifeProperty</c>
        /// </summary>
        public override void SetSwingProperty()
        {
            Projectile.extraUpdates = 4;
            ownerOrientationLock = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10 * updateCount;
            Projectile.width = Projectile.height = 182;
            overOffsetCachesRoting = MathHelper.ToRadians(6);
            IgnoreImpactBoxSize = true;
            CanDrawSlashTrail = true;
            Incandescence = true;
            drawTrailHighlight = false;
            drawTrailBtommWidth = 60;
            drawTrailTopWidth = 130;
            distanceToOwner = 120;
            otherMeleeSize = 1f;
            unitOffsetDrawZkMode = -8;
            SwingData.starArg = 60;
            SwingData.baseSwingSpeed = 4;
            ShootSpeed = 20;
            Length = 124;
        }
        /// <summary>
        /// 首帧初始化（由基类回 调）：挥砍周期对齐物品 useTime（18 帧）、
        /// 离心量按 <c>IgnoreImpactBoxSize</c> 取 22/2、挥舞索引 0/1 交替
        /// </summary>
        public override void Initialize()
        {
            maxSwingTime = Item.useTime;
            SwingData.maxSwingTime = maxSwingTime;
            toProjCoreMode = (IgnoreImpactBoxSize ? 22 : Projectile.width) / 2f;
            if (++SwingIndex > 1)
            {
                SwingIndex = 0;
            }
        }
        /// <summary>挥舞逻辑：三种招式各一套曲线（见类注释），默认走 <c>SwingData</c></summary>
        public override void SwingAI()
        {
            SwingBehavior(SwingData);
        }
        /// <summary>
        /// 出手：<c>ai[0] == 2</c> 蓄满才甩 <see cref="NemesisAlt"/>（伤害 ×10）；
        /// <c>ai[0] == 1</c> 甩 <see cref="EXNemesisProj"/>（伤害 ×5、从光标上方偏侧砸下）；
        /// 否则一次性甩 3 颗 <see cref="NemesisProj"/>（伤害 ÷4，散开角度 ±0.2、速度 0.6~1.33 倍）。
        /// </summary>
        public override void Shoot()
        {
            int type = ModContent.ProjectileType<NemesisProj>();
            if (Projectile.ai[0] == 2)
            {
                if (Time < 140 * updateCount)
                {
                    return;
                }
                SoundEngine.PlaySound(SoundID.Item71, Owner.position);
                type = ModContent.ProjectileType<NemesisAlt>();
                Projectile.NewProjectile(Source, ShootSpanPos, ShootVelocity.RotatedByRandom(0.66f)
                    , type, Projectile.damage * 10, Projectile.knockBack, Owner.whoAmI, 0f, 0);
                return;
            }
            if (Projectile.ai[0] == 1)
            {
                SoundEngine.PlaySound(SoundID.Item69, Owner.position);
                type = ModContent.ProjectileType<EXNemesisProj>();
                Vector2 pos = InMousePos;
                pos.Y -= 800;
                pos.X -= Owner.direction * 320;
                Vector2 ver = new Vector2(Owner.direction * 2, 6);
                Projectile.NewProjectile(Source, pos, ver, type, Projectile.damage * 5
                    , Projectile.knockBack, Owner.whoAmI, 0f, 0);
                return;
            }
            Vector2 orig = ShootSpanPos + new Vector2(0, -800);
            Vector2 toMou = orig.To(InMousePos);
            for (int i = 0; i < 3; i++)
            {
                Vector2 spwanPos = orig + toMou.UnitVector() * 600;
                spwanPos.X += Main.rand.Next(-260, 260);
                spwanPos.Y -= 660;
                Vector2 ver = spwanPos.To(InMousePos).UnitVector() * 26;
                ver = ver.RotatedByRandom(0.2f);
                ver *= Main.rand.NextFloat(0.6f, 1.33f);
                Projectile.NewProjectile(Source, spwanPos, ver, type, Projectile.damage / 4
                    , Projectile.knockBack, Owner.whoAmI, 0f, 0);
            }
        }
        /// <summary>
        /// 挥舞前每帧：右键蓄力期间（<c>ai[0] == 2</c>）切换蓄力态与松手态两套曲线；
        /// 普通式在第 10 帧开启出手窗口；天罚式改走一套更快（22 帧上限、缩短离心）的挥舞参数并在首帧播起手音。
        /// </summary>
        public override bool PreInOwnerUpdate()
        {
            if (Projectile.ai[0] == 2 && Time == 140 * updateCount && Main.myPlayer == Projectile.owner && Main.mouseRight)
            {
                Time--;
            }
            if (Time == 0 && Projectile.ai[0] == 0)
            {
                SoundEngine.PlaySound(SoundID.Item71, Owner.position);
            }
            if (Projectile.ai[0] == 1)
            {
                shootSengs = 0.95f;
                maxSwingTime = 22;
                CanDrawSlashTrail = false;
                SwingData.starArg = 13;
                SwingData.baseSwingSpeed = 2;
                SwingData.ler1_UpLengthSengs = 0.1f;
                SwingData.ler1_UpSpeedSengs = 0.1f;
                SwingData.ler1_UpSizeSengs = 0.062f;
                SwingData.ler2_DownLengthSengs = 0.01f;
                SwingData.ler2_DownSpeedSengs = 0.14f;
                SwingData.ler2_DownSizeSengs = 0;
                SwingData.minClampLength = 160;
                SwingData.maxClampLength = 200;
                SwingData.maxSwingTime = 20;
                SwingData.ler1Time = 8;
                otherMeleeSize = 1.24f;
                return true;
            }
            else if (Projectile.ai[0] == 2)
            {
                if (DownRight && Time < 140 * updateCount)
                {
                    CanDrawSlashTrail = false;
                    SwingData.maxSwingTime = maxSwingTime = (int)(200 / SetSwingSpeed(1));
                    SwingData.starArg = 60;
                    SwingData.baseSwingSpeed = 0;
                    SwingData.minClampLength = 160;
                    SwingData.maxClampLength = 160;
                    SwingData.ler1_UpSizeSengs = 0;
                    if (Time == 130 * updateCount)
                    {
                        SoundEngine.PlaySound(SoundID.Item4, Projectile.Center);
                    }
                }
                else
                {
                    CanDrawSlashTrail = true;
                    if (Time < 140 * updateCount)
                    {
                        Projectile.Kill();
                    }
                    speed = MathHelper.ToRadians(SwingData.baseSwingSpeed) / SetSwingSpeed(1);
                    SwingData.baseSwingSpeed = 9;
                    SwingData.minClampLength = 160;
                    SwingData.maxClampLength = 200;
                    SwingData.ler1_UpSizeSengs = 0;
                    if (Time % 20 == 0)
                    {
                        canShoot = true;
                    }
                }
                return true;
            }
            if (Time == 10 * updateCount)
            {
                canShoot = true;
            }
            return base.PreInOwnerUpdate();
        }
        /// <summary>真近战命中：首次命中（<c>numHits == 0</c>）时在目标处迸发 6 颗可回血的龙陨星</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.numHits == 0)
            {
                int type = ModContent.ProjectileType<NemesisProj>();
                for (int i = 0; i < 6; i++)
                {
                    Vector2 spwanPos = target.Center;
                    Vector2 ver = CDUtil.randVr(3, 18);
                    Projectile.NewProjectile(Source, spwanPos, ver, type, Projectile.damage
                        , Projectile.knockBack, Owner.whoAmI, 1f, 0);
                }
            }
        }
        /// <summary>蓄力未满期间（<c>ai[0] == 2</c> 且未到 140×更新率 帧）禁止命中，避免蓄力时误触判定</summary>
        public override bool? CanHitNPC(NPC target)
        {
            if (Projectile.ai[0] == 2 && Time <= 140 * updateCount)
            {
                return false;
            }
            return base.CanHitNPC(target);
        }
        /// <summary>
        /// 绘制：<c>ai[0] == 2</c> 且在蓄力时，先在玩家下方 60 像素处画一条橙色蓄力条（按进度裁切前景图）；
        /// 非普通式（天罚 / 蓄力）时另画一层加亮的剑体贴图（带红色描边）。
        /// </summary>
        public override void DrawSwing(SpriteBatch spriteBatch, Color lightColor)
        {
            float newCharge = Time;
            float maxCharge = 140 * updateCount;
            if (Projectile.ai[0] == 2 && newCharge <= maxCharge)
            {
                Texture2D barBG = ModContent.Request<Texture2D>(BarBackPath).Value;
                Texture2D barFG = ModContent.Request<Texture2D>(BarFrontPath).Value;
                float barScale = 2f;
                Vector2 barOrigin = barBG.Size() * 0.5f;
                Vector2 drawPos = Owner.MountedCenter + new Vector2(0, 60) - Main.screenPosition;
                float sengs = 1 - (maxCharge - newCharge) / maxCharge;
                Rectangle frameCrop = new Rectangle(0, 0, (int)(sengs * barFG.Width), barFG.Height);
                Color color = Color.OrangeRed;
                spriteBatch.Draw(barBG, drawPos, null, color, 0f, barOrigin, barScale, 0, 0f);
                spriteBatch.Draw(barFG, drawPos, frameCrop, color * 0.8f, 0f, barOrigin, barScale, 0, 0f);
            }
            if (Projectile.ai[0] != 0)
            {
                Texture2D texture = TextureValue;
                Rectangle rect = new Rectangle(0, 0, texture.Width, texture.Height);
                Vector2 drawOrigin = new Vector2(texture.Width / 2, texture.Height / 2);
                SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;
                Vector2 toOwner = Projectile.Center - Owner.GetPlayerStabilityCenter();
                Vector2 offsetOwnerPos = toOwner.GetNormalVector() * -6 * Projectile.spriteDirection;
                Vector2 drawPosValue = Projectile.Center - RodingToVer(toProjCoreMode, (Projectile.Center - Owner.Center).ToRotation()) + offsetOwnerPos;
                Vector2 trueDrawPos = drawPosValue - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY;
                float drawRoting = Projectile.rotation;
                if (Projectile.spriteDirection == -1)
                {
                    drawRoting += MathHelper.Pi;
                }
                CDUtil.DrawRotatingMarginEffect(Main.spriteBatch, texture, Projectile.timeLeft, trueDrawPos
                    , null, new Color(255, 50, 50), drawRoting, drawOrigin, Projectile.scale, effects);
            }
            // 剑身附加缩放：CE 把它汇进 MeleeSize 后作用于刀身/离心量/判定箱，本工程只作用于刀身；
            // 临时放大 Projectile.scale 供基类绘制，画完立即还原（否则会泄漏进下一帧的弧光采样长度）
            float origScale = Projectile.scale;
            Projectile.scale = origScale * otherMeleeSize;
            base.DrawSwing(spriteBatch, lightColor);
            Projectile.scale = origScale;
        }
    }
}
