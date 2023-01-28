using Shockah.AdventuresInTheMines.Map;
using Shockah.CommonModCode;
using System;
using System.Collections.Generic;

namespace Shockah.AdventuresInTheMines
{
	internal static class FloodFill
	{
		public static IMap<bool> Run<TTile>(IMap<TTile> map, IntPoint startPoint, Func<IMap<TTile>, IntPoint, bool> canVisit)
		{
			if (map is IMap<TTile>.WithKnownSize knownSizeMap)
				return Run(knownSizeMap, startPoint, (map, point) => canVisit(map, point));

			bool checkingForValue = canVisit(map, startPoint);
			DictionaryMap<bool> outputMap = new(!checkingForValue);
			Run(map, startPoint, canVisit, outputMap, checkingForValue);
			return outputMap;
		}

		public static IMap<bool>.WithKnownSize Run<TTile>(IMap<TTile>.WithKnownSize map, IntPoint startPoint, Func<IMap<TTile>, IntPoint, bool> canVisit)
		{
			bool checkingForValue = canVisit(map, startPoint);
			ArrayMap<bool> outputMap = new(!checkingForValue, map.Width, map.Height, map.MinX, map.MinY);
			Run(map, startPoint, canVisit, outputMap, checkingForValue);
			return outputMap;
		}

		private static void Run<TTile>(IMap<TTile> inputMap, IntPoint startPoint, Func<IMap<TTile>, IntPoint, bool> canVisit, IMap<bool>.Writable outputMap, bool checkingForValue)
		{
			LinkedList<IntPoint> toCheck = new();
			HashSet<IntPoint> visited = new();
			toCheck.AddLast(startPoint);

			IMap<TTile>.WithKnownSize? knownSizeMap = inputMap as IMap<TTile>.WithKnownSize;
			while (toCheck.Count != 0)
			{
				var point = toCheck.First!.Value;
				toCheck.RemoveFirst();

				if (visited.Contains(point))
					continue;
				visited.Add(point);

				bool pointVisitable = canVisit(inputMap, point);
				if (pointVisitable != checkingForValue)
					continue;

				outputMap[point] = checkingForValue;
				foreach (var neighbor in point.Neighbors)
				{
					if (knownSizeMap is not null)
					{
						if (neighbor.X < knownSizeMap.MinX)
							continue;
						if (neighbor.Y < knownSizeMap.MinY)
							continue;
						if (neighbor.X > knownSizeMap.MaxX)
							continue;
						if (neighbor.Y > knownSizeMap.MaxY)
							continue;
					}

					toCheck.AddLast(neighbor);
				}
			}
		}
	}
}