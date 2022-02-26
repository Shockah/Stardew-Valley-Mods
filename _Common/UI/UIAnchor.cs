using Microsoft.Xna.Framework;

namespace Shockah.CommonModCode.UI
{
	public enum UIAnchorSide { TopLeft, Top, TopRight, Left, Center, Right, BottomLeft, Bottom, BottomRight }

	public readonly struct UIAnchor
	{
		public readonly UIAnchorSide From;
		public readonly Vector2 Offset;
		public readonly UIAnchorSide To;

		public UIAnchor(UIAnchorSide from, Vector2 offset, UIAnchorSide to)
		{
			this.From = from;
			this.Offset = offset;
			this.To = to;
		}

		public UIAnchor(UIAnchorSide from, float inset, Vector2 offset, UIAnchorSide to)
			: this(from, GetRealOffset(from, inset, offset), to) { }

		private static Vector2 GetRealOffset(UIAnchorSide side, float inset, Vector2 baseOffset)
		{
			var result = baseOffset;
			switch (side)
			{
				case UIAnchorSide.TopLeft:
					result.X += inset;
					result.Y += inset;
					break;
				case UIAnchorSide.TopRight:
					result.X -= inset;
					result.Y += inset;
					break;
				case UIAnchorSide.BottomLeft:
					result.X += inset;
					result.Y -= inset;
					break;
				case UIAnchorSide.BottomRight:
					result.X -= inset;
					result.Y -= inset;
					break;
				case UIAnchorSide.Top:
					result.Y += inset;
					break;
				case UIAnchorSide.Bottom:
					result.Y -= inset;
					break;
				case UIAnchorSide.Left:
					result.X += inset;
					break;
				case UIAnchorSide.Right:
					result.X -= inset;
					break;
				case UIAnchorSide.Center:
					break;
			}
			return result;
		}
	}
}
