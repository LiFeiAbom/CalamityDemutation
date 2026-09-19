using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 分形星影（FractalStarBlade，移植自 CalamityEntropy）：星熠分形挥砍过半时朝侧向甩出的追踪剑影。
    /// 前 46 帧边减速边自旋、并朝鼠标方向缓慢转向；第 46 帧锁定朝向、把速度提到 12 冲出，此后改为追踪
    /// 2400 像素内的敌怪（命中过就不再追）。命中附加太空感染减益、自身伤害递减 14% 并把剩余寿命压到 30 帧，
    /// 同时从上方砸下 3 颗 <see cref="AstralStarMelee"/> 星陨。
    /// <para>
    /// 与 CE 原版的差异：① CE 用 <c>player.Entropy().MouseWorld</c>（它自研的各端可见鼠标坐标），
    /// 本机没有该系统，按本工程既有口径改用 <c>Main.MouseWorld</c>，并删掉配套的 <c>MouseWorldListener</c> 标志；
    /// ② <c>GetOwner()</c>/<c>GetTexture()</c> 换成 <c>Main.player[owner]</c> 与 TextureAssets；
    /// ③ <c>CEUtils.FindTarget_HomingProj</c> / <c>RotateTowardsAngle</c> / <c>randomPointInCircle</c> /
    /// <c>normalize</c> 在 CE 侧属于工具库，这里内联；④ 减益不新建——现代版灾厄取 <c>AstralInfectionDebuff</c>，
    /// 经典版灾厄没有太空感染系减益，按既有口径退回原版 <c>BuffID.CursedInferno</c>（同为紫色 DoT，观感最接近），
    /// 走「解析成单一减益类型」的既有写法（与 WelkinFractalHeld 处理风寒同款）；⑤ CE 的 <c>UseBlendState</c>/<c>ExitShaderRegion</c>
    /// 本机 tML 没有，改用 End + 立即模式 Begin(Additive) 并在画完恢复默认批次；⑥ 去掉 OnHitNPC 里
    /// <c>NewProjectile(...).ToProj()</c> 那个没有接收者的返回值转换。
    /// </para>
    /// </summary>
    internal class FractalStarBlade:ModProjectile
    {
        /// <summary>直接复用武器本体的 Glow 贴图</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/StarlitFractalGlow";
        /// <summary>自身帧数计数</summary>
        private float counter = 0f;
        /// <summary>起手自旋速度（由 ai[1] 决定，逐帧衰减）</summary>
        private float rotSpeed = 0f;
        /// <summary>起手进度 0→1（仅前 46 帧内推进）</summary>
        private float pg = 0f;
        /// <summary>首帧标记：读 ai 取初始朝向与旋向</summary>
        private bool init = true;
        /// <summary>冲刺阶段的追踪目标</summary>
        private NPC homing = null;
        /// <summary>是否已命中过（命中后不再追踪）</summary>
        private bool hited = false;
        /// <summary>命中附加的减益：现代版灾厄取 <c>AstralInfectionDebuff</c>（太空感染），其余情况为原版诅咒狱火</summary>
        private static int astralDebuffType = BuffID.CursedInferno;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;         // 同时记录 oldPos 与 oldRot，供拖尾重绘
            ProjectileID.Sets.TrailCacheLength[Type] = 46;
            // 太空感染：现代版灾厄有 AstralInfectionDebuff 就直接用；经典版灾厄没有这一系减益，
            // 退回原版诅咒狱火（同为紫色 DoT，观感最接近），与 WelkinFractalHeld 处理风寒同款写法
            astralDebuffType = BuffID.CursedInferno;
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity) && calamity.TryFind<ModBuff>("AstralInfectionDebuff", out ModBuff astralInfection))
            {
                astralDebuffType = astralInfection.Type;
            }
        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.light = 1f;
            Projectile.penetrate = -1;                        // 无限穿透
            Projectile.timeLeft = 320 * 8;                    // 配合 8 倍速 → 40 秒
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.MaxUpdates = 8;
            Projectile.tileCollide = false;
        }
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (init)
            {
                rotSpeed = Projectile.ai[1] * 0.1f;
                Projectile.rotation = Projectile.ai[0];
                init = false;
            }
            if (counter < 46 * Projectile.MaxUpdates)
            {
                // 起手阶段：减速、自旋，并随进度越来越快地朝鼠标方向转
                Projectile.velocity *= 0.986f;
                pg = counter / (46 * Projectile.MaxUpdates);
                Projectile.rotation += rotSpeed * (1 - pg);
                rotSpeed *= 0.99f;
                Projectile.rotation = RotateTowardsAngle(Projectile.rotation, (Main.MouseWorld - Projectile.Center).ToRotation(), 0.022f * pg, false);
            }
            else
            {
                // 冲刺阶段：顺着速度方向，追踪 2400 像素内的目标（命中过就不追了）
                Projectile.rotation = Projectile.velocity.ToRotation();
                if (homing == null || !homing.active)
                {
                    homing = FindTargetHomingProj(2400f);
                }
                else if (!hited)
                {
                    Projectile.velocity += (homing.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 1f;
                    Projectile.velocity *= 0.92f;
                }
            }
            if (counter == 46 * Projectile.MaxUpdates)
            {
                Projectile.rotation = (Main.MouseWorld - Projectile.Center).ToRotation();
                Projectile.velocity = Projectile.rotation.ToRotationVector2() * 12;
            }
            counter++;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            int trailLength = ProjectileID.Sets.TrailCacheLength[Type];
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            for (int i = 0; i < trailLength; i++)
            {
                float prog = i / (float)trailLength;
                Draw(Projectile.oldPos[i] + new Vector2(Projectile.width, Projectile.height) * 0.5f, Color.White * 0.36f * (1 - prog), Projectile.oldRot[i], (int)Projectile.ai[1]);
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Draw(Projectile.Center, Color.White, Projectile.rotation, (int)Projectile.ai[1]);
            return false;
        }
        /// <summary>
        /// 命中：挂太空感染减益、自身伤害递减 14%、把剩余寿命压到 30 帧（换算到 8 倍速），
        /// 并从上方砸下 3 颗星陨
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            hited = true;
            target.AddBuff(astralDebuffType, 360);
            Projectile.damage = (int)(Projectile.damage * 0.86f);
            if (Projectile.timeLeft > 30 * Projectile.MaxUpdates)
            {
                Projectile.timeLeft = 30 * Projectile.MaxUpdates;
            }
            for (int i = 0; i < 3; i++)
            {
                Vector2 pos = target.Center + new Vector2(0, -900) + RandomPointInCircle(400);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), pos, (target.Center - pos).SafeNormalize(Vector2.Zero) * 42,
                    ModContent.ProjectileType<AstralStarMelee>(), Projectile.damage / 4, Projectile.owner);
            }
        }
        /// <summary>按 dir 决定是否水平翻转，并相应地取四分之一圈作为绘制旋转（CE 原样）</summary>
        private void Draw(Vector2 pos, Color lightColor, float rotation, int dir)
        {
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? rotation + MathHelper.PiOver4 : rotation + MathHelper.Pi * 0.75f;
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(texture, pos - Main.screenPosition, null, lightColor, rot, texture.Size() * 0.5f, Projectile.scale, effect);
        }
        /// <summary>CEUtils.FindTarget_HomingProj 的等价实现：取 maxDistance 内最近的可攻击敌怪</summary>
        private NPC FindTargetHomingProj(float maxDistance)
        {
            NPC target = null;
            float nearest = maxDistance;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile) || npc.friendly)
                {
                    continue;
                }
                float distance = Vector2.Distance(npc.Center, Projectile.Center);
                if (distance <= nearest)
                {
                    nearest = distance;
                    target = npc;
                }
            }
            return target;
        }
        /// <summary>CEUtils.randomPointInCircle 的等价实现：随机角度 × [-r, r] 的随机半径</summary>
        private static Vector2 RandomPointInCircle(float r) => Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(-r, r);
        /// <summary>
        /// CEUtils.RotateTowardsAngle 的等价实现：把角度与目标角都折到 (-π, π] 后取最短转向量，
        /// useFixedSpeed 为真时限制每帧最大转角、为假时把转角按 rotateSpeed 比例缩放。
        /// </summary>
        private static float RotateTowardsAngle(float currentRadians, float targetRadians, float rotateSpeed, bool useFixedSpeed)
        {
            currentRadians = MathHelper.WrapAngle(currentRadians);
            targetRadians = MathHelper.WrapAngle(targetRadians);
            float turnAmount = MathHelper.WrapAngle(targetRadians - currentRadians);
            if (useFixedSpeed)
            {
                turnAmount = MathHelper.Clamp(turnAmount, -rotateSpeed, rotateSpeed);
            }
            else
            {
                turnAmount *= MathHelper.Clamp(rotateSpeed, 0f, 1f);
            }
            return currentRadians + turnAmount;
        }
    }
}
