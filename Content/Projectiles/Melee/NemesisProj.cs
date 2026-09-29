using CalamityDemutation.Content.Buffs.NegativeBuffs;
using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
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
    /// 龙陨星（NemesisProj，移植自 CalamityEntropy 的 <c>Nemesis/NemesisProj.cs</c>）：
    /// 54×54、穿透 10、extraUpdates 3、存活 120 帧，落在玩家下方才撞地形（<c>tileCollide</c> 每帧按 y 比较）。
    /// 命中时挂 320 帧龙焰 + 生命压制，追加一次 <c>WeaponDamage/3</c> 的伤害，洒 60 颗尘并 <c>Explode(360)</c>；
    /// 若本体是 <c>ai[0] == 0</c>（普通星）且未派生过、场上不足 136 颗，则朝**远离玩家**的方向再派生一颗
    /// <c>ai[0] == 1</c> 的星。后者（由真近战命中生成）靠近玩家 <c>width</c> 范围内时回血 10~14。
    /// <para>
    /// 与 CE 的差异：① 回血改走本工程的 <c>CalamityDemutationPlayer.TryHealMeWithCd</c>
    /// （玩家级共享冷却，与 CE 的 <c>Entropy().TryHealMeWithCd</c> 同义；底层仍是 <c>Player.Heal</c>，自带超上限夹取）；② 描边改调本工程的
    /// <see cref="CDUtil.DrawRotatingMarginEffect"/>；③ 龙焰走软依赖施加；④ 补一份 PvP 命中（本工程约定）。
    /// </para>
    /// </summary>
    internal class NemesisProj:ModProjectile
    {
        /// <summary>本方是否已回过血（<c>ai[0] == 1</c> 的星只回一次）</summary>
        private bool canHeal;
        /// <summary>本方是否已派生过子星（<c>ai[0] == 0</c> 的星只派生一次）</summary>
        private bool spwanProj;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 1;
        }
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 54;
            Projectile.timeLeft = 120;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.penetrate = 10;
            Projectile.extraUpdates = 3;
        }
        /// <summary>落点越过玩家所在高度后才开始撞地形；<c>ai[0] == 1</c> 的星贴到玩家身上时回血（带冷却）</summary>
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.tileCollide = Projectile.position.Y > player.position.Y;
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3());
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            if (Projectile.ai[0] == 1 && Projectile.IsOwnedByLocalPlayer() && !canHeal
                && Projectile.Center.Distance(player.Center) < Projectile.width)
            {
                int num = Main.rand.Next(10, 15);
                player.CD().TryHealMeWithCd(num, 5);
                SoundEngine.PlaySound(SoundID.DD2_DarkMageHealImpact);
                canHeal = true;
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "Dragonfire", 320);
            target.AddBuff(ModContent.BuffType<LifeOppress>(), 320);
            player.ApplyDamageToNPC(target, player.GetWeaponDamage(player.HeldItem) / 3, 0f, 0
                , false, DamageClass.Default, true);
            SpawnExplosionDust(target.position, target.width, target.height, target.Center, target.velocity, target.rotation, true);
            Projectile.Explode(360);
            if (Projectile.ai[0] == 0 && !spwanProj && player.ownedProjectileCounts[Type] < 136)
            {
                Vector2 ver = player.Center.To(Projectile.Center).UnitVector() * -18;
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, ver.RotatedByRandom(0.22f)
                    , Type, Projectile.damage / 2, Projectile.knockBack / 2, Projectile.owner, 1);
                spwanProj = true;
            }
        }
        /// <summary>命中玩家（PvP）：与 OnHitNPC 同构，但跳过 NPC 专有的追加伤害与派生</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            CalamityDemutationPlayer.ApplyCalamityBuff(target, "CalamityMod", "Dragonfire", 320);
            target.AddBuff(ModContent.BuffType<LifeOppress>(), 320);
            // 玩家没有 NPC.rotation，取结构等价的 fullRotation（普通玩家恒为 0）
            SpawnExplosionDust(target.position, target.width, target.height, target.Center, target.velocity, target.fullRotation, true);
            Projectile.Explode(360);
        }
        public override void OnKill(int timeLeft)
        {
            SpawnExplosionDust(Projectile.position, Projectile.width, Projectile.height, Projectile.Center, Projectile.velocity, Projectile.rotation, false);
        }
        /// <summary>
        /// 源的爆炸尘特效（命中与死亡共用）：60 颗红/橙尘，绕目标随机角铺开、速度随目标速度拖曳。
        /// 命中时比死亡多一段「位置带随机系数」的偏移（源里两处唯一的差别）。
        /// <c>rotation</c> / <c>velocity</c> 由调用方按源分别取「命中目标」或「弹幕自身」。
        /// </summary>
        private static void SpawnExplosionDust(Vector2 position, int width, int height, Vector2 center, Vector2 velocity, float rotation, bool withRandomOffset)
        {
            Vector2 dustRotation = (rotation - MathHelper.PiOver2).ToRotationVector2();
            Vector2 dustVelocity = dustRotation * velocity.Length() / 6;
            _ = SoundEngine.PlaySound(SoundID.Item14, center);
            for (int j = 0; j < 60; j++)
            {
                float thirdDustScale = Main.rand.NextFloat(2, 4);
                bool noGvk = true;
                int dustId = DustID.InfernoFork;
                if (Main.rand.NextBool(2))
                {
                    noGvk = false;
                    dustId = DustID.FireworkFountain_Red;
                    thirdDustScale /= 6f;
                }
                int contactDust2 = Dust.NewDust(position, width, height, dustId, 0f, 0f, 0, default, thirdDustScale);
                Dust dust = Main.dust[contactDust2];
                Vector2 baseOffset = Vector2.UnitX.RotatedByRandom(MathHelper.Pi)
                    .RotatedBy(velocity.ToRotation()) * width / 3f;
                if (withRandomOffset)
                {
                    baseOffset *= Main.rand.NextFloat();
                }
                dust.position = center + baseOffset;
                dust.noGravity = noGvk;
                dust.velocity.Y -= 3f;
                dust.velocity *= 1.5f;
                dust.velocity += dustVelocity * (0.6f + 13.6f * Main.rand.NextFloat());
            }
        }
        /// <summary>绘制：拖影（品红渐隐）→ 旋转描边（普通星红 / 派生星金）→ 本体</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawOrigin = texture.Size() / 2;
            for (int k = 0; k < Projectile.oldPos.Length; k++)
            {
                Vector2 offsetPos = Projectile.oldPos[k].To(Projectile.position);
                Vector2 drawPos = Projectile.Center - Main.screenPosition - offsetPos;
                Color color = Projectile.GetAlpha(Color.Pink) * ((Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length);
                Main.EntitySpriteDraw(texture, drawPos, null, color, Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None, 0);
            }
            if (Projectile.ai[0] == 0)
            {
                CDUtil.DrawRotatingMarginEffect(Main.spriteBatch, texture, Projectile.timeLeft, Projectile.Center - Main.screenPosition
                , null, Color.Red, Projectile.rotation, drawOrigin, Projectile.scale, 0);
            }
            else
            {
                CDUtil.DrawRotatingMarginEffect(Main.spriteBatch, texture, Projectile.timeLeft, Projectile.Center - Main.screenPosition
                , null, Color.Gold, Projectile.rotation, drawOrigin, Projectile.scale * 1.05f, 0);
            }
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor)
                , Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
