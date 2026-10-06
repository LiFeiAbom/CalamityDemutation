using CalamityDemutation.Graphics.Metaballs;
using CalamityDemutation.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Typeless
{
    /// <summary>
    /// 虚空场（VoidFieldGenerator）—— 移植自灾厄 2.0.4 的 <c>Projectiles/Typeless/VoidFieldGenerator</c>。
    /// 虚无箭袋在周身半径 300 处环绕的四座虚空场：本体是一个只做判定、不出伤的隐形发生器，
    /// 每帧扫描主人的箭矢，凡进入场心 65 像素内的友方箭矢都会被打上
    /// <see cref="CalamityDemutationGlobalProjectile.nihilicArrow"/> 标记，
    /// 伤害 ×1.75、<c>extraUpdates + 1</c>（即"双倍速度"），每支箭只强化一次。
    /// 观感由 <see cref="VoidGeneratorMetaball"/> 的元球承担（本体贴图之上另有 Glow 叠层）。
    /// <para>
    /// 与源的差异：源里那个声明后从未被任何代码读取的 <c>start</c> 字段不再保留。
    /// </para>
    /// </summary>
    internal class VoidFieldGenerator : ModProjectile
    {
        // ── 实例字段 ──
        /// <summary>
        /// 本座虚空场对应的元球粒子句柄（ModProjectile 按弹幕实例化，故实例字段安全）。
        /// 为 null 表示尚未生成；AI 首帧生成后，每帧把它的中心与尺寸钉在弹幕上。
        /// </summary>
        public VoidGeneratorMetaball.CosmicParticle VoidAura;
        // ── 生命周期方法 ──
        /// <summary>
        /// 基础属性：50×50 碰撞箱、友方、无视水、单次穿透、不撞地形；
        /// 存活时间先 ×5 兜底，实际由 AI 里"标记有效即压到 2 帧"续命
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft *= 5;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
        }
        /// <summary>
        /// AI：校验虚无箭袋标记（失效即消散）、续命、出生音效、绕主人半径 300 环行、
        /// 强化场内的己方箭矢，并把元球粒子钉在自身中心
        /// </summary>
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            // 虚无箭袋卸下后，场上已有的虚空场立即消散
            if (!modPlayer.voidField)
            {
                Projectile.active = false;
                return;
            }
            if (player.dead)
                modPlayer.voidField = false;   // 玩家死亡时清空标记，复活后由饰品重新召唤
            // 标记有效时持续刷新存活时间，实现常驻跟随
            if (modPlayer.voidField)
                Projectile.timeLeft = 2;
            // 出生音效只在首帧播（首帧 ai[1] 尚为 0）
            if (Projectile.ai[1] == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item20, Projectile.position);
            }
            Vector2 vector = player.Center - Projectile.Center;
            // 场心位置：以主人为圆心半径 300，按 ai[0] 给出 90° 间隔、ai[1] 随时间公转
            Projectile.Center = player.Center + new Vector2(300, 0).RotatedBy(Projectile.ai[1] + Projectile.ai[0] * MathHelper.PiOver2);
            Projectile.ai[1] += 0.01f;
            // 速度只给一个极小值，供原版朝向计算使用（照源）
            Projectile.velocity.X = (vector.X > 0f) ? -0.000001f : 0f;
            // 扫描全场的己方箭矢：进入 65 像素即强化一次（nihilicArrow 标记去重）
            for (int k = 0; k < Main.projectile.Length; k++)
            {
                var proj = Main.projectile[k];
                if (proj.active && proj.owner == Projectile.owner && proj.arrow && !proj.GetGlobalProjectile<CalamityDemutationGlobalProjectile>().nihilicArrow && proj.friendly && Vector2.Distance(proj.Center, Projectile.Center) < 65)
                {
                    Main.projectile[k].damage = (int)(proj.damage * 1.75f);
                    proj.extraUpdates += 1;
                    Main.projectile[k].GetGlobalProjectile<CalamityDemutationGlobalProjectile>().nihilicArrow = true;
                    SoundEngine.PlaySound(SoundID.Item104 with { Volume = SoundID.Item104.Volume * 0.75f }, Projectile.Center);
                    // 沿箭身撒一圈暗影焰尘（照源）
                    for (int i = 0; i < 12; i++)
                    {
                        Vector2 dustpos = Vector2.UnitX * (float)-(float)proj.width / 2f;
                        dustpos += -Vector2.UnitY.RotatedBy((double)((float)i * 3.14159274f / 6f), default) * new Vector2(8f, 16f);
                        dustpos = dustpos.RotatedBy((double)(proj.rotation - 1.57079637f), default);
                        int dust = Dust.NewDust(proj.Center, 0, 0, DustID.Shadowflame, 0f, 0f, 100, Color.HotPink, 1f);
                        Main.dust[dust].scale = 1.1f;
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].position = proj.Center + dustpos;
                        Main.dust[dust].velocity = proj.velocity * 0.1f;
                        Main.dust[dust].velocity = Vector2.Normalize(proj.Center - proj.velocity * 3f - Main.dust[dust].position) * 1.25f;
                    }
                }
            }
            // 元球粒子：首帧生成，之后每帧把中心与尺寸钉在弹幕上（尺寸恒为 120）
            if (VoidAura == null)
            {
                VoidAura = VoidGeneratorMetaball.SpawnParticle(Projectile.Center, Vector2.Zero, 120f);
            }
            else
            {
                VoidAura.Center = Projectile.Center;
                VoidAura.Size = 120f;
            }
        }
        /// <summary>虚空场不参与挖矿（照源）</summary>
        public override bool? CanCutTiles() => false;
        /// <summary>
        /// 后绘制：在本体上叠一层 Glow 发光贴图（VoidFieldGenerator_Glow）。
        /// 本工程没有"自动绘制 _Glow"的全局钩子，需在此显式补画
        /// </summary>
        public override void PostDraw(Color lightColor)
        {
            Texture2D glow = ModContent.Request<Texture2D>(Texture + "_Glow").Value;
            Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, glow.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
        }
    }
}
