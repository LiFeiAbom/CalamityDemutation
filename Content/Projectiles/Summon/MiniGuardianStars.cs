using System;
using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 迷你神圣守卫星弹（移植自灾厄 2.2.2 的 MiniGuardianStars）：由治愈者守卫齐射抛出的环形放射性弹幕。
    /// 发射后 240 帧内持续加速（速度低于 16 时每帧 ×1.01），并强制把存活时间锁在 160 帧以上；
    /// 命中敌人或玩家立即自毁。绘制时用十字/斜向多次叠画做出"星光"效果。
    /// </summary>
    internal class MiniGuardianStars:ModProjectile
    {
        /// <summary>基础属性：30x30 碰撞箱、友方、无视水、不撞地形、完全透明（由 PreDraw 自绘）、穿透 1、存活 200 帧</summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 200;
        }
        /// <summary>
        /// AI：每 4 帧补一次光照（省开销）；伤害按主人召唤伤害加成实时刷新（抵消多次加成叠加）；
        /// ai[0] 在 240 帧内自增，期间把存活时间锁在 160 帧以上，保证星弹至少飞满一段距离。
        /// </summary>
        public override void AI()
        {
            if (Projectile.timeLeft % 4 == 0) // 每 4 帧才补一次光照
                Lighting.AddLight(Projectile.Center, 0.45f, 0.35f, 0f);
            Player owner = Main.player[Projectile.owner];
            Projectile.damage = (int)owner.GetTotalDamage<GenericDamageClass>().ApplyTo(Projectile.originalDamage);
            if (Projectile.ai[0] < 240f)
            {
                Projectile.ai[0] += 1f;
                if (Projectile.timeLeft < 160)
                    Projectile.timeLeft = 160;
            }
            // 速度未达上限 16 时持续轻微加速
            if (Projectile.velocity.Length() < 16f)
                Projectile.velocity *= 1.01f;
        }
        /// <summary>
        /// 绘制：用同一贴图按 4 个方向（上下左右）与 4 个斜向各叠画一层，
        /// lerpMult 同时做淡入淡出与周期性呼吸缩放，形成旋转十字星光。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            float lerpMult = Utils.GetLerpValue(15f, 30f, Projectile.timeLeft, clamped: true) * Utils.GetLerpValue(240f, 200f, Projectile.timeLeft, clamped: true) * (1f + 0.2f * (float)Math.Cos(Main.GlobalTimeWrappedHourly % 30f / 0.5f * (MathHelper.Pi * 2f) * 3f)) * 0.8f;
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            // 亵渎之魂四态配色（对齐 2.2.2 的 ProfanedSoulCrystal.GetColorForPsc 调用）
            Color baseColor = MiniGuardianHealer.PscColor(Main.player[Projectile.owner].GetModPlayer<CalamityDemutationPlayer>().pscState, Main.dayTime);
            baseColor *= 0.5f;
            baseColor.A = 0;
            Color colorA = baseColor;
            Color colorB = baseColor * 0.5f;
            colorA *= lerpMult;
            colorB *= lerpMult;
            Vector2 origin = texture.Size() / 2f;
            Vector2 scale = new Vector2(0.5f, 1.5f) * lerpMult;
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
                spriteEffects = SpriteEffects.FlipHorizontally;
            float upRight = MathHelper.PiOver4;
            float up = MathHelper.PiOver2;
            float upLeft = 3f * MathHelper.PiOver4;
            float left = MathHelper.Pi;
            Main.EntitySpriteDraw(texture, drawPos, null, colorA, upLeft, origin, scale, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorA, upRight, origin, scale, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorB, upLeft, origin, scale * 0.6f, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorB, upRight, origin, scale * 0.6f, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorA, up, origin, scale * 0.6f, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorA, left, origin, scale * 0.6f, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorB, up, origin, scale * 0.36f, spriteEffects, 0);
            Main.EntitySpriteDraw(texture, drawPos, null, colorB, left, origin, scale * 0.36f, spriteEffects, 0);
            return false;
        }
        /// <summary>自毁：播放爆炸音、把碰撞箱临时扩大到 50 并喷出两轮神圣色粉尘（白天更大更亮）</summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            Projectile.ExpandHitboxBy(50);
            int dustType = MiniGuardianHealer.HolyDustType(!Main.dayTime);
            // 第一轮：5 颗缓慢外扩的粉尘，随机一半改为小尺寸长淡入
            for (int d = 0; d < 5; d++)
            {
                int holy = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, 0f, 0f, 100, default, Main.dayTime ? 2f : 0.5f);
                Main.dust[holy].velocity *= 3f;
                Main.dust[holy].noGravity = true;
                if (Main.rand.NextBool())
                {
                    Main.dust[holy].scale = 0.5f;
                    Main.dust[holy].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
            }
            // 第二轮：8 组快速外扩的粉尘，每组额外补一颗慢速粉尘
            for (int d = 0; d < 8; d++)
            {
                int fire = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, 0f, 0f, 100, default, Main.dayTime ? 3f : 0.75f);
                Main.dust[fire].noGravity = true;
                Main.dust[fire].velocity *= 5f;
                fire = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, 0f, 0f, 100, default, Main.dayTime ? 2f : 0.5f);
                Main.dust[fire].velocity *= 2f;
                Main.dust[fire].noGravity = true;
            }
        }
        /// <summary>命中敌人即自毁（穿透为 1，命中一次即消耗）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.Kill();
        }
        /// <summary>命中玩家（PvP）同样自毁</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Projectile.Kill();
        }
    }
}
