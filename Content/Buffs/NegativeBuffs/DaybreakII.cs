using System.Collections.Generic;
using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Graphics;
using CalamityDemutation.Graphics.Buffers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Buffs.NegativeBuffs
{
    /// <summary>
    /// 破晓II：由烈火手套EX的近战命中附加。
    /// 每秒 450 生命、防御 -30、受到伤害 +15%；视觉完整移植燃烧IV的 MiracleBlight 风格。
    /// </summary>
    internal class DaybreakII : ModBuff
    {
        public const int BurnLifeRegen = 900;
        public const float DefenseLoss = 30f;
        public const float DamageTakenMultiplier = 1.15f;
        public const int Duration = 300;
        public const string ShaderKey = "CalamityDemutation:DaybreakII";
        private const float DissolveOpacity = 0.7f;
        public static readonly Color GlowColor = new Color(255, 84, 16);
        public static Asset<Texture2D> NoiseTexture;
        private static readonly HashSet<int> excludedNPCs = new HashSet<int>();
        private static bool exclusionListBuilt;
        public override void Load()
        {
            if (Main.dedServ)
                return;
            NoiseTexture = ModContent.Request<Texture2D>("CalamityDemutation/Assets/ExtraTextures/Neurons");
            On_Main.DrawNPC += DrawNPCWithDaybreakII;
        }
        public override void Unload()
        {
            On_Main.DrawNPC -= DrawNPCWithDaybreakII;
            NoiseTexture = null;
            excludedNPCs.Clear();
            exclusionListBuilt = false;
        }
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = true;
        }
        public override void Update(NPC npc, ref int buffIndex) => SpanSparks(npc);
        public override void Update(Player player, ref int buffIndex) => SpanSparks(player);
        /// <summary>复刻 MiracleBlight 的橙红火尘与 DRK_Spark 火花层。</summary>
        public static void SpanSparks(Entity entity)
        {
            Vector2 spawn = entity.Center + new Vector2(
                Main.rand.NextFloat(-entity.width / 2f, entity.width / 2f),
                Main.rand.NextFloat(-entity.height / 2f, entity.height / 2f));
            Color sparkColor = RandomSparkColor();
            if (Main.rand.NextBool(4))
            {
                Dust dust = Dust.NewDustPerfect(spawn, DustID.OrangeTorch, RandomSpawnVelocity());
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.7f, 0.85f) + 0.0000007f * entity.width * entity.height;
                dust.color = sparkColor;
            }
            if (Main.rand.NextBool(3))
            {
                Vector2 velOffset = RandomSpawnVelocity() * (Main.rand.NextFloat(5f, 9f) + 0.0002f * entity.width * entity.height);
                DRKLoader.AddParticle(new DRK_Spark(entity.Center + entity.velocity * 3f + velOffset * 1.5f
                    , -velOffset * 0.25f, false, 4, 0.6f, sparkColor));
            }
        }
        private static Color RandomSparkColor() => Main.rand.Next(4) switch
        {
            0 => new Color(255, 46, 0),
            1 => new Color(255, 96, 10),
            2 => new Color(255, 140, 28),
            _ => new Color(255, 190, 62),
        };
        private static Vector2 RandomSpawnVelocity() =>
            Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.4f, 3f);
        private static void DrawNPCWithDaybreakII(On_Main.orig_DrawNPC orig, Main self, int npcIndex, bool behindTiles)
        {
            NPC npc = Main.npc[npcIndex];
            if (!npc.HasBuff(ModContent.BuffType<DaybreakII>()) || IsExcludedFromDissolve(npc))
            {
                orig(self, npcIndex, behindTiles);
                return;
            }
            using (Main.spriteBatch.Scope())
            {
                using RenderTargetLease lease = ScreenspaceTargetPool.Shared.Rent(Main.instance.GraphicsDevice);
                using (lease.Scope(clearColor: Color.Transparent))
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp
                        , DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
                    orig(self, npcIndex, behindTiles);
                    Main.spriteBatch.End();
                }
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp
                    , DepthStencilState.None, RasterizerState.CullCounterClockwise);
                MiscShaderData dissolveShader = GameShaders.Misc[ShaderKey];
                dissolveShader.UseImage1(NoiseTexture);
                dissolveShader.UseColor(GlowColor);
                dissolveShader.UseOpacity(DissolveOpacity);
                dissolveShader.Apply();
                Main.spriteBatch.Draw(lease.Target, Vector2.Zero, Color.White);
                Main.spriteBatch.End();
            }
        }
        private static bool IsExcludedFromDissolve(NPC npc)
        {
            if (!exclusionListBuilt)
                BuildExclusionList();
            return excludedNPCs.Contains(npc.type);
        }
        private static void BuildExclusionList()
        {
            exclusionListBuilt = true;
            excludedNPCs.Add(NPCID.MoonLordCore);
            excludedNPCs.Add(NPCID.MoonLordHand);
            excludedNPCs.Add(NPCID.MoonLordHead);
            if (!ModLoader.TryGetMod("CalamityMod", out Mod calamity))
                return;
            string[] aresParts = { "AresBody", "AresGaussNuke", "AresLaserCannon", "AresPlasmaFlamethrower", "AresTeslaCannon" };
            foreach (string name in aresParts)
            {
                if (calamity.TryFind<ModNPC>(name, out ModNPC aresPart))
                    excludedNPCs.Add(aresPart.Type);
            }
        }
    }
}
