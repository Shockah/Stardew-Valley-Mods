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
using System.Text;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;

namespace Shockah.SeasonAffixes
{
	public class SeasonAffixes : BaseMod<ModConfig>, ISeasonAffixesApi
	{
		public static SeasonAffixes Instance { get; private set; } = null!;
		
		private Dictionary<string, ISeasonAffix> AllAffixesStorage { get; init; } = new();
		private List<ISeasonAffix> ActiveAffixesStorage { get; init; } = new();
		private List<Func<IReadOnlySet<ISeasonAffix>, OrdinalSeason, bool>> AffixConflictProviders { get; init; } = new();

		private readonly PerScreen<SaveData> PerScreenSaveData = new(() => new());
		private readonly PerScreen<AffixChoiceMenuConfig?> PerScreenAffixChoiceMenuConfig = new(() => null);
		private readonly PerScreen<Dictionary<Farmer, PlayerChoice>> PerScreenPlayerChoices = new(() => new());

		internal SaveData SaveData
		{
			get => PerScreenSaveData.Value;
			set => PerScreenSaveData.Value = value;
		}

		internal AffixChoiceMenuConfig? AffixChoiceMenuConfig
		{
			get => PerScreenAffixChoiceMenuConfig.Value;
			set => PerScreenAffixChoiceMenuConfig.Value = value;
		}

		internal Dictionary<Farmer, PlayerChoice> PlayerChoices
		{
			get => PerScreenPlayerChoices.Value;
			set => PerScreenPlayerChoices.Value = value;
		}

