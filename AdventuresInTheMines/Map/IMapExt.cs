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