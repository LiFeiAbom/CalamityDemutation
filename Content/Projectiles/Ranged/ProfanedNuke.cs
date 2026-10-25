using CalamityDemutation.Players;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Ranged
{
    /// <summary>
    /// 渎神核弹——照 CI ProfanedNuke：飞行时留下伤害粉尘，短暂加速后追踪敌人，终点爆炸并按火箭类型破砖或铺液。
    /// </summary>
    internal class ProfanedNuke : ModProjectile
    {
        private ref float RocketType => ref Projectile.ai[0];
        private int flareDustTimer;
        public override void SetStaticDefaults() => Main.projFrames[Type] = 3;
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 155;
            Projectile.DamageType = DamageClass.Ranged;
        }
        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 7)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 3)
                Projectile.frame = 0;
            flareDustTimer++;
            if (flareDustTimer % 12 == 0 && Projectile.owner == Main.myPlayer)
                SpawnFlareDust(Projectile.damage / 2, 0f);
            if (Math.Abs(Projectile.velocity.X) >= 8f || Math.Abs(Projectile.velocity.Y) >= 8f)
                SpawnTrailDust();
            Projectile.spriteDirection = Projectile.direction = (Projectile.velocity.X > 0f).ToDirectionInt();
            Projectile.rotation = Projectile.velocity.ToRotation() + (Projectile.spriteDirection == 1 ? 0f : MathHelper.Pi) + MathHelper.ToRadians(90f) * Projectile.direction;
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] > 5f)
                CDUtil.HomeInOnNPC(Projectile, false, 1800f, 22f, 15f);
            if (RocketType == ItemID.DryRocket || RocketType == ItemID.WetRocket || RocketType == ItemID.LavaRocket || RocketType == ItemID.HoneyRocket)
            {
                Projectile.ignoreWater = false;
                if (Projectile.wet)
                    Projectile.timeLeft = 1;
            }
        }
        private void SpawnFlareDust(int damage, float extraAge)
        {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<ProfanedNukeDust>(), damage, Projectile.knockBack, Projectile.owner, 0f, extraAge);
        }
        private void SpawnTrailDust()
        {
            Vector2 trailPosition = Projectile.Center - Projectile.velocity * 0.5f;
            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(trailPosition + Main.rand.NextVector2Circular(5f, 5f), DustID.CopperCoin,
                    -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.2f), 100, default, i == 0 ? Main.rand.NextFloat(2f, 3f) : Main.rand.NextFloat(0.5f, 1f));
                dust.noGravity = true;
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CalamityDemutationPlayer.ApplyHolyFlamesWithFallback(target, 300);
            SpawnFlareDust((int)(Projectile.damage * 0.25f), 2f);
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            CalamityDemutationPlayer.ApplyHolyFlamesWithFallback(target, 300);
            SpawnFlareDust((int)(Projectile.damage * 0.25f), 2f);
        }
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            SetHitboxSize(192);
            Projectile.maxPenetrate = -1;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.Damage();
            if (!Main.dedServ)
            {
                SpawnExplosionDust();
                SpawnExplosionGore();
            }
            if (Projectile.owner != Main.myPlayer)
                return;
            SetHitboxSize(22);
            int blastRadius = RocketType switch
            {
                ItemID.RocketII => 6,
                ItemID.RocketIV => 12,
                ItemID.MiniNukeII => 18,
                _ => 0
            };
            if (blastRadius > 0)
                ExplodeTiles(Projectile.Center, blastRadius);
            Point centerTile = Projectile.Center.ToTileCoordinates();
            DelegateMethods.v2_1 = centerTile.ToVector2();
            DelegateMethods.f_1 = 4f;
            if (RocketType == ItemID.DryRocket)
            {
                DelegateMethods.f_1 = 4.5f;
                Utils.PlotTileArea(centerTile.X, centerTile.Y, DelegateMethods.SpreadDry);
            }
            else if (RocketType == ItemID.WetRocket)
                Utils.PlotTileArea(centerTile.X, centerTile.Y, DelegateMethods.SpreadWater);
            else if (RocketType == ItemID.LavaRocket)
                Utils.PlotTileArea(centerTile.X, centerTile.Y, DelegateMethods.SpreadLava);
            else if (RocketType == ItemID.HoneyRocket)
                Utils.PlotTileArea(centerTile.X, centerTile.Y, DelegateMethods.SpreadHoney);
        }
        private void SetHitboxSize(int size)
        {
            Vector2 center = Projectile.Center;
            Projectile.width = Projectile.height = size;
            Projectile.position = center - Projectile.Size * 0.5f;
        }
        private void SpawnExplosionDust()
        {
            for (int i = 0; i < 40; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(96f, 96f), DustID.CopperCoin,
                    Main.rand.NextVector2Circular(12f, 12f), 100, default, Main.rand.NextFloat(0.5f, 2f));
                dust.noGravity = Main.rand.NextBool();
            }
            for (int i = 0; i < 60; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(96f, 96f), DustID.CopperCoin,
                    Main.rand.NextVector2Circular(18f, 18f), 100, default, Main.rand.NextFloat(2f, 3f));
                dust.noGravity = true;
            }
        }
        private void SpawnExplosionGore()
        {
            Vector2 source = Projectile.Center - new Vector2(24f, 24f);
            for (int i = 0; i < 10; i++)
            {
                float velocityMult = i < 3 ? 0.66f : i >= 7 ? 1f : 0.33f;
                for (int j = 0; j < 4; j++)
                {
                    int smoke = Gore.NewGore(Projectile.GetSource_Death(), source, Vector2.Zero, Main.rand.Next(61, 64), 1f);
                    Main.gore[smoke].velocity = new Vector2(j % 2 == 0 ? 1f : -1f, j < 2 ? 1f : -1f) * velocityMult;
                }
            }
        }
        private static void ExplodeTiles(Vector2 explosionPosition, int radius)
        {
            int centerX = (int)(explosionPosition.X / 16f);
            int centerY = (int)(explosionPosition.Y / 16f);
            int minX = Math.Max(centerX - radius, 1);
            int maxX = Math.Min(centerX + radius, Main.maxTilesX - 2);
            int minY = Math.Max(centerY - radius, 1);
            int maxY = Math.Min(centerY + radius, Main.maxTilesY - 2);
            HashSet<int> immuneTiles = new()
            {
                TileID.DemonAltar, TileID.ElderCrystalStand, TileID.Cobalt, TileID.Mythril, TileID.Adamantite,
                TileID.Palladium, TileID.Orichalcum, TileID.Titanium, TileID.Chlorophyte, TileID.LihzahrdBrick,
                TileID.LihzahrdAltar, TileID.DesertFossil
            };
            if (!Main.hardMode)
                immuneTiles.Add(TileID.Hellstone);
            bool allowWallDestruction = false;
            for (int x = minX; x <= maxX && !allowWallDestruction; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    if (distance < radius && Main.tile[x, y].WallType == WallID.None)
                    {
                        allowWallDestruction = true;
                        break;
                    }
                }
            }
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY)) >= radius)
                        continue;
                    Tile tile = Main.tile[x, y];
                    if (!tile.HasTile || immuneTiles.Contains(tile.TileType) || Main.tileContainer[tile.TileType] || Main.tileDungeon[tile.TileType] || !TileLoader.CanExplode(x, y))
                        continue;
                    bool failWhileDestroying = true;
                    bool failWhenChecking = false;
                    if (!TileLoader.CanKillTile(x, y, tile.TileType, ref failWhileDestroying) || !TileLoader.CanKillTile(x, y, tile.TileType, ref failWhenChecking))
                        continue;
                    WorldGen.KillTile(x, y, false, false, false);
                    if (!Main.tile[x, y].HasTile && Main.netMode != NetmodeID.SinglePlayer)
                        NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, x, y, 0f, 0, 0, 0);
                    if (!allowWallDestruction)
                        continue;
                    for (int wallX = x - 1; wallX <= x + 1; wallX++)
                    {
                        for (int wallY = y - 1; wallY <= y + 1; wallY++)
                        {
                            if (wallX < 1 || wallX >= Main.maxTilesX - 1 || wallY < 1 || wallY >= Main.maxTilesY - 1)
                                continue;
                            Tile wallTile = Main.tile[wallX, wallY];
                            if (wallTile.WallType <= WallID.None || !WallLoader.CanExplode(wallX, wallY, wallTile.WallType))
                                continue;
                            WorldGen.KillWall(wallX, wallY, false);
                            if (Main.tile[wallX, wallY].WallType == WallID.None && Main.netMode != NetmodeID.SinglePlayer)
                                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 2, wallX, wallY, 0f, 0, 0, 0);
                        }
                    }
                }
            }
        }
    }
}
