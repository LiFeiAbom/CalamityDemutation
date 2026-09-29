using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 天罚斩击（EXNemesisProj，移植自 CalamityEntropy 的 <c>Nemesis/EXNemesisProj.cs</c>）——
    /// 左键每满 6 次触发的「天罚」本体。贴图直接复用物品的 <c>Nemesis</c>，354×354、scale 5、穿透 -1、extraUpdates 3、存活 180 帧。
    /// 三段节奏（按 <c>ai[0]</c> 计时）：前 30 帧悬停且淡入 → 30~80 帧骤然加速并溅 6 颗光晕粒子 + 相机震动 → 之后减速淡出。
    /// <para>
    /// 与 CE 的差异：① 光晕粒子改用本工程的 <see cref="LightParticle"/>（CE 是 <c>PRT_Light</c>）与 <c>DRKLoader</c>；
    /// ② 描边改调本工程的 <see cref="CDUtil.DrawRotatingMarginEffect"/>；③ 龙焰走软依赖施加（缺灾厄该 buff 时静默跳过）；
    /// ④ <c>isFs</c> 的镜像绘制照抄源。
    /// </para>
    /// </summary>
    internal class EXNemesisProj:ModProjectile
    {
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/Nemesis";
        /// <summary>朝右为 true，决定绘制时的翻转与额外旋转（首帧按玩家朝向定死）</summary>
        private bool isFs;
        /// <summary>淡入淡出进度：0~300，前段每帧 +10、后段每帧 -10；>155 时才叠描边光圈</summary>
        private int alp;
        /// <summary>首帧记下的原始速度，用于每段统一取「单位方向 × 各自速率」</summary>
        private Vector2 origVer;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 1;
        }
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 354;
            Projectile.timeLeft = 180;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 3;
            Projectile.scale = 5;
            Projectile.tileCollide = false;
            alp = 0;
        }
        /// <summary>三段节奏：悬停淡入（&lt;30）→ 冲刺爆发（&lt;80，出粒子 + 相机震动）→ 减速淡出；角度比速度多转 45°</summary>
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (Projectile.ai[0] == 0)
            {
                origVer = Projectile.velocity;
                isFs = player.direction > 0;
            }
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3());
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            if (Projectile.ai[0] < 30)
            {
                Projectile.velocity = origVer.UnitVector() * 0.3f;
                if (alp < 300)
                {
                    alp += 10;
                }
            }
            else if (Projectile.ai[0] < 80)
            {
                alp = 300;
                Projectile.velocity = origVer.UnitVector() * 32;
                // 服务器上 PRTLoader.NewParticle 会给孤儿实例，故照抄源的 dedServ 守卫
                if (!Main.dedServ)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 ver = Projectile.velocity.GetNormalVector() * Main.rand.NextFloat(-116, 116);
                        LightParticle light = new LightParticle();
                        DRKLoader.NewParticle(light, Projectile.Center + Projectile.velocity * 10, ver, Color.OrangeRed, Main.rand.NextFloat(1.3f, 1.7f));
                        light.Configure(0.2f, lifetime: 32);
                    }
                }
                PunchCameraModifier modifier = new PunchCameraModifier(Projectile.Center
                        , (Main.rand.NextFloat() * ((float)Math.PI * 2f)).ToRotationVector2(), 20f, 6f, 20, 1000f, FullName);
                Main.instance.CameraModifiers.Add(modifier);
            }
            else
            {
                Projectile.velocity = origVer.UnitVector() * 0.3f;
                if (alp > 0)
                {
                    alp -= 10;
                }
            }
            Projectile.ai[0]++;
        }
        /// <summary>命中：挂 150 帧贝蒂诅咒 + 180 帧龙焰（灾厄本家 debuff，缺则静默跳过）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.BetsysCurse, 150);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "Dragonfire", 180);
        }
        /// <summary>命中玩家（PvP）：与 OnHitNPC 同构</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.BetsysCurse, 150);
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "Dragonfire", 180);
        }
        /// <summary>绘制：拖影按 alp 淡入淡出，alp &gt; 155 时叠红色旋转描边，最后按 <c>isFs</c> 决定镜像画本体</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawOrigin = texture.Size() / 2;
            float rot = Projectile.rotation;
            float newAlp = alp / 300f;
            if (!isFs)
            {
                rot += MathHelper.PiOver2;
            }
            SpriteEffects spriteEffects = isFs ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            for (int k = 0; k < Projectile.oldPos.Length; k++)
            {
                Vector2 offsetPos = Projectile.oldPos[k].To(Projectile.position);
                Vector2 drawPos = Projectile.Center - Main.screenPosition - offsetPos;
                Color color = Projectile.GetAlpha(Color.Pink) * ((Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length);
                Main.EntitySpriteDraw(texture, drawPos, null, color * newAlp, rot, drawOrigin, Projectile.scale, spriteEffects, 0);
            }
            if (alp > 155)
            {
                CDUtil.DrawRotatingMarginEffect(Main.spriteBatch, texture, Projectile.timeLeft, Projectile.Center - Main.screenPosition
                , null, Color.Red, rot, drawOrigin, Projectile.scale, spriteEffects);
            }
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White * newAlp
                , rot, drawOrigin, Projectile.scale, spriteEffects, 0);
            return false;
        }
    }
}
