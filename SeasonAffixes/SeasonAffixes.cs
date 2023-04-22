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
using Shockah.Kokoro.UI;
using Shockah.SeasonAffixes.Affixes;
using Shockah.CommonModCode.GMCM;
using Shockah.Kokoro.GMCM;

namespace Shockah.SeasonAffixes
{
	public class SeasonAffixes : BaseMod<ModConfig>, ISeasonAffixesApi
	{
		public static SeasonAffixes Instance { get; private set; } = null!;
		private bool IsConfigRegistered { get; set; } = false;
		internal Harmony Harmony { get; private set; } = null!;
		private ModConfig.AffixSetEntry NewAffixSetEntry = new();

		private bool DidRegisterSkillAffixes = false;
		private Dictionary<string, ISeasonAffix> AllAffixesStorage { get; init; } = new();
		private List<CombinedAffix> AffixCombinationsStorage { get; init; } = new();
		private List<Func<IReadOnlySet<ISeasonAffix>, OrdinalSeason, bool>> AffixConflictProviders { get; init; } = new();

		private readonly PerScreen<SaveData> PerScreenSaveData = new(() => new());
		private readonly PerScreen<bool> PerScreenIsAffixChoiceMenuQueued = new(() => false);
		private readonly PerScreen<AffixChoiceMenuConfig?> PerScreenAffixChoiceMenuConfig = new(() => null);
		private readonly PerScreen<Dictionary<Farmer, PlayerChoice>> PerScreenPlayerChoices = new(() => new());

		internal SaveData SaveData
		{
			get => PerScreenSaveData.Value;
			set => PerScreenSaveData.Value = value;
        }

