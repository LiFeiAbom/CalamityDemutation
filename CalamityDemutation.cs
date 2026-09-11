using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation
{
    /// <summary>
    /// 模组主类，作为模组的入口点
    /// </summary>
    public class CalamityDemutation : Mod
    {
        // ── 静态字段 ──
        /// <summary>
        /// 蜂类敌人 ID 列表：收录原版蜜蜂/黄蜂及灾厄瘟疫系列敌人，
        /// 供“蜂抗”等需要识别蜂类单位的效果使用
        /// </summary>
        public static List<int> beeEnemyList;
        /// <summary>
        /// 蜂类弹幕 ID 列表：收录原版蜂刺及灾厄瘟疫系列弹幕，
        /// 供蜂巢相关效果判断蜂类弹幕使用
        /// </summary>
        public static List<int> beeProjectileList;
        /// <summary>
        /// 持续伤害类 debuff ID 列表：原版毒素/燃烧系列 + 灾厄两版本的专属 debuff，
        /// 供需要统一处理「持续伤害」的效果批量判定
        /// </summary>
        public static List<int> debuffList;
        /// <summary>
        /// 模组单例：供全局静态访问当前 Mod 实例
        /// </summary>
        public static CalamityDemutation Instance;
        // ── 实例字段 ──
        /// <summary>
        /// GravityDontFlipScreen 模组的软依赖引用（未安装时为 null）。
        /// 供 BaseHeldProjCO 在重力翻转时判断是否需要修正持握弹幕的朝向
        /// </summary>
        internal Mod gravityDontFlipScreen = null;
        // ── 生命周期方法 ──
        /// <summary>
        /// 模组加载钩子：由 tModLoader 加载模组时自动调用一次，
        /// 缓存单例并构建蜂类单位 ID 列表
        /// </summary>
        public override void Load()
        {
            Instance = this;
            FindMod();
            SetupLists();
            Content.Projectiles.Melee.Core.SwingSystem.Load();
            if (!Main.dedServ)
            {
                Common.Effects.EffectLoader.LoadEffects();
            }
        }
        /// <summary>
        /// 模组卸载钩子：清空静态列表引用，防止跨世界或重载模组时残留脏数据
        /// </summary>
        public override void Unload()
        {
            beeProjectileList = null;
            beeEnemyList = null;
            debuffList = null;
            // 清空单例引用，避免卸载后模组实例仍被静态字段挂住
            // (对应日志里的 "mod class still using memory" 警告)
            Instance = null;
            emptyMod();
            Common.Effects.EffectLoader.UnLoad();
            Content.Projectiles.Melee.Core.SwingSystem.UnLoad();
            // 清空运行时懒加载的静态贴图引用，避免卸载后残留
            Content.Projectiles.Melee.NeutronGlaiveBeam.warpTex = null;
        }
        /// <summary>
        /// 处理客户端发来的自定义网络消息。当前仅有洋葱永久解锁上报
        /// （MsgPermanentUnlock）：非主机客户端吃了洋葱后，其本地标志服务器并不知道，
        /// 这里更新服务器上对应玩家的 ModPlayer 副本，再广播该玩家完整状态给所有端，
        /// 使其它客户端也能看到该玩家已解锁的额外饰品栏。
        /// </summary>
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            byte msgType = reader.ReadByte();
            if (msgType == Players.CalamityDemutationPlayer.MsgPermanentUnlock)
            {
                int targetWho = reader.ReadInt32();
                bool acc = reader.ReadBoolean();
                bool wing = reader.ReadBoolean();
                if (targetWho >= 0 && targetWho < Main.player.Length && Main.player[targetWho].active)
                {
                    Players.CalamityDemutationPlayer modPlayer = Main.player[targetWho].GetModPlayer<Players.CalamityDemutationPlayer>();
                    modPlayer.extraAccessoryML = acc;
                    modPlayer.extraWingSlot = wing;
                    NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, targetWho);
                }
            }
        }
        /// <summary>
        /// 回退灾厄对原版翅膀的削弱（飞行时间/水平速度/悬浮速度与加速度）。
        /// 灾厄在 CalamityGlobalItem.SetStaticDefaults 里改这些翅膀属性，此处在其后恢复原版值。
        /// </summary>
        public override void PostSetupContent()
        {
            if (!ModLoader.HasMod("CalamityMod"))
                return;
            if (Systems.CalamityDemutationConfigSystem.Instance?.RevertVanillaNerfs != true)
                return;
            var stats = ArmorIDs.Wing.Sets.Stats;
            stats[9].FlyTime = 160;                                                       // FlameWings 火焰翅膀：飞行时间 130→160
            stats[14].AccRunSpeedOverride = 7.5f;                                         // BatWings 蝙蝠翅膀：水平速度 6.75→7.5
            stats[44].FlyTime = 150;                                                      // RainbowWings 女皇翅膀：飞行时间 120→150
            stats[28].DownHoverSpeedOverride = 12f;                                       // BejeweledValkyrieWing：悬浮 10.8→12
            stats[28].DownHoverAccelerationMult = 12f;
            stats[33].DownHoverSpeedOverride = 12f;                                       // Yoraiz0rWings：悬浮 10.8→12
            stats[33].DownHoverAccelerationMult = 12f;
            stats[35].DownHoverSpeedOverride = 12f;                                       // SkiphsWings：悬浮 10.8→12
            stats[35].DownHoverAccelerationMult = 12f;
            stats[37].DownHoverSpeedOverride = 12f;                                       // BetsyWings：悬浮 10.8→12
            stats[37].DownHoverAccelerationMult = 12f;
            stats[45].AccRunAccelerationMult = 4.5f;                                      // LongRainbowTrailWings 天界星盘：加速 2.75→4.5
            stats[45].DownHoverSpeedOverride = 16f;                                       // 天界星盘：悬浮 12→16
            stats[45].DownHoverAccelerationMult = 16f;
        }
        // ── 公开方法 ──
        /// <summary>
        /// 重新解析外部模组引用：先调用 emptyMod() 清空旧引用，再用 ModLoader.TryGetMod 软依赖查找
        /// GravityDontFlipScreen，结果存入 gravityDontFlipScreen 字段，供 BaseHeldProjCO 在重力翻转时
        /// 修正持握弹幕的朝向。查找失败时字段保持 null，相关逻辑以 null 判定跳过，缺少该模组不会报错。
        /// 注意：灾厄双版本（CalamityMod / CalamityModClassicPreTrailer）的软依赖不在此解析，
        /// 而是由本类各内容列表与内容类中的 ModContent.TryFind / ModLoader.TryGetMod 分别处理。
        /// </summary>
        public void FindMod()
        {
            emptyMod();
            ModLoader.TryGetMod("GravityDontFlipScreen", out gravityDontFlipScreen);
        }
        /// <summary>
        /// 初始化蜂类弹幕与敌人 ID 列表，供蜂巢相关饰品效果判断蜂类单位使用
        /// </summary>
        public static void SetupLists()
        {
            // 基础蜂类弹幕：原版蜂刺与黄蜂毒刺
            beeProjectileList =
            [
                ProjectileID.Stinger,
                ProjectileID.HornetStinger,
             ];
            // 若已安装现代版灾厄（CalamityMod），补充其瘟疫系蜂类弹幕 ID（TryFind 避免硬依赖）
            if (ModContent.TryFind("CalamityMod", "PlagueStingerGoliath", out ModProjectile p1))
                beeProjectileList.Add(p1.Type);
            if (ModContent.TryFind("CalamityMod", "PlagueStingerGoliathV2", out ModProjectile p2))
                beeProjectileList.Add(p2.Type);
            // 若已安装经典版灾厄（CalamityModClassicPreTrailer），同样补充其瘟疫系蜂类弹幕 ID
            if (ModContent.TryFind("CalamityModClassicPreTrailer", "PlagueExplosion", out ModProjectile p3))
                beeProjectileList.Add(p3.Type);
            if (ModContent.TryFind("CalamityModClassicPreTrailer", "PlagueStingerGoliath", out ModProjectile p4))
                beeProjectileList.Add(p4.Type);
            if (ModContent.TryFind("CalamityModClassicPreTrailer", "PlagueStingerGoliathV2", out ModProjectile p5))
                beeProjectileList.Add(p5.Type);
            // 基础蜂类敌人：原版苔藓黄蜂、旋涡黄蜂、蜜蜂与蜂王
            beeEnemyList =
            [
                NPCID.GiantMossHornet,
                NPCID.BigMossHornet,
                NPCID.LittleMossHornet,
                NPCID.TinyMossHornet,
                NPCID.MossHornet,
                NPCID.VortexHornetQueen,
                NPCID.VortexHornet,
                NPCID.Bee,
                NPCID.BeeSmall,
                NPCID.QueenBee,
            ];
            // 若已安装现代版灾厄，逐一补充瘟疫使者系列敌人 ID（含大型/普通及微型变种）
            if (ModContent.TryFind("CalamityMod", "PlaguebringerGoliath", out ModNPC npc1))
                beeEnemyList.Add(npc1.Type);
            if (ModContent.TryFind("CalamityMod", "PlaguebringerMiniboss", out ModNPC npc2))
                beeEnemyList.Add(npc2.Type);
            if (ModContent.TryFind("CalamityMod", "PlagueChargerLarge", out ModNPC npc3))
                beeEnemyList.Add(npc3.Type);
            if (ModContent.TryFind("CalamityMod", "PlagueCharger", out ModNPC npc4))
                beeEnemyList.Add(npc4.Type);
            if (ModContent.TryFind("CalamityMod", "PlaguebringerShade", out ModNPC npc5))
                beeEnemyList.Add(npc5.Type);
            if (ModContent.TryFind("CalamityMod", "PlagueBeeLargeG", out ModNPC npc6))
                beeEnemyList.Add(npc6.Type);
            if (ModContent.TryFind("CalamityMod", "PlagueBeeLarge", out ModNPC npc7))
                beeEnemyList.Add(npc7.Type);
            if (ModContent.TryFind("CalamityMod", "PlagueBeeG", out ModNPC npc8))
                beeEnemyList.Add(npc8.Type);
            if (ModContent.TryFind("CalamityMod", "PlagueBee", out ModNPC npc9))
                beeEnemyList.Add(npc9.Type);
            // 若已安装经典版灾厄（CalamityModClassicPreTrailer），同样补充其瘟疫系蜂类敌人 ID
            if (ModContent.TryFind("CalamityModClassicPreTrailer", "PlaguebringerGoliath", out ModNPC npc10))
                beeEnemyList.Add(npc10.Type);
            if (ModContent.TryFind("CalamityModClassicPreTrailer", "PlaguebringerMiniboss", out ModNPC npc11))
                beeEnemyList.Add(npc11.Type);
            if (ModContent.TryFind("CalamityModClassicPreTrailer", "PlagueChargerLarge", out ModNPC npc12))
                beeEnemyList.Add(npc12.Type);
            if (ModContent.TryFind("CalamityModClassicPreTrailer", "PlagueCharger", out ModNPC npc13))
                beeEnemyList.Add(npc13.Type);
            if (ModContent.TryFind("CalamityModClassicPreTrailer", "PlaguebringerShade", out ModNPC npc14))
                beeEnemyList.Add(npc14.Type);
            if (ModContent.TryFind("CalamityModClassicPreTrailer", "PlagueBeeLargeG", out ModNPC npc15))
                beeEnemyList.Add(npc15.Type);
            if (ModContent.TryFind("CalamityModClassicPreTrailer", "PlagueBeeLarge", out ModNPC npc16))
                beeEnemyList.Add(npc16.Type);
            if (ModContent.TryFind("CalamityModClassicPreTrailer", "PlagueBeeG", out ModNPC npc17))
                beeEnemyList.Add(npc17.Type);
            if (ModContent.TryFind("CalamityModClassicPreTrailer", "PlagueBee", out ModNPC npc18))
                beeEnemyList.Add(npc18.Type);
            debuffList =
            [
                BuffID.Poisoned,
                BuffID.Darkness,
                BuffID.Cursed,
                BuffID.OnFire,
                BuffID.OnFire3,
                BuffID.Bleeding,
                BuffID.Confused,
                BuffID.Slow,
                BuffID.Weak,
                BuffID.Silenced,
                BuffID.BrokenArmor,
                BuffID.CursedInferno,
                BuffID.Frostburn,
                BuffID.Frostburn2,
                BuffID.Chilled,
                BuffID.Frozen,
                BuffID.Burning,
                BuffID.Suffocation,
                BuffID.Ichor,
                BuffID.Venom,
                BuffID.Blackout,
                BuffID.Electrified,
                BuffID.Rabies,
                BuffID.Webbed,
                BuffID.Stoned,
                BuffID.Dazed,
                BuffID.VortexDebuff,
                BuffID.WitheredArmor,
                BuffID.WitheredWeapon,
                BuffID.OgreSpit,
                BuffID.BetsysCurse
            ];
            if (ModContent.TryFind("CalamityMod", "Nightwither", out ModBuff buff1))
                debuffList.Add(buff1.Type);
            if (ModContent.TryFind("CalamityMod", "Voidfrost", out ModBuff buff2))
                debuffList.Add(buff2.Type);
            if (ModContent.TryFind("CalamityMod", "WindChilled", out ModBuff buff3))
                debuffList.Add(buff3.Type);
            if (ModContent.TryFind("CalamityMod", "CrushDepth", out ModBuff buff4))
                debuffList.Add(buff4.Type);
            if (ModContent.TryFind("CalamityMod", "Eutrophication", out ModBuff buff5))
                debuffList.Add(buff5.Type);
            if (ModContent.TryFind("CalamityMod", "HadopelagicPressure", out ModBuff buff6))
                debuffList.Add(buff6.Type);
            if (ModContent.TryFind("CalamityMod", "Riptide", out ModBuff buff7))
                debuffList.Add(buff7.Type);
            if (ModContent.TryFind("CalamityMod", "AstralInfection", out ModBuff buff8))
                debuffList.Add(buff8.Type);
            if (ModContent.TryFind("CalamityMod", "BrainRot", out ModBuff buff9))
                debuffList.Add(buff9.Type);
            if (ModContent.TryFind("CalamityMod", "BurningBlood", out ModBuff buff10))
                debuffList.Add(buff10.Type);
            if (ModContent.TryFind("CalamityMod", "Plague", out ModBuff buff11))
                debuffList.Add(buff11.Type);
            if (ModContent.TryFind("CalamityMod", "SagePoison", out ModBuff buff12))
                debuffList.Add(buff12.Type);
            if (ModContent.TryFind("CalamityMod", "SulphuricPoisoning", out ModBuff buff13))
                debuffList.Add(buff13.Type);
            if (ModContent.TryFind("CalamityMod", "WhisperingDeath", out ModBuff buff14))
                debuffList.Add(buff14.Type);
            if (ModContent.TryFind("CalamityMod", "StaticDischarge", out ModBuff buff15))
                debuffList.Add(buff15.Type);
            if (ModContent.TryFind("CalamityMod", "VermillionFlux", out ModBuff buff16))
                debuffList.Add(buff16.Type);
            if (ModContent.TryFind("CalamityMod", "BrimstoneFlames", out ModBuff buff17))
                debuffList.Add(buff17.Type);
            if (ModContent.TryFind("CalamityMod", "DemonicFlames", out ModBuff buff18))
                debuffList.Add(buff18.Type);
            if (ModContent.TryFind("CalamityMod", "GodSlayerInferno", out ModBuff buff19))
                debuffList.Add(buff19.Type);
            if (ModContent.TryFind("CalamityMod", "HolyFlames", out ModBuff buff20))
                debuffList.Add(buff20.Type);
            if (ModContent.TryFind("CalamityMod", "SearingLava", out ModBuff buff21))
                debuffList.Add(buff21.Type);
            if (ModContent.TryFind("CalamityMod", "ArmorCrunch", out ModBuff buff22))
                debuffList.Add(buff22.Type);
            if (ModContent.TryFind("CalamityMod", "ElementalMix", out ModBuff buff23))
                debuffList.Add(buff23.Type);
            if (ModContent.TryFind("CalamityMod", "HeavyBleeding", out ModBuff buff24))
                debuffList.Add(buff24.Type);
            if (ModContent.TryFind("CalamityMod", "Irradiated", out ModBuff buff25))
                debuffList.Add(buff25.Type);
            if (ModContent.TryFind("CalamityMod", "Laceration", out ModBuff buff26))
                debuffList.Add(buff26.Type);
            if (ModContent.TryFind("CalamityMod", "HolyInferno", out ModBuff buff27))
                debuffList.Add(buff27.Type);
            if (ModContent.TryFind("CalamityMod", "IcarusFolly", out ModBuff buff28))
                debuffList.Add(buff28.Type);
            if (ModContent.TryFind("CalamityModClassicPreTrailer", "BrimstoneFlames", out ModBuff buffC1))
                debuffList.Add(buffC1.Type);
            if (ModContent.TryFind("CalamityModClassicPreTrailer", "BurningBlood", out ModBuff buffC2))
                debuffList.Add(buffC2.Type);
            if (ModContent.TryFind("CalamityModClassicPreTrailer", "GlacialState", out ModBuff buffC3))
                debuffList.Add(buffC3.Type);
            if (ModContent.TryFind("CalamityModClassicPreTrailer", "GodSlayerInferno", out ModBuff buffC4))
                debuffList.Add(buffC4.Type);
            if (ModContent.TryFind("CalamityModClassicPreTrailer", "HolyLight", out ModBuff buffC5))
                debuffList.Add(buffC5.Type);
            if (ModContent.TryFind("CalamityModClassicPreTrailer", "Irradiated", out ModBuff buffC6))
                debuffList.Add(buffC6.Type);
            if (ModContent.TryFind("CalamityModClassicPreTrailer", "Plague", out ModBuff buffC7))
                debuffList.Add(buffC7.Type);
            if (ModContent.TryFind("CalamityModClassicPreTrailer", "AbyssalFlames", out ModBuff buffC8))
                debuffList.Add(buffC8.Type);
            if (ModContent.TryFind("CalamityModClassicPreTrailer", "CrushDepth", out ModBuff buffC9))
                debuffList.Add(buffC9.Type);
            if (ModContent.TryFind("CalamityModClassicPreTrailer", "Horror", out ModBuff buffC10))
                debuffList.Add(buffC10.Type);
            if (ModContent.TryFind("CalamityModClassicPreTrailer", "MarkedforDeath", out ModBuff buffC11))
                debuffList.Add(buffC11.Type);
        }
        // ── 私有工具 ──
        /// <summary>
        /// 清空本类缓存的全部外部模组引用（当前仅 gravityDontFlipScreen）
        /// </summary>
        private void emptyMod()
        {
            gravityDontFlipScreen = null;
        }
    }
}
