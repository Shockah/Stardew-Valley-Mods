using System.Collections.Generic;
using System.Linq;

namespace Shockah.FlexibleSprinklers
{
	internal struct IntPoint
	{
		internal static readonly IntPoint Zero = new IntPoint(0, 0);
		internal static readonly IntPoint One = new IntPoint(1, 1);
		internal static readonly IntPoint Left = new IntPoint(-1, 0);
		internal static readonly IntPoint Right = new IntPoint(1, 0);
		internal static readonly IntPoint Top = new IntPoint(0, -1);
		internal static readonly IntPoint Bottom = new IntPoint(0, 1);

		internal static readonly ICollection<IntPoint> NeighborOffsets = new List<IntPoint>() { Left, Right, Top, Bottom };

		internal int X;
		internal int Y;

		internal ICollection<IntPoint> Neighbors
		{
			get
			{
				var self = this;
				return NeighborOffsets.Select(e => e + self).ToList();
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
	}
}