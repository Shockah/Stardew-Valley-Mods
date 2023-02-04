using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shockah.AdventuresInTheMines.Config;
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

		private static readonly string IceLayerTexturePath = "Maps\\Festivals";
		private static readonly string IceLayerTileSheetName = "x_Festivals";
		private static readonly int[,] IceTileIndexes = new[,] { { 28 * 32 + 8 } };

		private const float MinimumFillRatio = 0.2f;
		private const float MaximumFillRatio = 0.35f;
		private const int MinimumRectangleGirth = 3;
		private const int MinimumInitialRectangleGirth = 5;
		private const int MinimumCardinalDistanceFromChestToIceBounds = 2;
		private const int MinimumDiagonalDistanceFromChestToIceBounds = 1;
		private const int MinimumArea = 60;

		private const float IceAligningStrength = 0.15f;

		private IceConfig Config { get; init; }
		private IMapOccupancyMapper MapOccupancyMapper { get; init; }
		private IReachableTileMapper ReachableTileMapper { get; init; }
		private ILootProvider LootProvider { get; init; }

		private readonly ConditionalWeakTable<MineShaft, StructRef<PreparedData>> PreparedDataTable = new();
		private readonly ConditionalWeakTable<MineShaft, RuntimeData> RuntimeDataTable = new();

		public IcePuzzlePopulator(IceConfig config, IMapOccupancyMapper mapOccupancyMapper, IReachableTileMapper reachableTileMapper, ILootProvider lootProvider)
		{
			this.Config = config;
			this.MapOccupancyMapper = mapOccupancyMapper;
			this.ReachableTileMapper = reachableTileMapper;
			this.LootProvider = lootProvider;
		}

		public double Prepare(MineShaft location, Random random)
		{
			// get config for location
			if (!Config.Enabled)
				return 0;
			var config = Config.Entries.GetMatchingConfig(location);
			if (config is null || config.Weight <= 0)
				return 0;

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
			var rectangles = new LinkedList<IntRectangle>(
				FindRectangles(possibleIceTiles)
					.Where(r => r.Width >= MinimumRectangleGirth && r.Height >= MinimumRectangleGirth)
					.OrderByDescending(r => r.Width * r.Height / Math.Sqrt(Math.Max(r.Width, r.Height)))
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
							if (Math.Min(r.Width, r.Height) < MinimumInitialRectangleGirth)
								return false;
							var first = rectangles.First!.Value;
							var firstArea = first.Width * first.Height;
							var rArea = r.Width * r.Height;
							return rArea >= firstArea * 0.75f;
						}

						foreach (var point in r.AllPointEnumerator())
						{
							if (currentIceMap[point])
								return true;
							if (point.X > currentIceMap.Bounds.Min.X && currentIceMap[new(point.X - 1, point.Y)])
								return true;
							if (point.X < currentIceMap.Bounds.Max.X && currentIceMap[new(point.X + 1, point.Y)])
								return true;
							if (point.Y > currentIceMap.Bounds.Min.Y && currentIceMap[new(point.X, point.Y - 1)])
								return true;
							if (point.Y < currentIceMap.Bounds.Max.Y && currentIceMap[new(point.X, point.Y + 1)])
								return true;
						}
						return false;
					}).ToList();

				if (validRectangles.Count == 0)
					break;
				var best = currentIceCount == 0
					? validRectangles[random.Next(validRectangles.Count)]
					: validRectangles.First();

				// TODO: split large rectangles, which should make more organic looking areas

				foreach (var point in best.AllPointEnumerator())
					currentIceMap[point] = true;
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
			foreach (var potentialChestPosition in coordinateCenter.GetSpiralingTiles(minDistanceFromCenter: 0, maxDistanceFromCenter: Math.Max(iceBounds.Value.Width, iceBounds.Value.Height)))
			{
				if (!currentIceMap.Bounds.Contains(potentialChestPosition))
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
			return config.Weight;
		}

		public void BeforePopulate(MineShaft location, Random random)
		{
		}

		public void AfterPopulate(MineShaft location, Random random)
		{
			if (!PreparedDataTable.TryGetValue(location, out var data))
				return;

			// creating the ice layer: upserting tile sheet
			if (!location.Map.TileSheets.TryFirst(t => t.ImageSource == IceLayerTexturePath, out var layerTileSheet))
			{
				var layerTexture = Game1.content.Load<Texture2D>(IceLayerTexturePath);
				layerTileSheet = new TileSheet(IceLayerTileSheetName, location.Map, IceLayerTexturePath, new(layerTexture.Width / 16, layerTexture.Height / 16), new(16, 16));
				location.Map.AddTileSheet(layerTileSheet);
			}

			// creating the ice layer: new layer
			int layerIndex = 1;
			while (true)
			{
				if (!location.Map.Layers.Any(l => l.Id == $"Back{layerIndex}"))
					break;
				layerIndex++;
			}
			var iceLayer = new Layer($"Back{layerIndex}", location.Map, location.Map.GetSize(), new(Game1.tileSize));
			location.Map.AddLayer(iceLayer);

			// creating the ice layer: populating
			foreach (var point in data.Value.IceMap.Bounds.AllPointEnumerator())
				if (data.Value.IceMap[point])
					iceLayer.Tiles[point.X, point.Y] = new StaticTile(iceLayer, layerTileSheet, BlendMode.Alpha, IceTileIndexes[point.X % IceTileIndexes.GetLength(0), point.Y % IceTileIndexes.GetLength(1)]);

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

		private static HashSet<IntRectangle> FindRectangles(IMap<bool>.WithKnownSize map, bool stateToLookFor = true)
		{
			List<IntRectangle> results = new();

			foreach (var point in map.Bounds.AllPointEnumerator())
			{
				if (map[point] != stateToLookFor)
					continue;

				// skipping tiles with all 4 empty spaces
				bool top = point.Y == map.Bounds.Min.Y || map[new(point.X, point.Y - 1)] == stateToLookFor;
				bool bottom = point.Y == map.Bounds.Max.Y || map[new(point.X, point.Y + 1)] == stateToLookFor;
				bool left = point.X == map.Bounds.Min.X || map[new(point.X - 1, point.Y)] == stateToLookFor;
				bool right = point.X == map.Bounds.Max.X || map[new(point.X + 1, point.Y)] == stateToLookFor;
				if ((top ? 1 : 0) + (bottom ? 1 : 0) + (left ? 1 : 0) + (right ? 1 : 0) > 3)
					continue;

				int maxWidth = 1;
				int maxHeight = 1;

				while (point.X + maxWidth - 1 < map.Bounds.Max.X && map[new(point.X + maxWidth, point.Y)] == stateToLookFor)
					maxWidth++;
				while (point.Y + maxHeight - 1 < map.Bounds.Max.Y && map[new(point.X, point.Y + maxHeight)] == stateToLookFor)
					maxHeight++;

				// finding all possible rectangles starting at this corner
				List<IntRectangle> possibleRectangles = new();
				for (int height = 1; height <= maxHeight; height++)
				{
					for (int width = 1; width <= maxWidth; width++)
					{
						for (int cellY = point.Y; cellY < point.Y + height; cellY++)
						{
							for (int cellX = point.X; cellX < point.X + width; cellX++)
							{
								if (map[new(cellX, cellY)] != stateToLookFor)
									goto cellLoopContinue;
							}
						}

						possibleRectangles.Add(new(point, width, height));

						cellLoopContinue:;
					}
				}

				// merge rectangles together
				foreach (var rectangle in ((IEnumerable<IntRectangle>)possibleRectangles).Reverse())
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

			return results.ToHashSet();
		}
	}
}