using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace CalamityDemutation.Common.Effects
{
    /// <summary>
    /// 刀光拖尾专用顶点结构（实现 IVertexType）：用 2D 位置 + 顶点颜色 + 3 分量「纹理坐标」描述一个顶点。
    /// 由 Content/Projectiles/Melee/Core/BaseSwingCO 的 WarpDraw() 组装成 TriangleStrip，
    /// 交给 Effects/KnifeDistortion.fx 绘制挥舞弧光；本模组中仅该处使用。
    /// 注意后两个字段并非字面 RGBA/UV，而是打包了着色器需要的自定义数据（见下方各行注释）。
    /// </summary>
    public struct CustomVertexInfo(Vector2 position, Color color, Vector3 texCoord) : IVertexType
    {
        /// <summary>顶点位置（世界坐标）；绘制时经着色器的 uTransform 变换到屏幕。</summary>
        public Vector2 Position = position;
        /// <summary>顶点颜色通道；BaseSwingCO 里打包为 (方向比例, 宽度, 0, 15) 供着色器取用，不是真实颜色。</summary>
        public Color Color = color;
        /// <summary>纹理坐标通道；Z 分量同样用于携带拖尾数据（KnifeDistortion 只读取 XY），不是真正的 3D 坐标。</summary>
        public Vector3 TexCoord = texCoord;
        /// <summary>
        /// 顶点声明：按字段顺序向显卡声明显存布局——
        /// Position(Vector2，起始偏移 0)、Color(RGBA 4 字节，偏移 8)、TexCoord(Vector3，偏移 12)，单顶点步长 24 字节。
        /// </summary>
        private static readonly VertexDeclaration _vertexDeclaration = new([
            new(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),           // 位置：2 个 float，偏移 0
            new(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),               // 颜色：4 字节 RGBA，偏移 8
            new(12, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0) // 纹理坐标：3 个 float，偏移 12
        ]);
        /// <summary>返回上面静态声明的顶点布局，供 GPU 解析顶点缓冲。</summary>
        public VertexDeclaration VertexDeclaration
        {
            get { return _vertexDeclaration; }
        }
    }
}
