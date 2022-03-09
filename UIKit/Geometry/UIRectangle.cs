using Microsoft.Xna.Framework;
using System;

namespace Shockah.UIKit.Geometry
{
	public readonly struct UIRectangle: IEquatable<UIRectangle>
	{
		public readonly UIVector2 Position { get; }
		public readonly UIVector2 Size { get; }

		public UIRectangle(UIVector2 position, UIVector2 size)
		{
			this.Position = position;
			this.Size = size;
		}

		public void Deconstruct(out UIVector2 position, out UIVector2 size)
		{
			position = Position;
			size = Size;
		}

		public override string ToString()
			=> $"Rectangle{{{Size.X}x{Size.Y} @ {Position}}}";

		public bool Equals(UIRectangle other)
			=> Position == other.Position && Size == other.Size;

		public override bool Equals(object? obj)
			=> obj is UIRectangle other && Equals(other);

		public override int GetHashCode()
			=> (Position, Size).GetHashCode();

		public static bool operator ==(UIRectangle left, UIRectangle right)
			=> left.Equals(right);

		public static bool operator !=(UIRectangle left, UIRectangle right)
			=> !(left == right);

		public static implicit operator UIRectangle(Rectangle r)
			=> new(r.Location, r.Size);

		public static explicit operator Rectangle(UIRectangle r)
			=> new((Point)r.Position, (Point)r.Size);

		public static implicit operator UIRectangle((UIVector2 position, UIVector2 size) t)
			=> new(t.position, t.size);
	}
}