        internal bool IsAffixChoiceMenuQueued
        {
            get => PerScreenIsAffixChoiceMenuQueued.Value;
            set => PerScreenIsAffixChoiceMenuQueued.Value = value;
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
			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.GameLoop.DayEnding += OnDayEnding;
			helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
			helper.Events.GameLoop.Saving += OnSaving;
			helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
			helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
			helper.Events.Multiplayer.PeerConnected += OnPeerConnected;

			RegisterModMessageHandler<NetMessage.QueueOvernightAffixChoice>(OnQueueOvernightAffixChoiceMessageReceived);
			RegisterModMessageHandler<NetMessage.UpdateAffixChoiceMenuConfig>(OnUpdateAffixChoiceMenuConfigMessageReceived);
			RegisterModMessageHandler<NetMessage.AffixSetChoice>(OnAffixSetChoiceMessageReceived);
			RegisterModMessageHandler<NetMessage.RerollChoice>(OnRerollChoiceMessageReceived);
			RegisterModMessageHandler<NetMessage.ConfirmAffixSetChoice>(OnConfirmAffixSetChoiceMessageReceived);
			RegisterModMessageHandler<NetMessage.UpdateActiveAffixes>(OnUpdateActiveAffixesMessageReceived);

			helper.ConsoleCommands.Add("affixes_list_all", "Lists all known (active or not) seasonal affixes.", (_, _) =>
			{
				var affixes = AllAffixes.Values.ToList();
				if (affixes.Count == 0)
				{
					Monitor.Log("There are no known (active or not) affixes.", LogLevel.Info);
					return;
				}

				var output = string.Join("\n\n", affixes.Select(a => $"ID: {a.UniqueID}\nName: {a.LocalizedName}\nDescription: {a.LocalizedDescription}"));
				Monitor.Log(output, LogLevel.Info);
			});
			helper.ConsoleCommands.Add("affixes_list_active", "Lists all active seasonal affixes.", (_, _) =>
			{
				var affixes = ActiveAffixes;
				if (affixes.Count == 0)
				{
					Monitor.Log("There are no active affixes.", LogLevel.Info);
					return;
				}

				var output = string.Join("\n\n", affixes.Select(a => $"ID: {a.UniqueID}\nName: {a.LocalizedName}\nDescription: {a.LocalizedDescription}"));
				Monitor.Log(output, LogLevel.Info);
			});
			helper.ConsoleCommands.Add("affixes_activate", "Activates a seasonal affix with given ID.", (_, args) =>
			{
				if (args.Length == 0)
				{
					Monitor.Log("You need to provide an affix ID.", LogLevel.Error);
					return;
				}
				var id = args[0];

				var affix = GetAffix(id);
				if (affix is null)
				{
					Monitor.Log($"Unknown affix with ID `{id}`.", LogLevel.Error);
					return;
				}

				ActivateAffix(affix);
			});
			helper.ConsoleCommands.Add("affixes_deactivate", "Deactivates a seasonal affix with given ID.", (_, args) =>
			{
				if (args.Length == 0)
				{
					Monitor.Log("You need to provide an affix ID.", LogLevel.Error);
					return;
				}
				var id = args[0];

				var affix = GetAffix(id);
				if (affix is null)
				{
					Monitor.Log($"Unknown affix with ID `{id}`.", LogLevel.Error);
					return;
				}

				DeactivateAffix(affix);
			});
			helper.ConsoleCommands.Add("affixes_deactivate_all", "Deactivates all seasonal affixes.", (_, _) => DeactivateAllAffixes());
			helper.ConsoleCommands.Add("affixes_queue_choice", "Queues an overnight affix choice menu.", (_, _) => QueueOvernightAffixChoice());

			Harmony = new(ModManifest.UniqueID);
			Harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(Game1), nameof(Game1.showEndOfNightStuff)),
				prefix: new HarmonyMethod(AccessTools.Method(typeof(SeasonAffixes), nameof(Game1_showEndOfNightStuff_Prefix)))
			);
			BillboardPatches.Apply(Harmony);
		}

		public override void MigrateConfig(ISemanticVersion? configVersion, ISemanticVersion modVersion)
		{
			// no migration required, for now
		}

		private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
		{
			// positive affixes
			foreach (var affix in new List<ISeasonAffix>()
			{
				// positive affixes
				new AgricultureAffix(),
				new DescentAffix(),
				new FairyTalesAffix(),
				new FortuneAffix(),
				new InnovationAffix(),
				new LootAffix(),
				new LoveAffix(),
				new MudAffix(),
				new RanchingAffix(),
				new TreasuresAffix(),

				// negative affixes
				new CrowsAffix(),
				new DroughtAffix(),
				new HardWaterAffix(),
				new HurricaneAffix(),
				new PoorYieldsAffix(),
				new RustAffix(),
				new SilenceAffix(),

				// neutral affixes
				new InflationAffix(),
				new ThunderAffix(),
				new TidesAffix(),
				new WildGrowthAffix(),
			})
				RegisterAffix(affix);

			// conflicts
			RegisterAffixConflictProvider((affixes, season) => affixes.Any(a => a is DroughtAffix) && affixes.Any(a => a is ThunderAffix));
			RegisterAffixConflictProvider((affixes, season) => affixes.Any(a => a is RustAffix) && affixes.Any(a => a is InnovationAffix));
			RegisterAffixConflictProvider((affixes, season) => affixes.Any(a => a is SilenceAffix) && affixes.Any(a => a is LoveAffix));
			RegisterAffixConflictProvider((affixes, season) => affixes.Any(a => a is HardWaterAffix) && affixes.Any(a => a is MudAffix));
			RegisterAffixConflictProvider((affixes, season) => affixes.Any(a => a is CrowsAffix) && affixes.Any(a => a is SkillAffix skillAffix && skillAffix.Skill.Equals(VanillaSkill.Farming)));
			RegisterAffixConflictProvider((affixes, season) => affixes.Any(a => a is HurricaneAffix) && affixes.Any(a => a is SkillAffix skillAffix && skillAffix.Skill.Equals(VanillaSkill.Foraging)));

			SetupConfig();
		}

		private void OnDayEnding(object? sender, DayEndingEventArgs e)
		{
			if (!Context.IsMainPlayer)
				return;

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

			foreach (var affix in SaveData.ActiveAffixes)
				affix.OnActivate();
			Monitor.Log($"Loaded save file. Active affixes:\n{string.Join("\n", SaveData.ActiveAffixes.Select(a => a.UniqueID))}", LogLevel.Info);
		}

		private void OnSaving(object? sender, SavingEventArgs e)
		{
			if (!Context.IsMainPlayer)
				return;

			var serializedData = new SaveDataSerializer().Serialize(SaveData);
			Helper.Data.WriteSaveData($"{ModManifest.UniqueID}.SaveData", serializedData);
		}

		private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
		{
			foreach (var affix in SaveData.ActiveAffixes)
				affix.OnDeactivate();
			Monitor.Log("Unloaded save file. Deactivating all affixes.", LogLevel.Debug);
		}

		private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
		{
			if (DidRegisterSkillAffixes)
				return;
			DidRegisterSkillAffixes = true;

			// skill-related affixes
			foreach (var skill in SkillExt.GetAllSkills())
				RegisterAffix(new SkillAffix(skill));
		}

		private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
		{
			SendModMessage(new NetMessage.UpdateActiveAffixes(ActiveAffixes.Select(a => a.UniqueID).ToHashSet()), e.Peer);
		}

		private void OnQueueOvernightAffixChoiceMessageReceived(NetMessage.QueueOvernightAffixChoice _)
		{
			IsAffixChoiceMenuQueued = true;
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
			if (Game1.activeClickableMenu is AffixChoiceMenu menu)
				menu.Config = newConfig;
		}

		private void OnAffixSetChoiceMessageReceived(Farmer sender, NetMessage.AffixSetChoice message)
		{
			var choice = new PlayerChoice.Choice(message.Affixes.Select(id => GetAffix(id)).WhereNotNull().ToHashSet());
			if (choice.Affixes.Count != message.Affixes.Count)
			{
				Monitor.Log($"Player {sender.displayName} voted, but seems to be running a different set of mods, making the vote invalid.", LogLevel.Error);
				RegisterChoice(sender, PlayerChoice.Invalid.Instance);
			}
			else
			{
				RegisterChoice(sender, choice);
			}
		}

		private void OnRerollChoiceMessageReceived(Farmer sender, NetMessage.RerollChoice _)
		{
			RegisterChoice(sender, PlayerChoice.Reroll.Instance);
		}

		private void OnConfirmAffixSetChoiceMessageReceived(NetMessage.ConfirmAffixSetChoice message)
		{
			if (Game1.activeClickableMenu is AffixChoiceMenu menu)
			{
				if (message.Affixes is null)
				{
					menu.exitThisMenu(playSound: false);
					return;
				}

				var affixes = message.Affixes.Select(id => GetAffix(id)).WhereNotNull().ToHashSet();
				if (affixes.Count != message.Affixes.Count)
				{
					Monitor.Log("Due to mod mismatch, the players chose an invalid set of affixes. Closing the menu.", LogLevel.Error);
					menu.exitThisMenu(playSound: false);
					return;
				}
				
				menu.SetConfirmedAffixSetChoice(affixes);
			}
			else
			{
				Monitor.Log("Tried to confirm affix set choice, but our menu is gone???", LogLevel.Error);
			}
		}

		private void OnUpdateActiveAffixesMessageReceived(NetMessage.UpdateActiveAffixes message)
		{
			var affixes = message.Affixes.Select(id => GetAffix(id)).WhereNotNull().ToList();
			LocalChangeActiveAffixes(affixes);
		}

		private void SetupConfig()
		{
			var api = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
			if (api is null)
				return;
			GMCMI18nHelper helper = new(api, ModManifest, Helper.Translation);

			if (IsConfigRegistered)
				api.Unregister(ModManifest);

			api.Register(
				ModManifest,
				reset: () => Config = new(),
				save: () =>
				{
					Config.AffixSetEntries = Config.AffixSetEntries
						.Where(entry => entry.IsValid())
						.ToList();
					if (NewAffixSetEntry.IsValid())
					{
						Config.AffixSetEntries.Add(NewAffixSetEntry);
						NewAffixSetEntry = new();
					}

					foreach (var affix in AllAffixesStorage.Values)
						affix.OnSaveConfig();
					WriteConfig();
					LogConfig();
					SetupConfig();
				}
			);

			helper.AddBoolOption("config.incremental", () => Config.Incremental);
			helper.AddNumberOption("config.choices", () => Config.Choices, min: 1, max: 4, interval: 1);
			helper.AddNumberOption("config.affixRepeatPeriod", () => Config.AffixRepeatPeriod, min: 0);
			helper.AddNumberOption("config.affixSetRepeatPeriod", () => Config.AffixSetRepeatPeriod, min: 0);

			void RegisterAffixSetEntrySection(int? index)
			{
				ModConfig.AffixSetEntry GetEntry()
					=> index is null ? NewAffixSetEntry : Config.AffixSetEntries[index.Value];

				void SetEntry(ModConfig.AffixSetEntry value)
				{
					if (index is null)
						NewAffixSetEntry = value;
					else
						Config.AffixSetEntries[index.Value] = value;
				}

				helper.AddSectionTitle("config.affixSetEntries.section", new { Number = index is null ? Config.AffixSetEntries.Count + 1 : index.Value + 1 });
				helper.AddNumberOption(
					keyPrefix: "config.affixSetEntries.positive",
					getValue: () => GetEntry().Positive,
					setValue: value => SetEntry(new(value, GetEntry().Negative, GetEntry().Weight)),
					min: 0, max: 5, interval: 1
				);
				helper.AddNumberOption(
					keyPrefix: "config.affixSetEntries.negative",
					getValue: () => GetEntry().Negative,
					setValue: value => SetEntry(new(GetEntry().Positive, value, GetEntry().Weight)),
					min: 0, max: 5, interval: 1
				);
				helper.AddNumberOption(
					keyPrefix: "config.affixSetEntries.weight",
					getValue: () => (float)GetEntry().Weight,
					setValue: value => SetEntry(new(GetEntry().Positive, GetEntry().Negative, value)),
					min: 0f, max: 10f, interval: 0.1f
				);
			}

			for (int i = 0; i < Config.AffixSetEntries.Count; i++)
				RegisterAffixSetEntrySection(i);
			RegisterAffixSetEntrySection(null);

			foreach (var affix in AllAffixesStorage.Values.OrderBy(a => a.LocalizedName).ThenBy(a => a.UniqueID))
			{
				api.AddSectionTitle(ModManifest, () => affix.LocalizedName, () => affix.LocalizedDescription);

				api.AddNumberOption(
					ModManifest,
					getValue: () => Config.AffixWeights.TryGetValue(affix.UniqueID, out var weight) ? (float)weight : 1f,
					setValue: value =>
					{
						if (value >= 0.999f && value <= 1.001f)
							Config.AffixWeights.Remove(affix.UniqueID);
						else
							Config.AffixWeights[affix.UniqueID] = value;
					},
					name: () => Helper.Translation.Get("config.affix.weight.name"),
					tooltip: () => Helper.Translation.Get("config.affix.weight.tooltip"),
					min: 0f, max: 10f, interval: 0.025f
				);

				affix.SetupConfig(ModManifest);
			}

			IsConfigRegistered = true;
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
				if (PlayerChoices.Count != Game1.getOnlineFarmers().Count)
					return;

				var groupedChoices = PlayerChoices
					.GroupBy(kvp => kvp.Value)
					.Select(group => (Choice: group.Key, Players: group.Select(kvp => kvp.Key).ToList()))
					.OrderByDescending(group => group.Players.Count)
					.ToList();
				int mostVotes = groupedChoices[0].Players.Count;
				var topChoices = groupedChoices.TakeWhile(group => group.Players.Count == mostVotes).Select(group => group.Choice).ToList();
				var choice = Game1.random.NextElement(topChoices);

				if (choice is PlayerChoice.Choice affixChoice)
				{
					var newAffixes = new HashSet<ISeasonAffix>();
					if (Config.Incremental)
						foreach (var affix in SaveData.ActiveAffixes)
							newAffixes.Add(affix);
					foreach (var affix in affixChoice.Affixes)
						newAffixes.Add(affix);

					ChangeActiveAffixes(newAffixes);
					SendModMessageToEveryone(new NetMessage.ConfirmAffixSetChoice(SaveData.ActiveAffixes.Select(a => a.UniqueID).ToHashSet()));

					if (Game1.activeClickableMenu is AffixChoiceMenu menu)
						menu.SetConfirmedAffixSetChoice(affixChoice.Affixes);
					else
						Monitor.Log("Tried to confirm affix set choice, but our menu is gone???", LogLevel.Error);
				}
				else if (choice is PlayerChoice.Reroll)
				{
					// TODO: handle reroll
				}
				else if (choice is PlayerChoice.Invalid)
				{
					Monitor.Log("Due to mod mismatch, the players chose an invalid set of affixes. Closing the menu.", LogLevel.Error);
					SendModMessageToEveryone(new NetMessage.ConfirmAffixSetChoice(null));
				}
			}
		}

		private void LocalActivateAffix(ISeasonAffix affix)
		{
			if (SaveData.ActiveAffixes.Contains(affix))
				return;
			SaveData.ActiveAffixes.Add(affix);
			affix.OnActivate();
			Monitor.Log($"Activated affix `{affix.UniqueID}`.", LogLevel.Info);
		}

		private void LocalDeactivateAffix(ISeasonAffix affix)
		{
			if (!SaveData.ActiveAffixes.Contains(affix))
				return;
			affix.OnDeactivate();
			SaveData.ActiveAffixes.Remove(affix);
			Monitor.Log($"Deactivated affix `{affix.UniqueID}`.", LogLevel.Info);
		}

		private void LocalChangeActiveAffixes(IEnumerable<ISeasonAffix> affixes)
		{
			var affixSet = affixes.ToHashSet();
			var toDeactivate = SaveData.ActiveAffixes.Where(a => !affixSet.Contains(a)).ToList();
			var toActivate = affixSet.Where(a => !SaveData.ActiveAffixes.Contains(a)).ToList();

			foreach (var affix in toDeactivate)
				LocalDeactivateAffix(affix);
			foreach (var affix in toActivate)
				LocalActivateAffix(affix);
		}

		private static void Game1_showEndOfNightStuff_Prefix()
		{
			if (!Instance.IsAffixChoiceMenuQueued)
				return;
			Instance.IsAffixChoiceMenuQueued = false;

			if (Game1.endOfNightMenus.Count == 0)
				Game1.endOfNightMenus.Push(new SaveGameMenu());

			if (Context.IsMainPlayer)
			{
				var date = Game1.Date; // it's already "tomorrow" by now
                OrdinalSeason season = new(date.Year, date.GetSeason());

                int seed = 0;
				seed = 31 * seed + (int)Game1.uniqueIDForThisGame;
				seed = 31 * seed + (int)season.Season;
				seed = 31 * seed + season.Year;
				Random random = new(seed);

				WeightedRandom<ModConfig.AffixSetEntry> affixSetEntries = new();
				foreach (var entry in Instance.Config.AffixSetEntries)
					affixSetEntries.Add(new(entry.Weight, entry));
				var affixSetEntry = affixSetEntries.Next(random);

				var affixesProvider = new CompoundAffixesProvider(
					new AffixesProvider(Instance.AllAffixesStorage.Values.Where(a => !Instance.Config.AffixWeights.TryGetValue(a.UniqueID, out var weight) || weight > 0)),
					new AffixesProvider(Instance.AffixCombinationsStorage.Where(combinedAffix => combinedAffix.Affixes.All(a => !Instance.Config.AffixWeights.TryGetValue(a.UniqueID, out var weight) || weight > 0)))
				).ApplicableToSeason(season);

				var affixSetGenerator = new AllCombinationsAffixSetGenerator(affixesProvider, affixSetEntry.Positive, affixSetEntry.Negative)
					.MaxAffixes(3)
					.NonConflicting(Instance.AffixConflictProviders)
					.NonConflictingWithCombinations()
					.WeightedRandom(random, a => Instance.Config.AffixWeights.TryGetValue(a.UniqueID, out var weight) ? weight : 1.0)
					.Decombined()
					.AvoidingChoiceHistoryDuplicates()
					.AvoidingSetChoiceHistoryDuplicates()
					//.AsLittleAsPossible()
					.AvoidingDuplicatesBetweenChoices();
				var choices = affixSetGenerator.Generate(season).Take(Instance.Config.Choices).ToList();

				Instance.SaveData.AffixChoiceHistory.Add(choices.SelectMany(set => set).ToHashSet());
				while (Instance.SaveData.AffixChoiceHistory.Count > Instance.Config.AffixRepeatPeriod)
					Instance.SaveData.AffixChoiceHistory.RemoveAt(0);

				Instance.SaveData.AffixSetChoiceHistory.Add(choices.Select(set => (ISet<ISeasonAffix>)set.ToHashSet()).ToHashSet());
				while (Instance.SaveData.AffixSetChoiceHistory.Count > Instance.Config.AffixSetRepeatPeriod)
					Instance.SaveData.AffixSetChoiceHistory.RemoveAt(0);

				Instance.AffixChoiceMenuConfig = new(new(date.Year, date.GetSeason()), Instance.Config.Incremental, choices, 0);
                Instance.SendModMessageToEveryone(new NetMessage.UpdateAffixChoiceMenuConfig(
					season,
					Instance.Config.Incremental,
					choices.Select(choice => choice.Select(a => a.UniqueID).ToHashSet()).ToList(),
					0
				));
			}

			Game1.endOfNightMenus.Push(new AffixChoiceMenu());
			Instance.PlayerChoices.Clear();
		}

		#region API

		public IReadOnlyDictionary<string, ISeasonAffix> AllAffixes => AllAffixesStorage.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		public IEnumerable<(ISeasonAffix Combined, IReadOnlySet<ISeasonAffix> Affixes)> AffixCombinations => AffixCombinationsStorage.Select(a => (Combined: (ISeasonAffix)a, Affixes: a.Affixes));
		public IReadOnlySet<ISeasonAffix> ActiveAffixes => SaveData.ActiveAffixes.ToHashSet();

		public ISeasonAffix? GetAffix(string uniqueID)
			=> AllAffixesStorage.TryGetValue(uniqueID, out var affix) ? affix : null;

		public void RegisterAffix(ISeasonAffix affix)
		{
			if (AllAffixesStorage.ContainsKey(affix.UniqueID))
				throw new ArgumentException($"An affix with ID `{affix.UniqueID}` is already registered.");
			AllAffixesStorage[affix.UniqueID] = affix;
			affix.OnRegister();

			if (IsConfigRegistered)
				SetupConfig();
		}

		public void UnregisterAffix(ISeasonAffix affix)
		{
			if (!AllAffixesStorage.ContainsKey(affix.UniqueID))
				return;
			DeactivateAffix(affix);
			affix.OnUnregister();
			AllAffixesStorage.Remove(affix.UniqueID);

			if (IsConfigRegistered)
				SetupConfig();
		}

		public void RegisterVisualAffixCombination(IReadOnlySet<ISeasonAffix> affixes, Func<TextureRectangle> icon, Func<string> localizedName, Func<string>? localizedDescription = null)
			=> RegisterAffixCombination(affixes, icon, localizedName, localizedDescription, _ => 0);

		public void RegisterAffixCombination(IReadOnlySet<ISeasonAffix> affixes, Func<TextureRectangle> icon, Func<string> localizedName, Func<string>? localizedDescription = null, Func<OrdinalSeason, double>? probabilityWeightProvider = null)
		{
			UnregisterAffixCombination(affixes);
			AffixCombinationsStorage.Add(new(affixes, icon, localizedName, localizedDescription, probabilityWeightProvider));
		}

		public void UnregisterAffixCombination(IReadOnlySet<ISeasonAffix> affixes)
		{
			int? index = AffixCombinationsStorage.FirstIndex(a => a.Affixes.SetEquals(affixes));
			if (index is not null)
				AffixCombinationsStorage.RemoveAt(index.Value);
		}

		public void RegisterAffixConflictProvider(Func<IReadOnlySet<ISeasonAffix>, OrdinalSeason, bool> handler)
			=> AffixConflictProviders.Add(handler);

		public void UnregisterAffixConflictProvider(Func<IReadOnlySet<ISeasonAffix>, OrdinalSeason, bool> handler)
			=> AffixConflictProviders.Remove(handler);

		public void ActivateAffix(ISeasonAffix affix)
		{
			if (SaveData.ActiveAffixes.Contains(affix))
				return;
			LocalActivateAffix(affix);
            SendModMessageToEveryone(new NetMessage.UpdateActiveAffixes(ActiveAffixes.Select(a => a.UniqueID).ToHashSet()));
		}

		public void DeactivateAffix(ISeasonAffix affix)
		{
			if (!SaveData.ActiveAffixes.Contains(affix))
				return;
			LocalDeactivateAffix(affix);
			SendModMessageToEveryone(new NetMessage.UpdateActiveAffixes(ActiveAffixes.Select(a => a.UniqueID).ToHashSet()));
		}

		public void DeactivateAllAffixes()
		{
			foreach (var affix in SaveData.ActiveAffixes.ToList())
				LocalDeactivateAffix(affix);
			SendModMessageToEveryone(new NetMessage.UpdateActiveAffixes(ActiveAffixes.Select(a => a.UniqueID).ToHashSet()));
		}

		public void ChangeActiveAffixes(IEnumerable<ISeasonAffix> affixes)
		{
			LocalChangeActiveAffixes(affixes);
			SendModMessageToEveryone(new NetMessage.UpdateActiveAffixes(ActiveAffixes.Select(a => a.UniqueID).ToHashSet()));
		}

		public IReadOnlyList<ISeasonAffix> GetUIOrderedAffixes(OrdinalSeason season, IEnumerable<ISeasonAffix> affixes)
		{
			var affixesLeft = affixes.ToList();
			foreach (var combination in AffixCombinationsStorage)
			{
				if (combination.Affixes.All(a => affixesLeft.Contains(a)))
				{
					foreach (var affix in combination.Affixes)
						affixesLeft.Remove(affix);
					affixesLeft.Add(combination);
				}
			}

            return affixesLeft
				.OrderByDescending(a => a.GetPositivity(season) - a.GetNegativity(season))
                .ThenBy(a => a.UniqueID)
                .ToList();
        }

        public string GetSeasonName(IReadOnlyList<ISeasonAffix> affixes)
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

        public string GetSeasonDescription(IReadOnlyList<ISeasonAffix> affixes)
            => string.Join("\n", affixes.Select(a => a.LocalizedDescription));

        public void QueueOvernightAffixChoice()
		{
			IsAffixChoiceMenuQueued = true;
			SendModMessageToEveryone(new NetMessage.QueueOvernightAffixChoice());
        }

		public IReadOnlySet<ISeasonAffix> GetAllPossibleAffixesForSeason(OrdinalSeason season)
			=> AllAffixesStorage.Values
				.Where(affix => affix.GetProbabilityWeight(season) > 0)
				.ToHashSet();

		#endregion
	}
}