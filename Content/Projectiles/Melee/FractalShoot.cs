using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 分形弹（FractalShoot，移植自 CalamityEntropy）：破碎分形刺出式射出的追踪弹。
    /// 出膛后先直飞 10 帧，随后每帧朝最近的敌怪做两次转向；命中伤害随剩余存活时间线性衰减
    /// （刚射出时为全额，临近消失时只剩一半）；死亡时喷出一圈发光火花。
    /// <para>
    /// 与 CE 原版的差异：① 追踪用的 <c>CEUtils.HomingToNPCNearby</c> / <c>FindTarget_HomingProj</c>
    /// 属于 CE 工具库，这里内联成 <see cref="HomingToNPCNearby"/>；② 死亡火花从 InnoVault 的
    /// <c>PRT_GlowSpark</c> 换成本模组行为一致的 <see cref="GlowSpark"/>；
    /// ③ <c>GetTexture()</c> 换成 TextureAssets；④ CE 的 <c>UseAdditive</c>/<c>ExitShaderRegion</c>
    /// 本机 tML 没有，改用 End + 立即模式 Begin(Additive)、画完再恢复默认批次。
    /// </para>
    /// </summary>
    internal class FractalShoot:ModProjectile
    {
        /// <summary>拖尾采样点（最多保留 26 个）</summary>
        private readonly List<Vector2> oldPos = new List<Vector2>();
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.MaxUpdates = 4;                   // 高速弹幕
            Projectile.friendly = true;
            Projectile.penetrate = 4;                    // 可穿透 4 个敌人
            Projectile.tileCollide = true;
            Projectile.light = 1f;
            Projectile.timeLeft = 120;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;         // 同一个敌人只结算一次
        }
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.timeLeft < 32)                // 最后 32 帧淡出
            {
                Projectile.Opacity -= 1 / 32f;
            }
            oldPos.Add(Projectile.Center);
            if (oldPos.Count > 26)
            {
                oldPos.RemoveAt(0);
            }
            if (Projectile.localAI[0]++ > 10 && Projectile.numHits == 0)// 飞行一小段后开始追踪；已命中过就不再追
            {
                HomingToNPCNearby(1f, 0.93f);
                HomingToNPCNearby(1f, 0.93f);
            }
        }
        /// <summary>伤害随剩余存活时间线性衰减：刚射出为 100%，临近消失时降到 50%</summary>
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= 0.5f * (Projectile.timeLeft / 120f) + 0.5f;
        }
        /// <summary>死亡时朝随机方向喷 12 颗发光火花</summary>
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * Main.rand.NextFloat(2f, 7f);
                DRKLoader.NewParticle(new GlowSpark(), Projectile.Center, velocity, Color.LightGoldenrodYellow, Main.rand.NextFloat(0.06f, 0.1f));
            }
        }
        /// <summary>自绘：加法混合下把历史采样点由旧到新逐层画出并递减缩放，形成拖尾，最后画本体</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            if (oldPos.Count > 1)
            {
                for (int i = 0; i < oldPos.Count; i++)
                {
                    float alpha = i / (oldPos.Count - 1f);
                    Main.EntitySpriteDraw(texture, oldPos[i] - Main.screenPosition, null, lightColor * Projectile.Opacity * alpha, Projectile.rotation, texture.Size() * 0.5f, alpha, SpriteEffects.None);
                }
            }
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity, Projectile.rotation, texture.Size() * 0.5f, 1f, SpriteEffects.None);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        /// <summary>CEUtils.HomingToNPCNearby + FindTarget_HomingProj 的内联：朝最近的可攻击敌怪转向（先按 velMult 减速再加 vel 的转向分量）</summary>
        private void HomingToNPCNearby(float vel, float velMult)
        {
            NPC target = null;
            float nearest = 600f;
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
            if (target == null)
            {
                return;
            }
            Projectile.velocity *= velMult;
            Projectile.velocity += Vector2.Normalize(target.Center - Projectile.Center) * vel;
        }
    }
}
