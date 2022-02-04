using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.FlexibleSprinklers
{
	internal struct IntPoint: IEquatable<IntPoint>
	{
		internal static readonly IntPoint Zero = new IntPoint(0, 0);
		internal static readonly IntPoint One = new IntPoint(1, 1);
		internal static readonly IntPoint Left = new IntPoint(-1, 0);
		internal static readonly IntPoint Right = new IntPoint(1, 0);
		internal static readonly IntPoint Top = new IntPoint(0, -1);
		internal static readonly IntPoint Bottom = new IntPoint(0, 1);

		internal static readonly IEnumerable<IntPoint> NeighborOffsets = new List<IntPoint>() { Left, Right, Top, Bottom };

		internal int X;
		internal int Y;

		internal IEnumerable<IntPoint> Neighbors
		{
			get
			{
				foreach (var neighbor in NeighborOffsets)
				{
					yield return this + neighbor;
				}
			}
		}

		internal IntPoint(int x, int y): this()
		{
			this.X = x;
			this.Y = y;
		}

		public override string ToString()
		{
			return $"[{X}, {Y}]";
		}

		public override bool Equals(object obj)
		{
			return obj is IntPoint point && Equals(point);
		}

		public bool Equals(IntPoint other)
		{
			return X == other.X && Y == other.Y;
		}

		public override int GetHashCode()
		{
			return X << 16 | Y;
		}

		public static IntPoint operator +(IntPoint a, IntPoint b)
		{
			return new IntPoint(a.X + b.X, a.Y + b.Y);
		}

		public static IntPoint operator -(IntPoint a, IntPoint b)
		{
			return new IntPoint(a.X - b.X, a.Y - b.Y);
		}

		public static IntPoint operator *(IntPoint point, int scalar)
		{
			return new IntPoint(point.X * scalar, point.Y * scalar);
		}

		public static IntPoint operator -(IntPoint point)
		{
			return new IntPoint(-point.X, -point.Y);
		}

		public static bool operator ==(IntPoint lhs, IntPoint rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(IntPoint lhs, IntPoint rhs)
		{
			return !lhs.Equals(rhs);
		}
	}
}