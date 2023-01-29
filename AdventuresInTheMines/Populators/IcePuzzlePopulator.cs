using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shockah.AdventuresInTheMines.Map;
using Shockah.CommonModCode;
using Shockah.CommonModCode.Stardew;
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

		private readonly struct PreparedData
		{
			public IntPoint ChestPosition { get; init; }
			public IMap<bool>.WithKnownSize IceMap { get; init; }
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
		private static readonly int[,] IceTileIndexes = new[,] { { 23 * 16 + 0, 24 * 16 + 0 }, { 23 * 16 + 1, 24 * 16 + 1 } };

		private const float MinimumFillRatio = 0.2f;
		private const float MaximumFillRatio = 0.35f;
		private const int MinimumRectangleGirth = 3;
		private const int MinimumInitialRectangleGirth = 5;
		private const int MinimumCardinalDistanceFromChestToIceBounds = 2;
		private const int MinimumDiagonalDistanceFromChestToIceBounds = 1;
		private const int MinimumArea = 60;

		private IMonitor Monitor { get; init; }
		private ILootProvider LootProvider { get; init; }

		private readonly ConditionalWeakTable<MineShaft, StructRef<PreparedData>> PreparedDataTable = new();

		public IcePuzzlePopulator(IMonitor monitor, ILootProvider lootProvider)
		{
			this.Monitor = monitor;
			this.LootProvider = lootProvider;
		}

		public double Prepare(MineShaft location, Random random)
		{
			double weight = GetWeight(location);
			if (weight <= 0)
				return weight;

			// finding ladder (starting) position
			// TODO: ladder position is actually already stored in MineShaft... but it's private
			var ladderPoint = FindLadder(location);
			if (ladderPoint is null)
			{
				Monitor.Log($"Could not find a ladder at location {location.Name}. Aborting {typeof(IcePuzzlePopulator)}.");
				return 0;
			}
			IntPoint belowLadderPoint = new(ladderPoint.Value.X, ladderPoint.Value.Y + 1);

			// creating an occupancy map (whether each tile can be traversed or an object can be placed in their spot)
			var occupancyMap = new OutOfBoundsValuesMap<PopulatorTile>(
				new ArrayMap<PopulatorTile>(point =>
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
				}, (int)(location.Map.DisplayWidth / 64f), (int)(location.Map.DisplayHeight / 64f)),
				PopulatorTile.Blocked
			);

			// creating a reachable tile map - tiles reachable by the player from the ladder
			var reachableMap = FloodFill.Run(occupancyMap, belowLadderPoint, (map, point) => map[point] != PopulatorTile.Blocked);
			int reachableTileCount = reachableMap.Count(reachable => reachable);

			// finding all possible tile rectangles in the reachable tiles
			var possibleIceTiles = new ArrayMap<bool>(
				point => reachableMap[point] && occupancyMap[point] is PopulatorTile.Empty,
				reachableMap.Bounds.Width, reachableMap.Bounds.Height, reachableMap.Bounds.Min.X, reachableMap.Bounds.Min.Y
			);
			var rectangles = new LinkedList<(IntPoint Min, IntPoint Max)>(
				FindRectangles(possibleIceTiles)
					.Where(r => r.Max.X - r.Min.X + 1 >= MinimumRectangleGirth && r.Max.Y - r.Min.Y + 1 >= MinimumRectangleGirth)
					.OrderByDescending(r => (r.Max.X - r.Min.X - 1) * (r.Max.Y - r.Min.Y - 1) / Math.Sqrt(Math.Max(r.Max.X - r.Min.X - 1, r.Max.Y - r.Min.Y - 1)))
			);

			// creating a map of ice tiles out of the largest (and "squarest") rectangles
			float fillRatio = MinimumFillRatio + (float)(random.NextDouble() * (MaximumFillRatio - MinimumFillRatio));
			ArrayMap<bool> currentIceMap = new(false, reachableMap.Bounds.Width, reachableMap.Bounds.Height, reachableMap.Bounds.Min.X, reachableMap.Bounds.Min.Y);
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
								if (x > currentIceMap.Bounds.Min.X && currentIceMap[new(x - 1, y)])
									return true;
								if (x < currentIceMap.Bounds.Max.X && currentIceMap[new(x + 1, y)])
									return true;
								if (y > currentIceMap.Bounds.Min.Y && currentIceMap[new(x, y - 1)])
									return true;
								if (y < currentIceMap.Bounds.Max.Y && currentIceMap[new(x, y + 1)])
									return true;
							}
						}
						return false;
					}).ToList();

				if (validRectangles.Count == 0)
					break;
				var best = currentIceCount == 0
					? validRectangles[random.Next(validRectangles.Count)]
					: validRectangles.First();

				// TODO: split large rectangles, which should make more organic looking areas

				for (int y = best.Min.Y; y <= best.Max.Y; y++)
					for (int x = best.Min.X; x <= best.Max.X; x++)
						currentIceMap[new(x, y)] = true;
				rectangles.Remove(best);
			}

			var iceTileCount = currentIceMap.Count(b => b);
			if (iceTileCount < MinimumArea)
				return 0;

			// finding ice bounds
			var iceBounds = currentIceMap.FindBounds(b => b);
			if (iceBounds is null)
				return 0;

			// placing chest
			IntPoint coordinateCenter = new((iceBounds.Value.Max.X + iceBounds.Value.Min.X) / 2, (iceBounds.Value.Max.Y + iceBounds.Value.Min.Y) / 2);
			IntPoint chestPosition = default;
			foreach (var potentialChestPosition in coordinateCenter.GetSpiralingTiles(minDistanceFromCenter: 0, maxDistanceFromCenter: Math.Max(iceBounds.Value.Max.X - iceBounds.Value.Min.X + 1, iceBounds.Value.Max.Y - iceBounds.Value.Min.Y + 1)))
			{
				if (potentialChestPosition.X < currentIceMap.Bounds.Min.X || potentialChestPosition.Y < currentIceMap.Bounds.Min.X || potentialChestPosition.X > currentIceMap.Bounds.Max.X || potentialChestPosition.Y > currentIceMap.Bounds.Max.Y)
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

				chestPosition = potentialChestPosition;
				goto chestPositionFound;

				potentialCheckPositionContinue:;
			}

			// did not find a spot for the chest, giving up
			return 0;
			chestPositionFound:;

			PreparedDataTable.AddOrUpdate(location, new PreparedData() { ChestPosition = chestPosition, IceMap = currentIceMap });
			return weight;
		}

		public void BeforePopulate(MineShaft location, Random random)
		{
		}

		public void AfterPopulate(MineShaft location, Random random)
		{
			if (!PreparedDataTable.TryGetValue(location, out var data))
				return;

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
			for (int y = data.Value.IceMap.Bounds.Min.Y; y <= data.Value.IceMap.Bounds.Max.Y; y++)
				for (int x = data.Value.IceMap.Bounds.Min.X; x <= data.Value.IceMap.Bounds.Max.X; x++)
					if (data.Value.IceMap[new(x, y)])
						iceLayer.Tiles[x, y] = new StaticTile(iceLayer, wallsAndFloorsTileSheet, BlendMode.Alpha, IceTileIndexes[x % IceTileIndexes.GetLength(0), y % IceTileIndexes.GetLength(1)]);

			// create chest
			location.RemoveAllPlaceables(data.Value.ChestPosition);
			Vector2 chestPositionVector = new(data.Value.ChestPosition.X, data.Value.ChestPosition.Y);
			location.objects[chestPositionVector] = new Chest(0, LootProvider.GenerateLoot().ToList(), chestPositionVector);
		}

		private static double GetWeight(MineShaft location)
		{
			if (location.mineLevel > 0 && location.mineLevel < MineShaft.mineFrostLevel)
				return 1.0 / 3.0;
			else if (location.mineLevel > MineShaft.mineFrostLevel && location.mineLevel < MineShaft.mineLavaLevel)
				return 1;
			else if (location.mineLevel > MineShaft.mineLavaLevel && location.mineLevel < MineShaft.bottomOfMineLevel)
				return 0;
			else if (location.mineLevel >= MineShaft.desertArea)
				return 1.0 / 3.0;
			else
				return 0;
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

			for (int y = map.Bounds.Min.Y; y <= map.Bounds.Max.Y; y++)
			{
				for (int x = map.Bounds.Min.X; x <= map.Bounds.Max.X; x++)
				{
					if (map[new(x, y)] != stateToLookFor)
						continue;

					// skipping tiles with all 4 empty spaces
					bool top = y == map.Bounds.Min.Y || map[new(x, y - 1)] == stateToLookFor;
					bool bottom = y == map.Bounds.Max.Y || map[new(x, y + 1)] == stateToLookFor;
					bool left = x == map.Bounds.Min.X || map[new(x - 1, y)] == stateToLookFor;
					bool right = x == map.Bounds.Max.X || map[new(x + 1, y)] == stateToLookFor;
					if ((top ? 1 : 0) + (bottom ? 1 : 0) + (left ? 1 : 0) + (right ? 1 : 0) > 3)
						continue;

					int maxWidth = 1;
					int maxHeight = 1;

					while (x + maxWidth - 1 < map.Bounds.Max.X && map[new(x + maxWidth, y)] == stateToLookFor)
						maxWidth++;
					while (y + maxHeight - 1 < map.Bounds.Max.Y && map[new(x, y + maxHeight)] == stateToLookFor)
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