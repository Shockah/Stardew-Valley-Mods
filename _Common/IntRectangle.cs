using System;

namespace Shockah.CommonModCode
{
	public readonly struct IntRectangle : IEquatable<IntRectangle>
	{
		public readonly IntPoint A { get; init; }
		public readonly IntPoint B { get; init; }

		public IntPoint Min
			=> new(Math.Min(A.X, B.X), Math.Min(A.Y, B.Y));

		public IntPoint Max
			=> new(Math.Max(A.X, B.X), Math.Max(A.Y, B.Y));

		public int Width
			=> Max.X - Min.X + 1;

		public int Height
			=> Max.Y - Min.Y + 1;

		public IntRectangle(IntPoint a, IntPoint b) : this()
		{
			this.A = a;
			this.B = b;
		}

		public IntRectangle(IntPoint point, int width, int height) : this()
		{
			this.A = point;
			this.B = new(point.X + width - 1, point.Y + height - 1);
		}

		public override string ToString()
			=> $"IntRectangle(Min = {Min}, Max = {Max}, Width = {Width}, Height = {Height})";

		public override bool Equals(object? obj)
			=> obj is IntRectangle rectangle && Equals(rectangle);

		public bool Equals(IntRectangle other)
			=> A == other.A && B == other.B;

		public override int GetHashCode()
			=> (A.GetHashCode() * 256) ^ B.GetHashCode();

		public void Deconstruct(out IntPoint min, out IntPoint max)
		{
			min = Min;
			max = Max;
		}

		public static bool operator ==(IntRectangle lhs, IntRectangle rhs)
			=> lhs.Equals(rhs);

		public static bool operator !=(IntRectangle lhs, IntRectangle rhs)
			=> !lhs.Equals(rhs);

		public bool Contains(IntPoint point)
			=> point.X >= Min.X && point.Y >= Min.Y && point.X <= Max.X && point.Y <= Max.Y;
	}
}