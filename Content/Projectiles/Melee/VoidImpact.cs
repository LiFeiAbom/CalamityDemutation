using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 虚影剑气（VoidImpact，移植自 CalamityEntropy）：虚影薄锋左键第 16 帧甩出的一道剑气。
    /// 飞行途中每帧把自身位置记进 <see cref="oldPos"/>（最多 64 段）当拖尾；
    /// 首次命中时朝四个斜角各炸两条 <see cref="VoidImpactParticle"/> 并**把自己的伤害清零**
    /// ——那个 <c>damage == 0</c> 同时充当"已命中过"的标记，之后剑气转为一团逐帧收小的消散拖尾
    /// （<c>ai[2]</c> 与速度各按 0.98 / 0.97 衰减）。
    /// <para>
    /// 与 CE 原版的差异：① 音效走本模组的 <see cref="CalamityDemutationSounds.VoidImpactHit"/>
    /// （CE 原名 flashback），音高按既有口径取 CE 值减 1（1.4 → 0.4），
    /// <c>CEUtils.WeapSound</c> 按 1.0 后音量取 CE 的 0.4 倍；② 粒子换成
    /// <see cref="VoidImpactParticle"/>（CE 的 PRT_VoidImpactParticle）；③ CE 的 <c>UseBlendState</c>
    /// 本机 tML 没有，改用 End + 立即模式 Begin，画完补上默认批次的恢复（CE 把批次留在加法混合下就不管了）。
    /// </para>
    /// </summary>
    internal class VoidImpact:ModProjectile
    {
        /// <summary>拖尾采样点（最多 64 段），首段起越靠后越淡越小</summary>
        public List<Vector2> oldPos = new List<Vector2>();
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 1;
        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 116;
            Projectile.height = 116;
            Projectile.friendly = true;
            Projectile.penetrate = -1;                   // 无限穿透
            Projectile.tileCollide = false;
            Projectile.light = 1f;
            Projectile.timeLeft = 100;
            Projectile.extraUpdates = 5;
            Projectile.scale = 2;
        }
        /// <summary>
        /// 首次命中：朝四个斜角各炸两条冲击条（共 8 条），并把自身伤害清零 ——
        /// 那个 0 同时是"已经炸过"的标记，<see cref="AI"/> 据此转入消散。
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(CalamityDemutationSounds.VoidImpactHit with { Pitch = 0.4f, Volume = 0.4f }, Projectile.Center);
            float[] rots = new float[] { MathHelper.PiOver4, -MathHelper.PiOver4, MathHelper.PiOver4 * 3, MathHelper.PiOver4 * -3 };
            for (int i = 0; i < rots.Length; i++)
            {
                float r = rots[i] + Projectile.rotation;
                VoidImpactParticle inner = new VoidImpactParticle();
                DRKLoader.NewParticle(inner, target.Center, r.ToRotationVector2() * 9, Color.White, 1.8f);
                inner.Configure(1f, r, 46);
                VoidImpactParticle outer = new VoidImpactParticle();
                DRKLoader.NewParticle(outer, target.Center, r.ToRotationVector2() * 12, Color.White, 2f);
                outer.Configure(1f, r, 46);
            }
            Projectile.damage = 0;
        }
        public override void AI()
        {
            if (Projectile.ai[2] == 0)
            {
                Projectile.ai[2] = 1;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            oldPos.Add(Projectile.Center);
            if (oldPos.Count > 64)
            {
                oldPos.RemoveAt(0);
            }
            if (Projectile.damage == 0)
            {
                // 命中后：转为一团缓慢消散的拖尾
                Projectile.ai[2] *= 0.98f;
                Projectile.velocity *= 0.97f;
            }
        }
        /// <summary>
        /// 自绘：先在当前批次下把 64 段历史点由淡到浓铺成拖尾，然后切到加法混合把剑气本体画三遍
        /// （1 / 0.8 / 0.6 三档尺寸叠出核心），最后恢复默认批次。生命末 30 帧整体淡出。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            lightColor = Color.White * Projectile.ai[2];
            if (Projectile.timeLeft < 30)
            {
                lightColor *= Projectile.timeLeft / 30f;
            }
            float scale = 0;
            float scale2 = 1 + (float)Math.Cos(Main.GameUpdateCount * 0.52f) * 0.12f;
            Texture2D tex = TextureAssets.Projectile[Projectile.type].Value;
            for (int i = 0; i < oldPos.Count; i++)
            {
                scale += 1f / oldPos.Count;
                Main.spriteBatch.Draw(tex, oldPos[i] - Main.screenPosition, null, lightColor * (i / (float)oldPos.Count) * 0.6f, Projectile.rotation, tex.Size() / 2, Projectile.scale * Projectile.ai[2] * scale * scale2 * 0.6f, SpriteEffects.None, 0);
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2, Projectile.scale * scale * scale2 * Projectile.ai[2], SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2, Projectile.scale * scale * scale2 * 0.8f * Projectile.ai[2], SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2, Projectile.scale * scale * scale2 * 0.6f * Projectile.ai[2], SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
