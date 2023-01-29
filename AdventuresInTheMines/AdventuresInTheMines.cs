using HarmonyLib;
using Shockah.AdventuresInTheMines.Populators;
using Shockah.CommonModCode;
using Shockah.CommonModCode.Stardew;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.AdventuresInTheMines
{
	public class AdventuresInTheMines : Mod
	{
		private const double TreasurePopulateChance = 0.2;

		private static AdventuresInTheMines Instance = null!;

		private List<IMineShaftPopulator> Populators { get; set; } = new();
		private Random? CurrentRandom { get; set; }
		private IMineShaftPopulator? CurrentPopulator { get; set; }

		public override void Entry(IModHelper helper)
		{
			Instance = this;

			helper.Events.GameLoop.DayStarted += OnDayStarted;
			helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

			var harmony = new Harmony(ModManifest.UniqueID);
			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(MineShaft), "populateLevel"),
				prefix: new HarmonyMethod(AccessTools.Method(typeof(AdventuresInTheMines), nameof(MineShaft_populateLevel_Prefix))),
				postfix: new HarmonyMethod(AccessTools.Method(typeof(AdventuresInTheMines), nameof(MineShaft_populateLevel_Postfix)))
			);
			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(Chest), nameof(Chest.performOpenChest)),
				prefix: new HarmonyMethod(AccessTools.Method(typeof(AdventuresInTheMines), nameof(Chest_performOpenChest_Prefix)))
			);
		}

		private void OnDayStarted(object? sender, DayStartedEventArgs e)
		{
			ResetPopulators();
		}

		private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
		{
			var locations = Game1.getAllFarmers()
				.Select(p => p.currentLocation)
				.OfType<MineShaft>()
				.ToList();

			foreach (var populator in Populators)
				foreach (var location in locations)
					populator.OnUpdate(location);
		}

		private void ResetPopulators()
		{
			var lootProvider = new LimitedWithAlternativeLootProvider(
				main: new BirthdayPresentLootProvider(Game1.Date.GetByAddingDays(1)),
				alternative: new DefaultMineShaftLootProvider()
			);

			Populators = new()
			{
				new IcePuzzlePopulator(Monitor, lootProvider),
				new BrazierCombinationPuzzlePopulator(Monitor, lootProvider),
				new BrazierSequencePuzzlePopulator(Monitor, lootProvider),
				new DisarmablePuzzlePopulator(Monitor, lootProvider)
			};
		}

		private static void MineShaft_populateLevel_Prefix(MineShaft __instance)
		{
			Instance.CurrentPopulator = null;
			Instance.CurrentRandom = null;

			if (__instance.mineLevel == MineShaft.quarryMineShaft)
				return;
			if (__instance.mineLevel < MineShaft.desertArea && __instance.mineLevel % 10 == 0)
				return;

			int seed = 0;
			seed = seed * 31 + (int)Game1.uniqueIDForThisGame;
			seed = seed * 31 + Game1.Date.TotalDays;
			seed = seed * 31 + __instance.mineLevel;
			Random random = new(seed);
			//if (random.NextDouble() > TreasurePopulateChance)
			//	return;
			Instance.CurrentRandom = random;

			List<(IMineShaftPopulator Populator, double Weight)> items = new();
			double weightSum = 0;

			foreach (var populator in Instance.Populators)
			{
				var weight = populator.Prepare(__instance, random);
				if (weight > 0)
				{
					items.Add((populator, weight));
					weightSum += weight;
				}
			}

			if (items.Count == 0)
				return;

			double weightedRandom = random.NextDouble() * weightSum;
			weightSum = 0;
			
			foreach (var (populator, weight) in items)
			{
				weightSum += weight;
				if (weightSum >= weightedRandom)
				{
					Instance.CurrentPopulator = populator;
					populator.BeforePopulate(__instance, random);
					return;
				}
			}
			throw new InvalidOperationException("Reached invalid state.");
		}

		private static void MineShaft_populateLevel_Postfix(MineShaft __instance)
		{
			if (Instance.CurrentPopulator is null || Instance.CurrentRandom is null)
				return;
			Instance.CurrentPopulator.AfterPopulate(__instance, Instance.CurrentRandom);
		}

		private static bool Chest_performOpenChest_Prefix(Chest __instance)
		{
			bool patchResult = true;

			foreach (var populator in Instance.Populators)
			{
				if (populator is not DisarmablePuzzlePopulator disarmable)
					continue;
				bool result = disarmable.OnChestOpen(__instance);
				if (result)
				{
					patchResult = false;
					// TODO: check if it even works in multiplayer properly
					__instance.GetMutex().ReleaseLock();
				}
			}

			return patchResult;
		}
	}
}