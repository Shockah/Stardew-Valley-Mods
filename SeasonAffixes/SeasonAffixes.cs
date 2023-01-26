using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;
using System;
using StardewValley.Menus;
using System.Linq;
using Shockah.Kokoro;
using Shockah.Kokoro.Stardew;

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

			var harmony = new Harmony(ModManifest.UniqueID);
			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(Game1), nameof(Game1.showEndOfNightStuff)),
				prefix: new HarmonyMethod(AccessTools.Method(typeof(SeasonAffixes), nameof(Game1_showEndOfNightStuff_Prefix)))
			);
		}

		private static ISeasonAffix? GetWeightedRandomAffix(IEnumerable<ISeasonAffix> possibleAffixes, Season season, int year, int seedExtra = 0)
		{
			List<(ISeasonAffix, double)> items = new();
			double weightSum = 0;

			foreach (var affix in possibleAffixes)
			{
				var weight = affix.GetProbabilityWeight(season, year);
				if (weight > 0)
				{
					items.Add((affix, weight));
					weightSum += weight;
				}
			}
			if (items.Count == 0)
				return null;

			int seed = 0;
			seed = 31 * seed + (int)Game1.uniqueIDForThisGame;
			seed = 31 * seed + (int)season;
			seed = 31 * seed + (int)year;
			seed = 31 * seed + seedExtra;
			Random random = new(seed);

			double weightedRandom = random.NextDouble() * weightSum;
			weightSum = 0;

			foreach (var (affix, weight) in items)
			{
				weightSum += weight;
				if (weightSum < weightedRandom)
					return affix;
			}
			throw new InvalidOperationException("Reached invalid state.");
		}

		private IClickableMenu CreateAffixChoiceMenu(Season season, int year, int rerollCount, Action<ISeasonAffix> onAffixChosen)
		{
			var choices = GetWeightedRandomAffixes(AllAffixesStorage.Values, Config.Choices, season, year);
			return CreateAffixChoiceMenu(choices, rerollCount, onAffixChosen);
		}

		private IClickableMenu CreateAffixChoiceMenu(IEnumerable<ISeasonAffix> choices, int rerollCount, Action<ISeasonAffix> onAffixChosen)
		{
			// TODO:
			throw new NotImplementedException();
		}

		private static void Game1_showEndOfNightStuff_Prefix()
		{
			var tomorrow = Game1.Date.GetByAddingDays(1);
			if (tomorrow.GetSeason() == Game1.Date.GetSeason())
				return;

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

		public IReadOnlySet<ISeasonAffix> GetWeightedRandomAffixes(IEnumerable<ISeasonAffix> possibleAffixes, int choices, Season season, int year)
		{
			HashSet<ISeasonAffix> affixesChosen = new();
			var affixesLeft = possibleAffixes.ToHashSet();

			for (int i = 0; i < choices; i++)
			{
				var affix = GetWeightedRandomAffix(affixesLeft, season, year, i);
				if (affix is null)
					continue;

				affixesLeft.Remove(affix);
				affixesChosen.Add(affix);
			}
			return affixesChosen;
		}

		public void PresentAffixChoiceMenu(Season season, int year, int rerollCount, Action<ISeasonAffix> onAffixChosen)
		{
			var choices = GetWeightedRandomAffixes(AllAffixesStorage.Values, Config.Choices, season, year);
			PresentAffixChoiceMenu(choices, rerollCount, onAffixChosen);
		}

		public void PresentAffixChoiceMenu(IEnumerable<ISeasonAffix> choices, int rerollCount, Action<ISeasonAffix> onAffixChosen)
		{
			Game1.activeClickableMenu = CreateAffixChoiceMenu(choices, rerollCount, onAffixChosen);
		}

		#endregion
	}
}