using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Melee
{
    /// <summary>
    /// 元素圣剑光束（ElementalExcaliburBeam） - 元素圣剑（ElementalExcalibur）左键挥砍发射的彩虹光束，
    /// 移植自灾厄现代版 2.0.3.9 的 <c>GayBeam</c>（该版的虹彩圣剑 IridescentExcalibur 的普通攻击弹幕）。
    /// <para>
    /// ai[0] 是颜色编号（0~11，与武器 MeleeEffects 里的调色板同序）。除换色外，每种颜色还各带一套独有的飞行行为：
    /// 0 红-普通；1 橙-飞到中段回旋追玩家；2 黄-到点分裂；3 淡绿-离主人过远就贴回去；4 绿-追敌；
    /// 5 青绿-提速并穿墙；6 青-只换色，消亡时炸出多道青绿光束；7 淡蓝-命中减速；8 蓝-撞墙/命中反弹；
    /// 9 紫-忽快忽慢；10 品红-起步极慢后持续加速；11 桃红-边飞边分裂出品红。
    /// </para>
    /// <para>
    /// 与源码的差异：源码命中敌人 / 玩家都只挂一个 MiracleBlight（600 帧），本工程改为软依赖双版本查找并分别补挂——
    /// 现代版 <c>CalamityMod</c>（VulnerabilityHex / MiracleBlight / Dragonfire）、经典版
    /// <c>CalamityModClassicPreTrailer</c>（DemonFlames / GodSlayerInferno / HolyLight），各 600 帧；
    /// 其余行为（含源码本就有的过 <c>OnHitPlayer</c> 与 <c>OnTileCollide</c>）与源码一致。
    /// </para>
    /// </summary>
    internal class ElementalExcaliburBeam:ModProjectile
    {
        /// <summary>半透明度基准值：既作为各色 <c>Color</c> 的 alpha 通道（50 = 半透），也作为粉尘的 alpha 参数</summary>
        private readonly int alpha = 50;
        /// <summary>是否已播过生成音：每端各自播一次（该字段不参与同步）</summary>
        private bool playedSound = false;
        /// <summary>
        /// 静态属性：预留 4 格残影缓存（配合 PreDraw 的残影绘制），TrailingMode=0 表示按位置记录
        /// </summary>
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }
        /// <summary>
        /// 基础属性：20x20 碰撞箱、友方近战、无视水、可穿透 3 个敌人、存活 1200 帧、
        /// 每帧额外更新 3 次（extraUpdates=3，故实际速度快且判定密）、每敌 8 帧可再命中一次
        /// </summary>
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;   // 同一个敌人每 8 帧可再命中一次
            Projectile.timeLeft = 1200;
            Projectile.extraUpdates = 3;
        }
        /// <summary>
        /// 额外同步 localAI[0] 与 localAI[1]：两者分别决定"收细/张开"呼吸状态与各色的分裂计时器，
        /// 不同步的话各端看到的缩放与分裂时机会各不相同
        /// </summary>
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
            writer.Write(Projectile.localAI[1]);
        }
        /// <summary>与 <see cref="SendExtraAI"/> 对应的读取端</summary>
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
            Projectile.localAI[1] = reader.ReadSingle();
        }
        /// <summary>
        /// AI：朝速度方向旋转并加 45°（贴图是斜着的十字光刃）；首帧播生成音；
        /// 用 localAI[0] 在"0=收细变淡 / 1=张开变实"两个状态间来回切换，形成喘息般的脉动；
        /// 随后按 ai[0] 的颜色编号执行各自的飞行行为，最后撒彩虹微尘
        /// </summary>
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(45f);   // 45° 是贴图本身的倾角
            if (!playedSound)
            {
                SoundEngine.PlaySound(SoundID.Item60, Projectile.Center);
                playedSound = true;
            }
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.scale -= 0.01f;   // 收细阶段：每帧缩 0.01、淡 15
                Projectile.alpha += 15;
                if (Projectile.alpha >= 250)
                {
                    Projectile.alpha = 255;
                    Projectile.localAI[0] = 1f;   // 缩到极淡后翻转到张开阶段
                }
            }
            else if (Projectile.localAI[0] == 1f)
            {
                Projectile.scale += 0.01f;   // 张开阶段：每帧涨 0.01、实 15
                Projectile.alpha -= 15;
                if (Projectile.alpha <= 0)
                {
                    Projectile.alpha = 0;
                    Projectile.localAI[0] = 0f;   // 恢复到全实后翻回收细阶段，如此循环
                }
            }
            Color color = new Color(255, 0, 0, alpha);
            switch ((int)Projectile.ai[0])   // ai[0] = 颜色编号，见类头说明；各分支未写明的颜色行为就是"只换色"
            {
                case 0: // 红：普通光束，无附加行为
                    break;
                case 1: // 橙：飞到中段回旋追玩家
                    color = new Color(255, 128, 0, alpha);
                    int p = Player.FindClosest(Projectile.Center, 1, 1);   // 最近的玩家（含主人自己），做出"回旋镖"手感
                    Projectile.ai[1] += 1f;   // ai[1] 兼作飞行计时器
                    if (Projectile.ai[1] < 220f && Projectile.ai[1] > 60f)
                    {
                        float homeSpeed = Projectile.velocity.Length();   // 先记下当前速度大小，回旋后原样恢复（只改方向不改快慢）
                        Vector2 vecToPlayer = Main.player[p].Center - Projectile.Center;
                        vecToPlayer.Normalize();
                        vecToPlayer *= homeSpeed;
                        Projectile.velocity = (Projectile.velocity * 24f + vecToPlayer) / 25f;   // 24:1 的滞后插值，转向很柔
                        Projectile.velocity.Normalize();
                        Projectile.velocity *= homeSpeed;
                    }
                    if (Projectile.velocity.Length() < 24f)
                    {
                        Projectile.velocity *= 1.02f;   // 速度不足 24 时每帧慢加速，避免回旋途中失速停死
                    }
                    break;
                case 2: // 黄：到点分裂
                    color = new Color(255, 255, 0, alpha);
                    Projectile.localAI[1] += 1f;
                    if (Projectile.localAI[1] >= 180f)
                    {
                        Projectile.localAI[1] = 0f;
                        int numProj = 2;
                        float rotation = MathHelper.ToRadians(10);
                        if (Projectile.owner == Main.myPlayer)   // 仅主人端生成，避免各端各复制一份
                        {
                            for (int i = 0; i < numProj + 1; i++)
                            {
                                // 注意分母是 numProj-1=1，故 t 取 0/1/2，三颗的偏转角依次为 -10°、+10°、+30°（源码即如此）
                                Vector2 perturbedSpeed = Projectile.velocity.RotatedBy(MathHelper.Lerp(-rotation, rotation, i / (numProj - 1)));
                                // 生成 3 颗红色（ai[0]=0）光束：半速、半伤、半击退
                                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, perturbedSpeed * 0.5f, ModContent.ProjectileType<ElementalExcaliburBeam>(), (int)(Projectile.damage * 0.5), Projectile.knockBack * 0.5f, Projectile.owner, 0f, 0f);
                            }
                        }
                        Projectile.Kill();
                    }
                    break;
                case 3: // 淡绿：离主人过远就贴回去
                    color = new Color(128, 255, 0, alpha);
                    float inertia = 75f;
                    float homingSpeed = 7.5f;
                    float minDist = 80f;
                    if (Main.player[Projectile.owner].active && !Main.player[Projectile.owner].dead)
                    {
                        if (Projectile.Distance(Main.player[Projectile.owner].Center) > minDist)   // 只在该距离外才拉回，贴身时不抖
                        {
                            Vector2 moveDirection = Projectile.SafeDirectionTo(Main.player[Projectile.owner].Center, Vector2.UnitY);
                            Projectile.velocity = (Projectile.velocity * (inertia - 1f) + moveDirection * homingSpeed) / inertia;   // 惯性 75 的滞后插值
                        }
                    }
                    else
                    {
                        if (Projectile.timeLeft > 30)
                        {
                            Projectile.timeLeft = 30;   // 主人已死/不在场就别再飘了，压到 30 帧尽快消失
                        }
                    }
                    break;
                case 4: // 绿：追敌
                    color = new Color(0, 255, 0, alpha);
                    // 追踪 300 像素内的敌人：速度 6、惯性 20；第二参传 !tileCollide，即"本来就不撞墙"的弹幕才会无视地形直追
                    CDUtil.HomeInOnNPC(Projectile, !Projectile.tileCollide, 300f, 6f, 20f);
                    break;
                case 5: // 青绿：提速并穿墙
                    color = new Color(0, 255, 128, alpha);
                    Projectile.tileCollide = false;
                    if (Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y) < 28f)
                    {
                        Projectile.velocity *= 1.035f;   // 速度分量和不足 28 时每帧 +3.5%，到 28 后不再加速
                    }
                    break;
                case 6: // 青：只换色，实际效果写在 OnKill（消亡时炸出多道青绿光束）
                    color = new Color(0, 255, 255, alpha);
                    break;
                case 7: // 淡蓝：命中减速，效果写在 OnHitNPC / OnHitPlayer
                    color = new Color(0, 128, 255, alpha);
                    break;
                case 8: // 蓝：撞墙/命中反弹，效果写在 OnTileCollide / OnHitNPC / OnHitPlayer
                    color = new Color(0, 0, 255, alpha);
                    break;
                case 9: // 紫：忽快忽慢
                    color = new Color(128, 0, 255, alpha);
                    Projectile.localAI[1] += 1f;
                    if (Projectile.localAI[1] <= 40f)
                    {
                        Projectile.velocity *= 0.95f;   // 第 1~40 帧每帧 -5%，持续减速
                    }
                    else if (Projectile.localAI[1] > 40f && Projectile.localAI[1] <= 79f)
                    {
                        Projectile.velocity *= 1.05f;   // 第 41~79 帧每帧 +5%，加回来
                    }
                    else if (Projectile.localAI[1] == 80f)
                    {
                        Projectile.localAI[1] = 0f;   // 第 80 帧归零重新计时；逐帧 +1 是精确整数，故可用 == 比较
                    }
                    break;
                case 10: // 品红：起步极慢然后越来越快
                    color = new Color(255, 0, 255, alpha);
                    if (Projectile.localAI[1] == 0f)   // 仅第一帧（localAI[1]==0 兼作"未初始化"标记）
                    {
                        Projectile.velocity *= 0.1f;   // 起步砍到 1/10 速度
                        Projectile.localAI[1] += 1f;
                    }
                    Projectile.velocity *= 1.01f;   // 此后每帧 +1%，越飞越快
                    break;
                case 11: // 桃红：边飞边分裂出品红
                    color = new Color(255, 0, 128, alpha);
                    if (Projectile.localAI[1] == 0f)
                    {
                        Projectile.velocity *= 0.33f;   // 第一帧减速到 1/3（localAI[1]==0 兼作"未初始化"标记）
                    }
                    Projectile.localAI[1] += 1f;
                    if (Projectile.localAI[1] >= 181f)   // 每 180 帧撒一颗
                    {
                        Projectile.localAI[1] = 1f;   // 复位到 1（不是 0），下一帧 +1 后从 2 继续计，周期正好 180 帧
                        if (Main.myPlayer == Projectile.owner)   // 仅主人端生成
                            // 在前方一颗处留下一枚品红（ai[0]=10）光束：半伤、半击退，自身继续飞
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + Projectile.velocity, Projectile.velocity, ModContent.ProjectileType<ElementalExcaliburBeam>(), Projectile.damage / 2, Projectile.knockBack * 0.5f, Projectile.owner, 10f, 0f);
                    }
                    break;
                default:
                    break;
            }
            if (Main.rand.NextBool(2))
            {
                // 每帧 1/2 概率撒一粒彩虹微尘：alpha 传 50（半透明）、缩放 1.5、颜色取当前 ai[0] 对应的色
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.RainbowMk2, 0f, 0f, alpha, color, 1.5f);
                Main.dust[dust].noGravity = true;
            }
        }
        /// <summary>
        /// 命中敌人：淡蓝（7）把速度砍到 25%、蓝色（8）把速度取反；再按软依赖给目标挂两版灾厄的持续伤害 debuff
        /// （各 600 帧 = 10 秒）。两版都装了也只各挂一次，因为用的是 <c>TryGetMod</c> + <c>TryFind</c>
        /// </summary>
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.ai[0] == 7f)
            {
                Projectile.velocity *= 0.25f;   // 淡蓝：急减速
            }
            else if (Projectile.ai[0] == 8f)
            {
                Projectile.velocity *= -1f;   // 蓝：反向弹开
            }
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))   // 现代版灾厄
            {
                if (calamity.TryFind<ModBuff>("VulnerabilityHex", out ModBuff vulnerabilityHex))
                    target.AddBuff(vulnerabilityHex.Type, 600);
                if (calamity.TryFind<ModBuff>("MiracleBlight", out ModBuff miracleBlight))
                    target.AddBuff(miracleBlight.Type, 600);
                if (calamity.TryFind<ModBuff>("Dragonfire", out ModBuff dragonfire))
                    target.AddBuff(dragonfire.Type, 600);
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))   // 经典版灾厄
            {
                if (calamity1.TryFind<ModBuff>("DemonFlames", out ModBuff demonFlames))
                    target.AddBuff(demonFlames.Type, 600);
                if (calamity1.TryFind<ModBuff>("GodSlayerInferno", out ModBuff godSlayerInferno))
                    target.AddBuff(godSlayerInferno.Type, 600);
                if (calamity1.TryFind<ModBuff>("HolyLight", out ModBuff holyLight))
                    target.AddBuff(holyLight.Type, 600);
            }
        }
        /// <summary>
        /// 命中玩家（PvP）：行为与 <see cref="OnHitNPC"/> 完全一致（淡蓝减速 / 蓝色反弹 + 两版灾厄 debuff 各 600 帧）
        /// </summary>
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Projectile.ai[0] == 7f)
            {
                Projectile.velocity *= 0.25f;   // 淡蓝：急减速
            }
            else if (Projectile.ai[0] == 8f)
            {
                Projectile.velocity *= -1f;   // 蓝：反向弹开
            }
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                if (calamity.TryFind<ModBuff>("VulnerabilityHex", out ModBuff vulnerabilityHex))
                    target.AddBuff(vulnerabilityHex.Type, 600);
                if (calamity.TryFind<ModBuff>("MiracleBlight", out ModBuff miracleBlight))
                    target.AddBuff(miracleBlight.Type, 600);
                if (calamity.TryFind<ModBuff>("Dragonfire", out ModBuff dragonfire))
                    target.AddBuff(dragonfire.Type, 600);
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                if (calamity1.TryFind<ModBuff>("DemonFlames", out ModBuff demonFlames))
                    target.AddBuff(demonFlames.Type, 600);
                if (calamity1.TryFind<ModBuff>("GodSlayerInferno", out ModBuff godSlayerInferno))
                    target.AddBuff(godSlayerInferno.Type, 600);
                if (calamity1.TryFind<ModBuff>("HolyLight", out ModBuff holyLight))
                    target.AddBuff(holyLight.Type, 600);
            }
        }
        /// <summary>
        /// 撞到方块：仅蓝色（8）弹反——把撞到的那一轴速度取反并返回 false 让它继续飞；其余颜色返回 true 正常炸掉
        /// </summary>
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.ai[0] == 8f)
            {
                if (Projectile.velocity.X != oldVelocity.X)   // X 轴被挡住才翻转 X，避免把没撞的那轴也翻掉
                {
                    Projectile.velocity.X = -oldVelocity.X;
                }
                if (Projectile.velocity.Y != oldVelocity.Y)
                {
                    Projectile.velocity.Y = -oldVelocity.Y;
                }
                return false;
            }
            return true;
        }
        /// <summary>
        /// 返回当前 ai[0] 对应的彩虹色（含半透明 alpha=50），供本体与残影着色
        /// </summary>
        public override Color? GetAlpha(Color lightColor)
        {
            Color color = new(255, 0, 0, alpha);
            switch ((int)Projectile.ai[0])
            {
                case 0: // 红
                    break;
                case 1: // 橙
                    color = new Color(255, 128, 0, alpha);
                    break;
                case 2: // 黄
                    color = new Color(255, 255, 0, alpha);
                    break;
                case 3: // 淡绿
                    color = new Color(128, 255, 0, alpha);
                    break;
                case 4: // 绿
                    color = new Color(0, 255, 0, alpha);
                    break;
                case 5: // 青绿
                    color = new Color(0, 255, 128, alpha);
                    break;
                case 6: // 青
                    color = new Color(0, 255, 255, alpha);
                    break;
                case 7: // 淡蓝
                    color = new Color(0, 128, 255, alpha);
                    break;
                case 8: // 蓝
                    color = new Color(0, 0, 255, alpha);
                    break;
                case 9: // 紫
                    color = new Color(128, 0, 255, alpha);
                    break;
                case 10: // 品红
                    color = new Color(255, 0, 255, alpha);
                    break;
                case 11: // 桃红
                    color = new Color(255, 0, 128, alpha);
                    break;
                default:
                    break;
            }
            return color;
        }
        /// <summary>
        /// 自绘：生成后前 5 帧（timeLeft&gt;1195）不画——那时残影缓存还没填满，画出来会拖一条错误的线；
        /// 之后只用残影绘制，本体不单独画
        /// </summary>
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.timeLeft > 1195)
                return false;
            CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
            return false;
        }
        /// <summary>
        /// 消亡：播爆炸音，把碰撞箱外扩 64 并改成无限穿透 + 每敌 10 帧冷却后立刻再结算一次伤害（"终爆"）；
        /// 青（6）时额外从屏幕两侧横向生成 3 道青绿光束；最后按当前颜色撒两档粉尘
        /// </summary>
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
            Projectile.ExpandHitboxBy(64);   // 向外扩 64，让终爆能扫到周围所有敌人
            Projectile.maxPenetrate = Projectile.penetrate = -1;   // 无限穿透
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.Damage();   // 用扩大的判定范围再打一次
            Color color = new Color(255, 0, 0, alpha);
            switch ((int)Projectile.ai[0])
            {
                case 0: // 红
                    break;
                case 1: // 橙
                    color = new Color(255, 128, 0, alpha);
                    break;
                case 2: // 黄
                    color = new Color(255, 255, 0, alpha);
                    break;
                case 3: // 淡绿
                    color = new Color(128, 255, 0, alpha);
                    break;
                case 4: // 绿
                    color = new Color(0, 255, 0, alpha);
                    break;
                case 5: // 青绿
                    color = new Color(0, 255, 128, alpha);
                    break;
                case 6: // 青：消亡特效——横向炸出 3 道青绿光束
                    color = new Color(0, 255, 255, alpha);
                    for (int x = 0; x < 3; x++)
                    {
                        bool fromRight = x == 1;   // 第 1 道固定从右侧来
                        if (x == 2)
                            fromRight = Main.rand.NextBool(2);   // 第 3 道随机左右
                        if (Projectile.owner == Main.myPlayer)   // 仅主人端生成
                        {
                            var source = Projectile.GetSource_FromThis();
                            // ProjectileBarrage(源, 起点, 目标, 从右来, x偏移500~500, y偏移0~500, 速度5, ...)：
                            // 在远处生成弹幕，再把返回实例的 ai[0] 手动改成 5（青绿），伤害 / 击退只有 20%
                            CDUtil.ProjectileBarrage(source, Projectile.Center, Projectile.Center, fromRight, 500f, 500f, 0f, 500f, 5f, ModContent.ProjectileType<ElementalExcaliburBeam>(), (int)(Projectile.damage * 0.2), Projectile.knockBack * 0.2f, Projectile.owner, false, 0f).ai[0] = 5f;
                        }
                    }
                    break;
                case 7: // 淡蓝
                    color = new Color(0, 128, 255, alpha);
                    break;
                case 8: // 蓝
                    color = new Color(0, 0, 255, alpha);
                    break;
                case 9: // 紫
                    color = new Color(128, 0, 255, alpha);
                    break;
                case 10: // 品红
                    color = new Color(255, 0, 255, alpha);
                    break;
                case 11: // 桃红
                    color = new Color(255, 0, 128, alpha);
                    break;
                default:
                    break;
            }
            for (int d = 0; d < 3; d++)   // 先补 3 粒小尘
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.RainbowMk2, 0f, 0f, alpha, color, 1.5f);
                Main.dust[dust].noGravity = true;
            }
            for (int d = 0; d < 30; d++)   // 再撒 30 组爆发尘：一大二小，带初速
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.RainbowMk2, 0f, 0f, alpha, color, 2.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 3f;
                dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.RainbowMk2, 0f, 0f, alpha, color, 1.5f);
                Main.dust[dust].velocity *= 2f;
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
