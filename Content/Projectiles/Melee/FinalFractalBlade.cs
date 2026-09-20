using CalamityDemutation.Content.Items.Weapons.Melee;
using CalamityDemutation.Players;
using CalamityDemutation.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 最终分形剑影（FinalFractalBlade，移植自 CalamityEntropy）：从屏幕外朝光标飞去的随机剑影，
    /// 每一把的贴图都从 <see cref="swords"/> 里随机抽（分形系列十把 + <see cref="Voidshade"/> + 原版若干名剑，
    /// 其中破碎剑柄出现两次）。
    /// 主打法（<c>ai[0] == 0</c>）不出手干预，只按速度方向出图、沿途按 oldPos 重绘一圈残影。
    /// <para>
    /// <b>ai[0] == 1（绕玩家环形排布）</b>：半径（<c>localAI[0]</c>）缓慢逼近 <c>ai[1]</c>（360 / 720 / 1080），
    /// 角度按 <c>ai[2]</c> 加全局帧数自转（<c>ai[1] == 720</c> 的那一圈反向转），玩家一松开使用键
    /// （<c>itemTime == 0</c>）就判定半径够不够：够（&gt; 0.9）则转为自由飞行并朝光标窜出，不够则当场自毁。
    /// CE 里这一支唯一的生成者是 <c>FinalFracRightClick</c>，而那个类在全树没有任何生成点（本模组不移植它），
    /// 所以这支在本模组同样跑不到 —— 但它属于这个类的完整行为（不是只写不读的字段），
    /// 按移植口径照搬保留，因此代码里仍按 CE 读跨端鼠标坐标。
    /// </para>
    /// <para>
    /// 与 CE 原版的差异：① 跨端鼠标坐标改走本模组的 <see cref="CalamityDemutationPlayer.GetMouseWorld"/>
    /// （CE 读 <c>Entropy().MouseWorld</c> 且没判归属，直读 <c>Main.MouseWorld</c> 在别的客户端上会指错方向）；
    /// ② 删掉从未被读取的 <c>init</c> / <c>counter</c> / <c>rotSpeed</c> / <c>pg</c> 四个字段；
    /// ③ <c>CEUtils</c> 工具一律内联（<c>GetOwner</c>→<c>Main.player[owner]</c>、<c>GetTexture</c>→TextureAssets）；
    /// ④ CE 的 <c>UseBlendState</c> / <c>ExitShaderRegion</c> 本机 tML 没有，改用 End + 立即模式
    /// Begin(Immediate, Additive, Main.DefaultSamplerState, CullNone)，画完恢复默认批次；⑤ 音高按既有口径取 CE 值减 1。
    /// </para>
    /// <para>
    /// 池子里的 <see cref="Voidshade"/>（虚影薄锋）是 CE 自研武器，本模组已一并移植，
    /// 于是 25 项与 CE 原池完全一致。
    /// </para>
    /// </summary>
    internal class FinalFractalBlade:ModProjectile
    {
        /// <summary>剑影池：进游戏后按需填一次，之后只读</summary>
        public static List<int> swords;
        /// <summary>本把剑影抽中的池内下标，首次绘制时随机定下，此后固定</summary>
        public int texType = -1;
        /// <summary>直接复用本体贴图的发光版（CE 的 <c>getTextureGlow()</c>）</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/FinalFractalGlow";
        public override void Load()
        {
            swords = new List<int>();
        }
        public override void Unload()
        {
            swords = null;
        }
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;         // 同时记录 oldPos 与 oldRot，供拖尾重绘
            ProjectileID.Sets.TrailCacheLength[Type] = 24;
        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.light = 1f;
            Projectile.penetrate = -1;                   // 无限穿透
            Projectile.timeLeft = 80 * 8;
            Projectile.usesLocalNPCImmunity = true;      // 每个敌人独立计算无敌帧
            Projectile.localNPCHitCooldown = -1;         // 同一把剑影对同一敌人只结算一次
            Projectile.MaxUpdates = 8;
            Projectile.tileCollide = false;
            Projectile.scale = 2;
        }
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            owner.GetModPlayer<CalamityDemutationPlayer>().MouseWorldListener = true;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.ai[0] == 1)
            {
                Projectile.timeLeft = 200 * 8;
                // 半径朝 ai[1]（360/720/1080）缓慢逼近，角度按 ai[2] 加全局帧数一起转
                Projectile.localAI[0] += (Projectile.ai[1] - Projectile.localAI[0]) * 0.001f;
                Projectile.ai[2] += 0.0001f;
                Projectile.Center = owner.MountedCenter + new Vector2(Projectile.localAI[0], 0)
                    .RotatedBy(Projectile.ai[2] + Main.GameUpdateCount * 0.06f * (Projectile.ai[1] == 720 ? -1 : 1));
                if (owner.itemTime == 0)
                {
                    // 手一松就收：半径够大就转为自由飞行（朝光标窜出），否则当场消散
                    if (Projectile.localAI[0] > 0.9f)
                    {
                        Projectile.ai[0] = 0;
                        Projectile.velocity = (owner.GetModPlayer<CalamityDemutationPlayer>().GetMouseWorld() - Projectile.Center) * 0.02f;
                    }
                    else
                    {
                        Projectile.Kill();
                    }
                }
                Projectile.rotation = (Projectile.Center - owner.Center)
                    .RotatedBy(MathHelper.PiOver2 * (Projectile.ai[1] == 720 ? -1 : 1)).ToRotation();
            }
        }
        /// <summary>命中音（CE 的 runesonghit，同一把剑影每次命中都播）</summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(CalamityDemutationSounds.RuneSongHit with { Pitch = Main.rand.NextFloat(0.6f, 1.4f) - 1f }, target.Center);
        }
        /// <summary>
        /// 自绘：首帧从池里抽一把剑定下贴图，然后在加法混合下按 24 段 oldPos 由远及近重绘残影
        /// （越靠后的残影越淡），最后恢复默认批次、把当前这一把实心画在 <c>Projectile.Center</c>。
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            if (texType == -1)
            {
                texType = Main.rand.Next(swords.Count);
            }
            if (swords.Count == 0)
            {
                // 池子在这里按需填一次：内容注册早已完成，此刻取物品类型是安全的（CE 同为延迟填池）
                swords = new List<int>(){
                    ModContent.ItemType<FinalFractal>(),
                    ModContent.ItemType<SpiritFractal>(),
                    ModContent.ItemType<StarlitFractal>(),
                    ModContent.ItemType<VoidFractal>(),
                    ModContent.ItemType<AbyssFractal>(),
                    ModContent.ItemType<ShatteredFractal>(),
                    ModContent.ItemType<BrilliantFractal>(),
                    ModContent.ItemType<WelkinFractal>(),
                    ModContent.ItemType<ElementalFractal>(),
                    ModContent.ItemType<BrokenHilt>(),
                    ItemID.StarWrath,
                    ItemID.Meowmere,
                    ItemID.InfluxWaver,
                    ModContent.ItemType<BrokenHilt>(),
                    ModContent.ItemType<Voidshade>(),
                    ItemID.Zenith,
                    ItemID.TerraBlade,
                    ItemID.BeamSword,
                    ItemID.EnchantedSword,
                    ItemID.Starfury,
                    ItemID.TrueExcalibur,
                    ItemID.TrueNightsEdge,
                    ItemID.BladeofGrass,
                    ItemID.IceBlade,
                    3063
                };
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Type]; i++)
            {
                float prog = (float)i / ProjectileID.Sets.TrailCacheLength[Type];
                Color clr = Color.White * 0.36f * (1 - prog);
                Draw(Projectile.oldPos[i] + new Vector2(Projectile.width, Projectile.height) * 0.5f, clr, Projectile.oldRot[i], (int)Projectile.ai[1]);
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Draw(Projectile.Center, Color.White * 0.8f, Projectile.rotation, (int)Projectile.ai[1]);
            return false;
        }
        /// <summary>画一把剑影：<paramref name="dir"/> 为正时贴图左下角为原点、为负时用水平翻转 + 135° 朝向（CE 原样）</summary>
        public void Draw(Vector2 pos, Color lightColor, float rotation, int dir)
        {
            SpriteEffects effect = dir > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float rot = dir > 0 ? rotation + MathHelper.PiOver4 : rotation + MathHelper.Pi * 0.75f;
            // 池里的 3063 是 CE 原样写的裸物品 ID（它不插 <ItemID> 常量），照抄即可，主实例能正常取到贴图
            Main.instance.LoadItem(swords[texType]);
            Texture2D tex = TextureAssets.Item[swords[texType]].Value;
            Main.EntitySpriteDraw(tex, pos - Main.screenPosition, null, lightColor, rot, tex.Size() * 0.5f, Projectile.scale, effect);
        }
    }
}
