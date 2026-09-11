using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace CalamityDemutation.Graphics.Primitives
{
    /// <summary>
    /// Primitive 渲染专用顶点结构：位置用 Vector2（Terraria 是 2D 游戏，无需 Vector4），
    /// 附带顶点颜色与纹理坐标。
    /// </summary>
    public readonly struct VertexPosition2DColorTexture : IVertexType
    {
        /// <summary>
        /// 顶点位置。
        /// </summary>
        public readonly Vector2 Position;
        /// <summary>
        /// 顶点颜色。
        /// </summary>
        public readonly Color Color;
        /// <summary>
        /// 顶点纹理坐标。
        /// </summary>
        /// <remarks>
        /// Z 分量与 3D 无关，它存储该点处拖尾的宽度修正系数——
        /// 由于顶点不能逐点保存任意数据，只能塞进预定义的格式里。
        /// </remarks>
        public readonly Vector3 TextureCoordinates;
        /// <summary>
        /// 顶点声明：向顶点着色器声明数据的布局与大小。
        /// </summary>
        public VertexDeclaration VertexDeclaration => VertexDeclaration2D;
        public static readonly VertexDeclaration VertexDeclaration2D = new(new VertexElement[]
        {
            new(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
            new(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new(12, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0)
        });
        /// <summary>
        /// 构造顶点：写入位置与颜色，并把二维纹理坐标与宽度修正系数合并进 TextureCoordinates（xy 为 UV，z 为宽度修正）
        /// </summary>
        /// <param name="position">顶点屏幕/世界坐标</param>
        /// <param name="color">顶点颜色</param>
        /// <param name="textureCoordinates">纹理坐标（UV）</param>
        /// <param name="widthCorrectionFactor">该点处拖尾的宽度修正系数，存入 TextureCoordinates.Z</param>
        public VertexPosition2DColorTexture(Vector2 position, Color color, Vector2 textureCoordinates, float widthCorrectionFactor)
        {
            Position = position;
            Color = color;
            TextureCoordinates = new(textureCoordinates, widthCorrectionFactor);
        }
    }
}
