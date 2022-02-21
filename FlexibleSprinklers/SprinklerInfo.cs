using Microsoft.Xna.Framework;

namespace Shockah.FlexibleSprinklers
{
	public struct SprinklerInfo
	{
		public Vector2[] Layout { get; set; }

		public int Power { get; set; }

		public SprinklerInfo(Vector2[] layout): this(layout, layout.Length) { }

		public SprinklerInfo(Vector2[] layout, int power)
		{
			this.Layout = layout;
			this.Power = power;
		}
	}
}