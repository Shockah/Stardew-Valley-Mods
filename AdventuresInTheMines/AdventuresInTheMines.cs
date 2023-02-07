﻿using HarmonyLib;
using Shockah.AdventuresInTheMines.Config;
using Shockah.AdventuresInTheMines.Populators;
using Shockah.CommonModCode;
using Shockah.CommonModCode.Stardew;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.AdventuresInTheMines
{
	public class AdventuresInTheMines : BaseMod<ModConfig>
	{
		internal static AdventuresInTheMines Instance = null!;

		private List<IMineShaftPopulator> Populators { get; set; } = new();
		private Random? CurrentRandom { get; set; }
		private IMineShaftPopulator? CurrentPopulator { get; set; }
		private LinkedList<string> QueuedObjectDialogue { get; init; } = new();

		public override void MigrateConfig(ISemanticVersion? configVersion, ISemanticVersion modVersion)
		{
			// do nothing, for now
		}

		public override void OnEntry(IModHelper helper)
		{
			Instance = this;

			helper.Events.GameLoop.DayStarted += OnDayStarted;
			helper.Events.GameLoop.UpdateTicking += OnUpdateTicking;
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

		private void OnUpdateTicking(object? sender, UpdateTickingEventArgs e)
		{
			// run populator updates

			var locations = Game1.getAllFarmers()
				.Select(p => p.currentLocation)
				.OfType<MineShaft>()
				.ToList();

			foreach (var populator in Populators)
				foreach (var location in locations)
					populator.OnUpdateTicking(location);
		}

		private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
		{
			// dequeue object dialogue
			var message = QueuedObjectDialogue.First;
			if (message is not null && Game1.activeClickableMenu is not DialogueBox)
			{
				QueuedObjectDialogue.RemoveFirst();
				Game1.drawObjectDialogue(message.Value);
			}

			// run populator updates

			var locations = Game1.getAllFarmers()
				.Select(p => p.currentLocation)
				.OfType<MineShaft>()
				.ToList();

			foreach (var populator in Populators)
				foreach (var location in locations)
					populator.OnUpdateTicked(location);
		}

		private void ResetPopulators()
		{
			var lootProvider = new LimitedWithAlternativeLootProvider(
				main: new BirthdayPresentLootProvider(Game1.Date.GetByAddingDays(1)),
				alternative: new DefaultMineShaftLootProvider()
			);
			var treasureGenerator = new LootChestTreasureGenerator(lootProvider);
			var ladderFinder = new LadderFinder().Caching();
			var mapOccupancyMapper = new MapOccupancyMapper(ladderFinder).Caching();
			var reachableTileMapper = new ReachableTileMapper(ladderFinder, mapOccupancyMapper).Caching();

			Populators = new()
			{
				new IcePuzzlePopulator(Config.Ice, mapOccupancyMapper, reachableTileMapper, treasureGenerator),
				new BrazierCombinationPuzzlePopulator(Config.BrazierCombination, mapOccupancyMapper, treasureGenerator),
				new BrazierSequencePuzzlePopulator(Config.BrazierSequence, reachableTileMapper, treasureGenerator),
				new BrazierLightUpPuzzlePopulator(Config.BrazierLightUp, mapOccupancyMapper, reachableTileMapper, treasureGenerator),
				new DisarmablePuzzlePopulator(Config.Disarmable, Helper.Translation, mapOccupancyMapper, reachableTileMapper, lootProvider)
			};
		}

		internal void QueueObjectDialogue(string message)
		{
			if (Game1.activeClickableMenu is DialogueBox)
				QueuedObjectDialogue.AddLast(message);
			else
				Game1.drawObjectDialogue(message);
		}

		private static void MineShaft_populateLevel_Prefix(MineShaft __instance)
		{
			Instance.CurrentPopulator = null;
			Instance.CurrentRandom = null;

			if (__instance.mineLevel == MineShaft.quarryMineShaft)
				return;
			if (__instance.mineLevel < MineShaft.desertArea && __instance.mineLevel % 10 == 0)
				return;
			var config = Instance.Config.PopulateEntries.GetMatchingConfig(__instance);
			if (config is null)
				return;

			int seed = 0;
			seed = seed * 31 + (int)Game1.uniqueIDForThisGame;
			seed = seed * 31 + Game1.Date.TotalDays;
			seed = seed * 31 + __instance.mineLevel;
			Random random = new(seed);
			if (random.NextDouble() > config.Chance)
				return;
			Instance.CurrentRandom = random;

			WeightedRandom<IMineShaftPopulator> items = new();
			foreach (var populator in Instance.Populators)
			{
				double weight = populator.Prepare(__instance, random);
				if (weight > 0)
					items.Add(new(weight, populator));
			}

			var randomPopulator = items.NextOrNull(random);
			if (randomPopulator is null)
				return;

			Instance.CurrentPopulator = randomPopulator;
			randomPopulator.BeforePopulate(__instance, random);
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
			if (__instance.FindGameLocation() is not MineShaft location)
				return false;

			foreach (var populator in Instance.Populators)
			{
				bool result = populator.HandleChestOpen(location, __instance);
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