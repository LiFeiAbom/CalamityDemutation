using CalamityDemutation.Content.Particles;
using CalamityDemutation.Content.Particles.Core;
using CalamityDemutation.Content.Projectiles.BaseProjectiles;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 中子长戟手持弹幕（NeutronGlaiveHeld） - 中子长戟右键蓄力的手持弹幕（移植自 CWR 的 NeutronGlaiveHeld）。
    /// 按住右键持续蓄力并朝鼠标方向喷射追踪球（NeutronsOrb），充能满后松手触发大爆点（EXNeutronExplode）；
    /// 玩家头顶绘制一条充能条（DrawBar）。CWR 原版靠灾厄的 allProjectilesHome 追踪，
    /// 本模组把追踪下沉到 NeutronsOrb 自追踪，此处只负责蓄力与喷射节奏。
    /// </summary>
    internal class NeutronGlaiveHeld : BaseHeldProjCO
    {
        /// <summary>
        /// 复用中子长戟的物品贴图（6 帧竖直排布），手持弹幕本体绘制用
        /// </summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/NeutronGlaive";
        // ── 常量 ──
        /// <summary>
        /// 充能上限（帧）：蓄力达到该值即充能满
        /// </summary>
        private const int maxatcck = 80;
        // ── 静态字段 ──
        /// <summary>
        /// 充能条前景贴图（未满档），懒加载
        /// </summary>
        private static Asset<Texture2D> bar1;
        /// <summary>
        /// 充能条前景贴图（满档），懒加载
        /// </summary>
        private static Asset<Texture2D> bar2;
        /// <summary>
        /// 充能条背景贴图（未满档），懒加载
        /// </summary>
        private static Asset<Texture2D> bar3;
        /// <summary>
        /// 充能条背景贴图（满档），懒加载
        /// </summary>
        private static Asset<Texture2D> bar4;
        // ── 实例字段 ──
        /// <summary>
        /// 是否已进入收尾喷发阶段（松手或死亡后置位，期间不再蓄力）
        /// </summary>
        private bool canatcck;
        /// <summary>
        /// 蓄力阶段开关（true 期间持续蓄力并跟随鼠标朝向）
        /// </summary>
        private bool canatcck2 = true;
        /// <summary>
        /// 充能满提示音一次性开关
        /// </summary>
        private bool canatcck3 = true;
        /// <summary>
        /// 充能条动画帧计数器
        /// </summary>
        private int uiframe;
        // ── 生命周期方法 ──
        /// <summary>
        /// 弹幕基础属性：112×112 判定框、近战伤害、隐藏默认绘制（由 PreDraw 自绘）
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 112;  // 正方形判定箱
            Projectile.friendly = true;                  // 友方弹幕
            Projectile.tileCollide = false;              // 不与物块碰撞
            Projectile.penetrate = -1;                   // -1 = 无限穿透
            Projectile.usesLocalNPCImmunity = true;      // 每个敌人独立计算免疫计时
            Projectile.localNPCHitCooldown = 4;          // 同一敌人 4 帧内只受击一次
            Projectile.DamageType = DamageClass.Melee;   // 吃近战伤害加成
            Projectile.hide = true;                      // 隐藏默认绘制，交给 PreDraw
        }
        /// <summary>
        /// 蓄力与喷射逻辑：按住右键（DownRight）蓄力，松手或玩家死亡进入收尾喷发；
        /// 充能满后松手触发大爆点，未满则朝鼠标方向喷追踪球直到蓄力耗尽
        /// </summary>
        public override void AI()
        {
            // 松手、玩家死亡/失活或已进入收尾阶段：进入收尾喷发
            if (Owner.dead || !Owner.active || canatcck || !DownRight)
            {
                canatcck = true;
                if (Projectile.ai[0] >= maxatcck)
                {
                    Projectile.Kill();   // 充能满直接销毁（OnKill 里触发大爆点）
                }
                else
                {
                    canatcck2 = false;
                    Projectile.scale = 1.25f;
                    if (++Projectile.ai[1] > 5)
                    {
                        SoundEngine.PlaySound(SoundID.Item4, Projectile.Center);
                        Vector2 pos = Projectile.Center + Projectile.velocity.UnitVector() * Main.rand.Next(-52, 112);
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), pos, Projectile.velocity.RotatedByRandom(0.2f), ModContent.ProjectileType<NeutronsOrb>(), Projectile.damage, 0);
                        for (int i = 0; i < 4; i++)
                        {
                            float rot1 = MathHelper.PiOver2 * i;
                            Vector2 vr = rot1.ToRotationVector2();
                            for (int j = 0; j < 13; j++)
                            {
                                BaseParticle spark = new DRK_Spark(pos, vr * (0.1f + j * 0.14f), false, 17, Main.rand.NextFloat(0.5f, 0.7f), Color.BlueViolet);
                                DRKLoader.AddParticle(spark);
                            }
                        }
                        Projectile.ai[1] = 0;
                    }
                    Projectile.ai[0]--;
                    if (Projectile.ai[0] <= 0)
                    {
                        Projectile.Kill();   // 蓄力耗尽销毁
                    }
                }
            }
            if (canatcck2)
            {
                Projectile.velocity = ToMouse.UnitVector() * 18;   // 蓄力期间跟随鼠标朝向
            }
            Projectile.Center = Owner.GetPlayerStabilityCenter() + Projectile.velocity.UnitVector() * 53 * Projectile.scale;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (!canatcck && Projectile.ai[0] <= maxatcck)
            {
                Projectile.ai[0]++;   // 蓄力计数递增
            }
            if (Projectile.ai[0] >= maxatcck)
            {
                if (canatcck3)
                {
                    SoundEngine.PlaySound(SoundID.Item4 with { Pitch = -0.2f }, Projectile.Center);   // 充能满提示音
                    canatcck3 = false;
                }
                Projectile.scale = 1.5f;
            }
            SetHeld();
            CDUtil.ClockFrame(ref Projectile.frame, 5, 5);
            if (canatcck2)
            {
                CDUtil.ClockFrame(ref uiframe, 5, 6);
            }
            float rot = (MathHelper.PiOver2 * SafeGravDir - Projectile.rotation) * DirSign * SafeGravDir;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rot * -DirSign);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, rot * -DirSign);
            Owner.direction = Math.Sign(Projectile.velocity.X);
        }
        /// <summary>
        /// 销毁时触发大爆点：仅蓄力阶段（canatcck2）被销毁才在朝向远方生成 EXNeutronExplode，
        /// 大爆点伤害 = 武器伤害 ×10（用户 2026-09-15 指定）
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            if (Projectile.IsOwnedByLocalPlayer() && canatcck2)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + Projectile.velocity.UnitVector() * 255, Vector2.Zero, ModContent.ProjectileType<EXNeutronExplode>(), Projectile.damage * 10, 0);
            }
        }
        /// <summary>
        /// 返回 false：禁用常规贴图绘制，由本类自定义绘制手持贴图与充能条
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            DrawBar(Owner, Projectile.ai[0], uiframe);
            Texture2D value = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(value, Projectile.Center - Main.screenPosition, CDUtil.GetRec(value, Projectile.frame, 6), Color.White, Projectile.rotation + MathHelper.PiOver4 * Owner.direction, CDUtil.GetOrig(value, 6) + new Vector2(0, 5 * Owner.direction), Projectile.scale, Owner.direction > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically, 0);
            return false;
        }
        // ── 公开方法 ──
        /// <summary>
        /// 卸载时清空懒加载的静态贴图引用（由 CalamityDemutation.Unload 调用）
        /// </summary>
        internal static void UnloadTextures()
        {
            bar1 = null;
            bar2 = null;
            bar3 = null;
            bar4 = null;
        }
        /// <summary>
        /// 在玩家头顶绘制充能条：背景 + 按充能进度裁剪的前景，满档时切换满档贴图。
        /// 中子枪的手持弹幕（NeutronGunHoldout）复用本方法，故为 internal
        /// </summary>
        internal static void DrawBar(Player Owner, float sengs, int uiframe)
        {
            if (sengs <= 0f)
            {
                return;
            }
            bar1 ??= ModContent.Request<Texture2D>(CalamityDemutationConstant.UI + "NeutronsBar");
            bar2 ??= ModContent.Request<Texture2D>(CalamityDemutationConstant.UI + "NeutronsBar2");
            bar3 ??= ModContent.Request<Texture2D>(CalamityDemutationConstant.UI + "NeutronsBarTop");
            bar4 ??= ModContent.Request<Texture2D>(CalamityDemutationConstant.UI + "NeutronsBarTop2");
            Texture2D barBG = bar3.Value;
            Texture2D barFG = bar1.Value;
            if (sengs >= maxatcck)
            {
                barBG = bar4.Value;
                barFG = bar2.Value;
            }
            float barScale = 1.2f;
            Vector2 drawPos = Owner.GetPlayerStabilityCenter() + new Vector2(0, 75) - Main.screenPosition;
            Rectangle frameCrop = new Rectangle(0, 0, (int)(sengs / maxatcck * barFG.Width), barFG.Height);
            Main.spriteBatch.Draw(barBG, drawPos, CDUtil.GetRec(barBG, uiframe, 7), Color.White, 0f, CDUtil.GetOrig(barBG, 7), barScale, 0, 0f);
            Main.spriteBatch.Draw(barFG, drawPos, frameCrop, Color.White, 0f, CDUtil.GetOrig(barFG, 1), barScale, 0, 0f);
        }
    }
}
