using CalamityDemutation.Common.Effects;
using CalamityDemutation.Enums;
using CalamityDemutation.Players;
using CalamityDemutation.Systems.Graphic;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Items.Accessories.Function
{
    /// <summary>
    /// 亵渎之魂神器（Profaned Soul Artifact） - 召唤向综合饰品（移植自灾厄 2.2.2，护盾为自实现替代）
    /// 效果：召唤一名治疗守护者；拥有至少 10 个仆从栏时追加防御守护者（移速 + 减伤），
    /// 穿戴塔拉贡召唤套（或更强）时追加攻击守护者（召唤伤害 + 仆从栏）；
    /// 另带一层耐久护盾（上限 25，破盾后延迟回充）。
    /// 三守护者的数值、治疗与护盾结算都在 CalamityDemutationPlayer 中完成，本类只负责置位标记。
    /// </summary>
    internal class ProfanedSoulArtifact:ModItem
    {
        // ── 静态常量 ──
        /// <summary>
        /// 护盾耐久上限（对应灾厄 2.2.2 的 ShieldDurabilityMax = 25）
        /// </summary>
        public const int ShieldDurabilityMax = 25;
        /// <summary>
        /// 破盾后的回充延迟（帧，5 秒）与总回充时长（帧，2 秒）
        /// </summary>
        public const int ShieldRechargeDelay = 300;
        public const int ShieldRechargeTime = 120;
        /// <summary>
        /// 受击时暂停回充的时长（帧，10 秒）：对齐灾厄 HitHurt 里对神器形态写死的 60*10；
        /// 上面的 ShieldRechargeDelay（5 秒）只在首次装备时使用
        /// </summary>
        public const int ShieldRechargeDelayOnHit = 600;
        // ── 生命周期方法 ──
        /// <summary>
        /// 物品外观动画：按灵魂类物品处理，6 帧竖直滚动、每 6 tick 换一帧
        /// </summary>
        public override void SetStaticDefaults()
        {
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(6, 6));   // 6 帧、每 6 tick 换一帧
            ItemID.Sets.AnimatesAsSoul[Type] = true;                             // 按灵魂类物品处理（浮动/发光表现）
        }
        /// <summary>
        /// 物品基础属性：32x40、饰品、价值 1 铂金 40 金、稀有度红色、月后自定义稀有度 21 级
        /// </summary>
        public override void SetDefaults()
        {
            Item.width = 32;                               // 贴图宽（像素）
            Item.height = 40;                              // 贴图高（像素）
            Item.accessory = true;                         // 作为饰品装备
            Item.value = Item.buyPrice(1, 50, 0, 0);       // 价值 1 铂金 50 金（对齐灾厄 Rarity12BuyPrice）
            Item.rare = ItemRarityID.Red;                  // 基础稀有度红色
            Item.GetGlobalItem<CalamityDemutationGlobalItem>().postMoonLordRarity = 21;   // 月后稀有度 21 级
        }
        /// <summary>
        /// 装备时置位 profanedSoulArtifact 标记（三守护者与护盾的结算依据）；
        /// hideVisual 时把护盾隐藏
        /// </summary>
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
            modPlayer.profanedSoulArtifact = true;             // 置位神器标记，供守护者召唤与加成结算读取
            modPlayer.profanedSoulShieldVisible = !hideVisual;  // 隐藏装备时不显示护盾
        }
        /// <summary>
        /// 戴在时装栏时同样让护盾可见（但不提供任何加成）
        /// </summary>
        public override void UpdateVanity(Player player)
        {
            player.GetModPlayer<CalamityDemutationPlayer>().profanedSoulShieldVisible = true;
        }
        // ── 护盾可见表现 ──
        /// <summary>
        /// 加载时把护盾绘制挂到 AfterPlayers 绘制层（玩家之后，对齐灾厄；灾厄原版用 IL 钩子把护盾画在 Inferno Ring 之前）
        /// </summary>
        public override void Load()
        {
            GeneralDrawLayerSystem.OnDrawLayer += DrawShieldOnLayer;
        }
        /// <summary>
        /// 卸载时摘掉护盾绘制订阅，避免静态事件一直挂住本模组实例
        /// </summary>
        public override void Unload()
        {
            GeneralDrawLayerSystem.OnDrawLayer -= DrawShieldOnLayer;
        }
        /// <summary>
        /// 在 AfterPlayers 层（玩家绘制之后）为护盾可见的玩家绘制亵渎护罩，视觉与灾厄一致：
        /// 第一段用 RoverDriveShield 着色器把 Neurons2 噪声贴图扭曲成护罩气泡，
        /// 第二段把 GreyscaleOpenCircle 边框圆叠四层做双环发光。
        /// 与灾厄的区别：强度改由本工程护盾耐久占比驱动（并开方）；护盾的耐久/回充冷却条由 Systems/Cooldowns 那套 UI 负责。
        /// </summary>
        private static void DrawShieldOnLayer(GeneralDrawLayer layer)
        {
            if (layer != GeneralDrawLayer.AfterPlayers)
                return;
            Asset<Effect> shaderAsset = EffectLoader.RoverDriveShieldShader;
            if (shaderAsset == null || !shaderAsset.IsLoaded)
                return;
            Effect shieldEffect = shaderAsset.Value;
            Texture2D noiseTex = ModContent.Request<Texture2D>(CalamityDemutationConstant.Masking + "Neurons2").Value;
            Texture2D circleTex = ModContent.Request<Texture2D>(CalamityDemutationConstant.Masking + "GreyscaleOpenCircle").Value;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead || player.outOfRange)
                    continue;
                CalamityDemutationPlayer modPlayer = player.GetModPlayer<CalamityDemutationPlayer>();
                // 护盾不可见或耐久为空时不绘制（其他玩家的耐久未做网络同步，故实际只显示本地玩家自己的护罩）
                if (!modPlayer.profanedSoulShieldVisible || modPlayer.profanedSoulShieldDurability <= 0)
                    continue;
                // 耐久占比：决定护罩亮度。水晶档上限 200、神器档 25，按对应常量归一化（否则占比恒 >1 会一直满亮）
                int shieldMax = modPlayer.profanedCrystal ? ProfanedSoulCrystal.ShieldDurabilityMax : ShieldDurabilityMax;
                // 强度曲线开方（对齐原版 visualShieldStrength = Pow(ratio, 0.5f)）：剩一半耐久时亮度仍有约 0.71，不会一下暗掉
                float strength = MathF.Pow(modPlayer.profanedSoulShieldDurability / (float)shieldMax, 0.5f);
                int whoAmI = player.whoAmI;
                // 尺寸随时间轻微脉动，并按玩家索引错开相位（对齐灾厄写法）
                float scale = 0.15f + 0.03f * (0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 0.5f + whoAmI * 0.2f));
                // 噪声叠加层的缩放独立脉动，与护罩本体不同步（对齐灾厄写法）
                float noiseScale = MathHelper.Lerp(0.4f, 0.8f, MathF.Sin(Main.GlobalTimeWrappedHourly * 0.3f) * 0.5f + 0.5f);
                float baseShieldOpacity = 0.9f + 0.1f * MathF.Sin(Main.GlobalTimeWrappedHourly * 1.95f);
                float finalShieldOpacity = baseShieldOpacity * MathHelper.Lerp(0.5f, 1f, strength);
                // 盾色随四态（对齐原版 UpdateDrawParameter_ProfanedShield）：Buffs 档及以上取昼夜插值色
                // （水晶四态各自有色，且随昼夜平滑过渡），Vanity 档（仅神器、或水晶戴在时装栏）取该档固定色
                int pscState = (int)ProfanedSoulCrystal.GetPscStateFor(player);
                Color shieldColor = pscState >= (int)ProfanedSoulCrystal.ProfanedSoulCrystalState.Buffs
                    ? ProfanedSoulCrystal.GetLerpedColorForPsc(player)
                    : ProfanedSoulCrystal.GetColorForPsc(pscState, Main.dayTime);
                Color primaryEdgeColor = new Color(230, 199, 102) * 0.8f;
                Color secondaryEdgeColor = new Color(249, 231, 217) * 0.8f;
                Color edgeColor = CDUtil.MulticolorLerp(Main.GlobalTimeWrappedHourly * 0.2f, primaryEdgeColor, secondaryEdgeColor);
                shieldEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.058f);   // 噪声滚动速度
                shieldEffect.Parameters["blowUpPower"].SetValue(2.8f);
                shieldEffect.Parameters["blowUpSize"].SetValue(0.4f);
                shieldEffect.Parameters["noiseScale"].SetValue(noiseScale);
                shieldEffect.Parameters["shieldOpacity"].SetValue(finalShieldOpacity);
                shieldEffect.Parameters["shieldEdgeBlendStrenght"].SetValue(4f);   // 着色器内拼写即为 Strenght
                shieldEffect.Parameters["shieldColor"].SetValue(shieldColor.ToVector3());
                shieldEffect.Parameters["shieldEdgeColor"].SetValue(edgeColor.ToVector3());
                Vector2 drawPos = player.MountedCenter - Main.screenPosition + new Vector2(0f, player.gfxOffY);
                // 第一段：噪声气泡（着色器按 uv 把噪声扭曲成护罩，噪声贴图本身作为采样源）
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, shieldEffect, Main.Transform);
                Main.spriteBatch.Draw(noiseTex, drawPos, null, Color.White, 0f, noiseTex.Size() / 2f, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                // 第二段：边框圆环（叠四层、两两同尺寸，形成双环发光）
                float shieldScale = scale * 1.75f;
                Rectangle shieldFrame = circleTex.Frame();
                Vector2 origin = shieldFrame.Size() * 0.5f;
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
                Main.spriteBatch.Draw(circleTex, drawPos, shieldFrame, shieldColor * 0.5f, player.fullRotation, origin, shieldScale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(circleTex, drawPos, shieldFrame, secondaryEdgeColor * 0.5f, player.fullRotation, origin, shieldScale * 0.95f, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(circleTex, drawPos, shieldFrame, shieldColor * 0.5f, player.fullRotation, origin, shieldScale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(circleTex, drawPos, shieldFrame, secondaryEdgeColor * 0.5f, player.fullRotation, origin, shieldScale * 0.95f, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
            }
        }
        /// <summary>
        /// 注册配方：现代版灾厄用 ExodiumCluster/Havocplate/DivineGeode 于秘银砧合成；
        /// 经典版灾厄没有前两者，故镜像经典版自带神器的配方（Cinderplate/CoreofCinder/DivineGeode/ExodiumClusterOre）于恶魔祭坛合成
        /// </summary>
        public override void AddRecipes()
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamity))
            {
                // 现代版灾厄：ExodiumCluster×25 + Havocplate×25 + DivineGeode×5（秘银砧）
                if (calamity.TryFind<ModItem>("ExodiumCluster", out ModItem exodiumCluster)
                    && calamity.TryFind<ModItem>("Havocplate", out ModItem havocplate)
                    && calamity.TryFind<ModItem>("DivineGeode", out ModItem divineGeode))
                {
                    Recipe recipe = CreateRecipe();
                    recipe.AddIngredient(exodiumCluster.Type, 25);
                    recipe.AddIngredient(havocplate.Type, 25);
                    recipe.AddIngredient(divineGeode.Type, 5);
                    recipe.AddTile(TileID.MythrilAnvil);
                    recipe.Register();
                }
            }
            if (ModLoader.TryGetMod("CalamityModClassicPreTrailer", out Mod calamity1))
            {
                // 经典版灾厄：Cinderplate×5 + CoreofCinder + DivineGeode×5 + ExodiumClusterOre×15（恶魔祭坛）
                if (calamity1.TryFind<ModItem>("Cinderplate", out ModItem cinderplate)
                    && calamity1.TryFind<ModItem>("CoreofCinder", out ModItem coreofCinder)
                    && calamity1.TryFind<ModItem>("DivineGeode", out ModItem classicDivineGeode)
                    && calamity1.TryFind<ModItem>("ExodiumClusterOre", out ModItem exodiumClusterOre))
                {
                    Recipe recipe1 = CreateRecipe();
                    recipe1.AddIngredient(cinderplate.Type, 5);
                    recipe1.AddIngredient(coreofCinder.Type);
                    recipe1.AddIngredient(classicDivineGeode.Type, 5);
                    recipe1.AddIngredient(exodiumClusterOre.Type, 15);
                    recipe1.AddTile(TileID.DemonAltar);
                    recipe1.Register();
                }
            }
        }
    }
}
