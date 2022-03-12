using Microsoft.Xna.Framework.Graphics;
using Shockah.UIKit.Geometry;

namespace Shockah.UIKit
{
	public struct RenderContext
	{
		public readonly SpriteBatch SpriteBatch { get; }
		public readonly UIVector2 Offset { get; }

		public RenderContext(SpriteBatch spriteBatch, UIVector2? offset = null)
		{
			this.SpriteBatch = spriteBatch;
			this.Offset = offset ?? UIVector2.Zero;
		}

		public RenderContext GetTranslated(UIVector2 translation)
			=> new(SpriteBatch, Offset + translation);
	}
}
