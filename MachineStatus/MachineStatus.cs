using HarmonyLib;
using Shockah.CommonModCode;
using Shockah.CommonModCode.GMCM;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using SObject = StardewValley.Object;

namespace Shockah.MachineStatus
{
	public class MachineStatus: Mod
	{
		private readonly struct MachineState
		{
			public readonly bool ReadyForHarvest;
			public readonly int MinutesUntilReady;

			public MachineState(bool readyForHarvest, int minutesUntilReady)
			{
				this.ReadyForHarvest = readyForHarvest;
				this.MinutesUntilReady = minutesUntilReady;
			}
		}

		private readonly (string titleKey, (int machineId, string machineName)[] machineNames)[] KnownMachineNames = new[]
		{
			("config.machine.category.artisan", new (int machineId, string machineName)[]
			{
				(10, "Bee House"),
				(163, "Cask"),
				(16, "Cheese Press"),
				(12, "Keg"),
				(17, "Loom"),
				(24, "Mayonnaise Machine"),
				(19, "Oil Maker"),
				(15, "Preserves Jar")
			}),
			("config.machine.category.refining", new (int machineId, string machineName)[]
			{
				(90, "Bone Mill"),
				(114, "Charcoal Kiln"),
				(21, "Crystalarium"),
				(13, "Furnace"),
				(182, "Geode Crusher"),
				(264, "Heavy Tapper"),
				(9, "Lightning Rod"),
				(20, "Recycling Machine"),
				(25, "Seed Maker"),
				(158, "Slime Egg-Press"),
				(231, "Solar Panel"),
				(105, "Tapper"),
				(211, "Wood Chipper"),
				(154, "Worm Bin")
			}),
			("config.machine.category.misc", new (int machineId, string machineName)[]
			{
				(246, "Coffee Maker"),
				(265, "Deconstructor"),
				(128, "Mushroom Box"),
				(254, "Ostrich Incubator"),
				(156, "Slime Incubator"),
				(127, "Statue of Endless Fortune"),
				(160, "Statue of Perfection"),
				(280, "Statue of True Perfection")
			}),
		};

		internal static MachineStatus Instance { get; set; } = null!;
		internal ModConfig Config { get; private set; } = null!;

		private readonly IList<(GameLocation location, SObject machine, Item? output)> DisplayedMachines = new List<(GameLocation location, SObject machine, Item? output)>();
		private readonly IDictionary<string, Regex> RegexCache = new Dictionary<string, Regex>();

