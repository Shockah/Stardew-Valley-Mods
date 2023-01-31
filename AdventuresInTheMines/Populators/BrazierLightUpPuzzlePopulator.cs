using Microsoft.Xna.Framework;
using Shockah.CommonModCode;
using Shockah.CommonModCode.Map;
using Shockah.CommonModCode.Stardew;
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
	internal sealed class BrazierLightUpPuzzlePopulator : IMineShaftPopulator
	{
		private readonly struct PreparedData
		{
			public IntPoint ChestPosition { get; init; }
			public HashSet<IntPoint> TorchPositions { get; init; }
			public HashSet<IntPoint> EnabledTorchPositions { get; init; }
		}

		private sealed class RuntimeData
		{
			public IntPoint ChestPosition { get; init; }
			public List<Torch> Torches { get; init; }
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

		private IMapOccupancyMapper MapOccupancyMapper { get; init; }
		private IReachableTileMapper ReachableTileMapper { get; init; }
		private ILootProvider LootProvider { get; init; }

		private readonly ConditionalWeakTable<MineShaft, StructRef<PreparedData>> PreparedDataTable = new();
		private readonly ConditionalWeakTable<MineShaft, RuntimeData> RuntimeDataTable = new();
		private bool TorchStateUpdateInProgress { get; set; } = false;

		public BrazierLightUpPuzzlePopulator(IMapOccupancyMapper mapOccupancyMapper, IReachableTileMapper reachableTileMapper, ILootProvider lootProvider)
		{
			this.MapOccupancyMapper = mapOccupancyMapper;
			this.ReachableTileMapper = reachableTileMapper;
			this.LootProvider = lootProvider;
		}

		public double Prepare(MineShaft location, Random random)
		{
			// creating an occupancy map (whether each tile can be traversed or an object can be placed in their spot)
			IMap<IMapOccupancyMapper.Tile>.WithKnownSize occupancyMap = new OutOfBoundsValuesMap<IMapOccupancyMapper.Tile>(
				MapOccupancyMapper.MapOccupancy(location),
				IMapOccupancyMapper.Tile.Blocked
			);

			// creating a reachable tile map - tiles reachable by the player from the ladder
			IMap<bool>.WithKnownSize reachableTileMap = new OutOfBoundsValuesMap<bool>(
				ReachableTileMapper.MapReachableTiles(location),
				false
			);

			// looking for free spots
			List<IntPoint> freeSpots = new();
			foreach (var point in reachableTileMap.Bounds.AllPointEnumerator())
				if (reachableTileMap[point] && occupancyMap[point] == IMapOccupancyMapper.Tile.Empty && IntPoint.NeighborOffsets.Where(o => occupancyMap[point + o * 2] == IMapOccupancyMapper.Tile.Empty).Count() >= 2)
					freeSpots.Add(point);

			if (freeSpots.Count == 0)
				return 0;

			// choosing torch positions
			int torchCount = GetTorchCount(location, random);
			HashSet<IntPoint> torchPositions = new();

			IntPoint? ChooseNeighboringPosition()
			{
				if (torchPositions.Count == 0)
					return random.NextElement(freeSpots);

				foreach (var existingTorchPosition in torchPositions.Shuffled(random))
					foreach (var possibleNewTorchPosition in IntPoint.NeighborOffsets.Shuffled(random).Select(o => existingTorchPosition + o * 2))
						if (!torchPositions.Contains(possibleNewTorchPosition) && reachableTileMap[possibleNewTorchPosition] && occupancyMap[possibleNewTorchPosition] == IMapOccupancyMapper.Tile.Empty)
							return possibleNewTorchPosition;

				return null;
			}

			while (torchPositions.Count < torchCount)
			{
				var newTorchPosition = ChooseNeighboringPosition();
				if (newTorchPosition is null)
				{
					// TODO: log failure i guess?
					return 0;
				}
				torchPositions.Add(newTorchPosition.Value);
			}

			var chestPosition = ChooseNeighboringPosition();
			if (chestPosition is null)
			{
				// TODO: log failure i guess?
				return 0;
			}

			// toggling switches initially
			// TODO: move this logic to `BeforePopulate`, there is no need to do this if this puzzle isn't chosen

			HashSet<IntPoint> enabledTorchPositions = new();

			void Toggle(IntPoint position)
			{
				void ToggleWithoutNeighbors(IntPoint position)
				{
					if (enabledTorchPositions.Contains(position))
						enabledTorchPositions.Remove(position);
					else
						enabledTorchPositions.Add(position);
				}

				ToggleWithoutNeighbors(position);
				foreach (var neighbor in IntPoint.NeighborOffsets.Select(o => position + o * 2))
					if (torchPositions.Contains(neighbor))
						ToggleWithoutNeighbors(neighbor);
			}

			for (int i = GetInitialToggleCount(location, random); i > 0; i--)
			{
				var torchPosition = random.NextElement(torchPositions);
				Toggle(torchPosition);
			}

			PreparedDataTable.AddOrUpdate(location, new PreparedData() { ChestPosition = chestPosition.Value, TorchPositions = torchPositions, EnabledTorchPositions = enabledTorchPositions });
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
			foreach (var brazierPosition in data.Value.TorchPositions)
			{
				location.RemoveAllPlaceables(brazierPosition);
				Vector2 brazierPositionVector = new(brazierPosition.X, brazierPosition.Y);
				var torch = CreateTorch(location, brazierPosition, data.Value.EnabledTorchPositions.Contains(brazierPosition));
				torches.Add(torch);
				location.objects[brazierPositionVector] = torch;
			}

			RuntimeDataTable.AddOrUpdate(location, new RuntimeData(data.Value.ChestPosition, torches));
		}

		private void OnTorchStateUpdate(MineShaft location, Torch torch)
		{
			if (!RuntimeDataTable.TryGetValue(location, out var data))
				throw new InvalidOperationException("Observed torch state update, but runtime data is not set; aborting.");

			var neighborTorches = IntPoint.NeighborOffsets
				.Select(o => torch.TileLocation + new Vector2(o.X * 2, o.Y * 2))
				.Select(p => data.Torches.FirstOrDefault(t => t.TileLocation == p))
				.Where(t => t is not null)
				.Select(t => t!);

			foreach (var neighborTorch in neighborTorches)
				neighborTorch.IsOn = !neighborTorch.IsOn;

			if (!data.IsActive)
				return;

			if (data.Torches.Any(t => !t.IsOn))
				return;

			// create chest
			location.RemoveAllPlaceables(data.ChestPosition);
			Vector2 chestPositionVector = new(data.ChestPosition.X, data.ChestPosition.Y);
			location.objects[chestPositionVector] = new Chest(0, LootProvider.GenerateLoot().ToList(), chestPositionVector);

			// making sound
			location.localSound("newArtifact");
		}

		private static int GetTorchCount(MineShaft location, Random random)
		{
			return GetDifficultyModifier(location) switch
			{
				0 => 8 + random.Next(3),
				1 => 9 + random.Next(6),
				2 => 10 + random.Next(9),
				3 => 11 + random.Next(12),
				_ => 12 + random.Next(15)
			};
		}

		private static int GetInitialToggleCount(MineShaft location, Random random)
		{
			return GetDifficultyModifier(location) switch
			{
				0 => 2 + random.Next(2),
				1 => 3 + random.Next(3),
				2 => 4 + random.Next(4),
				3 => 5 + random.Next(5),
				_ => 6 + random.Next(6)
			};
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

		[SuppressMessage("SMAPI.CommonErrors", "AvoidNetField:Avoid Netcode types when possible", Justification = "Registering for events")]
		private Torch CreateTorch(MineShaft location, IntPoint point, bool enabled)
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
			torch.IsOn = enabled;
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