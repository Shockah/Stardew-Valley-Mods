using Shockah.CommonModCode;
using System;

namespace Shockah.AdventuresInTheMines.Map
{
	internal sealed class ArrayMap<TTile> : IMap<TTile>.WithKnownSize, IMap<TTile>.Writable
	{
		public TTile this[IntPoint point]
		{
			get => Array[point.X + MinX, point.Y + MinY];
			set => Array[point.X + MinX, point.Y + MinY] = value;
		}

		public int Width { get; init; }
		public int Height { get; init; }

		public int MinX { get; init; }
		public int MinY { get; init; }

		public int MaxX
			=> MinX + Width - 1;

		public int MaxY
			=> MinY + Height - 1;

		private readonly TTile[,] Array;

		public ArrayMap(IMap<TTile>.WithKnownSize map)
		{
			this.Width = map.Width;
			this.Height = map.Height;
			this.MinX = map.MinX;
			this.MinY = map.MinY;
			this.Array = new TTile[Width, Height];

			for (int y = MinY; y <= MaxY; y++)
				for (int x = MinX; x <= MaxX; x++)
					Array[x - MinX, y - MinY] = map[new(x, y)];
		}

		public ArrayMap(TTile defaultTile, int width, int height, int minX = 0, int minY = 0) : this(_ => defaultTile, width, height, minX, minY) { }

		public ArrayMap(Func<IntPoint, TTile> defaultTile, int width, int height, int minX = 0, int minY = 0)
		{
			this.Width = width;
			this.Height = height;
			this.MinX = minX;
			this.MinY = minY;
			this.Array = new TTile[width, height];

			for (int y = MinY; y <= MaxY; y++)
				for (int x = MinX; x <= MaxX; x++)
					Array[x - MinX, y - MinY] = defaultTile(new(x, y));
		}

		public override bool Equals(object? obj)
		{
			if (obj is not ArrayMap<TTile> other)
				return false;
			if (other.MinX != MinX || other.MinY != MinY || other.MaxX != MaxX || other.MaxY != MaxY)
				return false;
			for (int y = MinY; y <= MaxY; y++)
				for (int x = MinX; x <= MaxX; x++)
					if (!Equals(other[new(x, y)], this[new(x, y)]))
						return false;
			return true;
		}
	}
}