using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 惊惧之灵（AbominateSpirit） - 女妖之爪重制版（CalamityOverhaul 0.4.0.1.3）的配套弹幕，逐行移植。
    /// <c>Status</c>（= <c>ai[0]</c>）决定贴图与命中效果：
    /// 0 大号（暗红，命中叠暗影焰/狱火/诅咒地狱火）、1 中号（暗绿，命中随机给主人一条武器附魔）、
    /// 2 小号（蓝，命中叠血腥与破晓）、3 治疗形态（金色，飞回主人回血，回血量按 <c>ai[1]</c> 的"目标最大生命"档决定）。
    /// </summary>
    internal class AbominateSpirit : ModProjectile
    {
        public override string Texture
        {
            get
            {
                switch (Status)
                {
                    case 0:
                        return "CalamityDemutation/Content/Projectiles/Melee/GhastlySoulLarge";
                    case 1:
                        return "CalamityDemutation/Content/Projectiles/Melee/GhastlySoulMedium";
                    default:
                        return "CalamityDemutation/Content/Projectiles/Melee/GhastlySoulSmall";
                }
            }
        }
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
        }
        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.scale = 1.5f;
            Projectile.alpha = 100;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Default;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.extraUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }
        public ref float Status => ref Projectile.ai[0];
        /// <summary>出场对准飞行方向并播放鬼魂死亡音（照源）</summary>
        public override void OnSpawn(IEntitySource source)
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            SoundEngine.PlaySound(SoundID.NPCDeath39, Projectile.Center);
        }
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.GameUpdateCount % 15 == 0)
            {
                Projectile.frameCounter++;
            }
            if (Projectile.frameCounter > 3)
            {
                Projectile.frameCounter = 0;
            }
            if (Status == 3)
            {
                // 治疗形态：只活 2 帧但有 extraUpdates，持续追踪主人；进入 60 像素后按档位回血并化尘消散
                Projectile.timeLeft = 2;
                Projectile.scale *= 1.001f;
                Player owner = Main.player[Projectile.owner];
                if (owner != null && owner.active)
                {
                    float length = Projectile.Center.To(owner.Center).Length();
                    Projectile.ChasingBehavior(owner.Center, 6 + length / 100f);
                    if (length < 60)
                    {
                        HealOwner(owner, Projectile.ai[1] > 10000 ? Main.rand.Next(10, 15) : Main.rand.Next(1, 3));
                        Projectile.Kill();
                    }
                }
                if (Projectile.scale > 5)
                {
                    Projectile.Kill();
                }
            }
            if (Projectile.timeLeft < 85)
            {
                Projectile.alpha = Projectile.timeLeft * 3;
            }
            else
            {
                Projectile.alpha = 195;
            }
        }
        /// <summary>回血 + 治疗特效（原版 HealEffect）与治疗尘（照源用满屏 13 颗治疗尘）</summary>
        private static void HealOwner(Player owner, int amount)
        {
            owner.statLife += amount;
            owner.HealEffect(amount);
            if (owner.statLife > owner.statLifeMax2)
                owner.statLife = owner.statLifeMax2;
            for (int i = 0; i < 13; i++)
            {
                Vector2 vr = new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f)) * Main.rand.Next(4, 7);
                Dust.NewDust(owner.Center, 13, 13, DustID.HealingPlus, vr.X, vr.Y);
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            switch ((int)Status)
            {
                case 0:
                    target.AddBuff(BuffID.ShadowFlame, 360);
                    target.AddBuff(BuffID.OnFire3, 360);
                    target.AddBuff(BuffID.CursedInferno, 360);
                    break;
                case 1:
                    int type = Main.rand.Next(0, 5);
                    Player owner = Main.player[Projectile.owner];
                    if (owner == null || !owner.active) return;
                    switch (type)
                    {
                        case 0:
                            owner.AddBuff(BuffID.WeaponImbueCursedFlames, 160);
                            break;
                        case 1:
                            owner.AddBuff(BuffID.WeaponImbueFire, 160);
                            break;
                        case 2:
                            owner.AddBuff(BuffID.WeaponImbueIchor, 160);
                            break;
                        case 3:
                            owner.AddBuff(BuffID.WeaponImbuePoison, 160);
                            break;
                        case 4:
                            owner.AddBuff(BuffID.WeaponImbueNanites, 160);
                            break;
                    }
                    break;
                case 2:
                    target.AddBuff(BuffID.BloodButcherer, 360);
                    target.AddBuff(BuffID.Daybreak, 360);
                    break;
            }
            Projectile.damage -= 20;
        }
        /// <summary>按 Status 着色（暗红/暗绿/蓝/金）并与白色对半混合后绘制</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Color color = Color.White;
            if (Status == 0) color = Color.DarkRed;
            else if (Status == 1) color = Color.DarkGreen;
            else if (Status == 2) color = Color.Blue;
            else color = Color.Gold;
            float alp = Projectile.alpha / 255f;
            color = CDUtil.RecombinationColor((color, 0.5f), (new Color(255, 255, 255), 0.5f));
            int frameHeight = texture.Height / 4;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition,
                new Rectangle(0, frameHeight * Projectile.frameCounter, texture.Width, frameHeight),
                color * alp, Projectile.rotation - MathHelper.PiOver2,
                new Vector2(texture.Width * 0.5f, frameHeight * 0.5f), Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
