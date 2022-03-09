using Microsoft.Xna.Framework;
using System;

namespace Shockah.UIKit.Geometry
{
	public readonly struct UIVector2: IEquatable<UIVector2>
	{
		public static UIVector2 Zero => _zero;
		public static UIVector2 One => _one;

		private static readonly UIVector2 _zero = new(0);
		private static readonly UIVector2 _one = new(1);

		public float X { get; }
		public float Y { get; }

		public UIVector2(float x, float y)
		{
			this.X = x;
			this.Y = y;
		}

		public UIVector2(float v) : this(v, v)
		{
		}

		public void Deconstruct(out float x, out float y)
		{
			x = X;
			y = Y;
		}

		public override string ToString()
			=> $"{{{X}, {Y}}}";

		public bool Equals(UIVector2 other)
			=> X == other.X && Y == other.Y;

		public override bool Equals(object? obj)
			=> obj is UIVector2 other && Equals(other);

		public override int GetHashCode()
			=> (X, Y).GetHashCode();

		public static bool operator ==(UIVector2 left, UIVector2 right)
			=> left.Equals(right);

		public static bool operator !=(UIVector2 left, UIVector2 right)
			=> !(left == right);

		public static UIVector2 operator -(UIVector2 v)
			=> new(-v.X, -v.Y);

		public static UIVector2 operator +(UIVector2 lhs, UIVector2 rhs)
			=> new(lhs.X + rhs.X, lhs.Y + rhs.Y);

		public static UIVector2 operator -(UIVector2 lhs, UIVector2 rhs)
			=> new(lhs.X - rhs.X, lhs.Y - rhs.Y);

		public static UIVector2 operator *(UIVector2 lhs, UIVector2 rhs)
			=> new(lhs.X * rhs.X, lhs.Y * rhs.Y);

		public static UIVector2 operator *(UIVector2 lhs, float rhs)
			=> new(lhs.X * rhs, lhs.Y * rhs);

		public static UIVector2 operator *(float lhs, UIVector2 rhs)
			=> new(lhs * rhs.X, lhs * rhs.Y);

		public static UIVector2 operator /(UIVector2 lhs, UIVector2 rhs)
			=> new(lhs.X / rhs.X, lhs.Y / rhs.Y);

		public static UIVector2 operator /(UIVector2 lhs, float rhs)
			=> new(lhs.X / rhs, lhs.Y / rhs);

		public static implicit operator UIVector2(Vector2 v)
			=> new(v.X, v.Y);

		public static implicit operator Vector2(UIVector2 v)
			=> new(v.X, v.Y);

		public static implicit operator UIVector2(Point p)
			=> new(p.X, p.Y);

		public static explicit operator Point(UIVector2 v)
			=> new((int)v.X, (int)v.Y);

		public static implicit operator UIVector2((float x, float y) t)
			=> new(t.x, t.y);

		public static implicit operator UIVector2(float v)
			=> new(v);
	}
}
