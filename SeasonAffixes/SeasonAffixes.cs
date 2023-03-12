﻿using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;
using System;
using StardewValley.Menus;
using System.Linq;
using Shockah.Kokoro;
using Shockah.Kokoro.Stardew;
using Shockah.SeasonAffixes.Affixes.Positive;
using Shockah.SeasonAffixes.Affixes.Negative;
using Shockah.SeasonAffixes.Affixes.Neutral;
using Shockah.Kokoro.Stardew.Skill;

namespace Shockah.SeasonAffixes
{
	public class SeasonAffixes : BaseMod<ModConfig>, ISeasonAffixesApi
	{
		public static SeasonAffixes Instance { get; private set; } = null!;
		
		private Dictionary<string, ISeasonAffix> AllAffixesStorage { get; init; } = new Dictionary<string, ISeasonAffix>();
		private List<ISeasonAffix> ActiveAffixesStorage { get; init; } = new List<ISeasonAffix>();

		public override void OnEntry(IModHelper helper)
		{
			Instance = this;

			// positive affixes
			RegisterAffix(new DescentAffix(this));
			RegisterAffix(new FairyTalesAffix(this));
			RegisterAffix(new FortuneAffix(this));
			RegisterAffix(new InnovationAffix(this));
			RegisterAffix(new LoveAffix(this));

			// negative affixes
			RegisterAffix(new CrowsAffix(this));
			RegisterAffix(new DroughtAffix(this));
			RegisterAffix(new HardWaterAffix(this));
			RegisterAffix(new HurricaneAffix(this));
			RegisterAffix(new RustAffix(this));
			RegisterAffix(new SilenceAffix(this));

			// neutral affixes
			RegisterAffix(new InflationAffix(this));
			RegisterAffix(new ThunderAffix(this));
			RegisterAffix(new TidesAffix(this));

			// special affixes
			for (int i = 0; i < 5; i++)
				RegisterAffix(new SkillAffix(this, new VanillaSkill(i), 2f / 5));

			var harmony = new Harmony(ModManifest.UniqueID);
			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(Game1), nameof(Game1.showEndOfNightStuff)),
				prefix: new HarmonyMethod(AccessTools.Method(typeof(SeasonAffixes), nameof(Game1_showEndOfNightStuff_Prefix)))
			);
		}

		private IClickableMenu CreateAffixChoiceMenu(Season season, int year, int rerollCount, Action<ISeasonAffix> onAffixChosen)
		{
			int seed = 0;
			seed = 31 * seed + (int)Game1.uniqueIDForThisGame;
			seed = 31 * seed + (int)season;
			seed = 31 * seed + year;
			Random random = new(seed);

			WeightedRandom<ModConfig.AffixSetEntry> affixSetEntries = new();
			foreach (var entry in Config.AffixSetEntries)
				affixSetEntries.Add(new(entry.Weight, entry));
			var affixSetEntry = affixSetEntries.Next(random);

			var allAffixesProvider = new AllAffixesProvider(this);
			var applicableToSeasonAffixesProvider = new ApplicableToSeasonAffixesProvider(allAffixesProvider, season, year);
			var allCombinationsAffixSetGenerator = new AllCombinationsAffixSetGenerator(applicableToSeasonAffixesProvider, affixSetEntry.Positive, affixSetEntry.Negative);
			var nonConflictingAffixSetGenerator = new NonConflictingAffixSetGenerator(allCombinationsAffixSetGenerator);
			var weightedRandomAffixSetGenerator = new WeightedRandomAffixSetGenerator(nonConflictingAffixSetGenerator, random);
			//var asLittleAsPossibleAffixSetGenerator = new AsLittleAsPossibleAffixSetGenerator(weightedRandomAffixSetGenerator);
			var avoidingDuplicatesIfPossibleAffixSetGenerator = new AvoidingDuplicatesIfPossibleAffixSetGenerator(weightedRandomAffixSetGenerator);

			var affixSets = avoidingDuplicatesIfPossibleAffixSetGenerator.Generate(season, year).Take(Config.Choices);
			return CreateAffixChoiceMenu(affixSets, rerollCount, onAffixChosen);
		}

		private IClickableMenu CreateAffixChoiceMenu(IEnumerable<IReadOnlySet<ISeasonAffix>> choices, int rerollCount, Action<ISeasonAffix> onAffixChosen)
		{
			return new AffixChoiceMenu(choices.Select(affixes => affixes.ToList()).ToList());
		}

		private static void Game1_showEndOfNightStuff_Prefix()
		{
			var tomorrow = Game1.Date.GetByAddingDays(1);
			//if (tomorrow.GetSeason() == Game1.Date.GetSeason())
			//	return;

			if (Game1.endOfNightMenus.Count == 0)
				Game1.endOfNightMenus.Push(new SaveGameMenu());

			Game1.endOfNightMenus.Push(Instance.CreateAffixChoiceMenu(
				season: tomorrow.GetSeason(),
				year: tomorrow.Year,
				rerollCount: Instance.Config.RerollsPerSeason,
				onAffixChosen: affix =>
				{
					if (!Instance.Config.Incremental)
						Instance.DeactivateAllAffixes();
					Instance.ActivateAffix(affix);
				}
			));
		}

		#region API

		public IReadOnlyDictionary<string, ISeasonAffix> AllAffixes => AllAffixesStorage.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		public IReadOnlyList<ISeasonAffix> ActiveAffixes => ActiveAffixes.ToList();

		public ISeasonAffix? GetAffix(string uniqueID)
			=> AllAffixesStorage.TryGetValue(uniqueID, out var affix) ? affix : null;

		public void RegisterAffix(ISeasonAffix affix)
			=> AllAffixesStorage[affix.UniqueID] = affix;

		public void UnregisterAffix(ISeasonAffix affix)
		{
			DeactivateAffix(affix);
			AllAffixesStorage.Remove(affix.UniqueID);
		}

		public void ActivateAffix(ISeasonAffix affix)
		{
			if (!ActiveAffixesStorage.Contains(affix))
				ActiveAffixesStorage.Add(affix);
		}

		public void DeactivateAffix(ISeasonAffix affix)
			=> ActiveAffixesStorage.Remove(affix);

		public void DeactivateAllAffixes()
			=> ActiveAffixesStorage.Clear();

		public IReadOnlySet<ISeasonAffix> GetAllPossibleAffixesForSeason(Season season, int year)
			=> AllAffixesStorage.Values
				.Where(affix => affix.GetProbabilityWeight(season, year) > 0)
				.ToHashSet();

		public void PresentAffixChoiceMenu(Season season, int year, int rerollCount, Action<ISeasonAffix> onAffixChosen)
		{
			Game1.activeClickableMenu = CreateAffixChoiceMenu(season, year, rerollCount, onAffixChosen);
		}

		public void PresentAffixChoiceMenu(IEnumerable<IReadOnlySet<ISeasonAffix>> choices, int rerollCount, Action<ISeasonAffix> onAffixChosen)
		{
			Game1.activeClickableMenu = CreateAffixChoiceMenu(choices, rerollCount, onAffixChosen);
		}

		#endregion
	}
}