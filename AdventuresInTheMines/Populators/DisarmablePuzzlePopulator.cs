using HarmonyLib;
using Microsoft.Xna.Framework;
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
using xTile.Tiles;
using SObject = StardewValley.Object;

namespace Shockah.AdventuresInTheMines.Populators
{
	internal sealed class DisarmablePuzzlePopulator : IMineShaftPopulator
	{
		private readonly struct PreparedData
		{
			public IntPoint ChestPosition { get; init; }
			public HashSet<IntPoint> ButtonPositions { get; init; }
		}

		private sealed class RuntimeData
		{
			public IntPoint ChestPosition { get; init; }
			public HashSet<IntPoint> ButtonPositionsLeft { get; init; }
			public HashSet<long> PlayersWhoAlreadyTriedToOpen { get; init; } = new();

			public RuntimeData(IntPoint chestPosition, HashSet<IntPoint> buttonPositions)
			{
				this.ChestPosition = chestPosition;
				this.ButtonPositionsLeft = buttonPositions;
			}
		}

		private const int ButtonTileIndex = 204;
		private const int NoButtonTileIndex = 169;

		private IMapOccupancyMapper MapOccupancyMapper { get; init; }
		private IReachableTileMapper ReachableTileMapper { get; init; }
		private ILootProvider LootProvider { get; init; }

		private readonly ConditionalWeakTable<MineShaft, StructRef<PreparedData>> PreparedDataTable = new();
		private readonly ConditionalWeakTable<MineShaft, RuntimeData> RuntimeDataTable = new();

		public DisarmablePuzzlePopulator(IMapOccupancyMapper mapOccupancyMapper, IReachableTileMapper reachableTileMapper, ILootProvider lootProvider)
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

			// looking for free spots
			List<IntPoint> freeSpots = new();
			foreach (var point in reachableTileMap.Bounds.AllPointEnumerator())
				if (reachableTileMap[point] && occupancyMap[point] == IMapOccupancyMapper.Tile.Empty)
					freeSpots.Add(point);

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

