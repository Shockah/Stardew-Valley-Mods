using System;

namespace Shockah.UIKit.Geometry
{
	public readonly struct UIEdgeInsets: IEquatable<UIEdgeInsets>
	{
		public readonly float Top { get; }
		public readonly float Right { get; }
		public readonly float Bottom { get; }
		public readonly float Left { get; }

		public float Horizontal
			=> Left + Right;

		public float Vertical
			=> Top + Bottom;

		public UIEdgeInsets(float insets) : this(insets, insets, insets, insets)
		{
		}

		public UIEdgeInsets(float top = 0f, float right = 0f, float bottom = 0f, float left = 0f)
		{
			this.Top = top;
			this.Right = right;
			this.Bottom = bottom;
			this.Left = left;
		}

		public override string ToString()
			=> $"UIEdgeInsets{{Top: {Top}, Right: {Right}, Bottom: {Bottom}, Left: {Left}}}";

		public bool Equals(UIEdgeInsets other)
			=> Top == other.Top && Right == other.Right && Bottom == other.Bottom && Left == other.Left;

		public override bool Equals(object? obj)
			=> obj is UIEdgeInsets other && Equals(other);

		public override int GetHashCode()
			=> (Top, Right, Bottom, Left).GetHashCode();

		public static bool operator ==(UIEdgeInsets left, UIEdgeInsets right)
			=> left.Equals(right);

		public static bool operator !=(UIEdgeInsets left, UIEdgeInsets right)
			=> !(left == right);
	}
}