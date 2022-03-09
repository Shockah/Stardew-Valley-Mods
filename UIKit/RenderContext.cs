using Microsoft.Xna.Framework.Graphics;

namespace Shockah.UIKit
{
	public struct RenderContext
	{
		public readonly SpriteBatch SpriteBatch { get; }
		public readonly float X { get; }
		public readonly float Y { get; }

		public RenderContext(SpriteBatch spriteBatch, float x = 0f, float y = 0f)
		{
			this.SpriteBatch = spriteBatch;
			this.X = x;
			this.Y = y;
		}

		public RenderContext GetTranslated(float x, float y)
			=> new(SpriteBatch, this.X + x, this.Y + y);
	}
}
