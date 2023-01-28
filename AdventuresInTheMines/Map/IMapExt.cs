using Shockah.CommonModCode;
using System;
using System.Text;

namespace Shockah.AdventuresInTheMines.Map
{
	public static class IMapExt
	{
		public static int Count<TTile>(this IMap<TTile>.WithKnownSize map, Func<IMap<TTile>.WithKnownSize, IntPoint, bool> predicate)
		{
			int count = 0;
			for (int y = map.MinY; y <= map.MaxY; y++)
				for (int x = map.MinX; x <= map.MaxX; x++)
					if (predicate(map, new(x, y)))
						count++;
			return count;
		}

		public static int Count<TTile>(this IMap<TTile>.WithKnownSize map, Func<TTile, bool> predicate)
			=> map.Count((map, point) => predicate(map[point]));

		public static (IntPoint Min, IntPoint Max)? FindBounds<TTile>(this IMap<TTile>.WithKnownSize map, Func<IMap<TTile>.WithKnownSize, IntPoint, bool> predicate)
		{
			int? minX = null;
			int? minY = null;
			int? maxX = null;
			int? maxY = null;

			for (int y = map.MinY; y <= map.MaxY; y++)
			{
				for (int x = map.MinX; x <= map.MaxX; x++)
				{
					if (!predicate(map, new(x, y)))
						continue;
					if (minX is null || minX.Value > x)
						minX = x;
					if (minY is null || minY.Value > y)
						minY = y;
					if (maxX is null || maxX.Value < x)
						maxX = x;
					if (maxY is null || maxY.Value < y)
						maxY = y;
				}
			}

			if (minX is null || minY is null || maxX is null || maxY is null)
				return null;
			else
				return (Min: new(minX.Value, minY.Value), Max: new(maxX.Value, maxY.Value));
		}

		public static (IntPoint Min, IntPoint Max)? FindBounds<TTile>(this IMap<TTile>.WithKnownSize map, Func<TTile, bool> predicate)
			=> map.FindBounds((map, point) => predicate(map[point]));

#if DEBUG
		public static string ToString<TTile>(this IMap<TTile>.WithKnownSize map, Func<TTile, char> charMapper)
		{
			StringBuilder sb = new();
			for (int y = map.MinY; y <= map.MaxY; y++)
			{
				if (y != map.MinY)
					sb.AppendLine();
				for (int x = map.MinX; x <= map.MaxX; x++)
					sb.Append(charMapper(map[new(x, y)]));
			}
			return $"{sb}";
		}
#endif
	}
}