using System;
using System.Collections.Generic;
using CalamityDemutation.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;
namespace CalamityDemutation.Graphics.Primitives
{
    /// <summary>
    /// GPU 图元（Primitive）拖尾渲染系统（完整移植自灾厄 1.4.4 的 PrimitiveRenderer）。
    /// 通过 <see cref="RenderTrail(Vector2[], PrimitiveSettings, int?)"/> 方法渲染拖尾：<br/>
    /// 第一个参数是拖尾使用的历史位置（世界坐标，自动减去 <see cref="Main.screenPosition"/>）；<br/>
    /// 第二个参数是 <see cref="PrimitiveSettings"/> 配置结构，用于定制拖尾外观；<br/>
    /// 第三个参数控制拖尾生成的采样点数。<br/>
    /// 注意：本模组未移植像素化渲染子系统，<see cref="PrimitiveSettings.Pixelate"/> 必须保持 false。
    /// </summary>
    [Autoload(Side = ModSide.Client)]
    public sealed class PrimitiveRenderer : ModSystem
    {
        // ── 常量 ──
        /// <summary>
        /// 判定浮点相等的极小阈值，供各处的除零与退化保护使用
        /// </summary>
        public const float Epsilon = 1e-6f;
        /// <summary>
        /// 索引缓冲容量上限
        /// </summary>
        private const short MaxIndices = 8192;
        /// <summary>
        /// 单条拖尾的最大采样点数
        /// </summary>
        private const short MaxPositions = 1000;
        /// <summary>
        /// 顶点缓冲容量上限
        /// </summary>
        private const short MaxVertices = 3072;
        // ── 静态字段 ──
        /// <summary>
        /// 本次渲染实际使用的图元拓扑（会因封口需求被提升为 TriangleList）
        /// </summary>
        private static PrimitiveTopology ActiveTopology;
        /// <summary>
        /// 样条控制点缓存，复用以避免每次渲染都分配列表
        /// </summary>
        private static readonly List<Vector2> ControlPointsCache = new(MaxPositions);
        /// <summary>
        /// 尾端封口中心点索引，-1 表示未生成
        /// </summary>
        private static short EndCapCenterIndex;
        /// <summary>
        /// 动态索引缓冲
        /// </summary>
        private static DynamicIndexBuffer IndexBuffer;
        /// <summary>
        /// 当前写入到 MainIndices 的位置
        /// </summary>
        private static short IndicesIndex;
        /// <summary>
        /// 各采样点沿拖尾的完成比例（0-1），由 AssignCompletionData 填充
        /// </summary>
        private static float[] MainCompletionRatios;
        /// <summary>
        /// 索引暂存数组
        /// </summary>
        private static short[] MainIndices;
        /// <summary>
        /// 各采样点的法线
        /// </summary>
        private static Vector2[] MainNormals;
        /// <summary>
        /// 各采样点的屏幕坐标位置
        /// </summary>
        private static Vector2[] MainPositions;
        /// <summary>
        /// 当前次渲染使用的设置
        /// </summary>
        private static PrimitiveSettings MainSettings;
        /// <summary>
        /// 各采样点的切线
        /// </summary>
        private static Vector2[] MainTangents;
        /// <summary>
        /// 顶点暂存数组（每点左右各一个顶点）
        /// </summary>
        private static VertexPosition2DColorTexture[] MainVertices;
        /// <summary>
        /// 非平滑路径下记录有效点原始下标用的暂存数组
        /// </summary>
        private static int[] NonSmoothIndexScratch;
        /// <summary>
        /// 当前写入到 MainPositions 的位置
        /// </summary>
        private static short PositionsIndex;
        /// <summary>
        /// 首端封口中心点索引，-1 表示未生成
        /// </summary>
        private static short StartCapCenterIndex;
        /// <summary>
        /// 整条拖尾的总长度（像素），距离模式下换算 U 坐标用
        /// </summary>
        private static float TotalTrailLength;
        /// <summary>
        /// 动态顶点缓冲
        /// </summary>
        private static DynamicVertexBuffer VertexBuffer;
        /// <summary>
        /// 当前写入到 MainVertices 的位置
        /// </summary>
        private static short VerticesIndex;
        /// <summary>
        /// 调试线框用的基础效果
        /// </summary>
        private static BasicEffect WireframeEffect;
        /// <summary>
        /// 当前已写入的线框顶点数
        /// </summary>
        private static int WireframeVertexCount;
        /// <summary>
        /// 线框顶点暂存数组
        /// </summary>
        private static VertexPositionColor[] WireframeVertices;
        // ── 生命周期方法 ──
        /// <summary>
        /// ILoadable 加载回调：把初始化动作投递到主线程执行（图形设备资源必须在主线程创建）。
        /// 内容为分配各类顶点/索引/切线与法线缓存数组，并用 ??= 惰性创建动态顶点缓冲、
        /// 动态索引缓冲以及线框 BasicEffect（已存在则复用，便于热重载）
        /// </summary>
        public override void OnModLoad()
        {
            Main.QueueMainThreadAction(() =>
            {
                MainPositions = new Vector2[MaxPositions];
                MainVertices = new VertexPosition2DColorTexture[MaxVertices];
                MainIndices = new short[MaxIndices];
                MainCompletionRatios = new float[MaxPositions];
                MainTangents = new Vector2[MaxPositions];
                MainNormals = new Vector2[MaxPositions];
                WireframeVertices = new VertexPositionColor[MaxPositions * 8];
                NonSmoothIndexScratch = new int[MaxPositions];
                VertexBuffer ??= new DynamicVertexBuffer(Main.instance.GraphicsDevice, VertexPosition2DColorTexture.VertexDeclaration2D, MaxVertices, BufferUsage.WriteOnly);
                IndexBuffer ??= new DynamicIndexBuffer(Main.instance.GraphicsDevice, IndexElementSize.SixteenBits, MaxIndices, BufferUsage.WriteOnly);
                WireframeEffect ??= new BasicEffect(Main.instance.GraphicsDevice)
                {
                    VertexColorEnabled = true,
                    TextureEnabled = false,
                    LightingEnabled = false
                };
            });
        }
        /// <summary>
        /// ILoadable 卸载回调：同样投递到主线程，把所有缓存数组置空并 Dispose 顶点缓冲、
        /// 索引缓冲与线框效果，防止卸载后残留图形设备资源
        /// </summary>
        public override void OnModUnload()
        {
            Main.QueueMainThreadAction(() =>
            {
                MainPositions = null;
                MainVertices = null;
                MainIndices = null;
                MainCompletionRatios = null;
                MainTangents = null;
                MainNormals = null;
                WireframeVertices = null;
                NonSmoothIndexScratch = null;
                VertexBuffer?.Dispose();
                VertexBuffer = null;
                IndexBuffer?.Dispose();
                IndexBuffer = null;
                WireframeEffect?.Dispose();
                WireframeEffect = null;
            });
        }
        // ── 公开方法 ──
        /// <summary>
        /// 渲染一条 Primitive 拖尾。
        /// </summary>
        /// <param name="positions">拖尾使用的位置列表（世界坐标，自动减去 <see cref="Main.screenPosition"/>）。使用平滑至少需要 4 个点。</param>
        /// <param name="settings">拖尾绘制设置。</param>
        /// <param name="pointsToCreate">生成的采样点数：越多越精细但越耗性能。默认等于提供的点数，不建议超过 100。</param>
        public static void RenderTrail(List<Vector2> positions, PrimitiveSettings settings, int? pointsToCreate = null) => RenderTrail(positions.ToArray(), settings, pointsToCreate);
        /// <summary>
        /// 渲染一条 Primitive 拖尾。
        /// </summary>
        /// <param name="positions">拖尾使用的位置数组（世界坐标，自动减去 <see cref="Main.screenPosition"/>）。使用平滑至少需要 4 个点。</param>
        /// <param name="settings">拖尾绘制设置。</param>
        /// <param name="pointsToCreate">生成的采样点数：越多越精细但越耗性能。默认等于提供的点数，不建议超过 100。</param>
        public static void RenderTrail(Vector2[] positions, PrimitiveSettings settings, int? pointsToCreate = null)
        {
            PerformPixelationSafetyChecks(settings);
            // 点太少画不出任何东西时直接返回
            if (positions.Length <= 2)
                return;
            // 点太多超过上限时直接返回
            if (positions.Length > MaxPositions)
                return;
            int desiredPointCount = pointsToCreate ?? positions.Length;
            desiredPointCount = Math.Clamp(desiredPointCount, 2, MaxPositions);
            MainSettings = settings;
            ActiveTopology = settings.CapStyle != PrimitiveCapStyle.None ? PrimitiveTopology.TriangleList : settings.Topology;
            // 无法构建有效位置轨迹时不再继续渲染
            if (!AssignPointsRectangleTrail(positions, settings, desiredPointCount))
                return;
            // 只有一个点或更少的轨迹没有可连接的线段，无法构成拖尾
            AssignCompletionData();
            if (PositionsIndex <= 2)
                return;
            AssignVerticesRectangleTrail();
            AssignIndices();
            // 一切就绪，正式渲染
            PrivateRender();
            return;
        }
        // ── 私有工具 ──
        /// <summary>
        /// 为丝带两端生成封口中心点顶点（CapStyle 非 None 且拓扑为 TriangleList 时）。
        /// 拓扑为三角带时无法插入独立中心点，直接跳过。
        /// </summary>
        private static void AddCaps()
        {
            if (MainSettings.CapStyle == PrimitiveCapStyle.None || PositionsIndex <= 0)
                return;
            if (ActiveTopology == PrimitiveTopology.TriangleStrip)
                return;
            StartCapCenterIndex = TryCreateCapVertex(0);
            if (PositionsIndex > 1)
                EndCapCenterIndex = TryCreateCapVertex(PositionsIndex - 1);
        }
        /// <summary>
        /// 向索引缓冲追加一个三角形（空间不足时静默丢弃）
        /// </summary>
        private static void AddTriangle(short i0, short i1, short i2)
        {
            if (IndicesIndex + 2 >= MaxIndices)
                return;
            MainIndices[IndicesIndex++] = i0;
            MainIndices[IndicesIndex++] = i1;
            MainIndices[IndicesIndex++] = i2;
        }
        /// <summary>
        /// 按两个顶点索引取位置追加一条线框线段（越界或缓冲不足时静默跳过）
        /// </summary>
        private static void AddWireframeLineFromIndices(int startVertexIndex, int endVertexIndex, Color color)
        {
            if (WireframeVertexCount + 1 >= WireframeVertices.Length)
                return;
            int vertexCount = VerticesIndex;
            if (startVertexIndex >= vertexCount || endVertexIndex >= vertexCount)
                return;
            ref readonly VertexPosition2DColorTexture start = ref MainVertices[startVertexIndex];
            ref readonly VertexPosition2DColorTexture end = ref MainVertices[endVertexIndex];
            WireframeVertices[WireframeVertexCount++] = new VertexPositionColor(new Vector3(start.Position, 0f), color);
            WireframeVertices[WireframeVertexCount++] = new VertexPositionColor(new Vector3(end.Position, 0f), color);
        }
        /// <summary>
        /// 为封口追加两个三角形：首端用 StartCapCenterIndex 连到第 0/1 号顶点，
        /// 尾端用 EndCapCenterIndex 连到最后一对左右顶点（未生成中心点时跳过对应一端）
        /// </summary>
        private static void AppendCapTriangles()
        {
            if (MainSettings.CapStyle == PrimitiveCapStyle.None)
                return;
            if (PositionsIndex <= 0)
                return;
            if (StartCapCenterIndex >= 0)
                AddTriangle((short)(StartCapCenterIndex), 0, 1);
            if (EndCapCenterIndex >= 0)
            {
                short leftIndex = (short)((PositionsIndex - 1) * 2);
                short rightIndex = (short)(leftIndex + 1);
                AddTriangle(leftIndex, rightIndex, EndCapCenterIndex);
            }
        }
        /// <summary>
        /// 计算各采样点沿拖尾的完成比例：先累加相邻点距离得到总长，
        /// 再统一归一化到 0-1（硬件支持时用 System.Numerics 向量化加速），末点强制为 1；
        /// 总长过小（退化轨迹）则除首点外全部置 0。
        /// </summary>
        private static void AssignCompletionData()
        {
            TotalTrailLength = 0f;
            if (PositionsIndex <= 0)
                return;
            MainCompletionRatios[0] = 0f;
            for (int i = 1; i < PositionsIndex; i++)
            {
                float segmentLength = Vector2.Distance(MainPositions[i], MainPositions[i - 1]);
                TotalTrailLength += segmentLength;
                MainCompletionRatios[i] = TotalTrailLength;
            }
            if (PositionsIndex <= 0)
                return;
            if (TotalTrailLength > Epsilon)
            {
                float inverseTotal = 1f / TotalTrailLength;
                int lastIndex = PositionsIndex - 1;
                if (System.Numerics.Vector.IsHardwareAccelerated && PositionsIndex - 1 >= System.Numerics.Vector<float>.Count)
                {
                    var scale = new System.Numerics.Vector<float>(inverseTotal);
                    int i = 1;
                    int upperBound = lastIndex - System.Numerics.Vector<float>.Count + 1;
                    for (; i <= upperBound; i += System.Numerics.Vector<float>.Count)
                    {
                        var values = new System.Numerics.Vector<float>(MainCompletionRatios, i);
                        (values * scale).CopyTo(MainCompletionRatios, i);
                    }
                    for (; i <= lastIndex; i++)
                        MainCompletionRatios[i] *= inverseTotal;
                }
                else
                {
                    for (int i = 1; i < PositionsIndex; i++)
                        MainCompletionRatios[i] *= inverseTotal;
                }
                MainCompletionRatios[PositionsIndex - 1] = 1f;
            }
            else
            {
                for (int i = 1; i < PositionsIndex; i++)
                    MainCompletionRatios[i] = 0f;
            }
        }
        /// <summary>
        /// 按当前拓扑生成索引缓冲：三角带直接用顺序索引；
        /// 三角列表则把每段展开成 2 个三角形（6 个索引）的小矩形，最后追加封口三角形。
        /// </summary>
        private static void AssignIndices()
        {
            IndicesIndex = 0;
            if (ActiveTopology == PrimitiveTopology.TriangleStrip)
            {
                for (short i = 0; i < VerticesIndex && IndicesIndex < MaxIndices; i++)
                    MainIndices[IndicesIndex++] = i;
                return;
            }
            // 这里的做法本质上是把顶点列表中的每个点表示为索引，
            // 这些索引拼合成代表拖尾每段的小矩形（每段 2 个三角形，共 6 个索引）。
            // 下面的逻辑决定哪些索引互相连接。
            for (short i = 0; i < PositionsIndex - 2 && IndicesIndex + 5 < MaxIndices; i++)
            {
                short connectToIndex = (short)(i * 2);
                MainIndices[IndicesIndex] = connectToIndex;
                IndicesIndex++;
                MainIndices[IndicesIndex] = (short)(connectToIndex + 1);
                IndicesIndex++;
                MainIndices[IndicesIndex] = (short)(connectToIndex + 2);
                IndicesIndex++;
                MainIndices[IndicesIndex] = (short)(connectToIndex + 2);
                IndicesIndex++;
                MainIndices[IndicesIndex] = (short)(connectToIndex + 1);
                IndicesIndex++;
                MainIndices[IndicesIndex] = (short)(connectToIndex + 3);
                IndicesIndex++;
            }
            AppendCapTriangles();
        }
        /// <summary>
        /// 生成拖尾的采样点位置，结果写入 MainPositions 并把 PositionsIndex 设为实际点数；
        /// 返回 false 表示有效点不足、无法构成拖尾。三条路径：
        /// Smoothen 为假时按点数在有效点之间线性重映射；
        /// SmoothingSegments &gt; 0 时逐控制边按指定细分量采样；
        /// 否则走旧版行为：按点数在整个控制多边形上做样条插值。
        /// </summary>
        private static bool AssignPointsRectangleTrail(Vector2[] positions, PrimitiveSettings settings, int pointsToCreate)
        {
            // 除非显式要求，否则不平滑点位
            if (!settings.Smoothen)
            {
                PositionsIndex = 0;
                int validCount = 0;
                for (int i = 0; i < positions.Length; i++)
                {
                    if (positions[i] == Vector2.Zero)
                        continue;
                    NonSmoothIndexScratch[validCount++] = i;
                }
                if (validCount <= 2)
                    return false;
                int lastIndex = validCount - 1;
                float inversePointCount = 1f / (pointsToCreate - 1);
                float lastIndexFloat = lastIndex;
                // 在指定长度上重新映射原始位置，不产生额外分配
                for (int i = 0; i < pointsToCreate; i++)
                {
                    float completionRatio = i * inversePointCount;
                    float scaledIndex = completionRatio * lastIndexFloat;
                    int currentIndex = (int)scaledIndex;
                    int nextIndex = Math.Min(currentIndex + 1, lastIndex);
                    float localInterpolant = scaledIndex - currentIndex;
                    Vector2 currentPoint = positions[NonSmoothIndexScratch[currentIndex]];
                    Vector2 nextPoint = positions[NonSmoothIndexScratch[nextIndex]];
                    Vector2 interpolatedWorld = Vector2.Lerp(currentPoint, nextPoint, localInterpolant);
                    Vector2 finalPos = interpolatedWorld - Main.screenPosition;
                    if (settings.OffsetFunction != null)
                        finalPos += settings.OffsetFunction(completionRatio, interpolatedWorld);
                    MainPositions[PositionsIndex++] = finalPos;
                }
                return true;
            }
            PositionsIndex = 0;
            // 创建样条曲线的控制点
            List<Vector2> controlPoints = ControlPointsCache;
            controlPoints.Clear();
            for (int i = 0; i < positions.Length; i++)
            {
                if (positions[i] == Vector2.Zero)
                    continue;
                float completionRatio = i / (float)positions.Length;
                Vector2 offset = -Main.screenPosition;
                if (settings.OffsetFunction != null)
                    offset += settings.OffsetFunction(completionRatio, positions[i]);
                controlPoints.Add(positions[i] + offset);
            }
            int controlCount = controlPoints.Count;
            if (controlCount <= 1)
            {
                controlPoints.Clear();
                return false;
            }
            int segmentCount = controlCount - 1;
            if (settings.SmoothingSegments > 0)
            {
                int segmentsPerEdge = Math.Max(1, settings.SmoothingSegments);
                PrimitiveSmoothingType smoothingType = settings.SmoothingType;
                for (int segment = 0; segment < segmentCount; segment++)
                {
                    Vector2 p0 = controlPoints[Math.Max(segment - 1, 0)];
                    Vector2 p1 = controlPoints[segment];
                    Vector2 p2 = controlPoints[segment + 1];
                    Vector2 p3 = controlPoints[Math.Min(segment + 2, controlCount - 1)];
                    for (int step = segment == 0 ? 0 : 1; step <= segmentsPerEdge; step++)
                    {
                        if (PositionsIndex >= MaxPositions - 1)
                        {
                            controlPoints.Clear();
                            return true;
                        }
                        float localT = step / (float)segmentsPerEdge;
                        Vector2 point = EvaluateCurve(smoothingType, p0, p1, p2, p3, localT, settings);
                        MainPositions[PositionsIndex++] = point;
                    }
                }
                controlPoints.Clear();
                return true;
            }
            // 旧版行为：按请求的点数使用 Catmull-Rom 风格插值采样
            PositionsIndex = 0;
            float controlCountMinusOne = controlCount - 1f;
            PrimitiveSmoothingType legacyType = settings.SmoothingType;
            for (int j = 0; j < pointsToCreate; j++)
            {
                if (PositionsIndex >= MaxPositions - 1)
                    break;
                float splineInterpolant = j / (float)pointsToCreate;
                float positionOnCurve = splineInterpolant * controlCountMinusOne;
                int localSplineIndex = (int)positionOnCurve;
                float localSplineInterpolant = positionOnCurve - localSplineIndex;
                Vector2 p0 = controlPoints[Math.Max(localSplineIndex - 1, 0)];
                Vector2 p1 = controlPoints[localSplineIndex];
                Vector2 p2 = controlPoints[Math.Min(localSplineIndex + 1, controlCount - 1)];
                Vector2 p3 = controlPoints[Math.Min(localSplineIndex + 2, controlCount - 1)];
                MainPositions[PositionsIndex] = EvaluateCurve(legacyType, p0, p1, p2, p3, localSplineInterpolant, settings);
                PositionsIndex++;
            }
            MainPositions[PositionsIndex] = controlPoints[controlCount - 1];
            PositionsIndex++;
            controlPoints.Clear();
            return true;
        }
        /// <summary>
        /// 为每个采样点生成左右两个顶点（写入 MainVertices 并推进 VerticesIndex），最后处理封口。
        /// 每点的颜色、宽度、U 坐标分别来自设置的三个回调；左右顶点关于法线对称偏移，
        /// 切线上反对称的 U 坐标（0.5±半宽/2）供着色器做边缘柔化，宽度写进 Z 分量供拖尾使用。
        /// </summary>
        private static void AssignVerticesRectangleTrail()
        {
            VerticesIndex = 0;
            StartCapCenterIndex = -1;
            EndCapCenterIndex = -1;
            ComputeFrameData();
            for (int i = 0; i < PositionsIndex; i++)
            {
                float completionRatio = GetCompletionRatioForIndex(i);
                float widthAtVertex = Math.Max(MainSettings.WidthFunction(completionRatio, MainPositions[i]), 0f);
                Color vertexColor = MainSettings.ColorFunction(completionRatio, MainPositions[i]);
                float textureU = ComputeTextureCoordinateForIndex(i, completionRatio);
                ComputeEdgePositions(i, widthAtVertex, out Vector2 left, out Vector2 right, out float effectiveHalfWidth);
                // 如请求则覆盖首顶点的左右侧位置
                if (i == 0 && MainSettings.InitialVertexPositionsOverride.HasValue && MainSettings.InitialVertexPositionsOverride.Value.Item1 != Vector2.Zero && MainSettings.InitialVertexPositionsOverride.Value.Item2 != Vector2.Zero)
                {
                    left = MainSettings.InitialVertexPositionsOverride.Value.Item1;
                    right = MainSettings.InitialVertexPositionsOverride.Value.Item2;
                    effectiveHalfWidth = Math.Max(Vector2.Distance(left, right) * 0.5f, Epsilon);
                }
                // 防止退化宽度
                effectiveHalfWidth = Math.Max(effectiveHalfWidth, Epsilon);
                Vector2 leftCurrentTextureCoord = new Vector2(textureU, 0.5f - effectiveHalfWidth * 0.5f);
                Vector2 rightCurrentTextureCoord = new Vector2(textureU, 0.5f + effectiveHalfWidth * 0.5f);
                MainVertices[VerticesIndex] = new VertexPosition2DColorTexture(left, vertexColor, leftCurrentTextureCoord, effectiveHalfWidth);
                VerticesIndex++;
                MainVertices[VerticesIndex] = new VertexPosition2DColorTexture(right, vertexColor, rightCurrentTextureCoord, effectiveHalfWidth);
                VerticesIndex++;
            }
            AddCaps();
        }
        /// <summary>
        /// 像素化模式专用的透视矩阵：直接按屏幕像素范围构造正交投影，视图矩阵取单位阵
        /// （像素化渲染经过缩放，常规的变换矩阵计算不适用）
        /// </summary>
        private static void CalcuatePixelatedPerspectiveMatrices(out Matrix viewMatrix, out Matrix projectionMatrix)
        {
            // 像素化渲染经过缩放，常规的变换矩阵计算不适用
            projectionMatrix = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);
            viewMatrix = Matrix.Identity;
        }
        /// <summary>
        /// 计算第 index 个采样点的左右边缘位置：先按连接方式求出该点的偏移方向，
        /// 再沿法线正负方向各偏移半个宽度。Flat 或首末点直接用法线偏移；
        /// Smooth 用前后与当前法线的平均；Miter 用相邻法线之和并按 JoinMiterLimit 夹取斜接长度。
        /// 宽度非正时左右点重合（退化为零宽）。effectiveHalfWidth 为实际采用的半宽。
        /// </summary>
        private static void ComputeEdgePositions(int index, float halfWidth, out Vector2 left, out Vector2 right, out float effectiveHalfWidth)
        {
            Vector2 currentPosition = MainPositions[index];
            if (halfWidth <= 0f)
            {
                left = currentPosition;
                right = currentPosition;
                effectiveHalfWidth = Epsilon;
                return;
            }
            Vector2 defaultNormal = MainNormals[index];
            if (defaultNormal.LengthSquared() <= Epsilon)
                defaultNormal = Vector2.UnitY;
            if (MainSettings.JoinStyle == PrimitiveJoinStyle.Flat || PositionsIndex <= 2 || index == 0 || index == PositionsIndex - 1)
            {
                Vector2 offset = defaultNormal * halfWidth;
                left = currentPosition - offset;
                right = currentPosition + offset;
                effectiveHalfWidth = halfWidth;
                return;
            }
            Vector2 prevNormal = MainNormals[Math.Max(index - 1, 0)];
            if (prevNormal.LengthSquared() <= Epsilon)
                prevNormal = defaultNormal;
            Vector2 nextNormal = MainNormals[Math.Min(index + 1, PositionsIndex - 1)];
            if (nextNormal.LengthSquared() <= Epsilon)
                nextNormal = defaultNormal;
            switch (MainSettings.JoinStyle)
            {
                case PrimitiveJoinStyle.Smooth:
                    {
                        Vector2 averageNormal = (prevNormal + defaultNormal + nextNormal) * (1f / 3f);
                        if (averageNormal.LengthSquared() <= Epsilon)
                            averageNormal = defaultNormal;
                        Vector2 offset = averageNormal.SafeNormalize(defaultNormal) * halfWidth;
                        left = currentPosition - offset;
                        right = currentPosition + offset;
                        effectiveHalfWidth = halfWidth;
                        return;
                    }
                case PrimitiveJoinStyle.Miter:
                    {
                        Vector2 prev = prevNormal.SafeNormalize(defaultNormal);
                        Vector2 next = nextNormal.SafeNormalize(defaultNormal);
                        Vector2 miter = prev + next;
                        if (miter.LengthSquared() <= Epsilon)
                            miter = defaultNormal;
                        miter = miter.SafeNormalize(defaultNormal);
                        float denom = Vector2.Dot(miter, next);
                        if (Math.Abs(denom) < Epsilon)
                            denom = denom >= 0f ? Epsilon : -Epsilon;
                        float miterLength = halfWidth / denom;
                        float maxLength = halfWidth * MainSettings.JoinMiterLimit;
                        miterLength = MathHelper.Clamp(miterLength, -maxLength, maxLength);
                        Vector2 offset = miter * miterLength;
                        left = currentPosition - offset;
                        right = currentPosition + offset;
                        effectiveHalfWidth = Math.Max(Math.Abs(miterLength), Epsilon);
                        return;
                    }
                default:
                    {
                        Vector2 offset = defaultNormal * halfWidth;
                        left = currentPosition - offset;
                        right = currentPosition + offset;
                        effectiveHalfWidth = halfWidth;
                        return;
                    }
            }
        }
        /// <summary>
        /// 计算各采样点的切线（写入 MainTangents）与法线（写入 MainNormals）。
        /// 切线由 ComputeTangent 逐点求出并归一化；
        /// 法线默认取切线的左法线，FrameTransportMode 为 ParallelTransport 时用前一法线按切线旋转量做平行传输，抑制扭转。
        /// </summary>
        private static void ComputeFrameData()
        {
            if (PositionsIndex <= 0)
                return;
            Vector2 fallbackTangent = Vector2.UnitX;
            for (int i = 0; i < PositionsIndex; i++)
            {
                Vector2 tangent = ComputeTangent(i, fallbackTangent);
                tangent = tangent.SafeNormalize(fallbackTangent.SafeNormalize(Vector2.UnitX));
                MainTangents[i] = tangent;
                fallbackTangent = tangent;
            }
            Vector2 previousNormal = Vector2.Zero;
            for (int i = 0; i < PositionsIndex; i++)
            {
                Vector2 tangent = MainTangents[i];
                if (tangent.LengthSquared() <= Epsilon)
                    tangent = fallbackTangent.SafeNormalize(Vector2.UnitX);
                Vector2 baseNormal = new Vector2(-tangent.Y, tangent.X);
                Vector2 normal;
                if (MainSettings.FrameTransportMode == PrimitiveFrameTransportMode.ParallelTransport && i > 0 && previousNormal.LengthSquared() > Epsilon)
                {
                    Vector2 previousTangent = MainTangents[i - 1];
                    float cosine = MathHelper.Clamp(Vector2.Dot(previousTangent, tangent), -1f, 1f);
                    float sine = Cross(previousTangent, tangent);
                    Vector2 transported = new Vector2(
                        cosine * previousNormal.X - sine * previousNormal.Y,
                        sine * previousNormal.X + cosine * previousNormal.Y
                    );
                    normal = transported;
                }
                else
                    normal = baseNormal;
                if (normal.LengthSquared() <= Epsilon)
                    normal = previousNormal.LengthSquared() > Epsilon ? previousNormal : baseNormal;
                normal = normal.SafeNormalize(previousNormal.LengthSquared() > Epsilon ? previousNormal : Vector2.UnitY);
                MainNormals[i] = normal;
                previousNormal = normal;
            }
        }
        /// <summary>
        /// 求第 index 个采样点的切线：末点取前一段方向，首点取后一段方向，
        /// 中间点取前后段方向之和（和退化时取较长的一段）。
        /// 结果退化时回退到 fallback（再退化为 +X）。
        /// </summary>
        private static Vector2 ComputeTangent(int index, Vector2 fallback)
        {
            int last = PositionsIndex - 1;
            Vector2 tangent;
            if (PositionsIndex <= 1)
            {
                tangent = fallback;
            }
            else if (index <= 0)
            {
                tangent = MainPositions[1] - MainPositions[0];
            }
            else if (index >= last)
            {
                tangent = MainPositions[last] - MainPositions[last - 1];
            }
            else
            {
                Vector2 forward = MainPositions[index + 1] - MainPositions[index];
                Vector2 backward = MainPositions[index] - MainPositions[index - 1];
                tangent = forward + backward;
                if (tangent.LengthSquared() <= Epsilon)
                    tangent = forward.LengthSquared() >= backward.LengthSquared() ? forward : backward;
            }
            if (tangent.LengthSquared() <= Epsilon)
                tangent = fallback.LengthSquared() > Epsilon ? fallback : Vector2.UnitX;
            return tangent;
        }
        /// <summary>
        /// 按 TextureCoordinateMode 计算第 index 个采样点的 U 坐标：
        /// 有自定义覆盖函数时直接交付；Distance 模式按沿拖尾的实际距离换算，
        /// 否则用完成比例乘以循环长度。两种情况最后都叠加 TextureScrollOffset。
        /// </summary>
        private static float ComputeTextureCoordinateForIndex(int index, float completionRatio)
        {
            float clampedCompletion = MathHelper.Clamp(completionRatio, 0f, 1f);
            if (MainSettings.TextureCoordinateFunction != null)
                return MainSettings.TextureCoordinateFunction(clampedCompletion);
            float cycleLength = MainSettings.TextureCycleLength;
            if (Math.Abs(cycleLength) <= Epsilon)
                cycleLength = cycleLength >= 0f ? 1f : -1f;
            switch (MainSettings.TextureCoordinateMode)
            {
                case PrimitiveTextureMode.Distance:
                    float distance = clampedCompletion * TotalTrailLength + MainSettings.TextureScrollOffset;
                    return distance / cycleLength;
                default:
                    return clampedCompletion * cycleLength + MainSettings.TextureScrollOffset;
            }
        }
        /// <summary>
        /// 二维叉积（a × b 的 Z 分量），用于平行传输时求切线间的旋转量
        /// </summary>
        private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
        /// <summary>
        /// 用线框效果按已生成的线框几何绘制调试线框
        /// </summary>
        private static void DrawWireframe(Matrix view, Matrix projection, int lineCount)
        {
            if (lineCount <= 0 || WireframeEffect == null)
                return;
            WireframeEffect.World = Matrix.Identity;
            WireframeEffect.View = view;
            WireframeEffect.Projection = projection;
            var device = Main.instance.GraphicsDevice;
            foreach (EffectPass pass in WireframeEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserPrimitives(PrimitiveType.LineList, WireframeVertices, 0, lineCount);
            }
        }
        /// <summary>
        /// 三次 Bezier 求值：按 t 在 p0-p1-p2-p3 四点定义的曲线上取点
        /// </summary>
        private static Vector2 EvaluateBezierSpan(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1f - t;
            float u2 = u * u;
            float u3 = u2 * u;
            float t2 = t * t;
            float t3 = t2 * t;
            return u3 * p0 + 3f * u2 * t * p1 + 3f * u * t2 * p2 + t3 * p3;
        }
        /// <summary>
        /// 按指定平滑方式在 p1-p2 之间取插值点：Linear 直接线性插值；
        /// Cardinal 用 SmoothingTension 求切线后走 Hermite；Hermite 用中心差分切线；
        /// CubicBezier 用 Catmull-Rom 手柄转成 Bezier；其余（含 CatmullRom）走 Vector2.CatmullRom。
        /// </summary>
        private static Vector2 EvaluateCurve(PrimitiveSmoothingType type, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t, PrimitiveSettings settings)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            switch (type)
            {
                case PrimitiveSmoothingType.Linear:
                    return Vector2.Lerp(p1, p2, t);
                case PrimitiveSmoothingType.Cardinal:
                    {
                        float tension = MathHelper.Clamp(settings.SmoothingTension, -1f, 1f);
                        float scale = (1f - tension) * 0.5f;
                        Vector2 m0 = (p2 - p0) * scale;
                        Vector2 m1 = (p3 - p1) * scale;
                        return EvaluateHermiteSpan(p1, p2, m0, m1, t);
                    }
                case PrimitiveSmoothingType.Hermite:
                    {
                        Vector2 m0 = 0.5f * (p2 - p0);
                        Vector2 m1 = 0.5f * (p3 - p1);
                        return EvaluateHermiteSpan(p1, p2, m0, m1, t);
                    }
                case PrimitiveSmoothingType.CubicBezier:
                    {
                        Vector2 handle1 = p1 + (p2 - p0) / 3f;
                        Vector2 handle2 = p2 - (p3 - p1) / 3f;
                        return EvaluateBezierSpan(p1, handle1, handle2, p2, t);
                    }
                default:
                    return Vector2.CatmullRom(p0, p1, p2, p3, t);
            }
        }
        /// <summary>
        /// 三次 Hermite 求值：按首末点及其切线、插值参数 t 计算曲线上的点
        /// </summary>
        private static Vector2 EvaluateHermiteSpan(Vector2 start, Vector2 end, Vector2 tangentStart, Vector2 tangentEnd, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            float h00 = 2f * t3 - 3f * t2 + 1f;
            float h10 = t3 - 2f * t2 + t;
            float h01 = -2f * t3 + 3f * t2;
            float h11 = t3 - t2;
            return h00 * start + h10 * tangentStart + h01 * end + h11 * tangentEnd;
        }
        /// <summary>
        /// 取第 index 个采样点的完成比例；越界时夹取到首/末点的值
        /// </summary>
        private static float GetCompletionRatioForIndex(int index)
        {
            if (PositionsIndex <= 0)
                return 0f;
            if (index <= 0)
                return MainCompletionRatios[0];
            if (index >= PositionsIndex)
                return MainCompletionRatios[PositionsIndex - 1];
            return MainCompletionRatios[index];
        }
        /// <summary>
        /// 像素化安全阀：本模组未移植像素化渲染子系统，误开 Pixelate 时直接抛异常，
        /// 避免静默走到未实现的渲染路径
        /// </summary>
        private static void PerformPixelationSafetyChecks(PrimitiveSettings settings)
        {
            // 本模组未移植像素化渲染子系统，不允许误开像素化模式
            if (settings.Pixelate)
                throw new Exception("错误：CalamityDemutation 未移植像素化渲染子系统，PrimitiveSettings.Pixelate 必须为 false。");
        }
        /// <summary>
        /// 核心渲染：校验顶点/索引数量后设置裁剪矩形与透视矩阵，
        /// 应用着色器（未指定则用默认的 StandardPrimitiveShader），上传顶点与索引缓冲，
        /// 按拓扑调用 DrawIndexedPrimitives；开启 DebugWireframe 时最后叠加线框。
        /// </summary>
        private static void PrivateRender()
        {
            if (VerticesIndex <= 3)
                return;
            if (ActiveTopology == PrimitiveTopology.TriangleList)
            {
                if (IndicesIndex < 6 || IndicesIndex % 3 != 0)
                    return;
            }
            else if (ActiveTopology == PrimitiveTopology.TriangleStrip && IndicesIndex < 4)
                return;
            // 屏幕剔除，提升性能
            Main.instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            Main.instance.GraphicsDevice.RasterizerState.ScissorTestEnable = true;
            Main.instance.GraphicsDevice.ScissorRectangle = new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);
            Matrix view;
            Matrix projection;
            if (MainSettings.Pixelate || MainSettings.UseUnscaledMatrices)
                CalcuatePixelatedPerspectiveMatrices(out view, out projection);
            else
                CDUtil.CalculatePerspectiveMatricies(out view, out projection);
            var shaderToUse = MainSettings.Shader ?? GameShaders.Misc["CalamityDemutation:StandardPrimitiveShader"];
            shaderToUse.Shader.Parameters["uWorldViewProjection"].SetValue(view * projection);
            shaderToUse.Apply();
            VertexBuffer.SetData(MainVertices, 0, VerticesIndex, SetDataOptions.Discard);
            IndexBuffer.SetData(MainIndices, 0, IndicesIndex, SetDataOptions.Discard);
            Main.instance.GraphicsDevice.SetVertexBuffer(VertexBuffer);
            Main.instance.GraphicsDevice.Indices = IndexBuffer;
            PrimitiveType primitiveType = ActiveTopology == PrimitiveTopology.TriangleStrip ? PrimitiveType.TriangleStrip : PrimitiveType.TriangleList;
            int primitiveCount = primitiveType == PrimitiveType.TriangleStrip ? Math.Max(IndicesIndex - 2, 0) : IndicesIndex / 3;
            Main.instance.GraphicsDevice.DrawIndexedPrimitives(primitiveType, 0, 0, VerticesIndex, 0, primitiveCount);
            if (MainSettings.DebugWireframe && WireframeEffect != null && TryBuildWireframeGeometry(MainSettings.WireframeColor, out int lineCount))
                DrawWireframe(view, projection, lineCount);
        }
        /// <summary>
        /// 生成调试线框几何：沿相邻采样点连左右两条竖线，再逐点连一条横向线，
        /// 结果写入 WireframeVertices，lineCount 输出线段数；无线框缓冲或段数不足时返回 false。
        /// </summary>
        private static bool TryBuildWireframeGeometry(Color lineColor, out int lineCount)
        {
            lineCount = 0;
            if (WireframeVertices == null)
                return false;
            int segments = PositionsIndex - 1;
            if (segments <= 0)
                return false;
            WireframeVertexCount = 0;
            for (int i = 0; i < segments; i++)
            {
                int currentLeft = i * 2;
                int currentRight = currentLeft + 1;
                int nextLeft = currentLeft + 2;
                int nextRight = nextLeft + 1;
                AddWireframeLineFromIndices(currentLeft, nextLeft, lineColor);
                AddWireframeLineFromIndices(currentRight, nextRight, lineColor);
            }
            for (int i = 0; i < PositionsIndex; i++)
            {
                int leftIndex = i * 2;
                AddWireframeLineFromIndices(leftIndex, leftIndex + 1, lineColor);
            }
            lineCount = WireframeVertexCount / 2;
            return lineCount > 0;
        }
        /// <summary>
        /// 为指定的采样点索引生成封口中心点顶点（位置取该点、颜色取左右顶点均值、
        /// 半宽取左右顶点 Z 分量较大者、U 取左右顶点均值），
        /// 返回新顶点索引；缓冲不足或该点顶点尚未生成时返回 -1。
        /// </summary>
        private static short TryCreateCapVertex(int positionIndex)
        {
            if (VerticesIndex >= MaxVertices - 1)
                return -1;
            int leftVertexIndex = positionIndex * 2;
            int rightVertexIndex = leftVertexIndex + 1;
            if (rightVertexIndex >= VerticesIndex)
                return -1;
            ref readonly VertexPosition2DColorTexture leftVertex = ref MainVertices[leftVertexIndex];
            ref readonly VertexPosition2DColorTexture rightVertex = ref MainVertices[rightVertexIndex];
            Vector2 centerPosition = MainPositions[positionIndex];
            Color centerColor = Color.Lerp(leftVertex.Color, rightVertex.Color, 0.5f);
            float centerHalfWidth = Math.Max(Math.Max(leftVertex.TextureCoordinates.Z, rightVertex.TextureCoordinates.Z), Epsilon);
            float centerU = (leftVertex.TextureCoordinates.X + rightVertex.TextureCoordinates.X) * 0.5f;
            Vector2 centerTexcoord = new(centerU, 0.5f);
            short newVertexIndex = VerticesIndex;
            MainVertices[VerticesIndex++] = new VertexPosition2DColorTexture(centerPosition, centerColor, centerTexcoord, centerHalfWidth);
            return newVertexIndex;
        }
    }
}
