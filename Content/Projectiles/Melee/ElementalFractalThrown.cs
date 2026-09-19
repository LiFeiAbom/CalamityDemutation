using CalamityDemutation.Players;
using CalamityDemutation.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 掷出的元素剑影（ElementalFractalThrown，移植自 CalamityEntropy）：元素分形刺出式甩出的强力剑影，
    /// 威力是本体伤害的 1.6 倍、穿透无限并附带「无视防御」的护甲穿透。拖尾逐段渐隐，本体随寿命淡化。
    /// <para>
    /// 与 CE 原版的差异：① 贴图换成本模组 Content/Items/Weapons/Melee/ElementalFractalGlow；
    /// ② <c>GetTexture()</c> 换成 TextureAssets；③ 去掉 <c>hit.DamageType = NoDRMelee.Instance;</c>——
    /// <c>NoDRMelee</c> 是 CE 自研的免伤减免职业（本工程没有，参照 DragonRage 移植的先例统一并到 <c>Melee</c>），
    /// 且 tML 的 <c>OnHitNPC</c> 传进来的 <c>hit</c> 是按值传递，这行本来就改不到实际伤害；
    /// ④ 减益不新建——本工程已有 `FractalBlight` 立好的口径：现代版灾厄取 <c>ElementalMix</c>、
    /// 经典版没有它则按经典版元素武器的口径用四元素等效，统一经
    /// <see cref="CalamityDemutationPlayer.ApplyCalamityBuff"/> 挂载；⑤ 音效走本模组
    /// <see cref="CalamityDemutationSounds.FractalSwingHit"/>（CE 原名 sf_hit，音量 0.6）；
    /// ⑥ CE 的 <c>UseBlendState</c>/<c>ExitShaderRegion</c> 本机 tML 没有，改用 End + 立即模式 Begin(Additive)、
    /// 画完再恢复默认批次。
    /// </para>
    /// </summary>
    internal class ElementalFractalThrown:ModProjectile
    {
        /// <summary>直接复用武器本体的 Glow 贴图</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/ElementalFractalGlow";
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;         // 同时记录 oldPos 与 oldRot，供拖尾重绘
            ProjectileID.Sets.TrailCacheLength[Type] = 26;
        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.friendly = true;
            Projectile.light = 1f;
            Projectile.penetrate = -1;                        // 无限穿透
            Projectile.timeLeft = 60 * 2;                     // 配合 2 倍速 → 2 秒
            Projectile.scale *= 1.6f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.MaxUpdates = 2;
            Projectile.tileCollide = false;
        }
        public override void AI()
        {
            Projectile.Opacity = 0.4f + 0.6f * Projectile.timeLeft / 120f;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        /// <summary>自绘：加法混合下沿 oldPos 逐段重画剑影（越靠后越淡越小），最后画本体</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            int trailLength = ProjectileID.Sets.TrailCacheLength[Type];
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            for (int i = 0; i < trailLength; i++)
            {
                float prog = i / (float)trailLength;
                Draw(Projectile.oldPos[i] + new Vector2(Projectile.width, Projectile.height) * 0.5f, Color.White * 0.36f * (1 - prog), Projectile.oldRot[i], (int)Projectile.ai[1], 0.4f + 0.6f * (1 - prog));
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Draw(Projectile.Center, Color.White, Projectile.rotation, Math.Sign(Projectile.velocity.X));
            return false;
        }
        /// <summary>命中：播命中音、挂元素减益（两版各取现成的）、炸一圈 Keybrand 粒子</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(CalamityDemutationSounds.FractalSwingHit with { Volume = 0.6f }, Projectile.Center);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "ElementalMix", 400);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "HolyLight", 400);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "GlacialState", 400);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "BrimstoneFlames", 400);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityModClassicPreTrailer", "Plague", 400);
            ParticleOrchestrator.RequestParticleSpawn(clientOnly: true, ParticleOrchestraType.Keybrand, new ParticleOrchestraSettings
            {
                PositionInWorld = target.Center,
                MovementVector = Vector2.Zero
            });
        }
        /// <summary>附带「无视防御」的护甲穿透（CE 原式：目标防御 + 64）</summary>
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ArmorPenetration += target.defense + 64;
        }
        /// <summary>消散时再炸一圈 Keybrand 粒子</summary>
        public override void OnKill(int timeLeft)
        {
            ParticleOrchestrator.RequestParticleSpawn(clientOnly: true, ParticleOrchestraType.Keybrand, new ParticleOrchestraSettings
            {
                PositionInWorld = Projectile.Center,
                MovementVector = Vector2.Zero
            });
        }
        /// <summary>按 dir 决定是否水平翻转，并相应地取四分之一圈作为绘制旋转（CE 原样）</summary>
        private void Draw(Vector2 pos, Color lightColor, float rotation, int dir, float scale = 1f)
        {
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? rotation + MathHelper.PiOver4 : rotation + MathHelper.Pi * 0.75f;
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(texture, pos - Main.screenPosition, null, lightColor * Projectile.Opacity, rot, texture.Size() * 0.5f, Projectile.scale * scale, effect);
        }
    }
}
