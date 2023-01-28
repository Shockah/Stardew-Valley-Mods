using Shockah.AdventuresInTheMines.Map;
using Shockah.CommonModCode;
using StardewModdingAPI;
using StardewValley.Locations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.AdventuresInTheMines.Populators
{
	internal sealed class IcePopulator : IMineShaftPopulator
	{
		private enum Tile
		{
			Empty,
			Passable,
			BelowLadder,
			Blocked
		}

#if DEBUG
		private static char GetCharacterForTile(Tile tile)
		{
			return tile switch
			{
				Tile.Empty => '.',
				Tile.Passable => '/',
				Tile.BelowLadder => 'V',
				Tile.Blocked => '#',
				_ => throw new ArgumentException($"{nameof(Tile)} has an invalid value."),
			};
		}
#endif

		private const int LadderTileIndex = 115;
		private const float FillRatio = 0.5f;
		private const int MinimumRectangleGirth = 4;

		private IMonitor Monitor { get; init; }

		public IcePopulator(IMonitor monitor)
		{
			this.Monitor = monitor;
		}

		public void BeforePopulate(MineShaft location)
		{
			// TODO: ladder position is actually already stored in MineShaft
			var ladderPoint = FindLadder(location);
			if (ladderPoint is null)
			{
				Monitor.Log($"Could not find a ladder at location {location.Name}. Aborting {typeof(IcePopulator)}.");
				return;
			}

			IntPoint belowLadderPoint = new(ladderPoint.Value.X, ladderPoint.Value.Y + 1);

			ArrayMap<Tile> occupancyMap = new(point =>
			{
				if (point == belowLadderPoint)
					return Tile.BelowLadder;
				else if (location.isTileClearForMineObjects(point.X, point.Y))
					return Tile.Empty;
				else if (location.isTileLocationOpen(new(point.X, point.Y)))
					return Tile.Passable;
				else
					return Tile.Blocked;
			}, (int)(location.Map.DisplayWidth / 64f), (int)(location.Map.DisplayHeight / 64f));
			Monitor.Log($"\n{occupancyMap.ToString(GetCharacterForTile)}", LogLevel.Debug);
			var reachableMap = FloodFill.Run(occupancyMap, belowLadderPoint, (map, point) => map[point] != Tile.Blocked);
			Monitor.Log($"\n{reachableMap.ToString(b => b ? '#' : '.')}", LogLevel.Debug);
			int reachableTileCount = reachableMap.Count(reachable => reachable);

			var rectangles = new LinkedList<(IntPoint Min, IntPoint Max)>(
				FindRectangles(reachableMap)
					.Where(r => r.Max.X - r.Min.X + 1 >= MinimumRectangleGirth && r.Max.Y - r.Min.Y + 1 >= MinimumRectangleGirth)
					.OrderByDescending(r => (r.Max.X - r.Min.X - 1) * (r.Max.Y - r.Min.Y - 1) / Math.Sqrt(Math.Max(r.Max.X - r.Min.X - 1, r.Max.Y - r.Min.Y - 1)))
			);

			ArrayMap<bool> currentIceMap = new(false, reachableMap.Width, reachableMap.Height, reachableMap.MinX, reachableMap.MinY);
			while (rectangles.Count != 0)
			{
				int currentIceCount = currentIceMap.Count(ice => ice);
				if ((float)currentIceCount / reachableTileCount >= FillRatio)
					break;

				if (currentIceCount == 0)
				{
					var (min, max) = rectangles.First!.Value;
					rectangles.RemoveFirst();

					for (int y = min.Y; y <= max.Y; y++)
						for (int x = min.X; x <= max.X; x++)
							currentIceMap[new(x, y)] = true;
					continue;
				}

				var best = rectangles
					.Where(r =>
					{
						for (int y = r.Min.Y; y <= r.Max.Y; y++)
						{
							for (int x = r.Min.X; x <= r.Max.X; x++)
							{
								if (currentIceMap[new(x, y)])
									return true;
								if (x > currentIceMap.MinX && currentIceMap[new(x - 1, y)])
									return true;
								if (x < currentIceMap.MaxX && currentIceMap[new(x + 1, y)])
									return true;
								if (y > currentIceMap.MinY && currentIceMap[new(x, y - 1)])
									return true;
								if (y < currentIceMap.MaxY && currentIceMap[new(x, y + 1)])
									return true;
							}
						}
						return false;
					})
					//.Select(r =>
					//{
					//	ArrayMap<bool> newIceMap = currentIceMap.Clone();
					//	for (int y = r.Min.Y; y <= r.Max.Y; y++)
					//		for (int x = r.Min.X; x <= r.Max.X; x++)
					//			newIceMap[new(x, y)] = true;
					//	return (IceMap: newIceMap, Rectangle: r);
					//})
					//.OrderByDescending(e => e.IceMap.Count(ice => ice))
					.FirstOrNull();
				if (best is null)
					break;

				for (int y = best.Value.Min.Y; y <= best.Value.Max.Y; y++)
					for (int x = best.Value.Min.X; x <= best.Value.Max.X; x++)
						currentIceMap[new(x, y)] = true;
				rectangles.Remove(best.Value);
			}

			Monitor.Log($"\n{currentIceMap.ToString(b => b ? '#' : '.')}", LogLevel.Debug);
			Console.WriteLine();
		}

		private static IntPoint? FindLadder(MineShaft location)
		{
			for (int y = 0; y < location.Map.DisplayHeight / 64f; y++)
				for (int x = 0; x < location.Map.DisplayWidth / 64f; x++)
					if (location.Map.GetLayer("Buildings").Tiles[new(x, y)]?.TileIndex == LadderTileIndex)
						return new(x, y);
			return null;
		}

		private static HashSet<(IntPoint Min, IntPoint Max)> FindRectangles(IMap<bool>.WithKnownSize map, bool stateToLookFor = true)
		{
			List<(IntPoint Min, IntPoint Max)> rectangles = new();

			for (int y = map.MinY; y <= map.MaxY; y++)
			{
				for (int x = map.MinX; x <= map.MaxX; x++)
				{
					if (map[new(x, y)] != stateToLookFor)
						continue;

					bool top = y == map.MinY || map[new(x, y - 1)] == stateToLookFor;
					bool bottom = y == map.MaxY || map[new(x, y + 1)] == stateToLookFor;
					bool left = x == map.MinX || map[new(x - 1, y)] == stateToLookFor;
					bool right = x == map.MaxX || map[new(x + 1, y)] == stateToLookFor;
					if ((top ? 1 : 0) + (bottom ? 1 : 0) + (left ? 1 : 0) + (right ? 1 : 0) > 3)
						continue;

					int maxWidth = 1;
					int maxHeight = 1;

					while (x + maxWidth - 1 < map.MaxX && map[new(x + maxWidth, y)] == stateToLookFor)
						maxWidth++;
					while (y + maxHeight - 1 < map.MaxY && map[new(x, y + maxHeight)] == stateToLookFor)
						maxHeight++;

					List<(IntPoint Min, IntPoint Max)> bestRectangles = new();
					int? bestRectangleArea = null;
					for (int height = 1; height <= maxHeight; height++)
					{
						for (int width = 1; width <= maxWidth; width++)
						{
							var area = width * height;
							if (bestRectangleArea is not null && bestRectangleArea.Value > area)
								continue;

							for (int cellY = y; cellY < y + height; cellY++)
							{
								for (int cellX = x; cellX < x + width; cellX++)
								{
									if (map[new(cellX, cellY)] != stateToLookFor)
										goto cellLoopContinue;
								}
							}

							if (bestRectangleArea is null || bestRectangleArea.Value < area)
								bestRectangles.Clear();
							bestRectangleArea = area;
							bestRectangles.Add((Min: new(x, y), Max: new(x + width - 1, y + height - 1)));

							cellLoopContinue:;
						}
					}

					// merge rectangles together
					foreach (var rectangle in bestRectangles)
					{
						for (int i = rectangles.Count - 1; i >= 0; i--)
						{
							var (existingMin, existingMax) = rectangles[i];
							if (existingMin.X <= rectangle.Min.X && existingMin.Y <= rectangle.Min.Y && existingMax.X >= rectangle.Max.X && existingMax.Y >= rectangle.Max.Y)
								goto bestRectanglesContinue;
							if (existingMin.X > rectangle.Min.X && existingMin.Y > rectangle.Min.Y && existingMax.X < rectangle.Max.X && existingMax.Y < rectangle.Max.Y)
							{
								rectangles.RemoveAt(i);
								goto existingRectanglesBreak;
							}
						}
						existingRectanglesBreak:;

						rectangles.Add(rectangle);

						bestRectanglesContinue:;
					}
				}
			}

			return rectangles.ToHashSet();
		}
	}
}