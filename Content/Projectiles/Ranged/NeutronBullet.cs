using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 中子弹（NeutronBullet） - 中子枪（NeutronGun）左键与右键射出的弹体，移植自 CWR 的 NeutronBullet。
    /// 本体不画贴图（PreDraw 恒返回 false），只画残影拖尾并参与屏幕扭曲；
    /// 命中时原地炸一个小爆点（<see cref="NeutronExplosionRanged"/>），
    /// 若为蓄力弹（<c>ai[0] &gt; 0</c>）且命中次数未超 8，额外从高空落下 3 道中子光束。
    /// <para>
    /// 贴图取自灾厄的 GodSlayerSlugBlue（2×136 的细长曳光条），CWR 原版也是直接引用灾厄那张；
    /// 本工程不引用外部 mod 贴图，故把它复制进工程并改名为与 .cs 同名。
    /// </para>
    /// </summary>
    internal class NeutronBullet : ModProjectile, IDrawWarp
    {
        /// <summary>本弹幕的贴图（从灾厄 GodSlayerSlugBlue 复制而来）</summary>
        public override string Texture => "CalamityDemutation/Content/Projectiles/Ranged/NeutronBullet";
        /// <summary>扭曲遮罩贴图路径（CWR 用的是它自己的 StarTexture_White，本工程取 ExtraTextures 里同用途的星形遮罩）</summary>
        private const string WarpTexture = "CalamityDemutation/Assets/ExtraTextures/StarTexture";
        /// <summary>
        /// 静态设置：开启 6 段残影缓存，追踪模式 0（只记位置，不记旋转）
        /// </summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }
        /// <summary>
        /// 基础属性：4×4 判定箱、远程伤害、透明度从全隐开始淡入、每帧更新 6 次（飞得极快）、
        /// 无限穿透且对同一敌人只结算一次（localNPCHitCooldown = -1）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.MaxUpdates = 6;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 60;
        }
        /// <summary>
        /// 每帧：淡入（alpha 每帧 -5），并把贴图转到与速度对齐（贴图竖直，故 +90°）
        /// </summary>
        public override void AI()
        {
            if (Projectile.alpha > 0)
                Projectile.alpha -= 5;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }
        /// <summary>
        /// 命中敌怪：原地炸一个小爆点；若本弹是蓄力弹（ai[0] &gt; 0）且累计命中未超 8 次，
        /// 再从目标上方随机横向位置落下 3 道中子光束。
        /// 生成动作只在主人端做，避免各客户端各生成一份幽灵弹幕
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.owner != Main.myPlayer)
                return;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero
                , ModContent.ProjectileType<NeutronExplosionRanged>(), Projectile.damage, 0f);
            if (Projectile.ai[0] > 0f && Projectile.numHits < 8)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 spawnPos = target.Center + new Vector2(Main.rand.Next(-120, 120), -Main.rand.Next(720, 850));
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPos, new Vector2(0f, 22f)
                        , ModContent.ProjectileType<NeutronLaser>(), Projectile.damage, 0f);
                }
            }
        }
        /// <summary>本体不走常规绘制，改由 IDrawWarp 的 costomDraw 画残影</summary>
        public override bool PreDraw(ref Color lightColor) => false;
        // ── 公开方法 ──
        /// <summary>IDrawWarp：允许 EffectsSystem 在扭曲结果之上再调用 costomDraw 绘制本体</summary>
        public bool canDraw() => true;
        /// <summary>
        /// IDrawWarp：画拖尾残影（mode 0 = 各段残影按透明度递减叠加），拖尾由 6 段历史位置构成
        /// </summary>
        public void costomDraw(SpriteBatch spriteBatch) => CDUtil.DrawAfterimages(Projectile, 0, Color.White);
        /// <summary>
        /// IDrawWarp：绘制屏幕扭曲遮罩。用星形遮罩叠画 3 层、暗灰底色且整体压到 0.1 透明度，
        /// 形成一枚很淡的空间涟漪（CWR 口径，本弹的扭曲刻意做得很轻）
        /// </summary>
        public void Warp()
        {
            Texture2D warpTex = ModContent.Request<Texture2D>(WarpTexture).Value;
            Color warpColor = new Color(45, 45, 45) * 0.1f;
            for (int i = 0; i < 3; i++)
            {
                Main.spriteBatch.Draw(warpTex, Projectile.Center - Main.screenPosition
                    , null, warpColor, Projectile.rotation, warpTex.Size() / 2, 0.2f, SpriteEffects.None, 0f);
            }
        }
    }
}
