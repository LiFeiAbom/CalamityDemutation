using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace CalamityDemutation.Effects
{
    /// <summary>
    /// 带颜色与三维纹理坐标的顶点结构（搬运自 CalamityEntropy 所依赖的 InnoVault 同名结构）：
    /// 顶点着色器类特效（如刀光三角带）用 <c>GraphicsDevice.DrawUserPrimitives</c> 直接送顶点时使用。
    /// 布局为「位置 Vector2(8B) + 颜色 Color(4B) + 纹理坐标 Vector3(12B)」，共 24 字节。
    /// </summary>
    internal struct ColoredVertex:IVertexType
    {
        private static readonly VertexDeclaration VertexDeclarationValue = new VertexDeclaration(new VertexElement[]
        {
            new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
            new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0)
        });
        /// <summary>顶点的屏幕坐标</summary>
        public Vector2 Position;
        /// <summary>顶点的颜色</summary>
        public Color Color;
        /// <summary>顶点的纹理坐标（第三维留给着色器自行取用）</summary>
        public Vector3 TexCoord;
        /// <summary>顶点声明，告诉渲染管线如何解析上面三个字段的内存布局</summary>
        public readonly VertexDeclaration VertexDeclaration => VertexDeclarationValue;
        /// <summary>按「位置、纹理坐标、颜色」构造顶点</summary>
        public ColoredVertex(Vector2 position, Vector3 texCoord, Color color)
        {
            Position = position;
            TexCoord = texCoord;
            Color = color;
        }
    }
}