		public override void Entry(IModHelper helper)
		{
			Instance = this;
			Config = helper.ReadConfig<ModConfig>();

			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
			helper.Events.World.ObjectListChanged += OnObjectListChanged;
			helper.Events.Display.RenderedHud += OnRenderedHud;

			var harmony = new Harmony(ModManifest.UniqueID);
			try
			{
				harmony.Patch(
					original: AccessTools.Method(typeof(SObject), nameof(SObject.checkForAction)),
					prefix: new HarmonyMethod(typeof(MachineStatus), nameof(Object_checkForAction_Prefix)),
					postfix: new HarmonyMethod(typeof(MachineStatus), nameof(Object_checkForAction_Postfix))
				);
				harmony.Patch(
					original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.passTimeForObjects)),
					postfix: new HarmonyMethod(typeof(MachineStatus), nameof(GameLocation_passTimeForObjects_Postfix))
				);
				harmony.Patch(
					original: AccessTools.Method(typeof(SObject), nameof(SObject.onReadyForHarvest)),
					postfix: new HarmonyMethod(typeof(MachineStatus), nameof(Object_onReadyForHarvest_Postfix))
				);
			}
			catch (Exception e)
			{
				Monitor.Log($"Could not patch methods - Machine Status probably won't work.\nReason: {e}", LogLevel.Error);
			}
		}

		private void SetupConfig()
		{
			var api = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
			if (api is null)
				return;
			GMCMI18nHelper helper = new(api, ModManifest, Helper.Translation);

			api.Register(
				ModManifest,
				reset: () => Config = new ModConfig(),
				save: () =>
				{
					Helper.WriteConfig(Config);
					ForceRefreshDisplayedMachines();
				}
			);

			string BuiltInMachineSyntax(string machineName)
				=> $"*|{machineName}";

			void SetupExceptionsPage(string typeKey, IList<string> exceptions)
			{
				helper.AddPage(typeKey, typeKey);

				helper.AddTextOption(
					keyPrefix: "config.exceptions.manual",
					getValue: () => exceptions.Where(ex => !KnownMachineNames.Any(section => section.machineNames.Any(machine => BuiltInMachineSyntax(machine.machineName) == ex))).Join(delimiter: ", "),
					setValue: value =>
					{
						var vanillaValues = exceptions.Where(ex => KnownMachineNames.Any(section => section.machineNames.Any(machineName => $"*|{machineName}" == ex)));
						var customValues = value.Split(',').Select(s => s.Trim());
						exceptions.Clear();
						foreach (var vanillaValue in vanillaValues)
							exceptions.Add(vanillaValue);
						foreach (var customValue in customValues)
							if (!exceptions.Contains(customValue))
								exceptions.Add(customValue);
					}
				);

				foreach (var (titleKey, machines) in KnownMachineNames)
				{
					helper.AddSectionTitle(titleKey);
					foreach (var (machineId, machineName) in machines)
					{
						var machineKey = BuiltInMachineSyntax(machineName);
						var localizedMachineName = machineName;
						if (Game1.bigCraftablesInformation.TryGetValue(machineId, out string? info))
							localizedMachineName = info.Split('/')[0];

						api!.AddBoolOption(
							mod: ModManifest,
							name: () => localizedMachineName,
							getValue: () => exceptions.Contains(machineKey),
							setValue: value =>
							{
								exceptions.Remove(machineKey);
								if (value)
									exceptions.Add(machineKey);
							}
						);
					}
				}
			}

			void SetupStateConfig(string optionKey, string pageKey, Expression<Func<bool>> boolProperty)
			{
				helper.AddBoolOption(optionKey, boolProperty);
				helper.AddPageLink(pageKey, "config.exceptions");
			}

			helper.AddSectionTitle("config.appearance.section");
			helper.AddEnumOption("config.anchor.screen", valuePrefix: "config.anchor", property: () => Config.ScreenAnchorSide);
			helper.AddEnumOption("config.anchor.panel", valuePrefix: "config.anchor", property: () => Config.PanelAnchorSide);
			helper.AddNumberOption("config.anchor.inset", () => Config.AnchorInset);
			helper.AddNumberOption("config.anchor.x", () => Config.AnchorOffsetX);
			helper.AddNumberOption("config.anchor.y", () => Config.AnchorOffsetY);

			helper.AddEnumOption("config.appearance.flowDirection", valuePrefix: "config.flowDirection", property: () => Config.FlowDirection);
			helper.AddNumberOption("config.appearance.scale", () => Config.Scale, min: 0f, max: 12f, interval: 0.05f);
			helper.AddNumberOption("config.appearance.spacing", () => Config.Spacing, min: -4f, max: 48f, interval: 0.25f);
			helper.AddNumberOption("config.appearance.maxColumns", () => Config.MaxColumns, min: 0, max: 20);

			helper.AddSectionTitle("config.groupingSorting.section");
			helper.AddEnumOption("config.groupingSorting.grouping", () => Config.Grouping);
			for (int i = 0; i < 4; i++)
			{
				int loopI = i;
				helper.AddEnumOption(
					"config.groupingSorting.sorting",
					valuePrefix: "config.sorting",
					tokens: new { Ordinal = loopI + 1 },
					getValue: () => loopI < Config.Sorting.Count ? Config.Sorting[loopI] : MachineRenderingOptions.Sorting.None,
					setValue: value =>
					{
						while (loopI >= Config.Sorting.Count)
							Config.Sorting.Add(MachineRenderingOptions.Sorting.None);
						Config.Sorting[loopI] = value;
					}
				);
			}

			helper.AddSectionTitle("config.show.section");
			SetupStateConfig("config.show.ready", "config.show.ready.exceptions", () => Config.ShowReady);
			SetupStateConfig("config.show.waiting", "config.show.waiting.exceptions", () => Config.ShowWaiting);
			SetupStateConfig("config.show.busy", "config.show.busy.exceptions", () => Config.ShowBusy);
			SetupExceptionsPage("config.show.ready.exceptions", Config.ShowReadyExceptions);
			SetupExceptionsPage("config.show.waiting.exceptions", Config.ShowWaitingExceptions);
			SetupExceptionsPage("config.show.busy.exceptions", Config.ShowBusyExceptions);
		}

		private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
		{
			SetupConfig();
		}

		private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
		{
			ForceRefreshDisplayedMachines();
		}

		private void OnObjectListChanged(object? sender, ObjectListChangedEventArgs e)
		{
			foreach (var @object in e.Removed)
				HideMachine(e.Location, @object.Value);
			foreach (var @object in e.Added)
				UpdateMachineState(e.Location, @object.Value);
		}

		private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
		{
		}

		private void ForceRefreshDisplayedMachines()
		{
			DisplayedMachines.Clear();
			if (!Context.IsPlayerFree)
				return;
			foreach (var location in Game1.locations)
				foreach (var @object in location.Objects.Values)
					UpdateMachineState(location, @object);
		}

		private bool MachineMatches(SObject machine, IList<string> list)
		{
			static bool RegexMatches(Regex regex, SObject machine)
			{
				if (regex.IsMatch(machine.Name) || regex.IsMatch(machine.DisplayName))
					return true;
				return false;
			}
			
			foreach (string entry in list)
			{
				if (RegexCache.TryGetValue(entry, out var regex))
				{
					if (RegexMatches(regex, machine))
						return true;
				}
				else if (entry.Contains('*') || entry.Contains('?') || entry.Contains('|'))
				{
					string pattern = "";
					foreach (string part in Regex.Split(entry, "(\\*+|[\\?\\|])"))
					{
						if (part.Length == 0)
							continue;
						pattern += part[0] switch
						{
							'*' => ".*",
							'?' => "\\w+",
							'|' => "\\b",
							_ => Regex.Escape(part),
						};
					}
					regex = new Regex($"^{pattern}$");
					RegexCache[entry] = regex;
					if (RegexMatches(regex, machine))
						return true;
				}
				else
				{
					if (machine.Name.Trim().Equals(entry, StringComparison.InvariantCultureIgnoreCase))
						return true;
					if (machine.DisplayName.Trim().Equals(entry, StringComparison.InvariantCultureIgnoreCase))
						return true;
				}
			}
			return false;
		}

		private bool IsMachine(GameLocation location, SObject @object)
		{
			if (@object.IsSprinkler())
				return false;
			return true;
		}

		private void SetMachineVisible(bool visible, GameLocation location, SObject machine, SObject? output = null)
		{
			if (visible)
				ShowMachine(location, machine, output);
			else
				HideMachine(location, machine);
		}

		private void ShowMachine(GameLocation location, SObject machine, SObject? output = null)
		{
			HideMachine(location, machine);
			DisplayedMachines.Add((location, machine, output ?? machine.heldObject.Value?.getOne()));
		}

		private void HideMachine(GameLocation location, SObject machine)
		{
			var existingEntry = DisplayedMachines.FirstOrNull(e => e.location == location && e.machine == machine);
			if (existingEntry is not null)
				DisplayedMachines.Remove(existingEntry.Value);
		}

		private void UpdateMachineState(GameLocation location, SObject machine, SObject? output = null)
		{
			if (!IsMachine(location, machine))
				return;

			if (machine.readyForHarvest.Value)
				SetMachineReadyForHarvest(location, machine, output ?? machine.heldObject.Value);
			else if (machine.MinutesUntilReady > 0)
				SetMachineBusy(location, machine);
			else
				SetMachineWaitingForInput(location, machine);
		}

		private void SetMachineReadyForHarvest(GameLocation location, SObject machine, SObject? output = null)
		{
			bool shouldShow = Config.ShowReady != MachineMatches(machine, Config.ShowReadyExceptions);
			SetMachineVisible(shouldShow, location, machine, output);
		}

		private void SetMachineWaitingForInput(GameLocation location, SObject machine)
		{
			bool shouldShow = Config.ShowWaiting != MachineMatches(machine, Config.ShowWaitingExceptions);
			SetMachineVisible(shouldShow, location, machine, null);
		}

		private void SetMachineBusy(GameLocation location, SObject machine)
		{
			bool shouldShow = Config.ShowBusy != MachineMatches(machine, Config.ShowBusyExceptions);
			SetMachineVisible(shouldShow, location, machine, null);
		}

		private static void Object_checkForAction_Prefix(SObject __instance, ref MachineState __state)
		{
			__state = new MachineState(__instance.readyForHarvest.Value, __instance.MinutesUntilReady);
		}

		private static void Object_checkForAction_Postfix(SObject __instance, ref MachineState __state, Farmer who)
		{
			if (__instance.readyForHarvest.Value == __state.ReadyForHarvest && __instance.MinutesUntilReady == __state.MinutesUntilReady)
				return;
			Instance.UpdateMachineState(who.currentLocation, __instance);
		}

		private static void GameLocation_passTimeForObjects_Postfix(GameLocation __instance)
		{
			foreach (var @object in __instance.Objects.Values)
				Instance.UpdateMachineState(__instance, @object);
		}

		private static void Object_onReadyForHarvest_Postfix(SObject __instance, GameLocation environment)
		{
			Instance.UpdateMachineState(environment, __instance);
		}
	}
}