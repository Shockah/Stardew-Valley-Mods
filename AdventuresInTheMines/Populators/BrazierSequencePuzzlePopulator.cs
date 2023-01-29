﻿using Microsoft.Xna.Framework;
using Shockah.AdventuresInTheMines.Map;
using Shockah.CommonModCode;
using Shockah.CommonModCode.Stardew;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using SObject = StardewValley.Object;

namespace Shockah.AdventuresInTheMines.Populators
{
	internal sealed class BrazierSequencePuzzlePopulator : IMineShaftPopulator
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
			public HashSet<IntPoint> BrazierPositions { get; init; }
		}

		private sealed class RuntimeData
		{
			public IntPoint ChestPosition { get; init; }
			public List<Torch> Torches { get; init; }
			public List<Torch> Sequence { get; init; } = new();
			public bool IsActive { get; set; } = true;

			public RuntimeData(IntPoint chestPosition, List<Torch> torches)
			{
				this.ChestPosition = chestPosition;
				this.Torches = torches;
			}
		}

		private const int LadderTileIndex = 115;
		private const int StoneBrazierIndex = 144;
		private const int SkullBrazierIndex = 149;
		private const int MarbleBrazierIndex = 151;

		private const int MaximumDistanceFromChestToBrazier = 12;
		private const int MinimumDistanceBetweenElements = 3;

		private IMonitor Monitor { get; init; }
		private ILootProvider LootProvider { get; init; }

		private readonly ConditionalWeakTable<MineShaft, StructRef<PreparedData>> PreparedDataTable = new();
		private readonly ConditionalWeakTable<MineShaft, RuntimeData> RuntimeDataTable = new();
		private bool TorchStateUpdateInProgress { get; set; } = false;

		public BrazierSequencePuzzlePopulator(IMonitor monitor, ILootProvider lootProvider)
		{
			this.Monitor = monitor;
			this.LootProvider = lootProvider;
		}

		public double Prepare(MineShaft location, Random random)
		{
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
					if (reachableMap[new(x, y)])
						freeSpots.Add(new(x, y));

			if (freeSpots.Count == 0)
				return 0;

			// choosing a chest position; filtering out spots by distance
			var chestPosition = freeSpots[random.Next(freeSpots.Count)];
			freeSpots = freeSpots
				.Where(p => Math.Abs(chestPosition.X - p.X) + Math.Abs(chestPosition.Y - p.Y) <= MaximumDistanceFromChestToBrazier)
				.Where(p => Math.Abs(chestPosition.X - p.X) + Math.Abs(chestPosition.Y - p.Y) >= MinimumDistanceBetweenElements)
				.ToList();

			if (freeSpots.Count == 0)
				return 0;

			// choosing brazier positions and filtering out spots further
			int brazierCount = ChooseBrazierCount(location, random);
			HashSet<IntPoint> brazierPositions = new();
			while (brazierPositions.Count < brazierCount)
			{
				if (freeSpots.Count == 0)
					return 0;

				var brazierPosition = freeSpots[random.Next(freeSpots.Count)];
				brazierPositions.Add(brazierPosition);
				freeSpots = freeSpots
					.Where(p => Math.Abs(brazierPosition.X - p.X) + Math.Abs(brazierPosition.Y - p.Y) >= MinimumDistanceBetweenElements)
					.ToList();
			}

			PreparedDataTable.AddOrUpdate(location, new PreparedData() { ChestPosition = chestPosition, BrazierPositions = brazierPositions });
			return 1;
		}

		public void BeforePopulate(MineShaft location, Random random)
		{
		}

		public void AfterPopulate(MineShaft location, Random random)
		{
			if (!PreparedDataTable.TryGetValue(location, out var data))
				return;

			// placing braziers
			List<Torch> torches = new();
			foreach (var brazierPosition in data.Value.BrazierPositions)
			{
				location.RemoveAllPlaceables(brazierPosition);
				Vector2 brazierPositionVector = new(brazierPosition.X, brazierPosition.Y);
				var torch = CreateTorch(location, brazierPosition);
				torches.Add(torch);
				location.objects[brazierPositionVector] = torch;
			}

			RuntimeDataTable.AddOrUpdate(location, new RuntimeData(data.Value.ChestPosition, torches));
		}

		private void OnTorchStateUpdate(MineShaft location, Torch torch)
		{
			if (!RuntimeDataTable.TryGetValue(location, out var data))
				throw new InvalidOperationException("Observed torch state update, but runtime data is not set; aborting.");
			if (!data.IsActive)
				return;

			if (!torch.IsOn)
			{
				while (data.Sequence.Contains(torch))
					data.Sequence.RemoveAt(data.Sequence.Count - 1);
				return;
			}

			if (data.Torches[data.Sequence.Count] != torch)
			{
				data.Sequence.Clear();
				foreach (var dataTorch in data.Torches)
					dataTorch.IsOn = false;
				location.localSound("cancel");
				return;
			}

			data.Sequence.Add(torch);

			if (data.Sequence.Count == data.Torches.Count)
			{
				data.IsActive = false;

				// create chest
				location.RemoveAllPlaceables(data.ChestPosition);
				Vector2 chestPositionVector = new(data.ChestPosition.X, data.ChestPosition.Y);
				location.objects[chestPositionVector] = new Chest(0, LootProvider.GenerateLoot().ToList(), chestPositionVector);

				// making sound
				location.localSound("newArtifact");
			}
		}

		private static int ChooseBrazierCount(MineShaft location, Random random)
		{
			if (location.mineLevel > 0 && location.mineLevel < MineShaft.mineFrostLevel)
				return 4;
			else if (location.mineLevel > MineShaft.mineFrostLevel && location.mineLevel < MineShaft.mineLavaLevel)
				return 4 + (random.NextBool() ? 1 : 0);
			else if (location.mineLevel > MineShaft.mineLavaLevel && location.mineLevel < MineShaft.bottomOfMineLevel)
				return 5;
			else if (location.mineLevel >= MineShaft.desertArea)
				return 5 + (random.NextBool() ? 1 : 0);
			else
				throw new InvalidOperationException($"Invalid mine floor {location.mineLevel}");
		}

		[SuppressMessage("SMAPI.CommonErrors", "AvoidNetField:Avoid Netcode types when possible", Justification = "Registering for events")]
		private Torch CreateTorch(MineShaft location, IntPoint point)
		{
			Vector2 pointVector = new(point.X, point.Y);

			Torch CreateFloorSpecificTorch()
			{
				if (location.mineLevel > 0 && location.mineLevel < MineShaft.mineFrostLevel)
					return new Torch(pointVector, StoneBrazierIndex, bigCraftable: true);
				else if (location.mineLevel > MineShaft.mineFrostLevel && location.mineLevel < MineShaft.mineLavaLevel)
					return new Torch(pointVector, MarbleBrazierIndex, bigCraftable: true);
				else if (location.mineLevel > MineShaft.mineLavaLevel && location.mineLevel < MineShaft.bottomOfMineLevel)
					return new Torch(pointVector, StoneBrazierIndex, bigCraftable: true);
				else if (location.mineLevel >= MineShaft.desertArea)
					return new Torch(pointVector, SkullBrazierIndex, bigCraftable: true);
				else
					throw new InvalidOperationException($"Invalid mine floor {location.mineLevel}");
			}

			var torch = CreateFloorSpecificTorch();
			torch.tileLocation.Value = pointVector;
			torch.initializeLightSource(pointVector, mineShaft: true);
			torch.Fragility = SObject.fragility_Indestructable;
			torch.IsOn = false;
			torch.isOn.fieldChangeVisibleEvent += (_, _, _) =>
			{
				if (TorchStateUpdateInProgress)
					return;
				TorchStateUpdateInProgress = true;
				OnTorchStateUpdate(location, torch);
				TorchStateUpdateInProgress = false;
			};
			return torch;
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