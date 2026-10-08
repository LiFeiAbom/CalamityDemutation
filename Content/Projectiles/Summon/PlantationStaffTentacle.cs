using CalamityDemutation.Content.Items.Weapons.Summon;
using CalamityDemutation.Players;
using CalamityDemutation.Systems;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityDemutation.Content.Projectiles.Summon
{
    /// <summary>
    /// 苍华之庭·触手（PlantationStaffTentacle，移植自灾厄 2.0.3.9 的同名弹幕）——
    /// 树灵进入冲撞时挂到它身上的 6 条触手：先缠在宿主身上（用 `PlantationStaffTentacleChain` 画链条），
    /// 宿主一离开冲撞态就脱落、转为自主追击（前 30 帧沿当前朝向直飞，之后转向追敌）。
    /// </summary>
    internal class PlantationStaffTentacle:ModProjectile
    {
        public Player Owner => Main.player[Projectile.owner];
        public CalamityDemutationPlayer ModdedOwner => Owner.GetModPlayer<CalamityDemutationPlayer>();
        public NPC Target => Projectile.Center.MinionHoming(PlantationStaff.EnemyDistanceDetection, Owner);
        public Projectile MainMinion => Main.projectile[(int)MainMinionIndex];

        public ref float TentacleIndex => ref Projectile.ai[0];
        public ref float MainMinionIndex => ref Projectile.ai[1];
        public ref float AITimer => ref Projectile.localAI[0];
        /// <summary>0 缠在宿主身上 / 1 自主追击</summary>
        public enum AIState
        {
            Attached,
            Seeking
        }
        public AIState State
        {
            get => (AIState)Projectile.ai[2];
            set => Projectile.ai[2] = (int)value;
        }

        /// <summary>相对宿主的挂点偏移（生成时按序号算好，需同步）</summary>
        public Vector2 DesiredLocation;

        /// <summary>4 帧动画 + 残影缓存</summary>
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 4;
        }
        /// <summary>基础属性（照源）：22×22、无限穿透、不撞地形、逐敌冷却 -1（即不重复吃同一敌人）</summary>
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Summon;
            Projectile.localNPCHitCooldown = -1;
            Projectile.width = Projectile.height = 22;
            Projectile.penetrate = -1;

            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.netImportant = true;
        }
        #region Variable Syncing
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(AITimer);
            writer.WritePackedVector2(DesiredLocation);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            AITimer = reader.ReadSingle();
            DesiredLocation = reader.ReadPackedVector2();
        }
        #endregion
        public override void AI()
        {
            CheckMinionExistence();
            DoAnimation();

            switch (State)
            {
                case AIState.Attached:
                    AttachedState();
                    break;
                case AIState.Seeking:
                    SeekingState();
                    break;
            }
        }
        #region AI Methods
        /// <summary>缠在宿主身上：按插值贴向挂点；一旦宿主不再处于冲撞态就脱落转追击</summary>
        private void AttachedState()
        {
            AITimer++;
            float interpolant = Utils.Remap(AITimer, 0f, PlantationStaff.TimeBeforeRamming, 0f, .4f);

            Projectile.Center = Vector2.Lerp(Projectile.Center, MainMinion.Center + DesiredLocation, interpolant);
            Projectile.rotation = (Projectile.Center - MainMinion.Center).ToRotation();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                // 源这里读 `proj.ModProjectile<PlantationStaffSummon>().State`；tML 的 ModProjectile 是**每类型单例**，
                // 跨弹幕读它的属性会读到"最后一个被处理的弹幕"身上，故这里直接读宿主弹幕的 ai[0]（State 的真身）。
                if (proj is null || !proj.active || proj.owner != Owner.whoAmI || proj.type != ModContent.ProjectileType<PlantationStaffSummon>() || proj.ai[0] == (float)PlantationStaffSummon.AIState.Ramming)
                    continue;

                State = AIState.Seeking;
                AITimer = 0f;
                Projectile.velocity = Vector2.Zero;
                Projectile.penetrate = 1;

                for (int dustIndex = 0; dustIndex < 20; dustIndex++)
                    Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.JunglePlants);

                SoundEngine.PlaySound(SoundID.NPCDeath1, Projectile.Center);

                Projectile.netUpdate = true;
            }
        }
        /// <summary>自主追击：前 30 帧沿脱落时的朝向直飞，之后按 35:1 的惯性转向追敌；没目标就消失</summary>
        private void SeekingState()
        {
            if (Target is not null)
            {
                AITimer++;

                if (AITimer <= 30f)
                {
                    Projectile.velocity = Projectile.rotation.ToRotationVector2() * 10f;
                }
                else
                {
                    Projectile.velocity = (Projectile.velocity * 35f + Projectile.SafeDirectionTo(Target.Center) * PlantationStaff.TentacleSpeed) / 36f;
                    Projectile.rotation = Projectile.velocity.ToRotation();
                }
            }
            else
                Projectile.Kill();
        }
        /// <summary>宿主索引非法或宿主已不是树灵时自毁；召唤标志有效时续命</summary>
        private void CheckMinionExistence()
        {
            if (Projectile.ai[1] < 0 || Projectile.ai[1] >= Main.maxProjectiles)
            {
                Projectile.Kill();
                return;
            }

            // If something has gone wrong with either the tentacle or the host plant, destroy the projectile.
            if (Projectile.type != ModContent.ProjectileType<PlantationStaffTentacle>() || !MainMinion.active || MainMinion.type != ModContent.ProjectileType<PlantationStaffSummon>())
            {
                Projectile.Kill();
                return;
            }

            if (ModdedOwner.plantationSummon)
                Projectile.timeLeft = 2;
        }
        /// <summary>4 帧动画（每 3 帧推一帧）</summary>
        private void DoAnimation()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 3)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
        }
        #endregion
        /// <summary>生成时按序号算好挂在宿主身上的方位（6 等分再随机抖一点）并同步</summary>
        public override void OnSpawn(IEntitySource source)
        {
            DesiredLocation = (MathHelper.TwoPi / 6f * TentacleIndex).ToRotationVector2().RotatedByRandom(MathHelper.PiOver4 / 1.5f) * 100f;
            Projectile.netUpdate = true;
        }
        /// <summary>只有自主追击阶段才造成伤害（源写法：追击返回 null = 按默认结算，缠身阶段 false）</summary>
        public override bool? CanDamage() => (State == AIState.Seeking) ? null : false;
        /// <summary>消亡时喷一圈丛林植物尘</summary>
        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
                Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.JunglePlants);
        }
        /// <summary>自绘：缠身阶段沿两心连线铺满链条贴图；追击阶段叠残影（性能模式下不叠）</summary>
        public override bool PreDraw(ref Color lightColor)
        {
            if (MainMinionIndex < 0 || MainMinionIndex >= Main.maxProjectiles)
                return false;

            // If something has gone wrong with either the tentacle or the host plant, return.
            if (Type != ModContent.ProjectileType<PlantationStaffTentacle>() || !MainMinion.active || MainMinion.type != ModContent.ProjectileType<PlantationStaffSummon>())
                return false;

            if (State == AIState.Attached)
            {
                Vector2 source = MainMinion.Center;
                Texture2D chain = ModContent.Request<Texture2D>("CalamityDemutation/Content/Projectiles/Summon/PlantationStaffTentacleChain").Value;
                Vector2 goal = Projectile.Center;
                Rectangle? sourceRectangle = null;
                float textureHeight = chain.Height;
                Vector2 drawVector = source - goal;
                float rotation = drawVector.ToRotation() - MathHelper.PiOver2;
                bool shouldDraw = true;
                if (float.IsNaN(goal.X) && float.IsNaN(goal.Y))
                {
                    shouldDraw = false;
                }
                if (float.IsNaN(drawVector.X) && float.IsNaN(drawVector.Y))
                {
                    shouldDraw = false;
                }
                while (shouldDraw)
                {
                    if (drawVector.Length() < textureHeight + 1f)
                    {
                        shouldDraw = false;
                    }
                    else
                    {
                        Vector2 value2 = drawVector;
                        value2.Normalize();
                        goal += value2 * textureHeight;
                        drawVector = source - goal;
                        Color color = Lighting.GetColor((int)goal.X / 16, (int)(goal.Y / 16f));
                        Main.EntitySpriteDraw(chain, goal - Main.screenPosition, sourceRectangle, color, rotation, chain.Size() / 2f, 1f, SpriteEffects.None, 0);
                    }
                }
            }
            else if (ConfigSystem.Instance?.PerformanceMode != true)
                CDUtil.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor);

            return true;
        }
        /// <summary>
        /// 由**最后一条**触手（序号 5，也是弹幕数组里最后一个）补画宿主本体，
        /// 这样触手链条会被树灵的贴图盖住（源写法）。
        /// </summary>
        public override void PostDraw(Color lightColor)
        {
            // Only 1 tentacle needs to draw this, the last one spawned because it's latest in the projectile array.
            if (TentacleIndex < 5)
                return;

            if (MainMinionIndex < 0 || MainMinionIndex >= Main.maxProjectiles)
                return;

            // If something has gone wrong with either the tentacle or the host plant, return.
            if (Projectile.type != ModContent.ProjectileType<PlantationStaffTentacle>() || !MainMinion.active || MainMinion.type != ModContent.ProjectileType<PlantationStaffSummon>())
                return;

            Texture2D texture = TextureAssets.Projectile[MainMinion.type].Value;
            int height = texture.Height / Main.projFrames[MainMinion.type];
            int frameHeight = height * MainMinion.frame;
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (MainMinion.spriteDirection == -1)
                spriteEffects = SpriteEffects.FlipHorizontally;
            Color color = Lighting.GetColor((int)MainMinion.Center.X / 16, (int)(MainMinion.Center.Y / 16f));

            Main.EntitySpriteDraw(texture, MainMinion.Center - Main.screenPosition + new Vector2(0f, MainMinion.gfxOffY), new Microsoft.Xna.Framework.Rectangle?(new Rectangle(0, frameHeight, texture.Width, height)), color, MainMinion.rotation, new Vector2((float)texture.Width / 2f, (float)height / 2f), MainMinion.scale, spriteEffects, 0);
        }
    }
}
