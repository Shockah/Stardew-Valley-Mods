using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shockah.AdventuresInTheMines.Map;
using Shockah.CommonModCode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using xTile.Layers;
using xTile.Tiles;

namespace Shockah.AdventuresInTheMines.Populators
{
	internal sealed class IcePuzzlePopulator : IMineShaftPopulator
	{
		private enum PopulatorTile
		{
			Empty,
			Dirt,
			Passable,
			BelowLadder,
			Blocked
		}

#if DEBUG
		private static char GetCharacterForTile(PopulatorTile tile)
		{
			return tile switch
			{
				PopulatorTile.Empty => '.',
				PopulatorTile.Dirt => 'o',
				PopulatorTile.Passable => '/',
				PopulatorTile.BelowLadder => 'V',
				PopulatorTile.Blocked => '#',
				_ => throw new ArgumentException($"{nameof(PopulatorTile)} has an invalid value."),
			};
		}
#endif

		private const int LadderTileIndex = 115;
		private const float MinimumFillRatio = 0.2f;
		private const float MaximumFillRatio = 0.35f;
		private const int MinimumRectangleGirth = 3;
		private const int MinimumInitialRectangleGirth = 5;
		private const int MinimumCardinalDistanceFromChestToIceBounds = 2;
		private const int MinimumDiagonalDistanceFromChestToIceBounds = 1;
		private const int MinimumArea = 60;
		private static readonly int[,] IceTileIndexes = new[,] { { 23 * 16 + 0, 24 * 16 + 0 }, { 23 * 16 + 1, 24 * 16 + 1 } };

		private IMonitor Monitor { get; init; }
		private ILootProvider LootProvider { get; init; }

		private readonly ConditionalWeakTable<MineShaft, Action> AfterPopulateActions = new();

		public IcePuzzlePopulator(IMonitor monitor, ILootProvider lootProvider)
		{
			this.Monitor = monitor;
			this.LootProvider = lootProvider;
		}

		private void QueueForAfterPopulate(MineShaft location, Action code)
		{
			if (AfterPopulateActions.TryGetValue(location, out var currentCode))
				code = () =>
				{
					currentCode();
					code();
				};
			AfterPopulateActions.AddOrUpdate(location, code);
		}

		public bool BeforePopulate(MineShaft location)
		{
			// finding ladder (starting) position
			// TODO: ladder position is actually already stored in MineShaft... but it's private
			var ladderPoint = FindLadder(location);
			if (ladderPoint is null)
			{
				Monitor.Log($"Could not find a ladder at location {location.Name}. Aborting {typeof(IcePuzzlePopulator)}.");
				return false;
			}
			IntPoint belowLadderPoint = new(ladderPoint.Value.X, ladderPoint.Value.Y + 1);

			// creating an occupancy map (whether each tile can be traversed or an object can be placed in their spot)
			ArrayMap<PopulatorTile> occupancyMap = new(point =>
			{
				if (point == belowLadderPoint)
					return PopulatorTile.BelowLadder;
				else if (location.isTileLocationOpenIgnoreFrontLayers(new(point.X, point.Y)) && location.isTileClearForMineObjects(point.X, point.Y))
					return PopulatorTile.Empty;
				else if (location.doesEitherTileOrTileIndexPropertyEqual(point.X, point.Y, "Type", "Back", "Dirt"))
					return PopulatorTile.Dirt;
				else if (location.isTileLocationOpenIgnoreFrontLayers(new(point.X, point.Y)) && location.isTilePlaceable(new(point.X, point.Y)))
					return PopulatorTile.Passable;
				else
					return PopulatorTile.Blocked;
			}, (int)(location.Map.DisplayWidth / 64f), (int)(location.Map.DisplayHeight / 64f));
			Monitor.Log($"\n{occupancyMap.ToString(GetCharacterForTile)}", LogLevel.Debug);

			// creating a reachable tile map - tiles reachable by the player from the ladder
			var reachableMap = FloodFill.Run(occupancyMap, belowLadderPoint, (map, point) => map[point] != PopulatorTile.Blocked);
			Monitor.Log($"\n{reachableMap.ToString(b => b ? '#' : '.')}", LogLevel.Debug);
			int reachableTileCount = reachableMap.Count(reachable => reachable);

			// finding all possible tile rectangles in the reachable tiles
			var possibleIceTiles = new ArrayMap<bool>(
				point => reachableMap[point] && occupancyMap[point] is PopulatorTile.Empty,
				reachableMap.Width, reachableMap.Height, reachableMap.MinX, reachableMap.MinY
			);
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
				var validRectangles = rectangles
					.Where(r =>
					{
						if (currentIceCount == 0)
						{
							if (Math.Min(r.Max.X - r.Min.X + 1, r.Max.Y - r.Min.Y + 1) < MinimumInitialRectangleGirth)
								return false;
							var (firstMin, firstMax) = rectangles.First!.Value;
							var firstArea = (firstMax.X - firstMin.X + 1) * (firstMax.Y - firstMin.Y + 1);
							var rArea = (r.Max.X - r.Min.X + 1) * (r.Max.Y - r.Min.Y + 1);
							return rArea >= firstArea * 0.75f;
						}

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
					}).ToList();

				if (validRectangles.Count == 0)
					break;
				var best = currentIceCount == 0
					? validRectangles[Game1.random.Next(validRectangles.Count)]
					: validRectangles.First();

				// TODO: split large rectangles, which should make more organic looking areas

				for (int y = best.Min.Y; y <= best.Max.Y; y++)
					for (int x = best.Min.X; x <= best.Max.X; x++)
						currentIceMap[new(x, y)] = true;
				rectangles.Remove(best);
			}

