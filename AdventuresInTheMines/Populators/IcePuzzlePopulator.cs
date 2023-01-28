using Shockah.AdventuresInTheMines.Map;
using Shockah.CommonModCode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.AdventuresInTheMines.Populators
{
	internal sealed class IcePuzzlePopulator : IMineShaftPopulator
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
		private const float MinimumFillRatio = 0.25f;
		private const float MaximumFillRatio = 0.4f;
		private const int MinimumRectangleGirth = 4;
		private const int MinimumArea = 60;

		private IMonitor Monitor { get; init; }

		public IcePuzzlePopulator(IMonitor monitor)
		{
			this.Monitor = monitor;
		}

		public void BeforePopulate(MineShaft location)
		{
			// finding ladder (starting) position
			// TODO: ladder position is actually already stored in MineShaft... but it's private
			var ladderPoint = FindLadder(location);
			if (ladderPoint is null)
			{
				Monitor.Log($"Could not find a ladder at location {location.Name}. Aborting {typeof(IcePuzzlePopulator)}.");
				return;
			}
			IntPoint belowLadderPoint = new(ladderPoint.Value.X, ladderPoint.Value.Y + 1);

			// creating an occupancy map (whether each tile can be traversed or an object can be placed in their spot)
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

			// creating a reachable tile map - tiles reachable by the player from the ladder
			var reachableMap = FloodFill.Run(occupancyMap, belowLadderPoint, (map, point) => map[point] != Tile.Blocked);
			Monitor.Log($"\n{reachableMap.ToString(b => b ? '#' : '.')}", LogLevel.Debug);
			int reachableTileCount = reachableMap.Count(reachable => reachable);

			// finding all possible tile rectangles in the reachable tiles
			var possibleIceTiles = new ArrayMap<bool>(point => reachableMap[point] && point != belowLadderPoint, reachableMap.Width, reachableMap.Height, reachableMap.MinX, reachableMap.MinY);
			var rectangles = new LinkedList<(IntPoint Min, IntPoint Max)>(
				FindRectangles(possibleIceTiles)
					.Where(r => r.Max.X - r.Min.X + 1 >= MinimumRectangleGirth && r.Max.Y - r.Min.Y + 1 >= MinimumRectangleGirth)
					.OrderByDescending(r => (r.Max.X - r.Min.X - 1) * (r.Max.Y - r.Min.Y - 1) / Math.Sqrt(Math.Max(r.Max.X - r.Min.X - 1, r.Max.Y - r.Min.Y - 1)))
			);

			// creating a map of ice tiles out of the largest (and "squarest") rectangles
			float fillRatio = MinimumFillRatio + (float)(Game1.random.NextDouble() * (MaximumFillRatio - MinimumFillRatio));
			Monitor.Log($"Fill ratio: {fillRatio}", LogLevel.Debug);
			ArrayMap<bool> currentIceMap = new(false, reachableMap.Width, reachableMap.Height, reachableMap.MinX, reachableMap.MinY);
			while (rectangles.Count != 0)
			{
				int currentIceCount = currentIceMap.Count(ice => ice);
				if ((float)currentIceCount / reachableTileCount >= fillRatio)
					break;

				// TODO: validate if the created region is wide enough
				var best = rectangles
					.Where(r =>
					{
						if (currentIceCount == 0)
							return true;

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
					.FirstOrNull();
				if (best is null)
					break;

				// TODO: split large rectangles, which should make more organic looking areas

				for (int y = best.Value.Min.Y; y <= best.Value.Max.Y; y++)
					for (int x = best.Value.Min.X; x <= best.Value.Max.X; x++)
						currentIceMap[new(x, y)] = true;
				rectangles.Remove(best.Value);
			}

			Monitor.Log($"\n{currentIceMap.ToString(b => b ? '#' : '.')}", LogLevel.Debug);

			var iceTileCount = currentIceMap.Count(b => b);
			Monitor.Log($"Area: {iceTileCount} | Satisfied: {iceTileCount >= MinimumArea}", LogLevel.Debug);
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
			List<(IntPoint Min, IntPoint Max)> results = new();

			for (int y = map.MinY; y <= map.MaxY; y++)
			{
				for (int x = map.MinX; x <= map.MaxX; x++)
				{
					if (map[new(x, y)] != stateToLookFor)
						continue;

					// skipping tiles with all 4 empty spaces
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

					// finding all possible rectangles starting at this corner
					List<(IntPoint Min, IntPoint Max)> possibleRectangles = new();
					for (int height = 1; height <= maxHeight; height++)
					{
						for (int width = 1; width <= maxWidth; width++)
						{
							for (int cellY = y; cellY < y + height; cellY++)
							{
								for (int cellX = x; cellX < x + width; cellX++)
								{
									if (map[new(cellX, cellY)] != stateToLookFor)
										goto cellLoopContinue;
								}
							}

							possibleRectangles.Add((Min: new(x, y), Max: new(x + width - 1, y + height - 1)));

							cellLoopContinue:;
						}
					}

					// merge rectangles together
					foreach (var rectangle in ((IEnumerable<(IntPoint Min, IntPoint Max)>)possibleRectangles).Reverse())
					{
						for (int i = results.Count - 1; i >= 0; i--)
						{
							var (existingMin, existingMax) = results[i];
							if (existingMin.X <= rectangle.Min.X && existingMin.Y <= rectangle.Min.Y && existingMax.X >= rectangle.Max.X && existingMax.Y >= rectangle.Max.Y)
								goto bestRectanglesContinue;
							if (existingMin.X > rectangle.Min.X && existingMin.Y > rectangle.Min.Y && existingMax.X < rectangle.Max.X && existingMax.Y < rectangle.Max.Y)
							{
								results.RemoveAt(i);
								goto existingRectanglesBreak;
							}
						}
						existingRectanglesBreak:;

						results.Add(rectangle);

						bestRectanglesContinue:;
					}
				}
			}

			return results.ToHashSet();
		}
	}
}