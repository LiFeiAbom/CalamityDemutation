using System;
using Microsoft.Xna.Framework;
using Terraria.Graphics.Shaders;
namespace CalamityDemutation.Graphics.Primitives
{
    /// <summary>
    /// 控制 Primitive 拖尾 U 纹理坐标的生成方式。
    /// </summary>
    public enum PrimitiveTextureMode
    {
        /// <summary>
        /// U 坐标沿整条拖尾从 0 到 1 分布（旧版行为）。
        /// </summary>
        Normalized,
        /// <summary>
        /// U 坐标按沿拖尾的距离推进，每 <see cref="PrimitiveSettings.TextureCycleLength"/> 像素循环一整张贴图。
        /// </summary>
        Distance
    }
    /// <summary>
    /// 控制相邻线段在展开成四边形时的连接方式。
    /// </summary>
    public enum PrimitiveJoinStyle
    {
        /// <summary>
        /// 旧版行为：每段仅使用自身的前向方向展开。
        /// </summary>
        Flat,
        /// <summary>
        /// 使用相邻线段的平均切线来平滑拐角。
        /// </summary>
        Smooth,
        /// <summary>
        /// 使用斜接（miter）连接，在急转弯处保持宽度连续。
        /// </summary>
        Miter
    }
    /// <summary>
    /// 决定平滑插值时如何生成中间采样点。
    /// </summary>
    public enum PrimitiveSmoothingType
    {
        /// <summary>
        /// 经典 Catmull-Rom 插值。
        /// </summary>
        CatmullRom,
        /// <summary>
        /// 带可调张力的 Cardinal 样条插值。
        /// </summary>
        Cardinal,
        /// <summary>
        /// 相邻点之间的直线插值。
        /// </summary>
        Linear,
        /// <summary>
        /// 使用中心差分切线的三次 Hermite 插值。
        /// </summary>
        Hermite,
        /// <summary>
        /// 使用 Catmull-Rom 手柄的三次 Bezier 插值。
        /// </summary>
        CubicBezier
    }
    /// <summary>
    /// 选择朝向帧（法线）沿拖尾的传播方式。
    /// </summary>
    public enum PrimitiveFrameTransportMode
    {
        /// <summary>
        /// 由瞬时切线推导法线（旧版行为）。
        /// </summary>
        Basic,
        /// <summary>
        /// 使用平行传输（parallel transport）最小化扭转。
        /// </summary>
        ParallelTransport
    }
    /// <summary>
    /// 决定提交给 GPU 的几何拓扑。
    /// </summary>
    public enum PrimitiveTopology
    {
        /// <summary>
        /// 将几何展开为独立三角形（旧版行为）。
        /// </summary>
        TriangleList,
        /// <summary>
        /// 使用连续的三角带（triangle strip）连接几何。
        /// </summary>
        TriangleStrip
    }
    /// <summary>
    /// 控制拖尾起点与终点的封口方式。
    /// </summary>
    public enum PrimitiveCapStyle
    {
        /// <summary>
        /// 丝带两端开放（旧版行为）。
        /// </summary>
        None,
        /// <summary>
        /// 在丝带两端添加扁平三角形封口。
        /// </summary>
        Flat
    }
    /// <summary>
    /// 创建 Primitive 拖尾时的全部可选项。可自由新增成员而不破坏已有实现。
    /// </summary>
    public readonly struct PrimitiveSettings
    {
        // ── 嵌套类型 ──
        /// <summary>
        /// 动态决定拖尾各点宽度的委托。
        /// </summary>
        /// <param name="trailLengthInterpolant">当前点在整条拖尾上的 0-1 插值位置。</param>
        /// <param name="vertexPosition">当前点的世界坐标。</param>
        /// <returns>当前点的宽度。</returns>
        public delegate float VertexWidthFunction(float trailLengthInterpolant, Vector2 vertexPosition);
        /// <summary>
        /// 动态决定拖尾各点颜色的委托。
        /// </summary>
        /// <param name="trailLengthInterpolant">当前点在整条拖尾上的 0-1 插值位置。</param>
        /// <param name="vertexPosition">当前点的世界坐标。</param>
        /// <returns>当前点的颜色。</returns>
        public delegate Color VertexColorFunction(float trailLengthInterpolant, Vector2 vertexPosition);
        /// <summary>
        /// 动态决定拖尾各点偏移量的委托。
        /// </summary>
        /// <param name="trailLengthInterpolant">当前点在整条拖尾上的 0-1 插值位置。</param>
        /// <param name="vertexPosition">当前点的世界坐标。</param>
        /// <returns>当前点的偏移量。</returns>
        public delegate Vector2 VertexOffsetFunction(float trailLengthInterpolant, Vector2 vertexPosition);
        // ── 实例字段 ──
        /// <summary>
        /// 用于决定各顶点宽度。
        /// </summary>
        public readonly VertexWidthFunction WidthFunction;
        /// <summary>
        /// 用于决定各顶点颜色。
        /// </summary>
        public readonly VertexColorFunction ColorFunction;
        /// <summary>
        /// 用于在生成位置时偏移每个点。
        /// </summary>
        public readonly VertexOffsetFunction OffsetFunction;
        /// <summary>
        /// 生成顶点位置时是否进行平滑插值，推荐默认开启。
        /// </summary>
        public readonly bool Smoothen;
        /// <summary>
        /// 是否像素化渲染图元（本模组未移植像素化子系统，必须为 false）。
        /// </summary>
        public readonly bool Pixelate;
        /// <summary>
        /// 渲染时是否使用未缩放的透视矩阵。仅推荐在系统脱离常规上下文使用时开启，
        /// 例如在 CustomSky 里渲染背景图元。
        /// </summary>
        public readonly bool UseUnscaledMatrices;
        /// <summary>
        /// 渲染时应用的着色器。
        /// </summary>
        public readonly MiscShaderData Shader;
        /// <summary>
        /// 可选覆盖：强制拖尾使用给定位置作为首顶点的左右侧位置（屏幕空间）。
        /// </summary>
        public readonly (Vector2, Vector2)? InitialVertexPositionsOverride;
        /// <summary>
        /// 决定 U 纹理坐标的生成方式。
        /// </summary>
        public readonly PrimitiveTextureMode TextureCoordinateMode;
        /// <summary>
        /// 控制生成的 U 坐标尺度：在 <see cref="PrimitiveTextureMode.Normalized"/> 下是乘数，
        /// 在 <see cref="PrimitiveTextureMode.Distance"/> 下是贴图完整循环一次的距离（像素）。
        /// </summary>
        public readonly float TextureCycleLength;
        /// <summary>
        /// 为生成的 U 坐标添加额外偏移。距离模式下该值以像素为单位。
        /// </summary>
        public readonly float TextureScrollOffset;
        /// <summary>
        /// 用自定义函数覆盖自动生成的 U 坐标：接收归一化完成比例（0-1），返回期望的 U 坐标。
        /// </summary>
        public readonly Func<float, float> TextureCoordinateFunction;
        /// <summary>
        /// 决定展开成四边形时相邻线段的连接方式。
        /// </summary>
        public readonly PrimitiveJoinStyle JoinStyle;
        /// <summary>
        /// 斜接连接的最大长度乘数，用于抑制尖刺。
        /// </summary>
        public readonly float JoinMiterLimit;
        /// <summary>
        /// 为 true 时在图元上叠加调试线框。
        /// </summary>
        public readonly bool DebugWireframe;
        /// <summary>
        /// 调试线框使用的颜色。
        /// </summary>
        public readonly Color WireframeColor;
        /// <summary>
        /// 平滑控制点列表时使用的插值方式。
        /// </summary>
        public readonly PrimitiveSmoothingType SmoothingType;
        /// <summary>
        /// 平滑时每条控制线段生成的中间细分数量，0 表示自动分配。
        /// </summary>
        public readonly int SmoothingSegments;
        /// <summary>
        /// 支持张量的平滑方式（如 Cardinal 样条）使用的附加张力。
        /// </summary>
        public readonly float SmoothingTension;
        /// <summary>
        /// 决定法线沿曲线的传播方式。
        /// </summary>
        public readonly PrimitiveFrameTransportMode FrameTransportMode;
        /// <summary>
        /// 提交几何到 GPU 时使用的拓扑。
        /// </summary>
        public readonly PrimitiveTopology Topology;
        /// <summary>
        /// 决定丝带两端的封口方式。
        /// </summary>
        public readonly PrimitiveCapStyle CapStyle;
        // ── 构造函数 ──
        /// <summary>
        /// 创建 Primitive 拖尾的全部可选项。
        /// </summary>
        /// <param name="widthFunction">用于决定各顶点宽度。</param>
        /// <param name="colorFunction">用于决定各顶点颜色。</param>
        /// <param name="offsetFunction">用于偏移每个生成位置。</param>
        /// <param name="smoothen">生成顶点位置时是否进行平滑插值，推荐开启。</param>
        /// <param name="pixelate">是否像素化渲染（本模组未移植像素化子系统，保持 false）。</param>
        /// <param name="shader">渲染时应用的着色器。</param>
        /// <param name="useUnscaledMatrices">是否使用未缩放的透视矩阵，仅在非常规上下文（如 CustomSky）中使用。</param>
        /// <param name="initialVertexPositionsOverride">可选：强制首顶点使用给定的左右侧屏幕空间位置。</param>
        /// <param name="textureCoordinateMode">决定 U 纹理坐标的生成方式。</param>
        /// <param name="textureCycleLength">控制 U 坐标推进速度：归一化模式下为乘数，距离模式下为每次循环的距离。</param>
        /// <param name="textureScrollOffset">生成的 U 坐标偏移量，距离模式下以像素为单位。</param>
        /// <param name="textureCoordinateFunction">可选：接收完成比例并返回自定义 U 坐标的覆盖函数。</param>
        /// <param name="joinStyle">决定相邻线段的连接方式。</param>
        /// <param name="joinMiterLimit">斜接连接的最大长度乘数，仅 <paramref name="joinStyle"/> 为 <see cref="PrimitiveJoinStyle.Miter"/> 时生效。</param>
        /// <param name="debugWireframe">为 true 时绘制调试线框。</param>
        /// <param name="wireframeColor">线框颜色覆盖，为 null 时使用亮绿色。</param>
        /// <param name="smoothingType">平滑控制点时使用的插值方式。</param>
        /// <param name="smoothingSegments">大于 0 时指定每条控制边生成的细分数量。</param>
        /// <param name="smoothingTension">某些平滑方式（如 Cardinal 样条）使用的张力参数。</param>
        /// <param name="frameTransportMode">控制朝向帧沿曲线的传播方式。</param>
        /// <param name="topology">提交几何时使用的图元拓扑。</param>
        /// <param name="capStyle">选择丝带两端的封口方式。</param>
        public PrimitiveSettings(VertexWidthFunction widthFunction, VertexColorFunction colorFunction, VertexOffsetFunction offsetFunction = null, bool smoothen = true, bool pixelate = false, MiscShaderData shader = null, bool useUnscaledMatrices = false, (Vector2, Vector2)? initialVertexPositionsOverride = null, PrimitiveTextureMode textureCoordinateMode = PrimitiveTextureMode.Normalized, float textureCycleLength = 1f, float textureScrollOffset = 0f, Func<float, float> textureCoordinateFunction = null, PrimitiveJoinStyle joinStyle = PrimitiveJoinStyle.Smooth, float joinMiterLimit = 4f, bool debugWireframe = false, Color? wireframeColor = null, PrimitiveSmoothingType smoothingType = PrimitiveSmoothingType.CatmullRom, int smoothingSegments = 0, float smoothingTension = 0f, PrimitiveFrameTransportMode frameTransportMode = PrimitiveFrameTransportMode.ParallelTransport, PrimitiveTopology topology = PrimitiveTopology.TriangleStrip, PrimitiveCapStyle capStyle = PrimitiveCapStyle.None)
        {
            WidthFunction = widthFunction;
            ColorFunction = colorFunction;
            OffsetFunction = offsetFunction;
            Smoothen = smoothen;
            Pixelate = pixelate;
            Shader = shader;
            UseUnscaledMatrices = useUnscaledMatrices;
            InitialVertexPositionsOverride = initialVertexPositionsOverride;
            TextureCoordinateMode = textureCoordinateMode;
            TextureCycleLength = Math.Abs(textureCycleLength) <= PrimitiveRenderer.Epsilon ? 1f : textureCycleLength;
            TextureScrollOffset = textureScrollOffset;
            TextureCoordinateFunction = textureCoordinateFunction;
            JoinStyle = joinStyle;
            JoinMiterLimit = Math.Max(joinMiterLimit, 1f);
            DebugWireframe = debugWireframe;
            WireframeColor = wireframeColor ?? Color.LimeGreen;
            SmoothingType = smoothingType;
            SmoothingSegments = Math.Max(smoothingSegments, 0);
            SmoothingTension = smoothingTension;
            FrameTransportMode = frameTransportMode;
            Topology = topology;
            CapStyle = capStyle;
        }
    }
}
