using Microsoft.Xna.Framework;
using Shockah.AdventuresInTheMines.Config;
using Shockah.CommonModCode;
using Shockah.CommonModCode.Map;
using Shockah.CommonModCode.Stardew;
using StardewValley;
using StardewValley.Locations;
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

		private const int StoneBrazierIndex = 144;
		private const int SkullBrazierIndex = 149;
		private const int MarbleBrazierIndex = 151;

		private const int MaximumDistanceFromChestToBrazier = 8;
		private const int MinimumDistanceBetweenElements = 3;

		private BrazierSequenceConfig Config { get; init; }
		private IReachableTileMapper ReachableTileMapper { get; init; }
		private ITreasureGenerator TreasureGenerator { get; init; }

		private readonly ConditionalWeakTable<MineShaft, StructRef<PreparedData>> PreparedDataTable = new();
		private readonly ConditionalWeakTable<MineShaft, RuntimeData> RuntimeDataTable = new();
		private bool TorchStateUpdateInProgress { get; set; } = false;

		public BrazierSequencePuzzlePopulator(BrazierSequenceConfig config, IReachableTileMapper reachableTileMapper, ITreasureGenerator treasureGenerator)
		{
			this.Config = config;
			this.ReachableTileMapper = reachableTileMapper;
			this.TreasureGenerator = treasureGenerator;
		}

		public double Prepare(MineShaft location, Random random)
		{
			// get config for location
			if (!Config.Enabled)
				return 0;
			var config = Config.Entries.GetMatchingConfig(location);
			if (config is null || config.Weight <= 0)
				return 0;
			int? brazierCount = ChooseBrazierCount(config, random);
			if (brazierCount is null)
				return 0;

			// creating a reachable tile map - tiles reachable by the player from the ladder
			var reachableTileMap = new OutOfBoundsValuesMap<bool>(
				ReachableTileMapper.MapReachableTiles(location),
				false
			);

			// looking for free spots
			List<IntPoint> freeSpots = new();
			foreach (var point in reachableTileMap.Bounds.AllPointEnumerator())
				if (reachableTileMap[point])
					freeSpots.Add(point);

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
			return config.Weight;
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
				TreasureGenerator.GenerateTreasure(location, data.ChestPosition, pregenerated: false);
			}
		}

		private static int? ChooseBrazierCount(BrazierSequenceConfigEntry config, Random random)
		{
			WeightedRandom<int> weightedRandom = new();
			foreach (var weightedItem in config.BrazierCountWeightItems)
				weightedRandom.Add(new(weightedItem.Weight, weightedItem.BrazierCount));
			return weightedRandom.NextOrNull(random);
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
	}
}