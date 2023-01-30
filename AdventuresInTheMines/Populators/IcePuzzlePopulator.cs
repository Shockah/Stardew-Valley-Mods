using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shockah.CommonModCode;
using Shockah.CommonModCode.Map;
using Shockah.CommonModCode.Stardew;
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
		private readonly struct PreparedData
		{
			public IntPoint ChestPosition { get; init; }
			public IMap<bool>.WithKnownSize IceMap { get; init; }
		}

		private sealed class RuntimeData
		{
			public IMap<bool>.WithKnownSize IceMap { get; init; }
			public Dictionary<long, Vector2> IceLockedVelocities { get; init; } = new();

			public RuntimeData(IMap<bool>.WithKnownSize iceMap)
			{
				this.IceMap	= iceMap;
			}
		}

		private static readonly int[,] IceTileIndexes = new[,] { { 28 * 32 + 8 } };

		private const float MinimumFillRatio = 0.2f;
		private const float MaximumFillRatio = 0.35f;
		private const int MinimumRectangleGirth = 3;
		private const int MinimumInitialRectangleGirth = 5;
		private const int MinimumCardinalDistanceFromChestToIceBounds = 2;
		private const int MinimumDiagonalDistanceFromChestToIceBounds = 1;
		private const int MinimumArea = 60;

		private const float IceAligningStrength = 0.15f;

		private IMapOccupancyMapper MapOccupancyMapper { get; init; }
		private IReachableTileMapper ReachableTileMapper { get; init; }
		private ILootProvider LootProvider { get; init; }

		private readonly ConditionalWeakTable<MineShaft, StructRef<PreparedData>> PreparedDataTable = new();
		private readonly ConditionalWeakTable<MineShaft, RuntimeData> RuntimeDataTable = new();

		public IcePuzzlePopulator(IMapOccupancyMapper mapOccupancyMapper, IReachableTileMapper reachableTileMapper, ILootProvider lootProvider)
		{
			this.MapOccupancyMapper = mapOccupancyMapper;
			this.ReachableTileMapper = reachableTileMapper;
			this.LootProvider = lootProvider;
		}

		public double Prepare(MineShaft location, Random random)
		{
			double weight = GetWeight(location);
			if (weight <= 0)
				return weight;

			// creating an occupancy map (whether each tile can be traversed or an object can be placed in their spot)
			var occupancyMap = new OutOfBoundsValuesMap<IMapOccupancyMapper.Tile>(
				MapOccupancyMapper.MapOccupancy(location),
				IMapOccupancyMapper.Tile.Blocked
			);

			// creating a reachable tile map - tiles reachable by the player from the ladder
			var reachableTileMap = new OutOfBoundsValuesMap<bool>(
				ReachableTileMapper.MapReachableTiles(location),
				false
			);
			int reachableTileCount = reachableTileMap.Count(reachable => reachable);

			// finding all possible tile rectangles in the reachable tiles
			var possibleIceTiles = new ArrayMap<bool>(
				point => reachableTileMap[point] && occupancyMap[point] is IMapOccupancyMapper.Tile.Empty,
				reachableTileMap.Bounds.Width, reachableTileMap.Bounds.Height, reachableTileMap.Bounds.Min.X, reachableTileMap.Bounds.Min.Y
			);
			var rectangles = new LinkedList<(IntPoint Min, IntPoint Max)>(
				FindRectangles(possibleIceTiles)
					.Where(r => r.Max.X - r.Min.X + 1 >= MinimumRectangleGirth && r.Max.Y - r.Min.Y + 1 >= MinimumRectangleGirth)
					.OrderByDescending(r => (r.Max.X - r.Min.X - 1) * (r.Max.Y - r.Min.Y - 1) / Math.Sqrt(Math.Max(r.Max.X - r.Min.X - 1, r.Max.Y - r.Min.Y - 1)))
			);

			// creating a map of ice tiles out of the largest (and "squarest") rectangles
			float fillRatio = MinimumFillRatio + (float)(random.NextDouble() * (MaximumFillRatio - MinimumFillRatio));
			ArrayMap<bool> currentIceMap = new(false, reachableTileMap.Bounds.Width, reachableTileMap.Bounds.Height, reachableTileMap.Bounds.Min.X, reachableTileMap.Bounds.Min.Y);
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
			var wallsAndFloorsTexturePath = "Maps\\Festivals";
			if (!location.Map.TileSheets.TryFirst(t => t.ImageSource == wallsAndFloorsTexturePath, out var wallsAndFloorsTileSheet))
			{
				var wallsAndFloorsTexture = Game1.content.Load<Texture2D>(wallsAndFloorsTexturePath);
				wallsAndFloorsTileSheet = new TileSheet("x_Festivals", location.Map, wallsAndFloorsTexturePath, new(wallsAndFloorsTexture.Width / 16, wallsAndFloorsTexture.Height / 16), new(16, 16));
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

			RuntimeDataTable.AddOrUpdate(location, new RuntimeData(data.Value.IceMap));
		}

		public void OnUpdateTicked(MineShaft location)
		{
			if (!RuntimeDataTable.TryGetValue(location, out var data))
				return;

			foreach (var player in Game1.getAllFarmers())
			{
				if (player.currentLocation != location)
					continue;

				IntPoint tile = new(player.getTileX(), player.getTileY());
				bool isOnIce = data.IceMap[tile];

				if (isOnIce)
				{
					if (!data.IceLockedVelocities.TryGetValue(player.UniqueMultiplayerID, out var iceLockedVelocity))
					{
						float absXVelocity = Math.Abs(player.Position.X - player.lastPosition.X);
						float absYVelocity = Math.Abs(player.Position.Y - player.lastPosition.Y);
						if (absXVelocity < 1 && absYVelocity < 1)
							continue;

						if (absXVelocity >= absYVelocity)
							iceLockedVelocity = new(Math.Sign(player.Position.X - player.lastPosition.X) * player.getMovementSpeed(), 0);
						else
							iceLockedVelocity = new(0, Math.Sign(player.Position.Y - player.lastPosition.Y) * player.getMovementSpeed());
						data.IceLockedVelocities[player.UniqueMultiplayerID] = iceLockedVelocity;
					}

					Vector2 alignedPosition;
					if (Math.Abs(iceLockedVelocity.X) >= Math.Abs(iceLockedVelocity.Y))
						alignedPosition = new(player.Position.X, (float)Math.Round((player.Position.Y - 16f) / 64f) * 64f + 16f);
					else
						alignedPosition = new((float)Math.Round(player.Position.X / 64f) * 64f, player.Position.Y);
					var alignmentOffset = (alignedPosition - player.Position) * IceAligningStrength;

					var nextPosition = player.GetBoundingBox();
					nextPosition.X += (int)(iceLockedVelocity.X + alignmentOffset.X);
					nextPosition.Y += (int)(iceLockedVelocity.Y + alignmentOffset.Y);

					var nextPosition2 = new Rectangle(nextPosition.X, nextPosition.Y, nextPosition.Width, nextPosition.Height);
					nextPosition2.X += (int)(iceLockedVelocity.X);
					nextPosition2.Y += (int)(iceLockedVelocity.Y);

					if (location.isCollidingPosition(nextPosition, Game1.viewport, isFarmer: true, damagesFarmer: 0, glider: false, player) || location.isCollidingPosition(nextPosition2, Game1.viewport, isFarmer: true, damagesFarmer: 0, glider: false, player))
						data.IceLockedVelocities.Remove(player.UniqueMultiplayerID);
					else
						player.Position = player.lastPosition + iceLockedVelocity + alignmentOffset;
				}
				else
				{
					data.IceLockedVelocities.Remove(player.UniqueMultiplayerID);
				}
			}
		}

		public bool HandleChestOpen(MineShaft location, Chest chest)
		{
			if (!RuntimeDataTable.TryGetValue(location, out var data))
				return false;

			var player = chest.GetMutex().GetCurrentOwner() ?? Game1.player;
			if (data.IceLockedVelocities.ContainsKey(player.UniqueMultiplayerID))
				return true;

			return false;
		}

		private static double GetWeight(MineShaft location)
		{
			var isSlimeAreaGetter = AccessTools.PropertyGetter(typeof(MineShaft), "isSlimeArea");
			var isSlime = (bool)isSlimeAreaGetter.Invoke(location, null)!;
			if (isSlime)
				return 0;

			var isMonsterAreaGetter = AccessTools.PropertyGetter(typeof(MineShaft), "isMonsterArea");
			var isMonster = (bool)isMonsterAreaGetter.Invoke(location, null)!;
			if (isMonster)
				return 0;

			if (location.mineLevel > 0 && location.mineLevel < MineShaft.mineFrostLevel)
				return 1.0 / 3.0;
			else if (location.mineLevel > MineShaft.mineFrostLevel && location.mineLevel < MineShaft.mineLavaLevel)
				return location.GetAdditionalDifficulty() > 0 ? 1.0 / 3.0 : 1.0;
			else if (location.mineLevel > MineShaft.mineLavaLevel && location.mineLevel < MineShaft.bottomOfMineLevel)
				return 0;
			else if (location.mineLevel >= MineShaft.desertArea)
				return 1.0 / 3.0;
			else
				return 0;
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