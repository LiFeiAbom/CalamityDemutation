using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Rogue
{
    /// <summary>
    /// 弹道毒炸弹的本体（照灾厄 2.0 的 <c>BallisticPoisonBombProj</c>）：
    /// 24×24 判定、穿透无限、存活 200 帧、不撞物块（靠 <see cref="StickToTiles"/> 粘在方块上）；
    /// 每帧 1/6 概率冒毒尘，重力与空气阻力在 10 帧后启动；
    /// 命中敌人挂 3 秒「毒液」并立即引爆。
    /// <para>
    /// 引爆（含 <c>timeLeft ≤ 3</c> 的自然引爆）：先把手感判定框撑到 128，
    /// 然后撒 3~4 枚尖刺（伤害 ×0.5）+ 8~12 朵毒云（伤害 ×0.25）+ 尘/烟。
    /// <c>timeLeft ≤ 3</c> 时那句撑框只写在主人端（照源），因为它是给同步用的。
    /// </para>
    /// <para>
    /// 弹幕生成一律加主人端判据（第 5 节口径）。<c>StickToTiles</c> 与 <c>ExpandHitboxBy</c>
    /// 是灾厄的工具方法，本工程软依赖，按工程既有做法**内联**在这里。
    /// </para>
    /// </summary>
    internal class BallisticPoisonBombProj : ModProjectile
    {
        /// <summary>引爆时撑到的判定框边长（照源）</summary>
        private const int ExplosionSize = 128;
        /// <summary>毒尘类型（源里是裸数字 14）</summary>
        private const int PoisonDust = 14;
        /// <summary>火尘类型（源里是裸数字 6）</summary>
        private const int FireDust = 6;

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 200;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = CDUtil.GetRogueDamageClass();
        }
        /// <summary>粘性下落 + 10 帧后启动重力与空气阻力 + 随水平速度自转</summary>
        public override void AI()
        {
            if (Main.rand.NextBool(6))
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, PoisonDust, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f);
            StickToTiles();
            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft <= 3)
            {
                Projectile.tileCollide = false;
                Projectile.ai[1] = 0f;
                Projectile.alpha = 255;
                ExpandHitbox(ExplosionSize);
            }
            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] > 10f)
            {
                Projectile.ai[0] = 10f;
                if (Projectile.velocity.Y == 0f && Projectile.velocity.X != 0f)
                {
                    Projectile.velocity.X *= 0.97f;
                    if (Math.Abs(Projectile.velocity.X) < 0.01f)
                    {
                        Projectile.velocity.X = 0f;
                        Projectile.netUpdate = true;
                    }
                }
                Projectile.velocity.Y += 0.2f;
            }
            Projectile.rotation += Projectile.velocity.X * 0.1f;
        }
        /// <summary>命中敌人：挂 3 秒「毒液」并立即引爆</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Venom, 180);
            Projectile.Kill();
        }
        /// <summary>PvP 同理</summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Venom, 180);
            Projectile.Kill();
        }
        /// <summary>引爆：撑框 → 巨响 → 尖刺 3~4 枚 + 毒云 8~12 朵 → 毒尘/火尘/烟</summary>
        public override void OnKill(int timeLeft)
        {
            ExpandHitbox(ExplosionSize);
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            if (Projectile.owner == Main.myPlayer)
            {
                int spikeAmount = Main.rand.Next(3, 5);
                for (int i = 0; i < spikeAmount; i++)
                {
                    Vector2 velocity = RandomVelocity(100f, 70f, 100f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<BallisticPoisonBombSpike>(), (int)(Projectile.damage * 0.5f), 0f, Projectile.owner, 0f, 0f);
                }
                int cloudAmount = Main.rand.Next(8, 13);
                for (int i = 0; i < cloudAmount; i++)
                {
                    Vector2 velocity = RandomVelocity(100f, 10f, 200f, 0.01f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<BallisticPoisonCloud>(), (int)(Projectile.damage * 0.25f), 1f, Projectile.owner, 0f, Main.rand.Next(-45, 1));
                }
            }
            for (int i = 0; i < 5; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, PoisonDust, 0f, 0f, 100, default, 2f);
                Main.dust[dust].velocity *= 3f;
                if (Main.rand.NextBool(2))
                {
                    Main.dust[dust].scale = 0.5f;
                    Main.dust[dust].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
            }
            for (int i = 0; i < 9; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, FireDust, 0f, 0f, 100, default, 3f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 5f;
                dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, FireDust, 0f, 0f, 100, default, 2f);
                Main.dust[dust].velocity *= 2f;
            }
            if (Main.netMode != NetmodeID.Server)
                SpawnExplosionSmoke();
        }
        /// <summary>
        /// 内联灾厄 <c>CalamityGlobalProjectile.ExpandHitboxBy</c>：以圆心为基准重设判定框边长。
        /// </summary>
        private void ExpandHitbox(int newSize)
        {
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = newSize;
            Projectile.position -= Projectile.Size * 0.5f;
        }
        /// <summary>
        /// 内联灾厄 <c>ProjectileUtils.RandomVelocity</c>：在 ±directionMult 的方形里随机取一个方向，
        /// 长度取 <c>rand(speedLowerLimit, speedCap) × speedMult</c>（避免零向量）。
        /// </summary>
        private static Vector2 RandomVelocity(float directionMult, float speedLowerLimit, float speedCap, float speedMult = 0.1f)
        {
            Vector2 velocity = new Vector2(Main.rand.NextFloat(-directionMult, directionMult), Main.rand.NextFloat(-directionMult, directionMult));
            while (velocity.X == 0f && velocity.Y == 0f)
                velocity = new Vector2(Main.rand.NextFloat(-directionMult, directionMult), Main.rand.NextFloat(-directionMult, directionMult));
            velocity.Normalize();
            velocity *= Main.rand.NextFloat(speedLowerLimit, speedCap) * speedMult;
            return velocity;
        }
        /// <summary>
        /// 内联灾厄 <c>Projectile.StickToTiles(this Projectile, ignorePlatforms: true, stickToEverything: false)</c>：
        /// 扫身边 3×3 格，碰到**实心且未致动**的方块（平台与种植箱不算）就把速度清零、留一点向上的吸附力，
        /// 于是炸弹会挂在墙面/地面上（源就是这么写"粘性"的）。
        /// </summary>
        private void StickToTiles()
        {
            int xLeft = (int)(Projectile.position.X / 16f) - 1;
            int xRight = (int)((Projectile.position.X + Projectile.width) / 16f) + 2;
            int yBottom = (int)(Projectile.position.Y / 16f) - 1;
            int yTop = (int)((Projectile.position.Y + Projectile.height) / 16f) + 2;
            xLeft = Math.Max(xLeft, 0);
            xRight = Math.Min(xRight, Main.maxTilesX);
            yBottom = Math.Max(yBottom, 0);
            yTop = Math.Min(yTop, Main.maxTilesY);
            for (int x = xLeft; x < xRight; x++)
            {
                for (int y = yBottom; y < yTop; y++)
                {
                    Tile tile = Main.tile[x, y];
                    bool notPlatform = !TileID.Sets.Platforms[tile.TileType] && tile.TileType != TileID.PlanterBox;
                    if (!tile.HasUnactuatedTile || !notPlatform || !Main.tileSolid[tile.TileType])
                        continue;
                    Vector2 tilePos = new Vector2(x * 16, y * 16);
                    if (Projectile.position.X + Projectile.width - 4f > tilePos.X && Projectile.position.X + 4f < tilePos.X + 16f &&
                        Projectile.position.Y + Projectile.height - 4f > tilePos.Y && Projectile.position.Y + 4f < tilePos.Y + 16f)
                    {
                        Projectile.velocity.X = 0f;
                        Projectile.velocity.Y = -0.2f;
                    }
                }
            }
        }
        /// <summary>
        /// 爆炸烟：照源在非服务端撒 12 团原版烟（<c>Gore 61~63</c>，四方向各一团、每团三种速度档）。
        /// </summary>
        private void SpawnExplosionSmoke()
        {
            Vector2 source = Projectile.Center - new Vector2(24f, 24f);
            const int goreAmount = 3;
            for (int i = 0; i < goreAmount; i++)
            {
                float velocityMult = i < goreAmount / 3 ? 0.66f : (i >= 2 * goreAmount / 3 ? 1f : 0.33f);
                for (int direction = 0; direction < 4; direction++)
                {
                    int type = Main.rand.Next(61, 64);
                    int smoke = Gore.NewGore(Projectile.GetSource_Death(), source, default, type, 1f);
                    Gore gore = Main.gore[smoke];
                    gore.velocity *= velocityMult;
                    gore.velocity.X += direction == 0 || direction == 2 ? 1f : -1f;
                    gore.velocity.Y += direction < 2 ? 1f : -1f;
                }
            }
        }
    }
}
