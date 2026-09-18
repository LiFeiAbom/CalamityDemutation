using CalamityDemutation.Content.Projectiles.BaseProjectiles;
using CalamityDemutation.Sounds;
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
    /// 元素王者之剑手持弹幕（移植自灾厄 PrismaticBreakerHoldout）：按住左键蓄力，蓄满后放出元素王者激光。
    /// 蓄力期间周期性抛出棱镜之波（每抛一次频率加快 4 帧，散布角随蓄力从 30° 收窄到 0°），
    /// 第 200 帧播蓄力音，第 300 帧生成棱镜魔力阵（由它再放出主伤害光束）。
    /// 阵在场期间，本体的瞄准改用 0.94 的滞后跟随，形成原版 Last Prism 那种迟滞手感。
    /// <para>
    /// 与灾厄原版的两点区别：① 不关联物品（灾厄靠 AssociatedItemID 判断玩家是否仍持有该武器），
    /// 去留只由"是否仍在按住使用键"决定；② 贴图改为**直接引用武器本体 ElementalExcalibur 的贴图**
    /// （灾厄的 BaseGunHoldoutProjectile 同样是从关联物品推导贴图路径，只是这里写死而已），
    /// 因此 <see cref="SetDefaults"/> 的 width/height 也用武器的 112×112——它决定发射口 GunTipPosition 的位置。
    /// </para>
    /// </summary>
    internal class ElementalExcaliburBreakerHoldout : BaseGunHoldoutCO
    {
        /// <summary>直接复用武器本体的贴图（灾厄也是从关联物品推导贴图路径）</summary>
        public override string Texture => "CalamityDemutation/Content/Items/Weapons/Melee/ElementalExcalibur";
        /// <summary>
        /// 本体距手臂的距离。灾厄原值是 40（其物品贴图 50 宽），本武器贴图 112 宽，
        /// 按同一比例放大到约 90——否则 112 宽的剑以贴图中心为原点、只有 40 的离手距离，
        /// 会有一半压在玩家身上，观感变成"抱着大剑"而不是"持械前指"。
        /// 注意：发射口 GunTipPosition 由 Projectile.Center 派生，故推远后激光起点仍落在剑尖上。
        /// </summary>
        public override float MaxOffsetLengthFromArm => 90f;
        /// <summary>存在帧数（存于 ai[0]）</summary>
        public ref float Timer => ref Projectile.ai[0];
        /// <summary>蓄满所需的帧数</summary>
        public const float LaserChargeTime = 300f;
        /// <summary>激光的瞄准滞后系数（越大转向越慢；原版 Last Prism 是 0.92）</summary>
        public const float LaserAimLag = 0.94f;
        /// <summary>激光伤害相对本体 originalDamage 的倍率</summary>
        public const float LaserDamageMult = 2f;
        /// <summary>激光生成后本体的继续存活帧数</summary>
        public const float LaserLifetime = 360f;
        /// <summary>距下次抛星还有多少帧</summary>
        public float StarTimer = 0f;
        /// <summary>抛星的间隔帧数（每抛一次递减 4）</summary>
        public float StarFrequency = 47f;
        /// <summary>
        /// 基础属性：在基类默认值之上把伤害类型改为近战，并把尺寸设为贴图尺寸
        /// （GunTipPosition 依赖 width，尺寸不对会让发射口偏移）
        /// </summary>
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = 112;
            Projectile.height = 112;
            Projectile.DamageType = DamageClass.Melee;
        }
        /// <summary>
        /// 手持行为：主人无法继续持握且场上无法阵时自毁；超过"蓄力 + 激光存活"总时长也自毁；
        /// 蓄力期内按 StarFrequency 抛 3 颗棱镜之波（散布角随蓄力收窄、缩放渐大，且频率每抛一次加快 4 帧）；
        /// 第 200 帧播蓄力音（音效跟随本体生命期），第 300 帧生成棱镜魔力阵。
        /// </summary>
        /// <summary>
        /// 去留判据覆写：本武器左右键共用一件物品，不能用基类默认的 CantUseHoldout（它依赖 player.channel——
        /// 左右键共用一件物品时 vanilla 的 channel 会被两键互相干扰，右键刚生出的手持弹幕会当帧被判已松开而自杀）。
        /// 改判"鼠标右键是否仍按住"：松开、死亡、被控或物品栏被锁时销毁。
        /// </summary>
        public override void KillHoldoutLogic()
        {
            if (Owner.HoldoutReleased())
                Projectile.Kill();
        }
        public override void HoldoutAI()
        {
            if (Owner.HoldoutReleased())
            {
                if (!CDUtil.AnyProjectiles(ModContent.ProjectileType<ElementalExcaliburMagicCircle>()))
                    Projectile.Kill();
            }
            // 按住不放时不再按总时长自毁：否则手持弹幕会重生、进而重复生成魔力阵与激光；
            // 只在松开后按"蓄力 + 激光寿命"的总时长收尾
            if (Owner.HoldoutReleased() && Timer > LaserChargeTime + LaserLifetime)
                Projectile.Kill();
            Timer++;
            if (Timer <= LaserChargeTime)
            {
                StarTimer++;
                if (StarTimer >= StarFrequency && Main.myPlayer == Projectile.owner)
                {
                    StarTimer = 0f;
                    SoundEngine.PlaySound(SoundID.Item43 with { Volume = 0.6f }, Projectile.Center);
                    for (int i = 0; i < 3; i++)
                    {
                        float clampedChargeTime = MathHelper.Clamp(Timer / LaserChargeTime, 0f, 1f);
                        // 散布角随蓄力从 30° 收窄到 0°
                        float starOffset = MathHelper.Lerp(MathHelper.Pi / 6f, 0f, clampedChargeTime);
                        Vector2 velocity = Vector2.Normalize(Projectile.velocity).RotatedByRandom(starOffset) * 17f;
                        Projectile star = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), GunTipPosition, velocity, ModContent.ProjectileType<ElementalExcaliburWave>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, Main.rand.Next(12));
                        star.scale = MathHelper.Lerp(0.75f, 1f, clampedChargeTime);
                    }
                    StarFrequency -= 4f;
                }
            }
            if (Timer == LaserChargeTime - 100f)
                SoundEngine.PlaySound(CalamityDemutationSounds.CrystylCharge, Projectile.Center, _ => new ProjectileAudioTracker(Projectile).IsActiveAndInGame());
            if (Timer == LaserChargeTime && Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), GunTipPosition, Projectile.velocity, ModContent.ProjectileType<ElementalExcaliburMagicCircle>(), (int)(Projectile.originalDamage * LaserDamageMult), Projectile.knockBack, Projectile.owner);
            }
        }
        /// <summary>
        /// 在基类持握逻辑之上追加：若场上已有魔力阵，则本体的瞄准改用 0.94 的滞后跟随
        /// （先记下基类算出的速度，再把速度朝"鼠标方向"按滞后系数插值），朝向随之对齐
        /// </summary>
        public override void ManageHoldout()
        {
            Vector2 storedVelocity = Projectile.velocity;
            base.ManageHoldout();
            if (Owner.ownedProjectileCounts[ModContent.ProjectileType<ElementalExcaliburMagicCircle>()] > 0)
            {
                Vector2 aimVector = (Main.MouseWorld - Owner.RotatedRelativePoint(Owner.MountedCenter, true)).SafeNormalize(Vector2.UnitY);
                aimVector = Vector2.Normalize(Vector2.Lerp(aimVector, Vector2.Normalize(storedVelocity), LaserAimLag));
                if (aimVector != storedVelocity)
                    Projectile.netUpdate = true;
                Projectile.velocity = aimVector;
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
        }
        /// <summary>
        /// 自绘：基类的方向处理之外再多转 45°（剑贴图本身朝右上），
        /// 并按 spriteDirection/重力方向做翻转（贴图是斜向的，不能沿用基类的纯水平翻转）
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float drawRotation = Projectile.rotation + (MathHelper.PiOver4 * Projectile.spriteDirection) + (Projectile.spriteDirection == -1 ? MathHelper.Pi : 0f) - (Owner.gravDir == -1 ? MathHelper.PiOver2 * Owner.direction : 0f);
            Vector2 rotationPoint = texture.Size() * 0.5f;
            SpriteEffects flipSprite = (Projectile.spriteDirection * Owner.gravDir == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), drawRotation, rotationPoint, Projectile.scale * Owner.gravDir, flipSprite);
            return false;
        }
    }
}
