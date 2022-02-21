using System;
using System.Collections.Generic;

namespace Shockah.FlexibleSprinklers
{
	internal struct IntPoint: IEquatable<IntPoint>
	{
		public static readonly IntPoint Zero = new(0, 0);
		public static readonly IntPoint One = new(1, 1);
		public static readonly IntPoint Left = new(-1, 0);
		public static readonly IntPoint Right = new(1, 0);
		public static readonly IntPoint Top = new(0, -1);
		public static readonly IntPoint Bottom = new(0, 1);

		public static readonly IEnumerable<IntPoint> NeighborOffsets = new[] { Left, Right, Top, Bottom };

		public int X;
		public int Y;

		public IEnumerable<IntPoint> Neighbors
		{
			get
			{
				foreach (var neighbor in NeighborOffsets)
				{
					yield return this + neighbor;
				}
			}
		}

		public IntPoint(int x, int y): this()
		{
			this.X = x;
			this.Y = y;
		}

		public override string ToString()
			=> $"[{X}, {Y}]";

		public override bool Equals(object? obj)
			=> obj is IntPoint point && Equals(point);

		public bool Equals(IntPoint other)
			=> X == other.X && Y == other.Y;

		public override int GetHashCode()
			=> (X, Y).GetHashCode();

		public static IntPoint operator +(IntPoint a, IntPoint b)
			=> new(a.X + b.X, a.Y + b.Y);

		public static IntPoint operator -(IntPoint a, IntPoint b)
			=> new(a.X - b.X, a.Y - b.Y);

		public static IntPoint operator *(IntPoint point, int scalar)
			=> new(point.X * scalar, point.Y * scalar);

		public static IntPoint operator -(IntPoint point)
			=> new(-point.X, -point.Y);

		public static bool operator ==(IntPoint lhs, IntPoint rhs)
			=> lhs.Equals(rhs);

		public static bool operator !=(IntPoint lhs, IntPoint rhs)
			=> !lhs.Equals(rhs);
	}
}