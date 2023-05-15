using HarmonyLib;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.CommonModCode.GMCM;
using Shockah.Kokoro;
using Shockah.Kokoro.GMCM;
using Shockah.Kokoro.Stardew;
using Shockah.SeasonAffixes.Affixes;
using Shockah.SeasonAffixes.Affixes.Negative;
using Shockah.SeasonAffixes.Affixes.Neutral;
using Shockah.SeasonAffixes.Affixes.Positive;
using Shockah.SeasonAffixes.Affixes.Varying;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using SObject = StardewValley.Object;

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
		private HashSet<ISeasonAffix> ActivePermanentAffixesStorage { get; init; } = new();
		private Dictionary<string, HashSet<ISeasonAffix>> AffixTagDictionary { get; init; } = new();
		private List<Func<IReadOnlySet<ISeasonAffix>, OrdinalSeason, bool?>> AffixConflictInfoProviders { get; init; } = new();
		private List<Func<IReadOnlySet<ISeasonAffix>, OrdinalSeason, double?>> AffixCombinationWeightProviders { get; init; } = new();

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
			helper.ConsoleCommands.Add("affixes_list_active", "Lists all active affixes.", (_, _) =>
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
			helper.ConsoleCommands.Add("affixes_list_seasonal", "Lists all active seasonal affixes.", (_, _) =>
			{
				var affixes = ActiveSeasonalAffixes;
				if (affixes.Count == 0)
				{
					Monitor.Log("There are no active seasonal affixes.", LogLevel.Info);
					return;
				}

				var output = string.Join("\n\n", affixes.Select(a => $"ID: {a.UniqueID}\nName: {a.LocalizedName}\nDescription: {a.LocalizedDescription}"));
				Monitor.Log(output, LogLevel.Info);
			});
			helper.ConsoleCommands.Add("affixes_list_permanent", "Lists all active seasonal affixes.", (_, _) =>
			{
				var affixes = ActivePermanentAffixes;
				if (affixes.Count == 0)
				{
					Monitor.Log("There are no active permanent affixes.", LogLevel.Info);
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
			helper.ConsoleCommands.Add("affixes_clear_history", "Clears affix choice history.", (_, _) =>
			{
				SaveData.AffixChoiceHistory.Clear();
				SaveData.AffixSetChoiceHistory.Clear();
			});

			Harmony = new(ModManifest.UniqueID);
			BillboardPatches.Apply(Harmony);

			Harmony.TryPatch(
				monitor: Monitor,
				original: () =>
				{
					var game1Type = typeof(Game1);
					var compilerType = AccessTools.Inner(game1Type, "<>c");
					if (compilerType is null)
						return null;

					foreach (var method in compilerType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
						if (method.Name.StartsWith("<showEndOfNightStuff>") && method.ReturnType == typeof(void) && method.GetParameters().Select(p => p.ParameterType).SequenceEqual(Array.Empty<Type>()))
							return method;
					return null;
				},
				transpiler: new HarmonyMethod(AccessTools.Method(GetType(), nameof(Game1_showEndOfNightStuff_Transpiler)))
			);
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
				new AccumulationAffix(),
				new BoonsAffix(),
				new CavernsAffix(),
				new CompetitionAffix(),
				new DescentAffix(),
				new FairyTalesAffix(),
				//new FortuneAffix(),
				new HivemindAffix(),
				new LootAffix(),
				new LoveAffix(),
				new MeteoritesAffix(),
				new MudAffix(),
				new TreasuresAffix(),

				// negative affixes
				new CrowsAffix(),
				new DroughtAffix(),
				new HardWaterAffix(),
				new HurricaneAffix(),
				new MediocrityAffix(),
				new ResilienceAffix(),
				new SapAffix(),
				new SilenceAffix(),
				new SlumberAffix(),
				new TenacityAffix(),

				// neutral affixes
				new BurstingAffix(),
				new InflationAffix(),
				new MigrationAffix(),
				new ThunderAffix(),
				new TidesAffix(),
				new WildGrowthAffix(),

				// variantable affixes
				new InnovationAffix(AffixVariant.Positive), new InnovationAffix(AffixVariant.Negative),
				new RegrowthAffix(AffixVariant.Positive), new RegrowthAffix(AffixVariant.Negative),

				// other varying affixes
				new ItemValueAffix(
					"Agriculture",
					new HashSet<string> { VanillaSkill.CropsAspect },
					() => new(Game1.objectSpriteSheet, new(96, 176, 16, 16)),
					i => i.Category is SObject.FruitsCategory or SObject.VegetableCategory,
					() => Config.AgricultureValue,
					value => Config.AgricultureValue = value,
					season => Config.WinterCrops || season.Season != Season.Winter ? 1 : 0.5
				),
				new ItemValueAffix(
					"Ranching",
					new HashSet<string> { VanillaSkill.AnimalsAspect },
					() => new(Game1.objectSpriteSheet, new(256, 272, 16, 16)),
					i => i.isAnimalProduct(),
					() => Config.RanchingValue,
					value => Config.RanchingValue = value
				),
				new ItemValueAffix(
					"Seafood",
					new HashSet<string> { VanillaSkill.FishingAspect, VanillaSkill.TrappingAspect, VanillaSkill.PondsAspect },
					() => new(Game1.objectSpriteSheet, new(96, 128, 16, 16)),
					i => i.Category == SObject.FishCategory || (!i.bigCraftable.Value && i.ParentSheetIndex == 812), // Roe
					() => Config.SeafoodValue,
					value => Config.SeafoodValue = value
				)
			})
				RegisterAffix(affix);

			// conflicts
			RegisterAffixConflictInfoProvider((affixes, _) => affixes.Any(a => a is DroughtAffix) && affixes.Any(a => a is ThunderAffix) ? true : null);
			RegisterAffixConflictInfoProvider((affixes, _) => affixes.Any(a => a is SilenceAffix) && affixes.Any(a => a is LoveAffix) ? true : null);
			RegisterAffixConflictInfoProvider((affixes, _) => affixes.Any(a => a is HardWaterAffix) && affixes.Any(a => a is MudAffix) ? true : null);
			RegisterAffixConflictInfoProvider((affixes, _) => affixes.Any(a => a is SapAffix) && affixes.Any(a => a is AccumulationAffix) ? true : null);
			RegisterAffixConflictInfoProvider((affixes, _) =>
			{
				var varianted = affixes.OfType<BaseVariantedSeasonAffix>().ToList();
				var variantedTypes = varianted.Select(v => v.GetType()).ToHashSet();
				return varianted.Count == variantedTypes.Count ? null : true;
			});

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
			{
				affix.OnActivate();
				AffixActivated?.Invoke(affix);
			}
			Monitor.Log($"Loaded save file. Active affixes:\n{string.Join("\n", SaveData.ActiveAffixes.Select(a => a.UniqueID))}", LogLevel.Info);

			UpdatePermanentlyActiveAffixes();
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
			{
				affix.OnDeactivate();
				AffixDeactivated?.Invoke(affix);
			}
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

					UpdatePermanentlyActiveAffixes();
					WriteConfig();
					LogConfig();
					SetupConfig();
				}
			);

			helper.AddBoolOption("config.incremental", () => Config.Incremental);
			helper.AddNumberOption("config.choices", () => Config.Choices, min: 1, max: 4, interval: 1);
			helper.AddNumberOption("config.affixRepeatPeriod", () => Config.AffixRepeatPeriod, min: 0);
			helper.AddNumberOption("config.affixSetRepeatPeriod", () => Config.AffixSetRepeatPeriod, min: 0);
			helper.AddBoolOption("config.winterCrops", () => Config.WinterCrops);

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

				api.AddBoolOption(
					ModManifest,
					getValue: () => Config.PermanentAffixes.Contains(affix.UniqueID),
					setValue: value =>
					{
						if (value)
							Config.PermanentAffixes.Add(affix.UniqueID);
						else
							Config.PermanentAffixes.Remove(affix.UniqueID);
					},
					name: () => Helper.Translation.Get("config.affix.permanent.name"),
					tooltip: () => Helper.Translation.Get("config.affix.permanent.tooltip")
				);

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

		private void UpdatePermanentlyActiveAffixes()
		{
			var newAffixes = AllAffixesStorage.Where(kvp => Config.PermanentAffixes.Contains(kvp.Key)).Select(kvp => kvp.Value).ToHashSet();
			var toDeactivate = ActivePermanentAffixesStorage.Where(a => !newAffixes.Contains(a)).ToList();
			var toActivate = newAffixes.Where(a => !ActivePermanentAffixesStorage.Contains(a)).ToList();

			foreach (var affix in toDeactivate)
			{
				affix.OnDeactivate();
				ActivePermanentAffixesStorage.Remove(affix);
				AffixDeactivated?.Invoke(affix);
				Monitor.Log($"Activated (permanent) affix `{affix.UniqueID}`.", LogLevel.Info);
			}
			foreach (var affix in toActivate)
			{
				ActivePermanentAffixesStorage.Add(affix);
				affix.OnActivate();
				AffixActivated?.Invoke(affix);
				Monitor.Log($"Deactivated (permanent) affix `{affix.UniqueID}`.", LogLevel.Info);
			}
		}

		private void LocalActivateAffix(ISeasonAffix affix)
		{
			if (SaveData.ActiveAffixes.Contains(affix))
				return;
			SaveData.ActiveAffixes.Add(affix);
			affix.OnActivate();
			AffixActivated?.Invoke(affix);
			Monitor.Log($"Activated affix `{affix.UniqueID}`.", LogLevel.Info);
		}

		private void LocalDeactivateAffix(ISeasonAffix affix)
		{
			if (!SaveData.ActiveAffixes.Contains(affix))
				return;
			affix.OnDeactivate();
			SaveData.ActiveAffixes.Remove(affix);
			AffixDeactivated?.Invoke(affix);
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

		private static IAffixScoreProvider CreateAffixScoreProvider()
			=> new DefaultAffixScoreProvider()
			.Caching();

		private static IAffixProbabilityWeightProvider CreateAffixProbabilityWeightProvider()
			=> new DefaultAffixProbabilityWeightProvider()
			.Caching();

		private IAffixesProvider CreateAffixesProvider(IAffixScoreProvider scoreProvider, IAffixProbabilityWeightProvider probabilityWeightProvider, OrdinalSeason season)
			=> new AffixesProvider(AllAffixesStorage.Values.Where(a => !Config.AffixWeights.TryGetValue(a.UniqueID, out var weight) || weight > 0))
			.Excluding(ActivePermanentAffixesStorage)
			.ApplicableToSeason(probabilityWeightProvider, season)
			.Effective(scoreProvider, season);

		private IAffixTagPairCandidateProvider CreateAffixTagPairCandidateProvider()
			=> new FunctionAffixTagPairCandidateProvider(GetTagPairCandidatesForAffix)
			.Caching();

		private IAffixSetWeightProvider CreateNonPairingUpAffixSetWeightProvider(IAffixProbabilityWeightProvider probabilityWeightProvider)
			=> new DefaultProbabilityAffixSetWeightProvider(probabilityWeightProvider)
			.MultiplyingBy(new ConfigAffixSetWeightProvider((IReadOnlyDictionary<string, double>)Config.AffixWeights))
			.MultiplyingBy(new CustomAffixSetWeightProvider(AffixCombinationWeightProviders));

		private IAffixSetWeightProvider CreateAffixSetWeightProvider(IAffixScoreProvider scoreProvider, IAffixProbabilityWeightProvider probabilityWeightProvider)
			=> CreateNonPairingUpAffixSetWeightProvider(probabilityWeightProvider)
			.MultiplyingBy(new PairingUpTagsAffixSetWeightProvider(scoreProvider, CreateAffixTagPairCandidateProvider(), c => c < 3 ? 1 : Math.Pow(0.5, c - 3 + 1), 0.25, 3, 0.5))
			.MultiplyingBy(new DelegateAffixSetWeightProvider((affixes, _) => affixes.Count >= 4 ? 0.5 : 1.0));

		private IAffixSetWeightProvider CreateSaveSpecificAffixSetWeightProvider(IAffixScoreProvider scoreProvider, IAffixProbabilityWeightProvider probabilityWeightProvider)
			=> CreateAffixSetWeightProvider(scoreProvider, probabilityWeightProvider)
			.MultiplyingBy(new AvoidingChoiceHistoryDuplicatesAffixSetWeightProvider(0.5))
			.MultiplyingBy(new AvoidingSetChoiceHistoryDuplicatesAffixSetWeightProvider(0.1));

		private IAffixSetGenerator CreateAffixSetGenerator(IAffixesProvider affixesProvider, IAffixScoreProvider scoreProvider, IAffixSetWeightProvider affixSetWeightProvider, ModConfig.AffixSetEntry affixSetEntry, Random random)
		{
			var otherAffixes = ActivePermanentAffixesStorage;
			if (Config.Incremental)
				otherAffixes = otherAffixes.Union(SaveData.ActiveAffixes).ToHashSet();

			return new AllCombinationsAffixSetGenerator(affixesProvider, scoreProvider, AffixConflictInfoProviders, otherAffixes, affixSetEntry.Positive, affixSetEntry.Negative, 4)
				.Benchmarking(Monitor, "AllCombinations")
				.WeightedRandom(random, affixSetWeightProvider)
				.Benchmarking(Monitor, "WeightedRandom")
				.AvoidingDuplicatesBetweenChoices()
				.Benchmarking(Monitor, "AvoidingDuplicates");
		}

		private static IEnumerable<CodeInstruction> Game1_showEndOfNightStuff_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				return new SequenceBlockMatcher<CodeInstruction>(instructions)
					.Find(
						ILMatches.Ldstr("newRecord"),
						ILMatches.Call("playSound")
					)
					.PointerMatcher(SequenceMatcherRelativeElement.AfterLast)
					.ExtractLabels(out var labels)
					.Insert(
						SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.JustInsertion,
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SeasonAffixes), nameof(Game1_showEndOfNightStuff_Transpiler_Event))).WithLabels(labels)
					)
					.AllElements();
			}
			catch (Exception ex)
			{
				Instance.Monitor.Log($"Could not patch methods - {Instance.ModManifest.Name} probably won't work.\nReason: {ex}", LogLevel.Error);
				return instructions;
			}
		}

		public static void Game1_showEndOfNightStuff_Transpiler_Event()
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

				var affixScoreProvider = CreateAffixScoreProvider();
				var affixProbabiliyWeightProvider = CreateAffixProbabilityWeightProvider();
				var affixesProvider = Instance.CreateAffixesProvider(affixScoreProvider, affixProbabiliyWeightProvider, season);
				var affixSetWeightProvider = Instance.CreateSaveSpecificAffixSetWeightProvider(affixScoreProvider, affixProbabiliyWeightProvider);
				var affixSetGenerator = Instance.CreateAffixSetGenerator(affixesProvider, affixScoreProvider, affixSetWeightProvider, affixSetEntry, random);

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

			List<IClickableMenu> newMenus = new();
			newMenus.AddRange(Game1.endOfNightMenus.Where(m => m is SaveGameMenu or ShippingMenu));
			newMenus.Add(new AffixChoiceMenu());
			newMenus.AddRange(Game1.endOfNightMenus.Where(m => !newMenus.Contains(m)));
			Game1.endOfNightMenus = new Stack<IClickableMenu>(newMenus);

			Instance.PlayerChoices.Clear();
		}

		#region API

		public event Action<ISeasonAffix>? AffixRegistered;
		public event Action<ISeasonAffix>? AffixUnregistered;
		public event Action<ISeasonAffix>? AffixActivated;
		public event Action<ISeasonAffix>? AffixDeactivated;

		public IReadOnlyDictionary<string, ISeasonAffix> AllAffixes => AllAffixesStorage.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		public IReadOnlySet<ISeasonAffix> ActiveAffixes => SaveData.ActiveAffixes.Union(ActivePermanentAffixesStorage).ToHashSet();
		public IReadOnlySet<ISeasonAffix> ActiveSeasonalAffixes => SaveData.ActiveAffixes.ToHashSet();
		public IReadOnlySet<ISeasonAffix> ActivePermanentAffixes => SaveData.ActiveAffixes.ToHashSet();

		public ISeasonAffix? GetAffix(string uniqueID)
			=> AllAffixesStorage.TryGetValue(uniqueID, out var affix) ? affix : null;

		public bool IsAffixActive(string uniqueID)
			=> SaveData.ActiveAffixes.Any(a => a.UniqueID == uniqueID) || ActivePermanentAffixesStorage.Any(a => a.UniqueID == uniqueID);

		public bool IsAffixActive(Func<ISeasonAffix, bool> predicate)
			=> SaveData.ActiveAffixes.Any(predicate) || ActivePermanentAffixesStorage.Any(predicate);

		public void RegisterAffix(ISeasonAffix affix)
		{
			if (AllAffixesStorage.ContainsKey(affix.UniqueID))
				throw new ArgumentException($"An affix with ID `{affix.UniqueID}` is already registered.");

			AllAffixesStorage[affix.UniqueID] = affix;
			foreach (var tag in affix.Tags)
			{
				if (!AffixTagDictionary.TryGetValue(tag, out var tagAffixes))
				{
					tagAffixes = new();
					AffixTagDictionary[tag] = tagAffixes;
				}
				tagAffixes.Add(affix);
			}

			affix.OnRegister();

			AffixRegistered?.Invoke(affix);
			if (IsConfigRegistered)
				SetupConfig();
		}

		public void UnregisterAffix(ISeasonAffix affix)
		{
			if (!AllAffixesStorage.ContainsKey(affix.UniqueID))
				return;
			DeactivateAffix(affix);

			affix.OnUnregister();

			foreach (var (_, tagAffixes) in AffixTagDictionary)
				tagAffixes.Remove(affix);
			AllAffixesStorage.Remove(affix.UniqueID);

			AffixUnregistered?.Invoke(affix);
			if (IsConfigRegistered)
				SetupConfig();
		}

		public void RegisterAffixConflictInfoProvider(Func<IReadOnlySet<ISeasonAffix>, OrdinalSeason, bool?> provider)
			=> AffixConflictInfoProviders.Insert(0, provider);

		public void UnregisterAffixConflictInfoProvider(Func<IReadOnlySet<ISeasonAffix>, OrdinalSeason, bool?> provider)
			=> AffixConflictInfoProviders.Remove(provider);

		public void RegisterAffixCombinationWeightProvider(Func<IReadOnlySet<ISeasonAffix>, OrdinalSeason, double?> provider)
			=> AffixCombinationWeightProviders.Insert(0, provider);

		public void UnregisterAffixCombinationWeightProvider(Func<IReadOnlySet<ISeasonAffix>, OrdinalSeason, double?> provider)
			=> AffixCombinationWeightProviders.Remove(provider);

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
			var affixScoreProvider = CreateAffixScoreProvider();
			var affixList = affixes.ToList();
			var relatedAffixDictionary = affixList.ToDictionary(a => a, a => affixList.Where(a2 => a2.Tags.Any(t => a.Tags.Contains(t))).ToHashSet());

			var results = new List<ISeasonAffix>();

			while (relatedAffixDictionary.Count != 0)
			{
				var (affix, relatedAffixes) = relatedAffixDictionary
					.OrderByDescending(kvp => kvp.Value.Count)
					.ThenByDescending(kvp => affixScoreProvider.GetPositivity(kvp.Key, season) - affixScoreProvider.GetNegativity(kvp.Key, season))
					.ThenBy(kvp => kvp.Key.UniqueID)
					.First();
				if (relatedAffixes.Count > 0)
				{
					results.AddRange(
						relatedAffixes
							.Where(a => relatedAffixDictionary.ContainsKey(a))
							.OrderBy(a => a.Tags.Count)
							.ThenByDescending(a => affixScoreProvider.GetPositivity(a, season) - affixScoreProvider.GetNegativity(a, season))
							.ThenBy(a => a.UniqueID)
					);
					foreach (var relatedAffix in relatedAffixes)
						relatedAffixDictionary.Remove(relatedAffix);
				}
				else
				{
					results.Add(affix);
					relatedAffixDictionary.Remove(affix);
				}
			}

			return results;
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
		{
			var affixProbabiliyWeightProvider = CreateAffixProbabilityWeightProvider();
			return AllAffixesStorage.Values
				.Where(affix => affixProbabiliyWeightProvider.GetProbabilityWeight(affix, season) > 0)
				.ToHashSet();
		}

		public IReadOnlySet<ISeasonAffix> GetTagPairCandidatesForAffix(ISeasonAffix affix, OrdinalSeason season)
		{
			if (affix.Tags.Count == 0)
				return new HashSet<ISeasonAffix>();
			HashSet<ISeasonAffix> workingSet = new() { affix };
			return affix.Tags
				.SelectMany(tag => AffixTagDictionary.TryGetValue(tag, out var tagAffixes) ? (IEnumerable<ISeasonAffix>)tagAffixes : Array.Empty<ISeasonAffix>())
				.Distinct()
				.Where(a =>
				{
					workingSet.Add(a);
					foreach (var provider in AffixConflictInfoProviders)
					{
						var result = provider(workingSet, season);
						if (result is not null)
						{
							workingSet.Remove(a);
							return !result.Value;
						}
					}
					workingSet.Remove(a);
					return true;
				})
				.ToHashSet();
		}

		#endregion
	}
}