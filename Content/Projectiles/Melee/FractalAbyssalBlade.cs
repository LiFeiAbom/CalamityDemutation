using CalamityDemutation.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 分形深渊刃（FractalAbyssalBlade，移植自 CalamityEntropy）：深渊分形挥砍过半时朝侧向甩出的追踪剑影。
    /// 前 46 帧边减速边转向最近的敌怪，到第 46 帧锁定朝向、播冲刺音、把速度提到 10 冲出，同时在原地
    /// 生成一道 <see cref="AbyssalCrack"/> 空间裂缝；命中后自身伤害递减 14%。贴图复用武器本体的 Glow 图。
    /// <para>
    /// 与 CE 原版的差异：① 音效走本模组的 <see cref="CalamityDemutationSounds.AbyssalBladeLaunch"/>；
    /// ② <c>GetOwner()</c>/<c>GetTexture()</c> 换成 <c>Main.player[owner]</c> 与 TextureAssets；
    /// ③ <c>CEUtils.FindTarget_HomingProj</c> 与 <c>CEUtils.RotateTowardsAngle</c> 在 CE 侧属于工具库，这里内联；
    /// ④ CE 的 <c>UseBlendState</c>/<c>ExitShaderRegion</c> 本机 tML 没有，改用 End + 立即模式 Begin(Additive)、
    /// 画完再恢复默认批次；⑤ 去掉 CE 里那个恒为 0 的 <c>rotSpeed</c>（初值 0 且只自乘衰减，自旋项实际不起作用，
    /// 转向完全由 RotateTowardsAngle 负责）与未使用的 <c>player</c> 局部变量。
    /// </para>
    /// </summary>
    internal class FractalAbyssalBlade:ModProjectile
    {
        /// <summary>直接复用武器本体的 Glow 贴图</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/AbyssFractalGlow";
        /// <summary>自身帧数计数</summary>
        private float counter = 0f;
        /// <summary>起手进度 0→1（仅前 46 帧内推进），转向速度按它放大</summary>
        private float pg = 0f;
        /// <summary>首帧标记：把朝向对齐到初速方向</summary>
        private bool init = true;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;         // 同时记录 oldPos 与 oldRot，供拖尾重绘
            ProjectileID.Sets.TrailCacheLength[Type] = 46;
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
            if (init)
            {
                Projectile.rotation = Projectile.velocity.ToRotation();
                init = false;
            }
            NPC target = FindTargetHomingProj(1600f);
            if (counter < 46 * Projectile.MaxUpdates)
            {
                // 起手阶段：减速，并随进度越来越快地转向目标
                Projectile.velocity *= 0.986f;
                pg = counter / (46 * Projectile.MaxUpdates);
                if (target != null)
                {
                    Projectile.rotation = RotateTowardsAngle(Projectile.rotation, (target.Center - Projectile.Center).ToRotation(), 0.022f * pg, false);
                }
            }
            if (counter == 46 * Projectile.MaxUpdates)
            {
                // 起手结束：锁定朝向、播冲刺音、提速冲出，并在原地撕开一道裂隙
                if (target != null)
                {
                    Projectile.rotation = (target.Center - Projectile.Center).ToRotation();
                }
                SoundEngine.PlaySound(CalamityDemutationSounds.AbyssalBladeLaunch with { Volume = 0.3f }, Projectile.Center);
                Projectile.velocity = Projectile.rotation.ToRotationVector2() * 10;
                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity * Projectile.MaxUpdates, ModContent.ProjectileType<AbyssalCrack>(), Projectile.damage / 6, 0, Projectile.owner);
                }
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
        /// <summary>每命中一个敌人自身伤害递减 14%（CE 原设定）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.damage = (int)(Projectile.damage * 0.86f);
        }
        /// <summary>按 dir 决定是否水平翻转，并相应地取四分之一圈加减 10 度作为绘制旋转（CE 原样）</summary>
        private void Draw(Vector2 pos, Color lightColor, float rotation, int dir)
        {
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? rotation + MathHelper.PiOver4 - MathHelper.ToRadians(10) : rotation + MathHelper.Pi * 0.75f + MathHelper.ToRadians(10);
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