		public void OnUpdateTicked(MineShaft location)
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
				data.PlayersWhoAlreadyTriedToOpen.Clear();
			}
		}

		public bool HandleChestOpen(MineShaft location, Chest chest)
		{
			if (location.Objects[chest.TileLocation] != chest)
				return false;
			if (!RuntimeDataTable.TryGetValue(location, out var data))
				return false;
			if (data.ButtonPositionsLeft.Count == 0)
				return false;

			TriggerTrap(location, chest, data);
			return true;
		}

		private static void TriggerTrap(MineShaft location, Chest chest, RuntimeData data)
		{
			var player = chest.GetMutex().GetCurrentOwner() ?? Game1.player;

			if (data.PlayersWhoAlreadyTriedToOpen.Contains(player.UniqueMultiplayerID))
			{
				// TODO: i18n
				AdventuresInTheMines.Instance.QueueObjectDialogue("The chest's security is still not disarmed. There has to be something nearby that could do it.");
				return;
			}
			data.PlayersWhoAlreadyTriedToOpen.Add(player.UniqueMultiplayerID);

			bool didExplode = false;
			bool didRot = false;

			void Explode(int radius, int damage)
			{
				location.explode(chest.TileLocation, radius, player, damage_amount: damage);

				if (!didExplode)
					location.localSound("explosion");
				didExplode = true;
			}

			void RotFood()
			{
				int foodCount = player.Items
					.OfType<SObject>()
					.Where(i => i.Edibility != SObject.inedible && (i.staminaRecoveredOnConsumption() > 0 || i.healthRecoveredOnConsumption() > 0))
					.Sum(i => i.Stack);

				int toRot = (int)Math.Pow(foodCount, 0.75);
				if (toRot == 0)
					return;

				while (toRot > 0)
				{
					int itemN = Game1.random.Next(foodCount);
					foreach (Item item in player.Items)
					{
						if (item is not SObject @object)
							continue;
						if (!(@object.Edibility != SObject.inedible && (@object.staminaRecoveredOnConsumption() > 0 || @object.healthRecoveredOnConsumption() > 0)))
							continue;
						if (itemN < @object.Stack)
						{
							if (@object.Stack == 1)
								player.removeItemFromInventory(item);
							else
								@object.Stack--;
							toRot--;
							goto toRotContinue;
						}
						else
						{
							itemN -= @object.Stack;
						}
					}

					toRotContinue:;
				}

				if (!didRot)
					location.localSound("croak");
				didRot = true;
			}

			if (location.mineLevel > 0 && location.mineLevel < MineShaft.mineFrostLevel)
			{
				if (Game1.random.NextBool())
					RotFood();
				else
					Explode(3, 50);
			}
			else if (location.mineLevel > MineShaft.mineFrostLevel && location.mineLevel < MineShaft.mineLavaLevel)
			{
				Explode(4, 100);
				if (Game1.random.Next(3) == 0)
					RotFood();
			}
			else if (location.mineLevel > MineShaft.mineLavaLevel && location.mineLevel < MineShaft.bottomOfMineLevel)
			{
				Explode(5, 150);
				if (Game1.random.NextBool())
					RotFood();
			}
			else if (location.mineLevel >= MineShaft.desertArea)
			{
				Explode(6, 200);
				RotFood();
			}
			else
			{
				throw new InvalidOperationException($"Invalid mine floor {location.mineLevel}");
			}

			// TODO: i18n
			bool firstMessage = true;
			if (didExplode)
			{
				if (firstMessage)
					AdventuresInTheMines.Instance.QueueObjectDialogue("The chest's security was not disarmed. Opening it brought up a fierce explosion, damaging you and the surroundings.");
				else
					AdventuresInTheMines.Instance.QueueObjectDialogue("Opening the chest also brought up a fierce explosion, damaging you and the surroundings.");
				firstMessage = false;
			}
			if (didRot)
			{
				if (firstMessage)
					AdventuresInTheMines.Instance.QueueObjectDialogue("The chest's security was not disarmed. Opening it sprung a poison trap, rotting some of your food.");
				else
					AdventuresInTheMines.Instance.QueueObjectDialogue("Opening the chest also sprung a poison trap, rotting some of your food.");
				firstMessage = false;
			}
			if (firstMessage)
				AdventuresInTheMines.Instance.QueueObjectDialogue("The chest's security was not disarmed, but opening it... did not set any trap off!");
		}

		private static int GetDifficultyModifier(MineShaft location)
		{
			int difficulty;

			if (location.mineLevel > 0 && location.mineLevel < MineShaft.mineFrostLevel)
				difficulty = 0;
			else if (location.mineLevel > MineShaft.mineFrostLevel && location.mineLevel < MineShaft.mineLavaLevel)
				difficulty = 1;
			else if (location.mineLevel > MineShaft.mineLavaLevel && location.mineLevel < MineShaft.bottomOfMineLevel)
				difficulty = 2;
			else if (location.mineLevel >= MineShaft.desertArea)
				difficulty = 3;
			else
				throw new InvalidOperationException($"Invalid mine floor {location.mineLevel}");

			if (location.GetAdditionalDifficulty() > 0)
				difficulty++;

			return difficulty;
		}

		private static int ChooseButtonCount(MineShaft location, Random random)
		{
			return GetDifficultyModifier(location) switch
			{
				0 => 1,
				1 => 2 + (random.NextBool() ? 1 : 0),
				2 => 3 + random.Next(3),
				3 => 2 + random.Next(5),
				_ => 0 + random.Next(8),
			};
		}

		private static double GetWeight(MineShaft location)
		{
			// excluding monster areas - too easy to see the buttons

			var isSlimeAreaGetter = AccessTools.PropertyGetter(typeof(MineShaft), "isSlimeArea");
			var isSlime = (bool)isSlimeAreaGetter.Invoke(location, null)!;
			if (isSlime)
				return 0;

			var isMonsterAreaGetter = AccessTools.PropertyGetter(typeof(MineShaft), "isMonsterArea");
			var isMonster = (bool)isMonsterAreaGetter.Invoke(location, null)!;
			if (isMonster)
				return 0;

			// excluding any floors which use tilesets without the button texture

			var isDinoAreaGetter = AccessTools.PropertyGetter(typeof(MineShaft), "isDinoArea");
			var isDino = (bool)isDinoAreaGetter.Invoke(location, null)!;
			if (isDino)
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
	}
}