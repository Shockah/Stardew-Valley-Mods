using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shockah.CommonModCode;
using Shockah.CommonModCode.GMCM;
using Shockah.CommonModCode.UI;
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

		private static readonly ItemRenderer ItemRenderer = new();
		private static readonly Vector2 DigitSize = new(5, 7);
		private static Vector2 SingleMachineSize => new(64, 64);
		private static readonly (string titleKey, (int machineId, string machineName)[] machineNames)[] KnownMachineNames = new[]
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

		private readonly IList<(GameLocation location, SObject machine)> DisplayedMachines = new List<(GameLocation location, SObject machine)>();
		private readonly IDictionary<string, Regex> RegexCache = new Dictionary<string, Regex>();
		private MachineRenderingOptions.Visibility Visibility = MachineRenderingOptions.Visibility.Normal;
		private float VisibilityAlpha = 1f;
		private bool IsHoveredOver = false;

		public override void Entry(IModHelper helper)
		{
			Instance = this;
			Config = helper.ReadConfig<ModConfig>();

			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
			helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
			helper.Events.World.ObjectListChanged += OnObjectListChanged;
			helper.Events.Input.ButtonPressed += OnButtonPressed;
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
					var newSortingOptions = Config.Sorting.Where(o => o != MachineRenderingOptions.Sorting.None).ToList();
					Config.Sorting.Clear();
					foreach (var sortingOption in newSortingOptions)
						Config.Sorting.Add(sortingOption);
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

			helper.AddSectionTitle("config.layout.section");
			helper.AddEnumOption("config.anchor.screen", valuePrefix: "config.anchor", property: () => Config.ScreenAnchorSide);
			helper.AddEnumOption("config.anchor.panel", valuePrefix: "config.anchor", property: () => Config.PanelAnchorSide);
			helper.AddNumberOption("config.anchor.inset", () => Config.AnchorInset);
			helper.AddNumberOption("config.anchor.x", () => Config.AnchorOffsetX);
			helper.AddNumberOption("config.anchor.y", () => Config.AnchorOffsetY);
			helper.AddEnumOption("config.layout.flowDirection", valuePrefix: "config.flowDirection", property: () => Config.FlowDirection);
			helper.AddNumberOption("config.layout.scale", () => Config.Scale, min: 0f, max: 12f, interval: 0.05f);
			helper.AddNumberOption("config.layout.xSpacing", () => Config.XSpacing, min: -16f, max: 64f, interval: 0.5f);
			helper.AddNumberOption("config.layout.ySpacing", () => Config.YSpacing, min: -16f, max: 64f, interval: 0.5f);
			helper.AddNumberOption("config.layout.maxColumns", () => Config.MaxColumns, min: 0, max: 20);

			helper.AddSectionTitle("config.bubble.section");
			helper.AddBoolOption("config.bubble.showItem", () => Config.ShowItemBubble);
			helper.AddNumberOption("config.bubble.itemCycleTime", () => Config.BubbleItemCycleTime, min: 0.2f, max: 5f, interval: 0.1f);
			helper.AddEnumOption("config.bubble.sway", () => Config.BubbleSway);

			helper.AddSectionTitle("config.appearance.section");
			helper.AddKeybindList("config.appearance.visibilityKeybind", () => Config.VisibilityKeybind);
			helper.AddNumberOption("config.appearance.alpha.focused", () => Config.FocusedAlpha, min: 0f, max: 1f, interval: 0.05f);
			helper.AddNumberOption("config.appearance.alpha.normal", () => Config.NormalAlpha, min: 0f, max: 1f, interval: 0.05f);

			helper.AddSectionTitle("config.groupingSorting.section");
			helper.AddEnumOption("config.groupingSorting.grouping", () => Config.Grouping);
			for (int i = 0; i < Math.Max(Config.Sorting.Count + 1, 3); i++)
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
						while (Config.Sorting.Count > 0 && Config.Sorting.Last() == MachineRenderingOptions.Sorting.None)
							Config.Sorting.RemoveAt(Config.Sorting.Count - 1);
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
			Visibility = MachineRenderingOptions.Visibility.Normal;
			VisibilityAlpha = Config.NormalAlpha;
			IsHoveredOver = false;
		}

		private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
		{
			ForceRefreshDisplayedMachines();
		}

		private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
		{
			var targetAlpha = Visibility switch
			{
				MachineRenderingOptions.Visibility.Hidden => 0f,
				MachineRenderingOptions.Visibility.Normal => IsHoveredOver ? Config.FocusedAlpha : Config.NormalAlpha,
				MachineRenderingOptions.Visibility.Focused => Config.FocusedAlpha,
				_ => throw new ArgumentException($"{nameof(Visibility)} has an invalid value."),
			};
			VisibilityAlpha += (targetAlpha - VisibilityAlpha) * 0.15f;
			if (VisibilityAlpha <= 0.01f)
				VisibilityAlpha = 0f;
			else if (VisibilityAlpha >= 0.99f)
				VisibilityAlpha = 1f;
		}

		private void OnObjectListChanged(object? sender, ObjectListChangedEventArgs e)
		{
			foreach (var @object in e.Removed)
				HideMachine(e.Location, @object.Value);
			foreach (var @object in e.Added)
				UpdateMachineState(e.Location, @object.Value);
		}

		private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
		{
			if (!Context.IsPlayerFree)
				return;
			if (!Config.VisibilityKeybind.JustPressed())
				return;

			Visibility = Visibility switch
			{
				MachineRenderingOptions.Visibility.Hidden => Config.FocusedAlpha == Config.NormalAlpha ? MachineRenderingOptions.Visibility.Focused : MachineRenderingOptions.Visibility.Normal,
				MachineRenderingOptions.Visibility.Normal => MachineRenderingOptions.Visibility.Focused,
				MachineRenderingOptions.Visibility.Focused => MachineRenderingOptions.Visibility.Hidden,
				_ => throw new ArgumentException($"{nameof(Visibility)} has an invalid value."),
			};
		}

		private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
		{
			if (!Context.IsWorldReady || Game1.eventUp)
				return;
			if (DisplayedMachines.Count == 0)
				return;
			if (VisibilityAlpha <= 0f)
				return;

			var preparedMachines = GroupMachines(SortMachines(DisplayedMachines, Game1.player));
			IList<((int column, int row) position, (GameLocation location, SObject machine, IList<SObject> heldItems) machine)> machineCoords
				= new List<((int column, int row) position, (GameLocation location, SObject machine, IList<SObject> heldItems) machine)>();
			int column = 0;
			int row = 0;

			foreach (var entry in preparedMachines)
			{
				machineCoords.Add((position: (column++, row), machine: entry));
				if (column == Config.MaxColumns)
				{
					column = 0;
					row++;
				}
			}

			var machineFlowCoords = machineCoords
				.Select(e => (position: Config.FlowDirection.GetXYPositionFromZeroOrigin(e.position), machine: e.machine))
				.ToList();
			var minX = machineFlowCoords.Min(e => e.position.x);
			var minY = machineFlowCoords.Min(e => e.position.y);
			var maxX = machineFlowCoords.Max(e => e.position.x);
			var maxY = machineFlowCoords.Max(e => e.position.y);
			var width = maxX - minX + 1;
			var height = maxY - minY + 1;

			var viewportBounds = Game1.graphics.GraphicsDevice.Viewport.Bounds;
			var screenSize = new Vector2(viewportBounds.Size.X, viewportBounds.Size.Y);
			var panelSize = (SingleMachineSize * new Vector2(width, height) + Config.Spacing * new Vector2(width - 1, height - 1)) * Config.Scale;
			var panelLocation = Config.Anchor.GetAnchoredPoint(Vector2.Zero, screenSize, panelSize);

			var mouseLocation = Game1.getMousePosition();
			IsHoveredOver =
				mouseLocation.X >= panelLocation.X &&
				mouseLocation.Y >= panelLocation.Y &&
				mouseLocation.X < panelLocation.X + panelSize.X &&
				mouseLocation.Y < panelLocation.Y + panelSize.Y;

			foreach (var ((x, y), (_, machine, heldItems)) in machineFlowCoords.OrderBy(e => e.position.y).ThenByDescending(e => e.position.x))
			{
				float GetBubbleSwayOffset()
				{
					return Config.BubbleSway switch
					{
						MachineRenderingOptions.BubbleSway.Static => 0f,
						MachineRenderingOptions.BubbleSway.Together => 2f * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250), 2),
						MachineRenderingOptions.BubbleSway.Wave => 2f * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250 + x + y), 2),
						_ => throw new ArgumentException($"{nameof(Config.BubbleSway)} has an invalid value."),
					};
				}
				
				var machineUnscaledOffset = new Vector2(x - minX, y - minY) * SingleMachineSize + new Vector2(x - minX, y - minY) * Config.Spacing;
				var machineLocation = panelLocation + machineUnscaledOffset * Config.Scale;

				ItemRenderer.DrawItem(
					e.SpriteBatch, machine,
					machineLocation, SingleMachineSize * Config.Scale,
					Color.White
				);

				float timeVariableOffset = GetBubbleSwayOffset();

				void DrawEmote(int emoteX, int emoteY)
				{
					var xEmoteRectangle = new Rectangle(emoteX * 16, emoteY * 16, 16, 16);
					float emoteScale = 2.5f;

					e.SpriteBatch.Draw(
						Game1.emoteSpriteSheet,
						machineLocation + new Vector2(SingleMachineSize.X * 0.5f - xEmoteRectangle.Width * emoteScale * 0.5f, timeVariableOffset - xEmoteRectangle.Height * emoteScale * 0.5f) * Config.Scale,
						xEmoteRectangle,
						Color.White * 0.75f * VisibilityAlpha,
						0f, Vector2.Zero, emoteScale * Config.Scale, SpriteEffects.None, 0.91f
					);
				}
				
				if (machine.readyForHarvest.Value && heldItems.Count != 0)
				{
					if (Config.ShowItemBubble)
					{
						var bubbleRectangle = new Rectangle(141, 465, 20, 24);
						float bubbleScale = 2f;

						e.SpriteBatch.Draw(
							Game1.mouseCursors,
							machineLocation + new Vector2(SingleMachineSize.X * 0.5f - bubbleRectangle.Width * bubbleScale * 0.5f, timeVariableOffset - bubbleRectangle.Height * bubbleScale * 0.5f) * Config.Scale,
							bubbleRectangle,
							Color.White * 0.75f * VisibilityAlpha,
							0f, Vector2.Zero, bubbleScale * Config.Scale, SpriteEffects.None, 0.91f
						);

						int heldItemVariableIndex = (int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / (1000.0 * Config.BubbleItemCycleTime)) % heldItems.Count;
						ItemRenderer.DrawItem(
							e.SpriteBatch, heldItems[heldItemVariableIndex],
							machineLocation + new Vector2(SingleMachineSize.X * 0.5f, timeVariableOffset - 4) * Config.Scale,
							new Vector2(bubbleRectangle.Size.X, bubbleRectangle.Size.Y) * bubbleScale * 0.8f * Config.Scale,
							Color.White,
							rectAnchorSide: UIAnchorSide.Center
						);
					}
					else
					{
						DrawEmote(3, 0);
					}
				}
				else if (machine.MinutesUntilReady <= 0 && heldItems.Count == 0)
				{
					DrawEmote(0, 4);
				}

				if (machine.Stack > 1)
				{
					Utility.drawTinyDigits(
						machine.Stack,
						e.SpriteBatch,
						machineLocation + SingleMachineSize * Config.Scale - DigitSize * new Vector2(((int)Math.Log10(machine.Stack) + 1), 1.2f) * 3 * Config.Scale,
						3f * Config.Scale,
						1f,
						Color.White * VisibilityAlpha
					);
				}
			}
		}

		private void ForceRefreshDisplayedMachines()
		{
			DisplayedMachines.Clear();
			if (!Context.IsWorldReady || Game1.currentLocation is null)
				return;
			foreach (var location in Game1.locations)
				foreach (var @object in location.Objects.Values)
					UpdateMachineState(location, @object);
		}

		private IEnumerable<(GameLocation location, SObject machine, IList<SObject> heldItems)> GroupMachines(IEnumerable<(GameLocation location, SObject machine)> machines)
		{
			SObject CopyMachine(SObject machine)
			{
				var newMachine = (SObject)machine.getOne();
				newMachine.readyForHarvest.Value = machine.readyForHarvest.Value;
				newMachine.showNextIndex.Value = machine.showNextIndex.Value;
				return newMachine;
			}

			IList<SObject> CopyHeldItems(SObject machine)
			{
				var list = new List<SObject>();
				if (machine.heldObject.Value is not null)
					list.Add(machine.heldObject.Value);
				return list;
			}

			void AddHeldItem(IList<SObject> heldItems, SObject newHeldItem)
			{
				foreach (var heldItem in heldItems)
				{
					if (heldItem.Name == newHeldItem.name && heldItem.Quality == newHeldItem.Quality)
						return;
				}
				heldItems.Add(newHeldItem);
			}
			
			IList<(GameLocation location, SObject machine, IList<SObject> heldItems)> results = new List<(GameLocation location, SObject machine, IList<SObject> heldItems)>();
			foreach (var (location, machine) in machines)
			{
				switch (Config.Grouping)
				{
					case MachineRenderingOptions.Grouping.None:
						results.Add((location, CopyMachine(machine), new List<SObject>()));
						break;
					case MachineRenderingOptions.Grouping.ByMachine:
						foreach (var (_, result, resultHeldItems) in results)
						{
							if (machine.Name == result.Name && machine.readyForHarvest.Value == result.readyForHarvest.Value)
							{
								result.Stack++;
								if (machine.heldObject.Value is not null)
									AddHeldItem(resultHeldItems, (SObject)machine.heldObject.Value.getOne());
								goto machineLoopContinue;
							}
						}
						results.Add((location, CopyMachine(machine), CopyHeldItems(machine)));
						break;
					case MachineRenderingOptions.Grouping.ByMachineAndItem:
						foreach (var (_, result, resultHeldItems) in results)
						{
							if (
								machine.Name == result.Name && machine.readyForHarvest.Value == result.readyForHarvest.Value &&
								machine.heldObject.Value?.bigCraftable.Value == resultHeldItems.FirstOrDefault()?.bigCraftable.Value &&
								machine.heldObject.Value?.Name == resultHeldItems.FirstOrDefault()?.Name
							)
							{
								result.Stack++;
								if (machine.heldObject.Value is not null)
									AddHeldItem(resultHeldItems, (SObject)machine.heldObject.Value.getOne());
								goto machineLoopContinue;
							}
						}
						results.Add((location, CopyMachine(machine), CopyHeldItems(machine)));
						break;
				}
				machineLoopContinue:;
			}
			return results;
		}

		private IEnumerable<(GameLocation location, SObject machine)> SortMachines(IEnumerable<(GameLocation location, SObject machine)> machines, Farmer player)
		{
			IEnumerable<(GameLocation location, SObject machine)> results = machines;

			void SortResults<T>(bool ascending, Func<(GameLocation location, SObject machine), T> keySelector) where T: IComparable<T>
			{
				results = results is IOrderedEnumerable<(GameLocation location, SObject machine)> ordered
					? (ascending ? ordered.ThenBy(keySelector) : ordered.ThenByDescending(keySelector))
					: (ascending ? results.OrderBy(keySelector) : results.OrderByDescending(keySelector));
			}

			foreach (var sorting in Config.Sorting)
			{
				switch (sorting)
				{
					case MachineRenderingOptions.Sorting.None:
						break;
					case MachineRenderingOptions.Sorting.ByMachineAZ:
					case MachineRenderingOptions.Sorting.ByMachineZA:
						SortResults(
							sorting == MachineRenderingOptions.Sorting.ByMachineAZ,
							e => e.machine.DisplayName
						);
						break;
					case MachineRenderingOptions.Sorting.ReadyFirst:
						SortResults(
							false,
							e => e.machine.readyForHarvest.Value
						);
						break;
					case MachineRenderingOptions.Sorting.WaitingFirst:
						SortResults(
							false,
							e => e.machine.heldObject.Value is null && e.machine.MinutesUntilReady <= 0
						);
						break;
					case MachineRenderingOptions.Sorting.BusyFirst:
						SortResults(
							false,
							e => e.machine.MinutesUntilReady > 0
						);
						break;
					case MachineRenderingOptions.Sorting.ByDistanceAscending:
					case MachineRenderingOptions.Sorting.ByDistanceDescending:
						SortResults(
							sorting == MachineRenderingOptions.Sorting.ByDistanceAscending,
							e => e.location == player.currentLocation ? (player.getTileLocation() - e.machine.TileLocation).Length() : float.PositiveInfinity
						);
						break;
					case MachineRenderingOptions.Sorting.ByItemAZ:
					case MachineRenderingOptions.Sorting.ByItemZA:
						SortResults(
							sorting == MachineRenderingOptions.Sorting.ByItemAZ,
							e => e.machine.heldObject.Value?.DisplayName ?? ""
						);
						break;
					case MachineRenderingOptions.Sorting.ByItemQualityBest:
					case MachineRenderingOptions.Sorting.ByItemQualityWorst:
						SortResults(
							sorting == MachineRenderingOptions.Sorting.ByItemQualityWorst,
							e => e.machine.heldObject.Value?.Quality ?? int.MinValue
						);
						break;
					default:
						throw new InvalidOperationException();
				}
			}
			return results;
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

		private void SetMachineVisible(bool visible, GameLocation location, SObject machine)
		{
			if (visible)
				ShowMachine(location, machine);
			else
				HideMachine(location, machine);
		}

		private void ShowMachine(GameLocation location, SObject machine)
		{
			HideMachine(location, machine);
			DisplayedMachines.Add((location, machine));
		}

		private void HideMachine(GameLocation location, SObject machine)
		{
			var existingEntry = DisplayedMachines.FirstOrNull(e => e.location == location && e.machine == machine);
			if (existingEntry is not null)
				DisplayedMachines.Remove(existingEntry.Value);
		}

		private void UpdateMachineState(GameLocation location, SObject machine)
		{
			if (!IsMachine(location, machine))
				return;

			if (machine.readyForHarvest.Value)
				SetMachineReadyForHarvest(location, machine);
			else if (machine.MinutesUntilReady > 0)
				SetMachineBusy(location, machine);
			else
				SetMachineWaitingForInput(location, machine);
		}

		private void SetMachineReadyForHarvest(GameLocation location, SObject machine)
		{
			bool shouldShow = Config.ShowReady != MachineMatches(machine, Config.ShowReadyExceptions);
			SetMachineVisible(shouldShow, location, machine);
		}

		private void SetMachineWaitingForInput(GameLocation location, SObject machine)
		{
			bool shouldShow = Config.ShowWaiting != MachineMatches(machine, Config.ShowWaitingExceptions);
			SetMachineVisible(shouldShow, location, machine);
		}

		private void SetMachineBusy(GameLocation location, SObject machine)
		{
			bool shouldShow = Config.ShowBusy != MachineMatches(machine, Config.ShowBusyExceptions);
			SetMachineVisible(shouldShow, location, machine);
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