		public override void OnEntry(IModHelper helper)
		{
			Instance = this;
			helper.Events.GameLoop.DayEnding += OnDayEnding;
			helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
			helper.Events.GameLoop.Saving += OnSaving;
			helper.Events.Multiplayer.PeerConnected += OnPeerConnected;

			RegisterModMessageHandler<NetMessage.UpdateAffixChoiceMenuConfig>(OnUpdateAffixChoiceMenuConfigMessageReceived);
			RegisterModMessageHandler<NetMessage.AffixSetChoice>(OnAffixSetChoiceMessageReceived);
			RegisterModMessageHandler<NetMessage.RerollChoice>(OnRerollChoiceMessageReceived);
			RegisterModMessageHandler<NetMessage.UpdateActiveAffixes>(OnUpdateActiveAffixesMessageReceived);

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
			foreach (var skill in SkillExt.GetAllSkills())
				RegisterAffix(new SkillAffix(this, skill));

			// conflicts
			RegisterAffixConflictProvider((affixes, season) => affixes.Any(a => a is DroughtAffix) && affixes.Any(a => a is ThunderAffix));
			RegisterAffixConflictProvider((affixes, season) => affixes.Any(a => a is RustAffix) && affixes.Any(a => a is InnovationAffix));
			RegisterAffixConflictProvider((affixes, season) => affixes.Any(a => a is SilenceAffix) && affixes.Any(a => a is LoveAffix));
			RegisterAffixConflictProvider((affixes, season) => affixes.Any(a => a is CrowsAffix) && affixes.Any(a => a is SkillAffix skillAffix && skillAffix.Skill.Equals(VanillaSkill.Farming)));
			RegisterAffixConflictProvider((affixes, season) => affixes.Any(a => a is HurricaneAffix) && affixes.Any(a => a is SkillAffix skillAffix && skillAffix.Skill.Equals(VanillaSkill.Foraging)));

			var harmony = new Harmony(ModManifest.UniqueID);
			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(Game1), nameof(Game1.showEndOfNightStuff)),
				prefix: new HarmonyMethod(AccessTools.Method(typeof(SeasonAffixes), nameof(Game1_showEndOfNightStuff_Prefix)))
			);
		}

		private void OnDayEnding(object? sender, DayEndingEventArgs e)
		{
			var tomorrow = Game1.Date.GetByAddingDays(1);
			if (tomorrow.GetSeason() == Game1.Date.GetSeason())
				return;
			QueueOvernightAffixChoice();
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

		private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
		{
			SendModMessage(new NetMessage.UpdateActiveAffixes(ActiveAffixes.Select(a => a.UniqueID).ToHashSet()), e.Peer);
		}

		private void OnUpdateAffixChoiceMenuConfigMessageReceived(NetMessage.UpdateAffixChoiceMenuConfig message)
		{
			if (Context.IsMainPlayer)
			{
				Monitor.Log("Received affix choice menu config, but we did not expect to receive it as the host.", LogLevel.Error);
				return;
			}

			AffixChoiceMenuConfig newConfig = new(
				message.Season,
				message.Incremental,
				message.Choices.Select(choice => choice.Select(id => GetAffix(id)).WhereNotNull().ToHashSet()).ToList(),
				message.RerollsLeft
			);

			if ((newConfig.Choices?.Sum(choice => choice.Count) ?? 0) != message.Choices.Sum(choice => choice.Count))
			{
				Monitor.Log("Received affix choice menu config, but it seems we are running a different set of mods.", LogLevel.Error);
				return;
			}
			AffixChoiceMenuConfig = newConfig;
		}

		private void OnAffixSetChoiceMessageReceived(Farmer sender, NetMessage.AffixSetChoice message)
		{
			var choice = new PlayerChoice.Choice(message.Affixes.Select(id => GetAffix(id)).WhereNotNull().ToHashSet());
			if (choice.Affixes.Count != message.Affixes.Count)
			{
				Monitor.Log($"Player {sender.displayName} voted, but seems to be running a different set of mods, making the vote invalid.", LogLevel.Error);
				RegisterChoice(sender, new PlayerChoice.Invalid());
			}
			else
			{
				RegisterChoice(sender, choice);
			}
		}

		private void OnRerollChoiceMessageReceived(Farmer sender, NetMessage.RerollChoice _)
		{
			RegisterChoice(sender, new PlayerChoice.Reroll());
		}

		private void OnUpdateActiveAffixesMessageReceived(NetMessage.UpdateActiveAffixes message)
		{
			ActiveAffixesStorage.Clear();
			ActiveAffixesStorage.AddRange(message.Affixes.Select(id => GetAffix(id)).WhereNotNull());
		}

		internal void RegisterChoice(Farmer player, PlayerChoice anyChoice)
		{
			PlayerChoices[player] = anyChoice;

			if (player == Game1.player)
			{
				if (anyChoice is PlayerChoice.Choice choice)
					SendModMessageToEveryone(new NetMessage.AffixSetChoice(choice.Affixes.Select(a => a.UniqueID).ToHashSet()));
				else if (anyChoice is PlayerChoice.Reroll)
					SendModMessageToEveryone(new NetMessage.RerollChoice());
				else if (anyChoice is PlayerChoice.Invalid)
				{ } // do nothing
				else
					throw new NotImplementedException($"Unimplemented player choice {anyChoice}.");
			}

			if (Context.IsMainPlayer)
			{
				// TODO: check if all choices done -> apply
				// TODO: check if reroll won

				if (!Config.Incremental)
					ActiveAffixesStorage.Clear();
				// TODO: activate chosen affixes;
				SendModMessageToEveryone(new NetMessage.UpdateActiveAffixes(ActiveAffixes.Select(a => a.UniqueID).ToHashSet()));
			}
		}

		internal string GetSeasonName(IReadOnlyList<ISeasonAffix> affixes)
		{
			StringBuilder sb = new();
			for (int i = 0; i < affixes.Count; i++)
			{
				if (i != 0)
					sb.Append(Helper.Translation.Get(i == affixes.Count - 1 ? "season.separator.last" : "season.separator.other"));
				sb.Append(affixes[i].LocalizedName);
			}
			return sb.ToString();
		}

		private static void Game1_showEndOfNightStuff_Prefix()
		{
			var menuConfig = Instance.AffixChoiceMenuConfig;
			if (menuConfig is null)
				return;

			if (Game1.endOfNightMenus.Count == 0)
				Game1.endOfNightMenus.Push(new SaveGameMenu());

			var tomorrow = Game1.Date.GetByAddingDays(1);
			OrdinalSeason season = new(tomorrow.Year, tomorrow.GetSeason());

			IReadOnlyList<IReadOnlySet<ISeasonAffix>>? choices = null;
			if (Context.IsMainPlayer)
			{
				int seed = 0;
				seed = 31 * seed + (int)Game1.uniqueIDForThisGame;
				seed = 31 * seed + (int)season.Season;
				seed = 31 * seed + season.Year;
				Random random = new(seed);

				WeightedRandom<ModConfig.AffixSetEntry> affixSetEntries = new();
				foreach (var entry in Instance.Config.AffixSetEntries)
					affixSetEntries.Add(new(entry.Weight, entry));
				var affixSetEntry = affixSetEntries.Next(random);

				var allAffixesProvider = new AllAffixesProvider(Instance);
				var applicableToSeasonAffixesProvider = new ApplicableToSeasonAffixesProvider(allAffixesProvider, season);

				var affixSetGenerator = new AllCombinationsAffixSetGenerator(applicableToSeasonAffixesProvider, affixSetEntry.Positive, affixSetEntry.Negative)
					.MaxAffixes(3)
					.NonConflicting(Instance.AffixConflictProviders)
					.WeightedRandom(random)
					.AvoidingChoiceHistoryDuplicates()
					.AvoidingSetChoiceHistoryDuplicates()
					.AsLittleAsPossible()
					.AvoidingDuplicatesBetweenChoices();

				choices = affixSetGenerator.Generate(season).Take(Instance.Config.Choices).ToList();
				Instance.SaveData.AffixChoiceHistory.Add(choices.SelectMany(set => set).ToHashSet());
				Instance.SaveData.AffixSetChoiceHistory.Add(choices.Select(set => (ISet<ISeasonAffix>)set.ToHashSet()).ToHashSet());

				Instance.SendModMessageToEveryone(new NetMessage.UpdateAffixChoiceMenuConfig(
					season,
					Instance.Config.Incremental,
					choices.Select(choice => choice.Select(a => a.UniqueID).ToHashSet()).ToList(),
					Instance.Config.RerollsPerSeason
				));
			}

			menuConfig = menuConfig.WithChoices(choices);
			Instance.AffixChoiceMenuConfig = menuConfig;

			AffixChoiceMenu menu = new(menuConfig);
			Game1.endOfNightMenus.Push(menu);
			Instance.PlayerChoices.Clear();
		}

		#region API

		public IReadOnlyDictionary<string, ISeasonAffix> AllAffixes => AllAffixesStorage.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		public IReadOnlyList<ISeasonAffix> ActiveAffixes => ActiveAffixesStorage.ToList();

		public ISeasonAffix? GetAffix(string uniqueID)
			=> AllAffixesStorage.TryGetValue(uniqueID, out var affix) ? affix : null;

		public void RegisterAffix(ISeasonAffix affix)
			=> AllAffixesStorage[affix.UniqueID] = affix;

		public void RegisterAffixConflictProvider(Func<IReadOnlySet<ISeasonAffix>, OrdinalSeason, bool> handler)
			=> AffixConflictProviders.Add(handler);

		public void UnregisterAffix(ISeasonAffix affix)
		{
			DeactivateAffix(affix);
			AllAffixesStorage.Remove(affix.UniqueID);
			SendModMessageToEveryone(new NetMessage.UpdateActiveAffixes(ActiveAffixes.Select(a => a.UniqueID).ToHashSet()));
		}

		public void ActivateAffix(ISeasonAffix affix)
		{
			if (!ActiveAffixesStorage.Contains(affix))
			{
				ActiveAffixesStorage.Add(affix);
				SendModMessageToEveryone(new NetMessage.UpdateActiveAffixes(ActiveAffixes.Select(a => a.UniqueID).ToHashSet()));
			}
		}

		public void DeactivateAffix(ISeasonAffix affix)
		{
			if (ActiveAffixesStorage.Contains(affix))
			{
				ActiveAffixesStorage.Remove(affix);
				SendModMessageToEveryone(new NetMessage.UpdateActiveAffixes(ActiveAffixes.Select(a => a.UniqueID).ToHashSet()));
			}
		}

		public void DeactivateAllAffixes()
		{
			ActiveAffixesStorage.Clear();
			SendModMessageToEveryone(new NetMessage.UpdateActiveAffixes(ActiveAffixes.Select(a => a.UniqueID).ToHashSet()));
		}

		public void QueueOvernightAffixChoice()
		{
			var tomorrow = Game1.Date.GetByAddingDays(1);
			AffixChoiceMenuConfig = new(new(tomorrow.Year, tomorrow.GetSeason()), Instance.Config.Incremental, null, Instance.Config.RerollsPerSeason);
		}

		public IReadOnlySet<ISeasonAffix> GetAllPossibleAffixesForSeason(OrdinalSeason season)
			=> AllAffixesStorage.Values
				.Where(affix => affix.GetProbabilityWeight(season) > 0)
				.ToHashSet();

		#endregion
	}
}