			Monitor.Log($"\n{currentIceMap.ToString(b => b ? '#' : '.')}", LogLevel.Debug);

			var iceTileCount = currentIceMap.Count(b => b);
			Monitor.Log($"Area: {iceTileCount} | Satisfied: {iceTileCount >= MinimumArea}", LogLevel.Debug);
			if (iceTileCount < MinimumArea)
				return false;

			// finding ice bounds
			var iceBounds = currentIceMap.FindBounds(b => b);
			if (iceBounds is null)
				return false;

			// placing chest
			IntPoint coordinateCenter = new((iceBounds.Value.Max.X + iceBounds.Value.Min.X) / 2, (iceBounds.Value.Max.Y + iceBounds.Value.Min.Y) / 2);
			foreach (var potentialChestPosition in coordinateCenter.GetSpiralingTiles(minDistanceFromCenter: 0, maxDistanceFromCenter: Math.Max(iceBounds.Value.Max.X - iceBounds.Value.Min.X + 1, iceBounds.Value.Max.Y - iceBounds.Value.Min.Y + 1)))
			{
				if (potentialChestPosition.X < currentIceMap.MinX || potentialChestPosition.Y < currentIceMap.MinX || potentialChestPosition.X > currentIceMap.MaxX || potentialChestPosition.Y > currentIceMap.MaxY)
					continue;
				if (!currentIceMap[potentialChestPosition])
					continue;

				for (int i = 1; i <= MinimumCardinalDistanceFromChestToIceBounds; i++)
				{
					if (!currentIceMap[new(potentialChestPosition.X - i, potentialChestPosition.Y)])
						goto potentialCheckPositionContinue;
					if (!currentIceMap[new(potentialChestPosition.X + i, potentialChestPosition.Y)])
						goto potentialCheckPositionContinue;
					if (!currentIceMap[new(potentialChestPosition.X, potentialChestPosition.Y - i)])
						goto potentialCheckPositionContinue;
					if (!currentIceMap[new(potentialChestPosition.X, potentialChestPosition.Y + i)])
						goto potentialCheckPositionContinue;
				}
				for (int i = 1; i <= MinimumDiagonalDistanceFromChestToIceBounds; i++)
				{
					if (!currentIceMap[new(potentialChestPosition.X - i, potentialChestPosition.Y - i)])
						goto potentialCheckPositionContinue;
					if (!currentIceMap[new(potentialChestPosition.X + i, potentialChestPosition.Y - i)])
						goto potentialCheckPositionContinue;
					if (!currentIceMap[new(potentialChestPosition.X - i, potentialChestPosition.Y + i)])
						goto potentialCheckPositionContinue;
					if (!currentIceMap[new(potentialChestPosition.X + 1, potentialChestPosition.Y + i)])
						goto potentialCheckPositionContinue;
				}

				QueueForAfterPopulate(location, () =>
				{
					Vector2 centerTileVector = new(potentialChestPosition.X, potentialChestPosition.Y);

					var featuresToRemove = location.largeTerrainFeatures
						.Where(f => centerTileVector.X >= f.currentTileLocation.X && centerTileVector.Y >= f.currentTileLocation.Y && centerTileVector.X <= f.currentTileLocation.X + 1 && centerTileVector.Y <= f.currentTileLocation.Y + 1)
						.ToList();
					foreach (var feature in featuresToRemove)
						location.largeTerrainFeatures.Remove(feature);

					location.objects[centerTileVector] = new Chest(0, LootProvider.GenerateLoot().ToList(), centerTileVector);
				});
				goto chestPositionFound;

				potentialCheckPositionContinue:;
			}

			// did not find a spot for the chest, giving up
			return false;
			chestPositionFound:;

			// creating the ice layer: upserting tile sheet
			var wallsAndFloorsTexturePath = "Maps\\walls_and_floors";
			if (!location.Map.TileSheets.TryFirst(t => t.ImageSource == wallsAndFloorsTexturePath, out var wallsAndFloorsTileSheet))
			{
				var wallsAndFloorsTexture = Game1.content.Load<Texture2D>(wallsAndFloorsTexturePath);
				wallsAndFloorsTileSheet = new TileSheet("x_WallsAndFloors", location.Map, wallsAndFloorsTexturePath, new(wallsAndFloorsTexture.Width / 16, wallsAndFloorsTexture.Height / 16), new(16, 16));
				location.Map.AddTileSheet(wallsAndFloorsTileSheet);
			}

			// creating the ice layer: new layer
			int layerIndex = 1;
			while (true)
			{
				if (!location.Map.Layers.Any(l => l.Id == $"Back{layerIndex}"))
					break;
				layerIndex++;
			}
			var iceLayer = new Layer($"Back{layerIndex}", location.Map, location.Map.DisplaySize, new(64, 64));
			location.Map.AddLayer(iceLayer);

			// creating the ice layer: populating
			for (int y = currentIceMap.MinY; y <= currentIceMap.MaxY; y++)
				for (int x = currentIceMap.MinX; x <= currentIceMap.MaxX; x++)
					if (currentIceMap[new(x, y)])
						iceLayer.Tiles[x, y] = new StaticTile(iceLayer, wallsAndFloorsTileSheet, BlendMode.Alpha, IceTileIndexes[x % IceTileIndexes.GetLength(0), y % IceTileIndexes.GetLength(1)]);

			return true;
		}

		public void AfterPopulate(MineShaft location)
		{
			if (!AfterPopulateActions.TryGetValue(location, out var code))
				return;
			AfterPopulateActions.Remove(location);
			code();
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