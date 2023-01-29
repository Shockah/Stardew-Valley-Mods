using HarmonyLib;
using Microsoft.Xna.Framework;
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
using xTile.Tiles;

namespace Shockah.AdventuresInTheMines.Populators
{
	internal sealed class DisarmablePuzzlePopulator : IMineShaftPopulator
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
			public HashSet<IntPoint> ButtonPositions { get; init; }
		}

		private sealed class RuntimeData
		{
			public IntPoint ChestPosition { get; init; }
			public HashSet<IntPoint> ButtonPositionsLeft { get; init; }

			public RuntimeData(IntPoint chestPosition, HashSet<IntPoint> buttonPositions)
			{
				this.ChestPosition = chestPosition;
				this.ButtonPositionsLeft = buttonPositions;
			}
		}

		private const int LadderTileIndex = 115;
		private const int ButtonTileIndex = 204;
		private const int NoButtonTileIndex = 169;

		private IMonitor Monitor { get; init; }
		private ILootProvider LootProvider { get; init; }

		private readonly ConditionalWeakTable<MineShaft, StructRef<PreparedData>> PreparedDataTable = new();
		private readonly ConditionalWeakTable<MineShaft, RuntimeData> RuntimeDataTable = new();

		public DisarmablePuzzlePopulator(IMonitor monitor, ILootProvider lootProvider)
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

			// looking for free spots
			List<IntPoint> freeSpots = new();
			for (int y = reachableMap.Bounds.Min.Y; y <= reachableMap.Bounds.Max.Y; y++)
				for (int x = reachableMap.Bounds.Min.X; x <= reachableMap.Bounds.Max.X; x++)
					if (reachableMap[new(x, y)] && occupancyMap[new(x, y)] == PopulatorTile.Empty)
						freeSpots.Add(new(x, y));

			if (freeSpots.Count == 0)
				return 0;

			// choosing a chest position
			var chestPosition = freeSpots[random.Next(freeSpots.Count)];
			freeSpots.Remove(chestPosition);

			// choosing button positions
			int buttonCount = ChooseButtonCount(location, random);
			HashSet<IntPoint> buttonPositions = new();
			while (buttonPositions.Count < buttonCount)
			{
				if (freeSpots.Count == 0)
					return 0;

				var buttonPosition = freeSpots[random.Next(freeSpots.Count)];
				buttonPositions.Add(buttonPosition);
				freeSpots.Remove(buttonPosition);
			}

			PreparedDataTable.AddOrUpdate(location, new PreparedData() { ChestPosition = chestPosition, ButtonPositions = buttonPositions });
			return weight;
		}

		public void BeforePopulate(MineShaft location, Random random)
		{
			if (!PreparedDataTable.TryGetValue(location, out var data))
				return;

			// editing tiles to buttons
			var layer = location.Map.GetLayer("Back");
			foreach (var buttonPosition in data.Value.ButtonPositions)
			{
				var tileSheet = layer.Tiles[buttonPosition.X, buttonPosition.Y].TileSheet;
				layer.Tiles[buttonPosition.X, buttonPosition.Y] = new StaticTile(layer, tileSheet, BlendMode.Alpha, ButtonTileIndex);
			}
		}

		public void AfterPopulate(MineShaft location, Random random)
		{
			if (!PreparedDataTable.TryGetValue(location, out var data))
				return;

			// clearing button spots from placeables
			foreach (var buttonPosition in data.Value.ButtonPositions)
				location.RemoveAllPlaceables(buttonPosition);

			// create chest
			location.RemoveAllPlaceables(data.Value.ChestPosition);
			Vector2 chestPositionVector = new(data.Value.ChestPosition.X, data.Value.ChestPosition.Y);
			var chest = new Chest(0, LootProvider.GenerateLoot().ToList(), chestPositionVector);
			location.objects[chestPositionVector] = chest;

			RuntimeDataTable.AddOrUpdate(location, new RuntimeData(data.Value.ChestPosition, data.Value.ButtonPositions));
		}

		public void OnUpdate(MineShaft location)
		{
			if (GameExt.GetMultiplayerMode() == MultiplayerMode.Client)
				return;
			if (!RuntimeDataTable.TryGetValue(location, out var data))
				return;
			if (data.ButtonPositionsLeft.Count == 0)
				return;

			foreach (var player in Game1.getAllFarmers())
			{
				if (player.currentLocation != location)
					continue;

				IntPoint tile = new(player.getTileX(), player.getTileY());
				if (!data.ButtonPositionsLeft.Contains(tile))
					continue;

				var layer = location.Map.GetLayer("Back");
				var tileSheet = location.Map.GetTileSheet("mine");
				if (tileSheet is null)
					continue;
				layer.Tiles[tile.X, tile.Y] = new StaticTile(layer, tileSheet, BlendMode.Alpha, NoButtonTileIndex);

				data.ButtonPositionsLeft.Remove(tile);
				location.localSound("button1");
			}
		}

		internal bool OnChestOpen(Chest chest)
		{
			if (chest.FindGameLocation() is not MineShaft location)
				return false;
			if (location.Objects[chest.TileLocation] != chest)
				return false;
			if (!RuntimeDataTable.TryGetValue(location, out var data))
				throw new InvalidOperationException("Doing update, but runtime data is not set; aborting.");
			if (data.ButtonPositionsLeft.Count == 0)
				return false;

			TriggerTrap(location, chest);
			return true;
		}

		private void TriggerTrap(MineShaft location, Chest chest)
		{
			//var player = chest.GetMutex().GetCurrentOwner() ?? Game1.player;

			if (location.mineLevel > 0 && location.mineLevel < MineShaft.mineFrostLevel)
			{
				if (Game1.random.NextBool())
				{
					RotFood(location, chest);
				}
				else
				{
					location.explode(chest.TileLocation, 3, null, damage_amount: 50);
				}
			}
			else if (location.mineLevel > MineShaft.mineFrostLevel && location.mineLevel < MineShaft.mineLavaLevel)
			{
				location.explode(chest.TileLocation, 4, null, damage_amount: 100);
				if (Game1.random.Next(3) == 0)
					RotFood(location, chest);
			}
			else if (location.mineLevel > MineShaft.mineLavaLevel && location.mineLevel < MineShaft.bottomOfMineLevel)
			{
				location.explode(chest.TileLocation, 5, null, damage_amount: 150);
				if (Game1.random.NextBool())
					RotFood(location, chest);
			}
			else if (location.mineLevel >= MineShaft.desertArea)
			{
				location.explode(chest.TileLocation, 6, null, damage_amount: 200);
				RotFood(location, chest);
			}
			else
			{
				throw new InvalidOperationException($"Invalid mine floor {location.mineLevel}");
			}
		}

		private void RotFood(MineShaft location, Chest chest)
		{
			// TODO: rot food
			// TODO: i18n
			Game1.drawObjectDialogue("The chest's security was not disarmed. Opening it sprung a poison trap, rotting some of your food.");
		}

		private static int ChooseButtonCount(MineShaft location, Random random)
		{
			if (location.mineLevel > 0 && location.mineLevel < MineShaft.mineFrostLevel)
				return 1;
			else if (location.mineLevel > MineShaft.mineFrostLevel && location.mineLevel < MineShaft.mineLavaLevel)
				return 2 + (random.NextBool() ? 1 : 0);
			else if (location.mineLevel > MineShaft.mineLavaLevel && location.mineLevel < MineShaft.bottomOfMineLevel)
				return 3 + random.Next(3);
			else if (location.mineLevel >= MineShaft.desertArea)
				return 2 + random.Next(5);
			else
				throw new InvalidOperationException($"Invalid mine floor {location.mineLevel}");
		}

		private static double GetWeight(MineShaft location)
		{
			// excluding any floors which use tilesets without the button texture

			var isDinoAreaGetter = AccessTools.PropertyGetter(typeof(MineShaft), "isDinoArea");
			var isDino = (bool)isDinoAreaGetter.Invoke(location, null)!;
			if (isDino)
				return 1;

			var isSlimeAreaGetter = AccessTools.PropertyGetter(typeof(MineShaft), "isSlimeArea");
			var isSlime = (bool)isSlimeAreaGetter.Invoke(location, null)!;
			if (isSlime)
				return 1;

			var loadedDarkAreaField = AccessTools.Field(typeof(MineShaft), "loadedDarkArea");
			var isDark = (bool)loadedDarkAreaField.GetValue(location)!;
			var isDangerous = location.GetAdditionalDifficulty() > 0;

			if (location.mineLevel > 0 && location.mineLevel < MineShaft.mineFrostLevel)
				return isDark && !isDangerous ? 0 : 1;
			else if (location.mineLevel > MineShaft.mineFrostLevel && location.mineLevel < MineShaft.mineLavaLevel)
				return isDark ? 0 : 1;
			else if (location.mineLevel > MineShaft.mineLavaLevel && location.mineLevel < MineShaft.bottomOfMineLevel)
				return isDark? 0 : 1;
			else if (location.mineLevel >= MineShaft.desertArea)
				return isDark && isDangerous ? 0 : 1;
			else
				throw new InvalidOperationException($"Invalid mine floor {location.mineLevel}");
		}

		private static IntPoint? FindLadder(MineShaft location)
		{
			for (int y = 0; y < location.Map.DisplayHeight / 64f; y++)
				for (int x = 0; x < location.Map.DisplayWidth / 64f; x++)
					if (location.Map.GetLayer("Buildings").Tiles[new(x, y)]?.TileIndex == LadderTileIndex)
						return new(x, y);
			return null;
		}
	}
}