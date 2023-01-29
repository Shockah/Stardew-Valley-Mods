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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using SObject = StardewValley.Object;

namespace Shockah.AdventuresInTheMines.Populators
{
	internal sealed class BrazierCombinationPuzzlePopulator : IMineShaftPopulator
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

		private const int LadderTileIndex = 115;
		private const int StoneBrazierIndex = 144;
		private const int SkullBrazierIndex = 149;
		private const int MarbleBrazierIndex = 151;

		private static readonly List<HashSet<IntPoint>> ThreeBrazierLayouts = new()
		{
			new() { new(-2, -2), new(0, -2), new(2, -2) },
			new() { new(0, -2), new(-2, 1), new(2, 1) }
		};

		private static readonly List<HashSet<IntPoint>> FourBrazierLayouts = new()
		{
			new() { new(-2, 0), new(2, 0), new(0, -2), new(0, 2) },
			new() { new(-2, -2), new(-2, 2), new(2, -2), new(2, 2) },
			new() { new(-2, 0), new(2, 0), new(1, -2), new(-1, 2) },
			new() { new(-3, -2), new(-1, -2), new(1, -2), new(3, -2) },
			new() { new(-1, -4), new(1, -4), new(-1, -2), new(1, -2) }
		};

		private static readonly List<HashSet<IntPoint>> FiveBrazierLayouts = new()
		{
			new() { new(0, -2), new(-2, 0), new(2, 0), new(-1, 2), new(1, 2) },
			new() { new(-4, -2), new(-2, -2), new(0, -2), new(2, -2), new(4, -2) },
			new() { new(-1, -4), new(1, -4), new(0, -3), new(-1, -2), new(1, -2) }
		};

		private IMonitor Monitor { get; init; }
		private ILootProvider LootProvider { get; init; }

		private readonly ConditionalWeakTable<MineShaft, StructRef<PreparedData>> PreparedDataTable = new();
		private readonly ConditionalWeakTable<MineShaft, RuntimeData> RuntimeDataTable = new();

		public BrazierCombinationPuzzlePopulator(IMonitor monitor, ILootProvider lootProvider)
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

			// selecting one possible layout out of many
			var layout = GetTransformedLayout(location, random);

			// looking for applicable positions for the layout
			List<IntPoint> possibleChestPositions = new();
			for (int y = occupancyMap.Bounds.Min.Y; y <= occupancyMap.Bounds.Max.Y; y++)
			{
				for (int x = occupancyMap.Bounds.Min.X; x <= occupancyMap.Bounds.Max.X; x++)
				{
					if (occupancyMap[new(x, y)] != PopulatorTile.Empty)
						continue;

					foreach (var brazierRelativePosition in layout)
						if (occupancyMap[new(x + brazierRelativePosition.X, y + brazierRelativePosition.Y)] != PopulatorTile.Empty)
							goto cellContinue;

					possibleChestPositions.Add(new(x, y));
					cellContinue:;
				}
			}

			if (possibleChestPositions.Count == 0)
				return 0;
			var chestPosition = possibleChestPositions[random.Next(possibleChestPositions.Count)];

			PreparedDataTable.AddOrUpdate(location, new PreparedData() { ChestPosition = chestPosition, Layout = layout });
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

		private static HashSet<IntPoint> GetTransformedLayout(MineShaft location, Random random)
		{
			var baseLayout = GetBaseLayout(location, random);
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

			// XY mirror
			AddTransformedLayoutIfUnique(baseLayout.Select(p => new IntPoint(-p.X, -p.Y)).ToHashSet());

			// 90* clockwise rotation
			AddTransformedLayoutIfUnique(baseLayout.Select(p => new IntPoint(-p.Y, p.X)).ToHashSet());

			// 90* counter-clockwise rotation
			AddTransformedLayoutIfUnique(baseLayout.Select(p => new IntPoint(p.Y, -p.X)).ToHashSet());

			// 180* rotation
			AddTransformedLayoutIfUnique(baseLayout.Select(p => new IntPoint(-p.X, -p.Y)).ToHashSet());

			return transformedLayouts[random.Next(transformedLayouts.Count)];
		}

		private static HashSet<IntPoint> GetBaseLayout(MineShaft location, Random random)
		{
			var layoutList = GetLayoutList(location, random);
			return layoutList[random.Next(layoutList.Count)];
		}

		private static List<HashSet<IntPoint>> GetLayoutList(MineShaft location, Random random)
		{
			List<(List<HashSet<IntPoint>> LayoutList, double Weight)> items = new();

			if (location.mineLevel > 0 && location.mineLevel < MineShaft.mineFrostLevel)
			{
				items.Add((ThreeBrazierLayouts, 1));
				items.Add((FourBrazierLayouts, 0.25));
			}
			else if(location.mineLevel > MineShaft.mineFrostLevel && location.mineLevel < MineShaft.mineLavaLevel)
			{
				items.Add((ThreeBrazierLayouts, 0.5));
				items.Add((FourBrazierLayouts, 1));
			}
			else if (location.mineLevel > MineShaft.mineLavaLevel && location.mineLevel < MineShaft.bottomOfMineLevel)
			{
				items.Add((ThreeBrazierLayouts, 0.25));
				items.Add((FourBrazierLayouts, 1));
				items.Add((FiveBrazierLayouts, 0.25));
			}
			else if (location.mineLevel >= MineShaft.desertArea)
			{
				items.Add((FourBrazierLayouts, 1));
				items.Add((FiveBrazierLayouts, 1));
			}
			else
			{
				throw new InvalidOperationException($"Invalid mine floor {location.mineLevel}");
			}

			double weightSum = items.Select(i => i.Weight).Sum();
			double weightedRandom = random.NextDouble() * weightSum;
			weightSum = 0;

			foreach (var (layoutList, weight) in items)
			{
				weightSum += weight;
				if (weightSum >= weightedRandom)
					return layoutList;
			}
			throw new InvalidOperationException("Reached invalid state.");
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