using CalamityDemutation.Graphics.Primitives;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 引力箭矢（EXNeutronArrow） - 洛希之弦右键蓄满射出的三连箭矢，逻辑逐行移植自 CWR 0.4.0.1.3。
    /// 140 次更新内穿透无限、每个敌人只打一次；命中非蠕虫体节时生成一次 EX 中子爆点（伤害 ×3）。
    /// 拖尾原版走灾厄的 HeavenlyGaleTrail 着色器 + 灾厄噪声贴图，本工程换成**已注册的同名着色器**
    /// （<c>CalamityDemutation:HeavenlyGaleTrail</c>）与自有的 <c>ExtraTextures/GreyscaleGradients/EternityStreak</c>，
    /// 用法与本工程 DefenseBeam 完全一致。
    /// </summary>
    internal class EXNeutronArrow : ModProjectile
    {
        // ── 静态字段 ──
        /// <summary>
        /// 蠕虫体节判定集合：大修 <c>CWRLoad.WormBodys</c> 那份硬编码表的等价物，
        /// 原版两项 + 按类名软依赖解析灾厄的 15 项（解析不到就丢弃，不报错）。
        /// 与 <see cref="CalamityDemutation.Content.Projectiles.Melee.EntropicClaymoreProj"/> 同一套做法，各自持一份
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
        /// <summary>命中伤害倍率：生成 EX 中子爆点时的倍率（大修 ×3）</summary>
        private const int HitExplosionDamageMult = 3;
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
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 50;
        }
        /// <summary>贴图取大修的中子箭大图（89×266，7 帧竖直排布）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/Ranged/NeutronArrow";
        /// <summary>
        /// 基础属性：32×32 判定箱、存活 320 tick、穿透无限、额外更新 5、忽略 80 点护甲、
        /// 本地无敌帧 -1（同一敌人只命中一次）、自带光照 0.6、出生时全透明（随后逐帧淡入）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.timeLeft = 320;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.light = 0.6f;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ArmorPenetration = 80;
            Projectile.MaxUpdates = 5;
        }
        /// <summary>每帧：淡入，并按速度摆正箭身（这张贴图是横向的，故不带 SetArrowRot 的 +90°）</summary>
        public override void AI()
        {
            if (Projectile.alpha > 0)
                Projectile.alpha -= 25;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        /// <summary>
        /// 命中非蠕虫体节时在靶心生成一次 EX 中子爆点（伤害 ×3）；
        /// 蠕虫体节跳过是为了避免长条蠕虫被一路洞穿时连环炸开
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.IsOwnedByLocalPlayer() && !IsWormBody(target))
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center
                    , Vector2.Zero, ModContent.ProjectileType<EXNeutronExplosionRanged>(), Projectile.damage * HitExplosionDamageMult, 0f);
            }
        }
        /// <summary>
        /// 是否蠕虫体节：优先用显式集合；灾厄的 15 项一个都没解析到时，
        /// 退回"挂在蠕虫链条上（realLife≥0）或走蠕虫 AI"的语义近似
        /// </summary>
        private static bool IsWormBody(NPC target) => WormBodyTypes.Contains(target.type)
            || (!calamityWormBodiesResolved && (target.realLife >= 0 || target.aiStyle == NPCAIStyleID.Worm));
        /// <summary>
        /// 拖尾宽度：整体宽度随弹幕缩放变化，基准 30 像素（大修 PrimitiveWidthFunction 的同值实现，
        /// 本工程这一版 PrimitiveSettings 取双参签名，故补上顶点参数）
        /// </summary>
        private float PrimitiveWidthFunction(float completionRatio, Vector2 _) => Projectile.scale * 30f;
        /// <summary>拖尾颜色：统一淡蓝白，透明度跟随弹幕淡入淡出</summary>
        private Color PrimitiveColorFunction(float _, Vector2 vertexPosition) => Color.AliceBlue * Projectile.Opacity;
        /// <summary>
        /// 先画 HeavenlyGaleTrail 光带拖尾（主副色随时间循环流动，用弹幕 identity 打散相位），
        /// 再按 7 帧切片画箭身
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            float localIdentityOffset = Projectile.identity * 0.1372f;
            Color mainColor = CDUtil.MulticolorLerp((Main.GlobalTimeWrappedHourly * 2f + localIdentityOffset) % 1f, Color.Blue, Color.White, Color.BlueViolet, Color.CadetBlue, Color.DarkBlue);
            Color secondaryColor = CDUtil.MulticolorLerp((Main.GlobalTimeWrappedHourly * 2f + localIdentityOffset + 0.2f) % 1f, Color.Blue, Color.White, Color.BlueViolet, Color.CadetBlue, Color.DarkBlue);
            mainColor = Color.Lerp(Color.White, mainColor, 0.85f);
            secondaryColor = Color.Lerp(Color.White, secondaryColor, 0.85f);
            Vector2 trailOffset = Projectile.Size * 0.5f;
            GameShaders.Misc["CalamityDemutation:HeavenlyGaleTrail"].UseImage1(ModContent.Request<Texture2D>("CalamityDemutation/ExtraTextures/GreyscaleGradients/EternityStreak"));
            GameShaders.Misc["CalamityDemutation:HeavenlyGaleTrail"].UseImage2("Images/Extra_189");
            GameShaders.Misc["CalamityDemutation:HeavenlyGaleTrail"].UseColor(mainColor);
            GameShaders.Misc["CalamityDemutation:HeavenlyGaleTrail"].UseSecondaryColor(secondaryColor);
            GameShaders.Misc["CalamityDemutation:HeavenlyGaleTrail"].Apply();
            PrimitiveRenderer.RenderTrail(Projectile.oldPos, new PrimitiveSettings(PrimitiveWidthFunction, PrimitiveColorFunction, (float _, Vector2 _) => trailOffset, smoothen: true, pixelate: false, GameShaders.Misc["CalamityDemutation:HeavenlyGaleTrail"]), 53);
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, CDUtil.GetRec(texture, Projectile.frame, 7)
                , Color.White, Projectile.rotation, CDUtil.GetOrig(texture, 7), Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
