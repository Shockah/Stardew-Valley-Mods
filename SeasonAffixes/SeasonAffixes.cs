using HarmonyLib;
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
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace Shockah.SeasonAffixes
{
	public class SeasonAffixes : BaseMod<ModConfig>, ISeasonAffixesApi
	{
		public static SeasonAffixes Instance { get; private set; } = null!;
		
		private Dictionary<string, ISeasonAffix> AllAffixesStorage { get; init; } = new();
		private List<ISeasonAffix> ActiveAffixesStorage { get; init; } = new();
		private List<Func<ISeasonAffix, ISeasonAffix, OrdinalSeason, bool>> AffixConflictProviders { get; init; } = new();

		private readonly PerScreen<SaveData> PerScreenSaveData = new(() => new());

		internal SaveData SaveData
		{
			get => PerScreenSaveData.Value;
			set => PerScreenSaveData.Value = value;
		}

		public override void OnEntry(IModHelper helper)
		{
			Instance = this;
			helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
			helper.Events.GameLoop.Saving += OnSaving;

			// positive affixes
			foreach (var affix in new List<ISeasonAffix>()
			{
				// positive affixes
				new AgricultureAffix(this),
				new ArtifactsAffix(this),
				new DescentAffix(this),
				new FairyTalesAffix(this),
				new FortuneAffix(this),
				new InnovationAffix(this),
				new LoveAffix(this),
				new RanchingAffix(this),

				// negative affixes
				new CrowsAffix(this),
				new DroughtAffix(this),
				new HardWaterAffix(this),
				new HurricaneAffix(this),
				new PoorYieldsAffix(this),
				new RustAffix(this),
				new SilenceAffix(this),

				// neutral affixes
				new InflationAffix(this),
				new ThunderAffix(this),
				new TidesAffix(this),
			})
				RegisterAffix(affix);

			// special affixes
			for (int i = 0; i < 5; i++)
				RegisterAffix(new SkillAffix(this, new VanillaSkill(i), 2f / 5));

			// conflicts
			RegisterAffixConflictProvider((a, b, season) => a is DroughtAffix && b is ThunderAffix);
			RegisterAffixConflictProvider((a, b, season) => a is RustAffix && b is InnovationAffix);
			RegisterAffixConflictProvider((a, b, season) => a is SilenceAffix && b is LoveAffix);
			RegisterAffixConflictProvider((a, b, season) => a is CrowsAffix && b is SkillAffix skillAffix && skillAffix.Skill.Equals(VanillaSkill.Farming));
			RegisterAffixConflictProvider((a, b, season) => a is HurricaneAffix && b is SkillAffix skillAffix && skillAffix.Skill.Equals(VanillaSkill.Foraging));
			RegisterAffixConflictProvider((a, b, season) => a is SkillAffix skillAffixA && b is SkillAffix skillAffixB && skillAffixA.Skill.Equals(skillAffixB.Skill));

			var harmony = new Harmony(ModManifest.UniqueID);
			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(Game1), nameof(Game1.showEndOfNightStuff)),
				prefix: new HarmonyMethod(AccessTools.Method(typeof(SeasonAffixes), nameof(Game1_showEndOfNightStuff_Prefix)))
			);
		}

		private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
		{
			if (!Context.IsMainPlayer)
				return;

			var serializedData = Helper.Data.ReadSaveData<SerializedSaveData>($"{ModManifest.UniqueID}.SaveData");
			SaveData = serializedData is null ? new() : new SaveDataSerializer().Deserialize(serializedData);
		}

		private void OnSaving(object? sender, SavingEventArgs e)
		{
			if (!Context.IsMainPlayer)
				return;

			Helper.Data.WriteSaveData($"{ModManifest.UniqueID}.SaveData", SaveData);
		}

		private IClickableMenu CreateAffixChoiceMenu(OrdinalSeason season, int rerollCount, Action<ISeasonAffix> onAffixChosen)
		{
			int seed = 0;
			seed = 31 * seed + (int)Game1.uniqueIDForThisGame;
			seed = 31 * seed + (int)season.Season;
			seed = 31 * seed + season.Year;
			Random random = new(seed);

			WeightedRandom<ModConfig.AffixSetEntry> affixSetEntries = new();
			foreach (var entry in Config.AffixSetEntries)
				affixSetEntries.Add(new(entry.Weight, entry));
			var affixSetEntry = affixSetEntries.Next(random);

			var allAffixesProvider = new AllAffixesProvider(this);
			var applicableToSeasonAffixesProvider = new ApplicableToSeasonAffixesProvider(allAffixesProvider, season);
			var allCombinationsAffixSetGenerator = new AllCombinationsAffixSetGenerator(applicableToSeasonAffixesProvider, affixSetEntry.Positive, affixSetEntry.Negative);
			var maxAffixesAffixSetGenerator = new MaxAffixesAffixSetGenerator(allCombinationsAffixSetGenerator, 3);
			var nonConflictingAffixSetGenerator = new NonConflictingAffixSetGenerator(maxAffixesAffixSetGenerator, AffixConflictProviders);
			var weightedRandomAffixSetGenerator = new WeightedRandomAffixSetGenerator(nonConflictingAffixSetGenerator, random);
			//var asLittleAsPossibleAffixSetGenerator = new AsLittleAsPossibleAffixSetGenerator(weightedRandomAffixSetGenerator);
			var avoidingDuplicatesIfPossibleAffixSetGenerator = new AvoidingDuplicatesIfPossibleAffixSetGenerator(weightedRandomAffixSetGenerator);

			var affixSets = avoidingDuplicatesIfPossibleAffixSetGenerator.Generate(season).Take(Config.Choices);
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
				season: new(tomorrow.Year, tomorrow.GetSeason()),
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

		public void RegisterAffixConflictProvider(Func<ISeasonAffix, ISeasonAffix, OrdinalSeason, bool> handler)
			=> AffixConflictProviders.Add(handler);

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

		public IReadOnlySet<ISeasonAffix> GetAllPossibleAffixesForSeason(OrdinalSeason season)
			=> AllAffixesStorage.Values
				.Where(affix => affix.GetProbabilityWeight(season) > 0)
				.ToHashSet();

		#endregion
	}
}