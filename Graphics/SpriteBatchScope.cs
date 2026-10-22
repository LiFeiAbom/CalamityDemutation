using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
namespace CalamityDemutation.Graphics
{
    /// <summary>
    /// 接管当前 SpriteBatch，离场时按原版实体绘制参数重新开启批次。
    /// 用于把带破晓II的敌怪单独画进渲染目标后再经过溶解着色器画回。
    /// </summary>
    internal readonly struct SpriteBatchScope : IDisposable
    {
        private readonly SpriteBatch spriteBatch;
        public SpriteBatchScope(SpriteBatch spriteBatch)
        {
            this.spriteBatch = spriteBatch;
            spriteBatch.End();
        }
        public void Dispose()
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState
                , DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
    internal static class SpriteBatchScopeExtensions
    {
        public static SpriteBatchScope Scope(this SpriteBatch spriteBatch) => new(spriteBatch);
    }
}
