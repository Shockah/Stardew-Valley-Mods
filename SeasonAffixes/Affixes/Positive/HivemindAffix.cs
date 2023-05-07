﻿using Microsoft.Xna.Framework;
using Shockah.CommonModCode.GMCM;
using Shockah.Kokoro.GMCM;
using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using SObject = StardewValley.Object;

namespace Shockah.SeasonAffixes.Affixes.Positive
{
	internal sealed class HivemindAffix : BaseSeasonAffix, ISeasonAffix
	{
		private static string ShortID => "Hivemind";
		public string LocalizedName => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.name");
		public string LocalizedDescription => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.description");
		public TextureRectangle Icon => new(Game1.objectSpriteSheet, new(64, 224, 16, 16));

		public HivemindAffix() : base($"{Mod.ModManifest.UniqueID}.{ShortID}") { }

		public int GetPositivity(OrdinalSeason season)
			=> 1;

		public int GetNegativity(OrdinalSeason season)
			=> 0;

		public double GetProbabilityWeight(OrdinalSeason season)
			=> season.Season != Season.Winter ? 1 : 0;

		public IReadOnlySet<string> Tags { get; init; } = new HashSet<string> { VanillaSkill.FlowersAspect };

		public void OnActivate()
		{
			MachineTracker.MachineChangedEvent += OnMachineChanged;
		}

		public void OnDeactivate()
		{
			MachineTracker.MachineChangedEvent -= OnMachineChanged;
		}

		public void SetupConfig(IManifest manifest)
		{
			var api = Mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu")!;
			GMCMI18nHelper helper = new(api, Mod.ModManifest, Mod.Helper.Translation);
			helper.AddNumberOption($"affix.positive.{ShortID}.config.range", () => Mod.Config.HivemindRange, min: 1, max: 10, interval: 1);
			helper.AddNumberOption($"affix.positive.{ShortID}.config.flowersFor1DayDecrease", () => Mod.Config.HivemindFlowersFor1DayDecrease, min: -1, max: 200, interval: 1, formatValue: value => value == -1 ? helper.Translations.Get($"affix.positive.{ShortID}.config.flowersForXDayDecrease.disabled") : $"{value}");
			helper.AddNumberOption($"affix.positive.{ShortID}.config.flowersFor2DayDecrease", () => Mod.Config.HivemindFlowersFor2DayDecrease, min: -1, max: 200, interval: 1, formatValue: value => value == -1 ? helper.Translations.Get($"affix.positive.{ShortID}.config.flowersForXDayDecrease.disabled") : $"{value}");
			helper.AddNumberOption($"affix.positive.{ShortID}.config.flowersFor3DayDecrease", () => Mod.Config.HivemindFlowersFor3DayDecrease, min: -1, max: 200, interval: 1, formatValue: value => value == -1 ? helper.Translations.Get($"affix.positive.{ShortID}.config.flowersForXDayDecrease.disabled") : $"{value}");
		}

		private void OnMachineChanged(GameLocation location, SObject machine, MachineProcessingState? oldState, MachineProcessingState? newState)
		{
			if (!Context.IsMainPlayer)
				return;
			if (newState is null || newState.Value.ReadyForHarvest || newState.Value.MinutesUntilReady == 0)
				return;
			if (!machine.bigCraftable.Value || machine.ParentSheetIndex != 10)
				return;

			int flowersAround = CountCloseFlowers(location, machine.TileLocation, Mod.Config.HivemindRange, crop => !crop.forageCrop.Value);
			if (flowersAround == 0)
				return;

			var date = Game1.Date;
			int seed = 0;
			seed = 31 * seed + date.Year * 4 + date.SeasonIndex;
			seed = 31 * seed + (int)machine.TileLocation.X;
			seed = 31 * seed + (int)machine.TileLocation.Y;
			Random random = new(seed);

			int flowersLeft = flowersAround;
			int dayDecrease = 0;

			static bool IsSatisfied(int flowersLeft, int flowersRequired, Random random)
			{
				if (flowersLeft >= flowersRequired)
					return true;
				else if (flowersLeft <= 0)
					return false;
				else
					return random.NextDouble() < 1.0 * flowersLeft / flowersRequired;
			}

			void HandleDecrease(int flowersRequired)
			{
				if (IsSatisfied(flowersLeft, flowersRequired, random))
					dayDecrease++;
				flowersLeft -= flowersRequired;
			}

			HandleDecrease(Mod.Config.HivemindFlowersFor1DayDecrease);
			HandleDecrease(Mod.Config.HivemindFlowersFor2DayDecrease - Mod.Config.HivemindFlowersFor1DayDecrease);
			HandleDecrease(Mod.Config.HivemindFlowersFor3DayDecrease - Mod.Config.HivemindFlowersFor2DayDecrease);

			if (newState.Value.MinutesUntilReady <= 24 * 60 * dayDecrease)
			{
				machine.MinutesUntilReady = 0;
				machine.readyForHarvest.Value = true;
				machine.showNextIndex.Value = true;
			}
		}

		private static int CountCloseFlowers(GameLocation location, Vector2 startTileLocation, int range, Func<Crop, bool>? additionalCheck = null)
		{
			int results = 0;
			Queue<Vector2> openList = new();
			HashSet<Vector2> closedList = new();
			openList.Enqueue(startTileLocation);
			while (openList.Count != 0)
			{
				var currentTile = openList.Dequeue();
				if (location.terrainFeatures.TryGetValue(currentTile, out var feature) && feature is HoeDirt { crop: Crop crop } && new SObject(crop.indexOfHarvest.Value, 1).Category == SObject.flowersCategory && crop.currentPhase.Value >= crop.phaseDays.Count - 1 && !crop.dead.Value && (additionalCheck is null || additionalCheck(crop)))
					results++;
				foreach (var v in Utility.getAdjacentTileLocations(currentTile))
					if (!closedList.Contains(v) && (range < 0 || Math.Abs(v.X - startTileLocation.X) + Math.Abs(v.Y - startTileLocation.Y) <= range))
						openList.Enqueue(v);
				closedList.Add(currentTile);
			}
			return results;
		}
	}
}