using System;
using CalamityDemutation.Content.Projectiles.Typeless;
using CalamityDemutation.Enums;
using CalamityDemutation.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Players
{
    /// <summary>
    /// 盾牌冲刺系统（ShieldSlam，移植自灾厄 2.0.3.9 的 CalPlayer/Dashes 下四个 CollisionType=ShieldSlam 的冲刺类）。
    /// 与灾厄实现的三点区别：
    /// 1) 不依赖灾厄的 dash 框架与反射注册——状态、位移、命中、阻拦原版冲刺全由本模组结算；
    /// 2) 冷却用自带计数器（收尾时置 30 帧）——原版 Player.dashDelay 会被 DashMovement 每帧清零（dashType 恒 0），不能当冷却用；
    /// 3) 灾厄扩展方法一律内联：ApplyArmorAccDamageBonusesTo 在本工程恒等（没有 Old Fashioned）故省略，
    ///    GetBestClassDamage 内联为「通用加成 + 近战/远程/魔法/召唤×0.75 取最高」（本工程没有盗贼职业）。
    /// 冲刺为灾厄的非全向型：纯水平、保留垂直速度，靠左右方向键双击触发（15 帧双击窗口），不使用快捷键。
    /// 入口是公开字段 <see cref="shieldSlamDash"/>（DashID 的等价物）——饰品/套装在 UpdateAccessory 里赋值即可，本文件不含任何饰品。
    /// 联机：本机玩家的冲刺由自己结算，另用 MsgShieldSlamDash / MsgShieldSlamDashHit 两条消息
    /// （见主类 CalamityDemutation.HandlePacket）把"开始/结束"与"撞击"广播给其他客户端，
    /// 让别人的屏幕上也看得到拖尾尘与撞击粒子。位移与原版一样由玩家网络同步负责，各客户端不模拟别人的物理。
    /// </summary>
    internal partial class CalamityDemutationPlayer : ModPlayer
    {
        // ── 常量 ──
        /// <summary>水平速度高于过程速度上限时的每帧衰减系数（灾厄 dashSpeedDecelerationFactor 的默认值）</summary>
        private const float ShieldSpeedDeceleration = 0.985f;
        /// <summary>速度落到跑步区间后的每帧衰减系数（灾厄 runSpeedDecelerationFactor 的默认值）</summary>
        private const float ShieldRunSpeedDeceleration = 0.94f;
        /// <summary>过程速度上限的默认值（灾厄默认 dashSpeed = 12f）：阿斯加德之英勇不覆写，即用此值</summary>
        private const float DefaultMidDashSpeed = 12f;
        /// <summary>冲刺收尾后的冷却帧数，即灾厄的 UniversalShieldSlamCooldown = 30 帧（用自带计数器承载，不写原版 dashDelay）</summary>
        private const int ShieldSlamCooldown = 30;
        /// <summary>双击判定窗口帧数，对应灾厄 HandleHorizontalDash 里的 dashTimeMod = ±15</summary>
        private const int DoubleTapWindow = 15;
        /// <summary>命中后玩家获得的碰撞免疫帧数（四个冲刺的 ShieldSlamIFrames 均为 12）</summary>
        private const int ShieldHitImmunityFrames = 12;
        /// <summary>命中后敌人冲刺免疫的下限帧数（灾厄 ModDashMovement 里强制补到 12 帧）</summary>
        private const int MinShieldNPCImmunityFrames = 12;
        /// <summary>远端玩家冲刺表现的兜底帧数上限：结束包丢失或发送者中途掉线时，尘不会一直喷下去</summary>
        private const int RemoteDashMaxFrames = 120;
        // ── 公开字段 ──
        /// <summary>
        /// 当前生效的盾牌冲刺，由提供冲刺的饰品/套装每帧设置（不设置即 None）。
        /// 与其它饰品标记一致：由 ResetEffects 每帧清零、UpdateDead 死亡时清零。
        /// </summary>
        public ShieldSlamDash shieldSlamDash;
        /// <summary>
        /// 屏幕震动强度（像素）。本工程原先没有该体系，为移植两个带震屏的冲刺而补，可被任意效果复用：
        /// 置一个正值即可，ModifyScreenPosition 每帧施加随机圆内偏移并线性衰减。
        /// </summary>
        public float GeneralScreenShakePower;
        // ── 私有字段 ──
        /// <summary>冲刺已进行的帧数；0 表示当前没有在冲刺（用自带计时器而非 Player.dashDelay，免受原版时序干扰）</summary>
        private int shieldSlamDashElapsed;
        /// <summary>冲刺收尾后的冷却剩余帧数；大于 0 时拒绝再次起手（原版 dashDelay 会被 DashMovement 清零，故用自带计数器）</summary>
        private int shieldSlamDashCooldown;
        /// <summary>双击窗口：正数=刚敲过右键、负数=刚敲过左键，每帧朝 0 靠拢（对应灾厄的 dashTimeMod）</summary>
        private int shieldSlamDashTimeMod;
        /// <summary>本次冲刺是否已触发过震屏（震屏只在每次冲刺的首个命中触发一次）</summary>
        private bool shieldSlamDashScreenShook;
        /// <summary>本次冲刺的表现相位（对应灾厄 AsgardianAegisDash 的 Time，每帧 +2，驱动尘环半径与旋角）</summary>
        private int shieldSlamDashTime;
        /// <summary>远端玩家正在跑的盾牌冲刺种类（None = 没在冲刺）：本机不是该玩家主人，由网络包驱动</summary>
        private ShieldSlamDash remoteShieldSlamDash;
        /// <summary>远端玩家冲刺已表现的帧数；0 表示没在表现，超过 RemoteDashMaxFrames 自动收尾</summary>
        private int remoteShieldSlamElapsed;
        /// <summary>远端玩家冲刺的表现相位（与 shieldSlamDashTime 同理，但两套独立以便同屏多人各跑各的）</summary>
        private int remoteShieldSlamTime;
        // ── 状态机 ──
        /// <summary>
        /// 冲刺状态机，由 PostUpdateRunSpeeds 每帧调用（灾厄把它的 ModDashMovement 挂在同一钩子）。
        /// 未冲刺时先做起手判定；起步那一帧只置速度与状态（对齐灾厄：它的命中/衰减分支要求 dashDelay &lt; 0，起步帧当帧不跑）；
        /// 之后逐帧：阻拦原版冲刺 → 命中判定 → 拖尾表现与速度衰减（内含收尾判定）。
        /// 非本机玩家走另一条路：只按收到的网络包刷拖尾尘（位移由原版的玩家网络同步负责，命中由主人那边结算）。
        /// </summary>
        public void ShieldSlamDashMovement()
        {
            if (Player.whoAmI != Main.myPlayer)
            {
                ShieldSlamDashRemoteVisuals();
                return;
            }
            if (shieldSlamDashCooldown > 0)
                shieldSlamDashCooldown--;
            // 双击窗口每帧朝 0 靠拢。灾厄把它放在 DoADash 开头（只有装了冲刺才会走），
            // 这里无条件递减：没装冲刺时窗口也能自然过期，不会留下一个陈旧的可触发状态
            if (shieldSlamDashTimeMod != 0)
                shieldSlamDashTimeMod -= shieldSlamDashTimeMod > 0 ? 1 : -1;
            bool startedThisFrame = false;
            if (shieldSlamDashElapsed <= 0)
            {
                UpdateShieldSlamDashInput();
                if (shieldSlamDashElapsed <= 0)
                    return;
                startedThisFrame = true;
            }
            BlockVanillaDash();
            if (startedThisFrame)
                return;
            ShieldSlamDashHits();
            ShieldSlamDashMidEffects();
        }
        /// <summary>
        /// 屏幕震动：每帧把玩家身上累积的震动强度换成一次随机圆内偏移加到屏幕位置，再线性衰减 0.185 并钳在 0~20。
        /// 与灾厄 CalamityPlayer.ModifyScreenPosition 一致（灾厄多一个 Screenshake 总开关，本工程未加配置项）。
        /// 另外叠加一层全局震动 <c>CalamityDemutation.ScreenShakeAmp</c>（CE 的 screenShakeAmp 口径）。
        /// </summary>
        public override void ModifyScreenPosition()
        {
            if (GeneralScreenShakePower > 0f)
            {
                Main.screenPosition += Main.rand.NextVector2Circular(GeneralScreenShakePower, GeneralScreenShakePower);
                GeneralScreenShakePower = MathHelper.Clamp(GeneralScreenShakePower - 0.185f, 0f, 20f);
            }
            // 全局屏幕震动：横轴抖幅是纵轴的 8 倍（CE 原式），衰减在 EffectsSystem 里每帧 -0.5。
            // CE 那句没有归属判断，而本钩子对每个活跃玩家各调一次，多人下整屏会被叠加好几遍；
            // 这里限定只由本地玩家施加一次——单机下与 CE 完全一致。
            if (Player.whoAmI == Main.myPlayer && CalamityDemutation.ScreenShakeAmp > 0f)
            {
                float amp = CalamityDemutation.ScreenShakeAmp;
                Main.screenPosition += new Vector2(Main.rand.Next((int)-amp * 8, (int)amp * 8 + 1), Main.rand.Next((int)-amp, (int)amp + 1));
            }
        }
        // ── 远端玩家的冲刺表现（联机） ──
        /// <summary>
        /// 远端玩家的拖尾表现：本机不是该玩家主人，位移由原版玩家网络同步负责、命中由主人那边结算，
        /// 这里只按收到的 MsgShieldSlamDash 包刷拖尾尘，让本机也能看到别人冲刺。
        /// 收到结束包即收尾；若结束包丢失或发送者中途掉线，超过 RemoteDashMaxFrames 帧自动收尾兜底。
        /// </summary>
        private void ShieldSlamDashRemoteVisuals()
        {
            if (Main.dedServ)
                return;
            if (remoteShieldSlamElapsed <= 0)
                return;
            // 该玩家中途死亡/离场就直接收尾，不要对着残影继续喷尘
            if (!Player.active || Player.dead)
            {
                ReceiveShieldSlamDash(ShieldSlamDash.None);
                return;
            }
            ShieldSlamDashTrailDust(remoteShieldSlamDash, ref remoteShieldSlamTime);
            if (++remoteShieldSlamElapsed > RemoteDashMaxFrames)
                ReceiveShieldSlamDash(ShieldSlamDash.None);
        }
        /// <summary>
        /// 收到其他玩家的盾牌冲刺网络消息（由主类 CalamityDemutation.HandlePacket 分流过来）：
        /// 传 None 表示该玩家的冲刺结束、清空表现状态；否则从第一帧开始表现该种类的拖尾。
        /// </summary>
        public void ReceiveShieldSlamDash(ShieldSlamDash dash)
        {
            remoteShieldSlamDash = dash;
            remoteShieldSlamElapsed = dash == ShieldSlamDash.None ? 0 : 1;
            remoteShieldSlamTime = 0;
        }
        /// <summary>
        /// 广播本机冲刺的开始（传冲刺种类）或结束（传 None）。单人游戏不发包。
        /// 主机自己就近广播给其他客户端；纯客户端发给服务端、由服务端代播给其余客户端。
        /// </summary>
        private void SendShieldSlamDashPacket(ShieldSlamDash dash)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;
            ModPacket packet = Mod.GetPacket();
            packet.Write(MsgShieldSlamDash);
            packet.Write((byte)Player.whoAmI);
            packet.Write((byte)dash);
            if (Main.netMode == NetmodeID.Server)
                packet.Send(-1, Player.whoAmI);
            else
                packet.Send();
        }
        /// <summary>
        /// 广播一次撞击（冲刺种类 + 被打的敌人索引），让其他客户端也在那个敌人身上刷一遍撞击粒子。
        /// 只广播粒子表现：伤害由 ApplyDamageToNPC、减益由 NPC.AddBuff 自己会同步，重复做会翻倍。
        /// </summary>
        private void SendShieldSlamDashHitPacket(ShieldSlamDash dash, int npcIndex)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;
            ModPacket packet = Mod.GetPacket();
            packet.Write(MsgShieldSlamDashHit);
            packet.Write((byte)Player.whoAmI);
            packet.Write((byte)dash);
            packet.Write((short)npcIndex);
            if (Main.netMode == NetmodeID.Server)
                packet.Send(-1, Player.whoAmI);
            else
                packet.Send();
        }
        /// <summary>
        /// 收到其他玩家的撞击消息（由主类 CalamityDemutation.HandlePacket 分流过来）：
        /// 该敌人还在场就在其中心刷一遍该冲刺的撞击粒子。
        /// </summary>
        public void ReceiveShieldSlamDashHit(ShieldSlamDash dash, int npcIndex)
        {
            if (npcIndex < 0 || npcIndex >= Main.maxNPCs || !Main.npc[npcIndex].active)
                return;
            ShieldSlamDashHitParticles(dash, Main.npc[npcIndex].Center);
        }
        // ── 起手 ──
        /// <summary>
        /// 起手判定（对应灾厄 DoADash → HandleHorizontalDash）：只在有冲刺、冷却归零、且没骑坐骑时进行。
        /// 左右方向键各按一次开始计时，15 帧内再按同向即触发该方向的水平冲刺——即灾厄非全向冲刺的双击手感。
        /// 本工程没有盾牌冲刺快捷键，故始终走灾厄「未绑定快捷键」那条分支。
        /// 另外与弑神者冲刺互斥：它正在冲刺时不接受盾牌冲刺的起手（灾厄只有一个 DashID，天然不会同时跑两个）。
        /// </summary>
        private void UpdateShieldSlamDashInput()
        {
            if (shieldSlamDash == ShieldSlamDash.None || godSlayerDashElapsed > 0 || shieldSlamDashCooldown > 0 || Player.mount.Active)
                return;
            bool rightInput = Player.controlRight && Player.releaseRight;
            bool leftInput = Player.controlLeft && Player.releaseLeft;
            if (rightInput)
            {
                if (shieldSlamDashTimeMod > 0)
                {
                    shieldSlamDashTimeMod = 0;
                    StartShieldSlamDash(1);
                }
                else
                    shieldSlamDashTimeMod = DoubleTapWindow;
            }
            else if (leftInput)
            {
                if (shieldSlamDashTimeMod < 0)
                {
                    shieldSlamDashTimeMod = 0;
                    StartShieldSlamDash(-1);
                }
                else
                    shieldSlamDashTimeMod = -DoubleTapWindow;
            }
        }
        /// <summary>
        /// 冲刺起步（对应灾厄 DoADash 里设置速度的一段）：水平速度置为 ±起手速度并保留垂直速度；
        /// 正前方或斜上方有实心块时把水平速度减半（防止贴墙冲刺被弹飞）；
        /// 最后把 Player.dashDelay 置 -1 表示冲刺中，并复位本次冲刺的相位与震屏标记。
        /// </summary>
        private void StartShieldSlamDash(int direction)
        {
            ShieldSlamProfile profile = GetShieldSlamProfile(shieldSlamDash);
            Player.velocity = new Vector2(direction * profile.StartSpeed, Player.velocity.Y);
            Point upwardTilePoint = (Player.Center + new Vector2(MathHelper.Clamp(direction, -1f, 1f) * Player.width / 2 + 2, Player.gravDir * -Player.height / 2f + Player.gravDir * 2f)).ToTileCoordinates();
            Point aheadTilePoint = (Player.Center + new Vector2(MathHelper.Clamp(direction, -1f, 1f) * Player.width / 2 + 2, 0f)).ToTileCoordinates();
            if (WorldGen.SolidOrSlopedTile(upwardTilePoint.X, upwardTilePoint.Y) || WorldGen.SolidOrSlopedTile(aheadTilePoint.X, aheadTilePoint.Y))
                Player.velocity.X /= 2f;
            Player.dashDelay = -1;
            shieldSlamDashElapsed = 1;
            shieldSlamDashTime = 0;
            shieldSlamDashScreenShook = false;
            SendShieldSlamDashPacket(shieldSlamDash);
        }
        // ── 冲刺中 ──
        /// <summary>
        /// 冲刺期间的每帧结算（顺序对齐灾厄 ModDashMovement）：先刷该冲刺的拖尾尘，再让朝向跟随速度、关掉星旋隐身，
        /// 最后做两级衰减——超过所选冲刺的过程速度上限按 0.985、落到跑步区间按 0.94；
        /// 速度落回跑步区间即视为冲刺结束，走 <see cref="EndShieldSlamDash"/> 写回冷却。
        /// </summary>
        private void ShieldSlamDashMidEffects()
        {
            ShieldSlamDashTrailDust(shieldSlamDash, ref shieldSlamDashTime);
            Player.vortexStealthActive = false;
            if (Player.velocity.X != 0f)
                Player.ChangeDir(Math.Sign(Player.velocity.X));
            ShieldSlamProfile profile = GetShieldSlamProfile(shieldSlamDash);
            float runSpeed = Math.Max(Player.accRunSpeed, Player.maxRunSpeed);
            if (Math.Abs(Player.velocity.X) > profile.MidDashSpeed)
                Player.velocity.X *= ShieldSpeedDeceleration;
            else if (Math.Abs(Player.velocity.X) > runSpeed)
                Player.velocity.X *= ShieldRunSpeedDeceleration;
            else
                EndShieldSlamDash(runSpeed);
        }
        /// <summary>
        /// 四个冲刺各自的每帧拖尾尘（对应各自的 MidDashEffects），全部按玩家护盾染色（cShield）：
        /// 阿斯加德之庇护——相位每帧 +2，绕玩家后方画一条 3 颗的螺旋尘环（181 冷焰 / 295 骨尘，alpha 220）再补 1 颗拖尾（180 / 295，alpha 170）；
        /// 极乐之庇护——每帧 4 组神圣尘（222 火焰 / 162 圣光，蓝尘额外上浮）再补 1 颗 228 号尘；
        /// 阿斯加德之英勇——每帧 4 颗覆盖身体的火焰尘（296 / 158）再按 1/3 概率补 1 颗 222 号尘；
        /// 华丽盾——每帧 3 颗冰尘（223 / 180）。
        /// 冲刺种类与相位都由参数传入，本机与远端玩家共用这一份（远端传 remoteShieldSlamDash / remoteShieldSlamTime）。
        /// </summary>
        private void ShieldSlamDashTrailDust(ShieldSlamDash dash, ref int time)
        {
            switch (dash)
            {
                case ShieldSlamDash.AsgardianAegis:
                {
                    time += 2;
                    float radiusFactor = MathHelper.Lerp(0f, 1f, Utils.GetLerpValue(2f, 2.5f, time, true));
                    for (int i = 0; i < 3; i++)
                    {
                        float offsetRotationAngle = Player.velocity.ToRotation() + time / 5f;
                        float radius = (15f + (float)Math.Cos(time / 3f) * 12f) * radiusFactor;
                        Vector2 dustPosition = Player.Center - Player.velocity * 2;
                        dustPosition += offsetRotationAngle.ToRotationVector2().RotatedBy(i / 5f * MathHelper.TwoPi) * radius;
                        Dust dust = Dust.NewDustPerfect(dustPosition, Main.rand.NextBool(5) ? 181 : 295);
                        dust.alpha = 220;
                        dust.noGravity = true;
                        dust.velocity = Player.velocity * 0.8f;
                        dust.scale = Main.rand.NextFloat(1.7f, 2.0f);
                        dust.shader = GameShaders.Armor.GetSecondaryShader(Player.cShield, Player);
                        Dust dust2 = Dust.NewDustPerfect(Player.Center + new Vector2(Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-15f, 15f)) + Player.velocity * 1.5f
                            , Main.rand.NextBool(8) ? 180 : 295, -Player.velocity.RotatedByRandom(MathHelper.ToRadians(30f)) * Main.rand.NextFloat(0.1f, 0.8f), 0, default, Main.rand.NextFloat(1.7f, 1.9f));
                        dust2.alpha = 170;
                        dust2.noGravity = true;
                        dust2.shader = GameShaders.Armor.GetSecondaryShader(Player.cShield, Player);
                    }
                    break;
                }
                case ShieldSlamDash.ElysianAegis:
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Dust sacredDust = Dust.NewDustPerfect(Player.Center + new Vector2(Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-15f, 15f)) - Player.velocity * 1.2f
                            , Main.rand.NextBool(8) ? DustID.FireworkFountain_Yellow : 162, -Player.velocity.RotatedByRandom(MathHelper.ToRadians(10f)) * Main.rand.NextFloat(0.1f, 0.8f), 0, default, Main.rand.NextFloat(1.8f, 2.8f));
                        sacredDust.shader = GameShaders.Armor.GetSecondaryShader(Player.cShield, Player);
                        sacredDust.noGravity = sacredDust.type != 222;
                        sacredDust.fadeIn = 0.5f;
                        if (sacredDust.type == 222)
                        {
                            sacredDust.scale = Main.rand.NextFloat(0.8f, 1.2f);
                            sacredDust.velocity += new Vector2(0, -2.5f) * Main.rand.NextFloat(0.8f, 1.2f);
                        }
                        if (sacredDust.type == 180)
                            sacredDust.scale = Main.rand.NextFloat(1.6f, 2.2f);
                        Dust dust = Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(6, 6) - Player.velocity * 2, DustID.GoldFlame);
                        dust.velocity = -Player.velocity * Main.rand.NextFloat(0.6f, 1.4f);
                        dust.scale = Main.rand.NextFloat(0.9f, 1.4f);
                        dust.noGravity = true;
                    }
                    break;
                }
                case ShieldSlamDash.AsgardsValor:
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Dust holyFireDust = Dust.NewDustDirect(Player.position + Vector2.UnitY * 4f, Player.width, Player.height - 8, Main.rand.NextBool() ? 296 : 158, 0f, 0f, 0, default, 1.2f);
                        holyFireDust.velocity = -Player.velocity * Main.rand.NextFloat(0.1f, 0.75f);
                        holyFireDust.scale *= Main.rand.NextFloat(1f, 1.2f);
                        holyFireDust.shader = GameShaders.Armor.GetSecondaryShader(Player.cShield, Player);
                        holyFireDust.noGravity = true;
                        if (Main.rand.NextBool())
                            holyFireDust.fadeIn = 0.1f;
                    }
                    if (Main.rand.NextBool(3))
                    {
                        Vector2 dustPosition = Player.Center + new Vector2(Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-15f, 15f)) - Player.velocity * 1.7f;
                        Dust dust = Dust.NewDustPerfect(dustPosition, DustID.FireworkFountain_Yellow, -Player.velocity * Main.rand.NextFloat(0.15f, 0.4f), 0, default, 0.5f);
                        dust.noGravity = false;
                        dust.shader = GameShaders.Armor.GetSecondaryShader(Player.cShield, Player);
                    }
                    break;
                }
                case ShieldSlamDash.OrnateShield:
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Dust iceDust = Dust.NewDustPerfect(Player.Center + new Vector2(Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-15f, 15f)) - Player.velocity * 1.7f
                            , Main.rand.NextBool(8) ? 223 : 180, -Player.velocity.RotatedByRandom(MathHelper.ToRadians(10f)) * Main.rand.NextFloat(0.1f, 0.8f), 0, default, Main.rand.NextFloat(0.6f, 0.8f));
                        iceDust.shader = GameShaders.Armor.GetSecondaryShader(Player.cShield, Player);
                        iceDust.noGravity = true;
                        iceDust.fadeIn = 0.5f;
                        if (iceDust.type == 180)
                            iceDust.scale = Main.rand.NextFloat(1.6f, 2.2f);
                    }
                    break;
                }
            }
        }
        /// <summary>
        /// 冲刺收尾（对应灾厄 ModDashMovement 里速度落回跑步区间后的分支）：
        /// 置 30 帧的冷却计数器、复位原版 dashDelay（否则卡在起步的 -1 会阻塞下次起手），把水平速度钳到跑步速度，并清空冲刺状态。
        /// </summary>
        private void EndShieldSlamDash(float runSpeed)
        {
            shieldSlamDashCooldown = ShieldSlamCooldown;
            Player.dashDelay = 0;
            if (Player.velocity.X < 0f)
                Player.velocity.X = -runSpeed;
            else if (Player.velocity.X > 0f)
                Player.velocity.X = runSpeed;
            shieldSlamDashElapsed = 0;
            shieldSlamDashTime = 0;
            SendShieldSlamDashPacket(ShieldSlamDash.None);
        }
        // ── 命中 ──
        /// <summary>
        /// 冲刺命中判定（对应灾厄 ModDashMovement 开头的撞击结算）：
        /// 以「玩家位置 + 速度的一半」为中心构造各向外扩 4 像素的判定框，
        /// 对每个可攻击且不在冲刺免疫中的敌人结算一次——伤害与击退按近战职业加成换算、暴击按近战暴击率掷骰、
        /// 命中后敌人冲刺免疫补到至少 12 帧、玩家获得 12 帧碰撞免疫，随后走该冲刺的专属撞击表现。
        /// </summary>
        private void ShieldSlamDashHits()
        {
            ShieldSlamProfile profile = GetShieldSlamProfile(shieldSlamDash);
            // 基础伤害为 0 直接跳过（对齐灾厄：它在 OnHitEffects 之后会因 BaseDamage <= 0 放弃本次结算）
            if (profile.Damage <= 0)
                return;
            Rectangle hitArea = new Rectangle((int)(Player.position.X + Player.velocity.X * 0.5f - 4f), (int)(Player.position.Y + Player.velocity.Y * 0.5f - 4f), Player.width + 8, Player.height + 8);
            int hitDirection = Player.direction;
            if (Player.velocity.X != 0f)
                hitDirection = Math.Sign(Player.velocity.X);
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (Player.dontHurtCritters && NPCID.Sets.CountsAsCritter[npc.type])
                    continue;
                if (npc.dontTakeDamage || npc.friendly || npc.immune[Player.whoAmI] > 0)
                    continue;
                if (!hitArea.Intersects(npc.getRect()) || !(npc.noTileCollide || Player.CanHit(npc)))
                    continue;
                int dashDamage = (int)Player.GetTotalDamage<MeleeDamageClass>().ApplyTo(profile.Damage);
                float dashKnockback = Player.GetTotalKnockback<MeleeDamageClass>().ApplyTo(profile.Knockback);
                bool critical = Main.rand.Next(100) < Player.GetTotalCritChance<MeleeDamageClass>();
                Player.ApplyDamageToNPC(npc, dashDamage, dashKnockback, hitDirection, critical, DamageClass.Melee, true);
                if (npc.immune[Player.whoAmI] < MinShieldNPCImmunityFrames)
                    npc.immune[Player.whoAmI] = MinShieldNPCImmunityFrames;
                if (Player.immuneTime < ShieldHitImmunityFrames)
                    Player.immuneTime = ShieldHitImmunityFrames;   // 不用原版 GiveImmuneTimeForCollisionAttack：它带反作弊，20 tick 内第 3 次起克扣无敌帧
                ShieldSlamDashHitEffects(npc, profile);
            }
        }
        /// <summary>
        /// 本机玩家冲刺的撞击结算（对应各自 OnHitEffects 的后半段）：震屏（只在本次冲刺首个命中触发一次）→
        /// 生成该冲刺的爆炸弹幕 → 撞击粒子 → 统一减益 → 广播一条撞击消息让其他客户端也刷一遍粒子。
        /// 爆炸弹幕不必广播：原版会把客户端生成的友方弹幕同步给其他客户端。
        /// </summary>
        private void ShieldSlamDashHitEffects(NPC npc, ShieldSlamProfile profile)
        {
            if (profile.ScreenShake > 0f && !shieldSlamDashScreenShook)
            {
                GeneralScreenShakePower = profile.ScreenShake;
                shieldSlamDashScreenShook = true;
            }
            if (shieldSlamDash == ShieldSlamDash.AsgardianAegis)
                Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, ModContent.ProjectileType<CosmicDashExplosion>()
                    , GetShieldExplosionDamage(profile), profile.ExplosionKnockback, Main.myPlayer, 3f, 0f);
            else if (shieldSlamDash == ShieldSlamDash.ElysianAegis)
                Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, ModContent.ProjectileType<HolyExplosionSupreme>()
                    , GetShieldExplosionDamage(profile), profile.ExplosionKnockback, Main.myPlayer, 1f, 0f);
            ShieldSlamDashHitParticles(shieldSlamDash, npc.Center);
            if (profile.DebuffFrames > 0)
            {
                if (profile.VanillaDebuff != 0)
                    npc.AddBuff(profile.VanillaDebuff, profile.DebuffFrames);
                else
                {
                    ApplyCalamityBuff(npc, "CalamityMod", profile.CalamityDebuff, profile.DebuffFrames);
                    ApplyCalamityBuff(npc, "CalamityModClassicPreTrailer", profile.ClassicDebuff, profile.DebuffFrames);
                }
            }
            // 只有前三种冲刺有撞击粒子（华丽盾没有额外表现），不必为它白发包；None 走不到这里（伤害为 0 已被上层挡掉）
            if (shieldSlamDash != ShieldSlamDash.OrnateShield)
                SendShieldSlamDashHitPacket(shieldSlamDash, npc.whoAmI);
        }
        /// <summary>
        /// 撞击的粒子表现，本机冲刺与远端冲刺共用这一份（故以参数传入冲刺种类与中心点）：
        /// 阿斯加德之庇护——中心叠一个青色定向脉冲环与一个洋红细节爆炸；
        /// 极乐之庇护——中心叠两个细节爆炸（灰色 0.6 倍 + 橙色，缩放随机）；
        /// 阿斯加德之英勇——中心喷 12 向三色尘（296/158/169）再补 5 颗 222 号尘，并播放 62 号音效；
        /// 华丽盾——无。尘一律按当前玩家的护盾染色（远端冲刺即远端玩家的染色）。
        /// </summary>
        private void ShieldSlamDashHitParticles(ShieldSlamDash dash, Vector2 center)
        {
            switch (dash)
            {
                case ShieldSlamDash.AsgardianAegis:
                {
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero, Color.Aqua, new Vector2(2f, 2f), 0f, 0.1f, 0.85f, 36));
                    GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, Color.Magenta, Vector2.One, Main.rand.NextFloat(-5f, 5f), 0f, 0.65f, 26));
                    break;
                }
                case ShieldSlamDash.ElysianAegis:
                {
                    float particleScale = Main.rand.NextFloat(0.45f, 0.55f);
                    GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, Color.Gray * 0.6f, Vector2.One, Main.rand.NextFloat(-5f, 5f), 0f, particleScale + 0.07f, 20, false));
                    GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, Color.Orange, Vector2.One, Main.rand.NextFloat(-5f, 5f), 0f, particleScale, 20));
                    break;
                }
                case ShieldSlamDash.AsgardsValor:
                {
                    int dustCount = 12;
                    float radians = MathHelper.TwoPi / dustCount;
                    Vector2 spinningPoint = Vector2.Normalize(new Vector2(-1f, -1f));
                    for (int k = 0; k < dustCount; k++)
                    {
                        Vector2 velocity = spinningPoint.RotatedBy(radians * k);
                        Dust dust = Dust.NewDustPerfect(center, DustID.CrimsonTorch, velocity * 3f, 0, default, 2.5f);
                        dust.noGravity = true;
                        dust.shader = GameShaders.Armor.GetSecondaryShader(Player.cShield, Player);
                        Dust dust2 = Dust.NewDustPerfect(center, DustID.OrangeTorch, velocity * 5f, 0, default, 2.2f);
                        dust2.noGravity = true;
                        dust2.shader = GameShaders.Armor.GetSecondaryShader(Player.cShield, Player);
                        dust2.color = Color.Salmon;
                        Dust dust3 = Dust.NewDustPerfect(center, DustID.IchorTorch, velocity * 7f, 0, default, 1.9f);
                        dust3.noGravity = true;
                        dust3.shader = GameShaders.Armor.GetSecondaryShader(Player.cShield, Player);
                        dust3.color = Color.SandyBrown;
                    }
                    for (int k = 0; k < 5; k++)
                    {
                        Dust dust = Dust.NewDustPerfect(center, DustID.FireworkFountain_Yellow, new Vector2(0, -3.5f).RotatedByRandom(0.7f) * Main.rand.NextFloat(0.8f, 1.4f), 0, default, 1.2f);
                        dust.noGravity = false;
                        dust.shader = GameShaders.Armor.GetSecondaryShader(Player.cShield, Player);
                    }
                    SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.6f, PitchVariance = 0.3f }, center);
                    break;
                }
            }
        }
        // ── 数值 ──
        /// <summary>
        /// 撞击爆炸弹幕的伤害（对应灾厄的 GetBestClassDamage + ApplyArmorAccDamageBonusesTo）。
        /// ApplyArmorAccDamageBonusesTo 在本工程恒等（没有 Old Fashioned）故省略；
        /// GetBestClassDamage 内联为：无职业加成部分照抄通用职业，职业加成部分取 近战/远程/魔法/召唤×0.75 中的最高
        /// （灾厄还比较盗贼职业，本工程没有该职业）。
        /// </summary>
        private int GetShieldExplosionDamage(ShieldSlamProfile profile)
        {
            StatModifier bestClassDamage = StatModifier.Default;
            StatModifier classless = Player.GetTotalDamage<GenericDamageClass>();
            bestClassDamage.Base = classless.Base;
            bestClassDamage *= classless.Multiplicative;
            bestClassDamage.Flat = classless.Flat;
            float strongest = 1f;
            strongest = Math.Max(strongest, Player.GetTotalDamage<MeleeDamageClass>().Additive);
            strongest = Math.Max(strongest, Player.GetTotalDamage<RangedDamageClass>().Additive);
            strongest = Math.Max(strongest, Player.GetTotalDamage<MagicDamageClass>().Additive);
            strongest = Math.Max(strongest, Player.GetTotalDamage<SummonDamageClass>().Additive * 0.75f);
            bestClassDamage += strongest - 1f;
            return (int)bestClassDamage.ApplyTo(profile.ExplosionDamage);
        }
        /// <summary>
        /// 取某个盾牌冲刺的数值档（对应灾厄四个冲刺类里硬编码的那些常量）：
        /// StartSpeed = CalculateDashSpeed 的返回值，MidDashSpeed = MidDashEffects 覆写的 dashSpeed，
        /// ScreenShake = OnHitEffects 里的 GeneralScreenShakePower，ExplosionDamage/Knockback = 撞击爆炸弹幕的伤害与击退。
        /// 阿斯加德之英勇不覆写过程速度，故用默认 12f；它也没有震屏、爆炸与减益。
        /// </summary>
        private static ShieldSlamProfile GetShieldSlamProfile(ShieldSlamDash dash) => dash switch
        {
            ShieldSlamDash.AsgardianAegis => new ShieldSlamProfile(23.3f, 16f, 1000, 15f, 5f, 300, "GodSlayerInferno", "GodSlayerInferno", 0, 1000, 20f),
            ShieldSlamDash.ElysianAegis => new ShieldSlamProfile(21.5f, 14f, 500, 12f, 3.5f, 300, "HolyFlames", "HolyLight", 0, 500, 15f),
            ShieldSlamDash.AsgardsValor => new ShieldSlamProfile(16.9f, DefaultMidDashSpeed, 200, 9f, 0f, 0, "", "", 0, 0, 0f),
            ShieldSlamDash.OrnateShield => new ShieldSlamProfile(16.9f, 12.5f, 50, 3f, 0f, 180, "", "", BuffID.Frostburn2, 0, 0f),
            _ => new ShieldSlamProfile(DefaultMidDashSpeed, DefaultMidDashSpeed, 0, 0f, 0f, 0, "", "", 0, 0, 0f)
        };
        /// <summary>
        /// 单个盾牌冲刺的数值档（只读）。字段与灾厄四个冲刺类里的硬编码常量逐一对应，
        /// 其中减益分两条路：VanillaDebuff 非 0 时用原版减益（华丽盾的霜燃 2），
        /// 否则按 CalamityDebuff / ClassicDebuff 分别在现代版与经典版灾厄里查找。
        /// </summary>
        private readonly struct ShieldSlamProfile
        {
            /// <summary>起手速度（灾厄 CalculateDashSpeed 的返回值）</summary>
            public readonly float StartSpeed;
            /// <summary>过程速度上限（灾厄 MidDashEffects 覆写的 dashSpeed）</summary>
            public readonly float MidDashSpeed;
            /// <summary>撞击基础伤害</summary>
            public readonly int Damage;
            /// <summary>撞击基础击退</summary>
            public readonly float Knockback;
            /// <summary>撞击震屏强度（0 = 不震屏）</summary>
            public readonly float ScreenShake;
            /// <summary>撞击附加减益的帧数（0 = 无减益）</summary>
            public readonly int DebuffFrames;
            /// <summary>现代版灾厄的减益类名（空串表示不用灾厄减益）</summary>
            public readonly string CalamityDebuff;
            /// <summary>经典版灾厄的减益类名（两个版本减益常改名，如神圣烈焰在现代版叫 HolyFlames、经典版叫 HolyLight）</summary>
            public readonly string ClassicDebuff;
            /// <summary>原版减益的 buff ID（0 = 不用原版减益）</summary>
            public readonly int VanillaDebuff;
            /// <summary>撞击爆炸弹幕的基础伤害（0 = 不生成爆炸弹幕）</summary>
            public readonly int ExplosionDamage;
            /// <summary>撞击爆炸弹幕的基础击退</summary>
            public readonly float ExplosionKnockback;
            /// <summary>按数值表构造一档冲刺数值</summary>
            public ShieldSlamProfile(float startSpeed, float midDashSpeed, int damage, float knockback, float screenShake, int debuffFrames
                , string calamityDebuff, string classicDebuff, int vanillaDebuff, int explosionDamage, float explosionKnockback)
            {
                StartSpeed = startSpeed;
                MidDashSpeed = midDashSpeed;
                Damage = damage;
                Knockback = knockback;
                ScreenShake = screenShake;
                DebuffFrames = debuffFrames;
                CalamityDebuff = calamityDebuff;
                ClassicDebuff = classicDebuff;
                VanillaDebuff = vanillaDebuff;
                ExplosionDamage = explosionDamage;
                ExplosionKnockback = explosionKnockback;
            }
        }
    }
}
