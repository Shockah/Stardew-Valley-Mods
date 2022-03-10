using Microsoft.Xna.Framework.Graphics;

namespace Shockah.UIKit
{
	public static class SpriteBatches
	{
		public static bool TryEnd(this SpriteBatch self)
		{
			try
			{
				self.End();
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}