using Microsoft.Xna.Framework;
using Shockah.AdventuresInTheMines.Config;
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
	internal sealed class BrazierCombinationPuzzlePopulator : IMineShaftPopulator
	{
		private readonly struct PreparedData
		{
			public IntPoint ChestPosition { get; init; }
			public HashSet<IntPoint> Layout { get; init; }
		}

		private sealed class RuntimeData
		{
			public IntPoint ChestPosition { get; init; }
			public List<Torch> Torches { get; init; }
			public List<bool> Combination { get; init; }
			public bool IsActive { get; set; } = true;

			public RuntimeData(IntPoint chestPosition, List<Torch> torches, List<bool> combination)
			{
				this.ChestPosition = chestPosition;
				this.Torches = torches;
				this.Combination = combination;
			}
		}

		private const int StoneBrazierIndex = 144;
		private const int SkullBrazierIndex = 149;
		private const int MarbleBrazierIndex = 151;

		private static readonly List<HashSet<IntPoint>> ThreeBrazierLayouts = new()
		{
			new() { new(-2, -2), new(0, -2), new(2, -2) },
			new() { new(0, -2), new(-2, 1), new(2, 1) },
			new() { new(-2, -2), new(-2, 2), new(2, -2) },
			new() { new(-2, 0), new(2, 0), new(0, -2) },
			new() { new(-2, -1), new(-2, 1), new(2, 0) },
			new() { new(2, 0), new(0, 2), new(2, 2) }
		};

		private static readonly List<HashSet<IntPoint>> FourBrazierLayouts = new()
		{
			new() { new(-2, 0), new(2, 0), new(0, -2), new(0, 2) },
			new() { new(-2, -2), new(-2, 2), new(2, -2), new(2, 2) },
			new() { new(-2, 0), new(2, 0), new(1, -2), new(-1, 2) },
			new() { new(-3, -2), new(-1, -2), new(1, -2), new(3, -2) },
			new() { new(-1, -4), new(1, -4), new(-1, -2), new(1, -2) },
			new() { new(-2, -1), new(-2, 1), new(2, -1), new(2, 1) }
		};

		private static readonly List<HashSet<IntPoint>> FiveBrazierLayouts = new()
		{
			new() { new(0, -2), new(-2, 0), new(2, 0), new(-1, 2), new(1, 2) },
			new() { new(-4, -2), new(-2, -2), new(0, -2), new(2, -2), new(4, -2) },
			new() { new(-1, -4), new(1, -4), new(0, -3), new(-1, -2), new(1, -2) },
			new() { new(-4, -1), new(-2, -2), new(0, -3), new(2, -2), new(4, -1) },
			new() { new(-3, 0), new(3, 0), new(-2, 2), new(2, 2), new(0, 3) },
			new() { new(-2, -1), new(2, -1), new(-2, 1), new(2, 1), new(0, 2) }
		};

		private BrazierCombinationConfig Config { get; init; }
		private IMapOccupancyMapper MapOccupancyMapper { get; init; }
		private ILootProvider LootProvider { get; init; }

		private readonly ConditionalWeakTable<MineShaft, StructRef<PreparedData>> PreparedDataTable = new();
		private readonly ConditionalWeakTable<MineShaft, RuntimeData> RuntimeDataTable = new();

		public BrazierCombinationPuzzlePopulator(BrazierCombinationConfig config, IMapOccupancyMapper mapOccupancyMapper, ILootProvider lootProvider)
		{
			this.Config = config;
			this.MapOccupancyMapper = mapOccupancyMapper;
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

			// selecting one possible layout out of many
			var layout = GetTransformedLayout(config, random);
			if (layout is null)
				return 0;

			// creating an occupancy map (whether each tile can be traversed or an object can be placed in their spot)
			var occupancyMap = new OutOfBoundsValuesMap<IMapOccupancyMapper.Tile>(
				MapOccupancyMapper.MapOccupancy(location),
				IMapOccupancyMapper.Tile.Blocked
			);

			// looking for applicable positions for the layout
			List<IntPoint> possibleChestPositions = new();
			foreach (var point in occupancyMap.Bounds.AllPointEnumerator())
			{
				if (occupancyMap[point] != IMapOccupancyMapper.Tile.Empty)
					continue;

				foreach (var brazierRelativePosition in layout)
					if (occupancyMap[point + brazierRelativePosition] != IMapOccupancyMapper.Tile.Empty)
						goto cellContinue;

				possibleChestPositions.Add(point);
				cellContinue:;
			}

			if (possibleChestPositions.Count == 0)
				return 0;
			var chestPosition = possibleChestPositions[random.Next(possibleChestPositions.Count)];

			PreparedDataTable.AddOrUpdate(location, new PreparedData() { ChestPosition = chestPosition, Layout = layout });
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
			foreach (var brazierRelativePosition in data.Value.Layout)
			{
				IntPoint brazierPosition = data.Value.ChestPosition + brazierRelativePosition;
				location.RemoveAllPlaceables(brazierPosition);
				Vector2 brazierPositionVector = new(brazierPosition.X, brazierPosition.Y);
				var torch = CreateTorch(location, brazierPosition, random);
				torches.Add(torch);
				location.objects[brazierPositionVector] = torch;
			}

			// setting up combination
			List<bool> combination = torches.Select(_ => random.NextBool()).ToList();

			// making sure combination isn't already satisfied
			for (int i = 0; i < torches.Count; i++)
				if (torches[i].IsOn != combination[i])
					goto combinationBreak;

			// combination is already satisfied; toggling a random bit
			int indexToToggle = random.Next(combination.Count);
			combination[indexToToggle] = !combination[indexToToggle];
			combinationBreak:;

			RuntimeDataTable.AddOrUpdate(location, new RuntimeData(data.Value.ChestPosition, torches, combination));
		}

		private void OnTorchStateUpdate(MineShaft location)
		{
			if (!RuntimeDataTable.TryGetValue(location, out var data))
				throw new InvalidOperationException("Observed torch state update, but runtime data is not set; aborting.");
			if (!data.IsActive)
				return;

			// checking if combination is now satisfied
			for (int i = 0; i < data.Torches.Count; i++)
				if (data.Torches[i].IsOn != data.Combination[i])
					return;

			data.IsActive = false;

			// create chest
			location.RemoveAllPlaceables(data.ChestPosition);
			Vector2 chestPositionVector = new(data.ChestPosition.X, data.ChestPosition.Y);
			location.objects[chestPositionVector] = new Chest(0, LootProvider.GenerateLoot().ToList(), chestPositionVector);

			// making sound
			location.localSound("newArtifact");
		}

		private static HashSet<IntPoint>? GetTransformedLayout(BrazierCombinationConfigEntry config, Random random)
		{
			var baseLayout = GetBaseLayout(config, random);
			if (baseLayout is null)
				return null;

			List<HashSet<IntPoint>> transformedLayouts = new() { baseLayout };

			bool ContainsTransformedLayout(HashSet<IntPoint> layout)
				=> transformedLayouts.Any(l => l.SequenceEqual(layout));

			void AddTransformedLayoutIfUnique(HashSet<IntPoint> layout)
			{
				if (!ContainsTransformedLayout(layout))
					transformedLayouts.Add(layout);
			}

			// X mirror
			AddTransformedLayoutIfUnique(baseLayout.Select(p => new IntPoint(-p.X, p.Y)).ToHashSet());

			// Y mirror
			AddTransformedLayoutIfUnique(baseLayout.Select(p => new IntPoint(p.X, -p.Y)).ToHashSet());

			// 90* clockwise rotation
			AddTransformedLayoutIfUnique(baseLayout.Select(p => new IntPoint(-p.Y, p.X)).ToHashSet());

			// 90* counter-clockwise rotation
			AddTransformedLayoutIfUnique(baseLayout.Select(p => new IntPoint(p.Y, -p.X)).ToHashSet());

			// 180* rotation
			AddTransformedLayoutIfUnique(baseLayout.Select(p => new IntPoint(-p.X, -p.Y)).ToHashSet());

			// something, idk
			AddTransformedLayoutIfUnique(baseLayout.Select(p => new IntPoint(-p.Y, -p.X)).ToHashSet());

			return random.NextElement(transformedLayouts);
		}

		private static HashSet<IntPoint>? GetBaseLayout(BrazierCombinationConfigEntry config, Random random)
		{
			var layoutList = GetLayoutList(config, random);
			if (layoutList is null)
				return null;
			return random.NextElement(layoutList);
		}

		private static List<HashSet<IntPoint>>? GetLayoutList(BrazierCombinationConfigEntry config, Random random)
		{
			WeightedRandom<List<HashSet<IntPoint>>> weightedRandom = new();
			foreach (var weightedItem in config.BrazierCountWeightItems)
			{
				List<HashSet<IntPoint>> layoutList = weightedItem.BrazierCount switch
				{
					3 => ThreeBrazierLayouts,
					4 => FourBrazierLayouts,
					5 => FiveBrazierLayouts,
					_ => throw new InvalidOperationException($"No layouts specified for {weightedItem.BrazierCount} braziers.")
				};
				weightedRandom.Add(new(weightedItem.Weight, layoutList));
			}
			return weightedRandom.NextOrNull(random);
		}

		[SuppressMessage("SMAPI.CommonErrors", "AvoidNetField:Avoid Netcode types when possible", Justification = "Registering for events")]
		private Torch CreateTorch(MineShaft location, IntPoint point, Random random)
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
			torch.IsOn = random.NextBool();
			torch.isOn.fieldChangeVisibleEvent += (_, _, _) => OnTorchStateUpdate(location);
			return torch;
		}
	}
}