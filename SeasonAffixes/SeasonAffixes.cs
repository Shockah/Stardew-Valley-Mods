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

namespace Shockah.SeasonAffixes
{
	public class SeasonAffixes : BaseMod<ModConfig>, ISeasonAffixesApi
	{
		public static SeasonAffixes Instance { get; private set; } = null!;
		internal Harmony Harmony { get; private set; } = null!;

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

		private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
		{
			// positive affixes
			foreach (var affix in new List<ISeasonAffix>()
			{
				// positive affixes
				new AgricultureAffix(),
				new TreasuresAffix(),
				new DescentAffix(),
				new FairyTalesAffix(),
				new FortuneAffix(),
				new InnovationAffix(),
				new LoveAffix(),
				new MudAffix(),
				new RanchingAffix(),

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
			var toDeactivate = SaveData.ActiveAffixes.Where(a => !affixes.Contains(a)).ToList();
			var toActivate = affixes.Where(a => !SaveData.ActiveAffixes.Contains(a)).ToList();

			foreach (var affix in toDeactivate)
			{
				affix.OnDeactivate();
				SaveData.ActiveAffixes.Remove(affix);
			}
			foreach (var affix in toActivate)
			{
				SaveData.ActiveAffixes.Add(affix);
				affix.OnActivate();
			}

			if (toDeactivate.Count == 0 && toActivate.Count == 0)
			{
				Monitor.Log("Received affix update. No changes.", LogLevel.Info);
				return;
			}

			var outputToDeactivate = string.Join("\n", toDeactivate.Select(a => $"- {a.UniqueID}"));
			var outputToActivate = string.Join("\n", toActivate.Select(a => $"+ {a.UniqueID}"));
			Monitor.Log($"Received affix update. Changed affixes:\n{outputToDeactivate}\n{outputToActivate}", LogLevel.Info);
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

					var toDeactivate = SaveData.ActiveAffixes.Where(a => !newAffixes.Contains(a)).ToList();
					var toActivate = newAffixes.Where(a => !SaveData.ActiveAffixes.Contains(a)).ToList();

					foreach (var affix in toDeactivate)
					{
						affix.OnDeactivate();
						SaveData.ActiveAffixes.Remove(affix);
					}
					foreach (var affix in toActivate)
					{
						SaveData.ActiveAffixes.Add(affix);
						affix.OnActivate();
					}

					if (toDeactivate.Count == 0 && toActivate.Count == 0)
					{
						Monitor.Log("Updating affixes. No changes.", LogLevel.Info);
					}
					else
					{
						var outputToDeactivate = string.Join("\n", toDeactivate.Select(a => $"- {a.UniqueID}"));
						var outputToActivate = string.Join("\n", toActivate.Select(a => $"+ {a.UniqueID}"));
						Monitor.Log($"Updating affixes. Changed affixes:\n{outputToDeactivate}\n{outputToActivate}", LogLevel.Info);
					}

					SendModMessageToEveryone(new NetMessage.UpdateActiveAffixes(SaveData.ActiveAffixes.Select(a => a.UniqueID).ToHashSet()));
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

        private static void Game1_showEndOfNightStuff_Prefix()
		{
			if (!Instance.IsAffixChoiceMenuQueued)
				return;
			Instance.IsAffixChoiceMenuQueued = false;

			if (Game1.endOfNightMenus.Count == 0)
				Game1.endOfNightMenus.Push(new SaveGameMenu());

			if (Context.IsMainPlayer)
			{
                var tomorrow = Game1.Date.GetByAddingDays(1);
                OrdinalSeason season = new(tomorrow.Year, tomorrow.GetSeason());

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
					new AffixesProvider(Instance.AllAffixesStorage.Values),
					new AffixesProvider(Instance.AffixCombinationsStorage)
				).ApplicableToSeason(season);

				var affixSetGenerator = new AllCombinationsAffixSetGenerator(affixesProvider, affixSetEntry.Positive, affixSetEntry.Negative)
					.MaxAffixes(3)
					.NonConflicting(Instance.AffixConflictProviders)
					.NonConflictingWithCombinations()
					.WeightedRandom(random)
					.Decombined()
					.AvoidingChoiceHistoryDuplicates()
					.AvoidingSetChoiceHistoryDuplicates()
					//.AsLittleAsPossible()
					.AvoidingDuplicatesBetweenChoices();

				var choices = affixSetGenerator.Generate(season).Take(Instance.Config.Choices).ToList();
				Instance.SaveData.AffixChoiceHistory.Add(choices.SelectMany(set => set).ToHashSet());
				Instance.SaveData.AffixSetChoiceHistory.Add(choices.Select(set => (ISet<ISeasonAffix>)set.ToHashSet()).ToHashSet());

                Instance.AffixChoiceMenuConfig = new(new(tomorrow.Year, tomorrow.GetSeason()), Instance.Config.Incremental, choices, Instance.Config.RerollsPerSeason);
                Instance.SendModMessageToEveryone(new NetMessage.UpdateAffixChoiceMenuConfig(
					season,
					Instance.Config.Incremental,
					choices.Select(choice => choice.Select(a => a.UniqueID).ToHashSet()).ToList(),
					Instance.Config.RerollsPerSeason
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
		}

		public void RegisterVisualAffixCombination(IReadOnlySet<ISeasonAffix> affixes, Func<string> localizedName, Func<string> localizedDescription, Func<TextureRectangle> icon)
			=> RegisterAffixCombination(affixes, localizedName, localizedDescription, icon, _ => 0);

		public void RegisterAffixCombination(IReadOnlySet<ISeasonAffix> affixes, Func<string> localizedName, Func<string> localizedDescription, Func<TextureRectangle> icon, Func<OrdinalSeason, double>? probabilityWeightProvider = null)
		{
			UnregisterAffixCombination(affixes);
			AffixCombinationsStorage.Add(new(affixes, localizedName, localizedDescription, icon, probabilityWeightProvider));
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

		public void UnregisterAffix(ISeasonAffix affix)
		{
			if (!AllAffixesStorage.ContainsKey(affix.UniqueID))
				return;
			DeactivateAffix(affix);
			affix.OnUnregister();
			AllAffixesStorage.Remove(affix.UniqueID);
		}

		public void ActivateAffix(ISeasonAffix affix)
		{
			if (SaveData.ActiveAffixes.Contains(affix))
				return;
			SaveData.ActiveAffixes.Add(affix);
			affix.OnActivate();
			Monitor.Log($"Activated affix `{affix.UniqueID}`.", LogLevel.Info);
            SendModMessageToEveryone(new NetMessage.UpdateActiveAffixes(ActiveAffixes.Select(a => a.UniqueID).ToHashSet()));
		}

		public void DeactivateAffix(ISeasonAffix affix)
		{
			if (!SaveData.ActiveAffixes.Contains(affix))
				return;
			affix.OnDeactivate();
            SaveData.ActiveAffixes.Remove(affix);
			Monitor.Log($"Deactivated affix `{affix.UniqueID}`.", LogLevel.Info);
			SendModMessageToEveryone(new NetMessage.UpdateActiveAffixes(ActiveAffixes.Select(a => a.UniqueID).ToHashSet()));
		}

		public void DeactivateAllAffixes()
		{
			foreach (var affix in SaveData.ActiveAffixes)
				affix.OnDeactivate();
			SaveData.ActiveAffixes.Clear();
			Monitor.Log("Deactivated all affixes.", LogLevel.Info);
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