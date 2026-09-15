using System;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 迷你神圣守卫的爆弹裂片（移植自灾厄 2.2.2 的 MiniGuardianFireballSplit，贴图源自 HolyFire2）：
    /// 由 MiniGuardianFireball 炸裂生成（ai[0] == 1）。存活 300 帧、4 帧动画，
    /// 横向速度不足 8 时每帧 ×1.05 加速，并持续朝 2000 像素内最近的敌人（优先主人右键锁定目标）转向，
    /// 转向时保持原速度大小（只改方向）。伤害按通用伤害折算。
    /// </summary>
    internal class MiniGuardianFireballSplit:ModProjectile
    {
        /// <summary>夜晚贴图路径（白天直接用类名对应的贴图）</summary>
        private const string NightTexture = "CalamityDemutation/Content/Projectiles/Summon/MiniGuardianFireballSplitNight";
        /// <summary>注册 4 帧动画、对邪教徒类敌人有抗性减免、可被右键锁定目标、鞭标记 0.3 倍增伤</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
            ProjectileID.Sets.SummonTagDamageMultiplier[Type] = 0.3f;
        }
        /// <summary>基础属性：26x26 碰撞箱、友方、无视水、穿透 -1、不撞地形、额外更新 1 次、存活 300 帧、每敌人 6 帧独立命中冷却</summary>
        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 300;
            Projectile.minion = true;
            Projectile.gfxOffY = -25f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
            Projectile.DamageType = DamageClass.Generic;
        }
        /// <summary>
        /// AI：每帧按通用伤害重算 damage、4 帧循环动画（每 6 帧切一帧）；横向速度不足 8 时每帧 ×1.05；
        /// 索敌后按惯性 15 把速度方向插值到目标方向、再归一化回原速度大小（只转向不加速），最后按横向速度微微侧倾。
        /// </summary>
        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.3f, 0.225f, 0f);
            var Owner = Main.player[Projectile.owner];
            Projectile.damage = (int)Owner.GetTotalDamage<GenericDamageClass>().ApplyTo(Projectile.originalDamage);
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 6)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 3)
                Projectile.frame = 0;
            if (Math.Abs(Projectile.velocity.X) < 8f)
                Projectile.velocity.X *= 1.05f;
            // 原为 CalamityUtils.MinionHoming，本工程用 MiniGuardianTargeting 的等价实现
            NPC target = MiniGuardianTargeting.MinionHoming(Projectile.Center, 2000f, Main.player[Projectile.owner], true);
            if (target != null)
            {
                float scaleFactor2 = Projectile.velocity.Length();
                Vector2 vector11 = target.Center - Projectile.Center;
                vector11.Normalize();
                vector11 *= scaleFactor2;
                float inertia = 15f;
                Projectile.velocity = (Projectile.velocity * (inertia - 1f) + vector11) / inertia;
                Projectile.velocity.Normalize();
                Projectile.velocity *= scaleFactor2;
            }
            Projectile.rotation = Projectile.velocity.X * 0.025f;
        }
        /// <summary>昼夜配色（白天橙红 / 夜晚青蓝）</summary>
        public override Color? GetAlpha(Color lightColor)
        {
            return MiniGuardianSpear.ProfanedColor(!Main.dayTime, Projectile.alpha);
        }
        /// <summary>绘制：夜晚换用 Night 贴图，先画十向背光再按当前帧画本体</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Main.dayTime ? TextureAssets.Projectile[Type].Value : ModContent.Request<Texture2D>(NightTexture, AssetRequestMode.ImmediateLoad).Value;
            int frameHeight = texture.Height / Main.projFrames[Type];
            int frameY = frameHeight * Projectile.frame;
            Rectangle frame = new Rectangle(0, frameY, texture.Width, frameHeight);
            Projectile.DrawBackglow(MiniGuardianSpear.ProfanedColor(!Main.dayTime, Projectile.alpha, true), 4f, texture);
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), frame, Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2(texture.Width / 2f, frameHeight / 2f), Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
        /// <summary>自毁：播放原版 SoundID.Item14（爆炸）并把判定框临时放大到 200 见方，按昼夜喷两轮粉尘</summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
            Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
            Projectile.width = Projectile.height = 200;
            Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
            int dust = MiniGuardianHealer.HolyDustType(!Main.dayTime);
            for (int num621 = 0; num621 < 4; num621++)
            {
                int num622 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dust, 0f, 0f, 100, default, Main.dayTime ? 2f : 0.5f);
                Main.dust[num622].velocity *= 3f;
                if (Main.rand.NextBool())
                {
                    Main.dust[num622].scale = 0.5f;
                    Main.dust[num622].fadeIn = 1f + (float)Main.rand.Next(10) * 0.1f;
                }
            }
            for (int num623 = 0; num623 < 12; num623++)
            {
                int num624 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dust, 0f, 0f, 100, default, Main.dayTime ? 3f : 0.5f);
                Main.dust[num624].noGravity = true;
                Main.dust[num624].velocity *= 5f;
                num624 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dust, 0f, 0f, 100, default, Main.dayTime ? 2f : 0.5f);
                Main.dust[num624].velocity *= 2f;
            }
        }
    }
}
