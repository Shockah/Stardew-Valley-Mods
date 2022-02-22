using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shockah.CommonModCode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using SObject = StardewValley.Object;

namespace Shockah.FlexibleSprinklers
{
	public class FlexibleSprinklers: Mod, IFlexibleSprinklersApi
	{
		private const int PressureNozzleParentSheetIndex = 915;
		internal static readonly string LineSprinklersModID = "hootless.LineSprinklers";
		internal static readonly string BetterSprinklersModID = "Speeder.BetterSprinklers";
		internal static readonly string PrismaticToolsModID = "stokastic.PrismaticTools";

		private const int FPS = 60;
		private const float SprinklerCoverageAlphaDecrement = 1f / FPS; // 1f per second

		public static FlexibleSprinklers Instance { get; private set; } = null!;

		internal ModConfig Config { get; private set; } = null!;
		internal ISprinklerBehavior SprinklerBehavior { get; private set; } = null!;
		private readonly List<Func<SObject, int?>> SprinklerTierProviders = new();
		private readonly List<Func<SObject, Vector2[]>> SprinklerCoverageProviders = new();
		private float SprinklerCoverageAlpha = 0f;

		internal ILineSprinklersApi? LineSprinklersApi { get; private set; }
		internal IBetterSprinklersApi? BetterSprinklersApi { get; private set; }

		public override void Entry(IModHelper helper)
		{
			Instance = this;

			Config = helper.ReadConfig<ModConfig>();

			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
			helper.Events.Display.RenderedWorld += OnRenderedWorld;
			helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
			helper.Events.GameLoop.DayEnding += OnDayEnding;
			helper.Events.World.ObjectListChanged += OnObjectListChanged;
			helper.Events.World.TerrainFeatureListChanged += OnTerrainFeatureListChanged;
			helper.Events.World.LargeTerrainFeatureListChanged += OnLargeTerrainFeatureListChanged;
			helper.Events.Input.ButtonPressed += OnButtonPressed;

			SetupSprinklerBehavior();
		}

		public override object GetApi()
		{
			return this;
		}

		private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
		{
			var harmony = new Harmony(ModManifest.UniqueID);
			VanillaPatches.Apply(harmony);

			LineSprinklersApi = Helper.ModRegistry.GetApi<ILineSprinklersApi>(LineSprinklersModID);
			if (LineSprinklersApi != null)
				LineSprinklersPatches.Apply(harmony);

			BetterSprinklersApi = Helper.ModRegistry.GetApi<IBetterSprinklersApi>(BetterSprinklersModID);
			if (BetterSprinklersApi != null)
				BetterSplinklersPatches.Apply(harmony);

			SetupConfig();
		}

		private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
		{
			SprinklerCoverageAlpha = Math.Max(SprinklerCoverageAlpha - SprinklerCoverageAlphaDecrement, 0f);
		}

		private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
		{
			if (SprinklerCoverageAlpha <= 0f)
				return;
			GameLocation location = Game1.currentLocation;
			if (location is null)
				return;

			var sprinklers = location.Objects.Values
				.Where(o => o.IsSprinkler())
				.Select(o => (new IntPoint((int)o.TileLocation.X, (int)o.TileLocation.Y), GetSprinklerInfo(o)));
			var sprinklerTiles = SprinklerBehavior.GetSprinklerTiles(new GameLocationMap(location), sprinklers);
			foreach (var sprinklerTile in sprinklerTiles)
			{
				var position = new Vector2(sprinklerTile.X * Game1.tileSize, sprinklerTile.Y * Game1.tileSize);
				e.SpriteBatch.Draw(
					Game1.mouseCursors,
					Game1.GlobalToLocal(position),
					new Rectangle(194, 388, 16, 16),
					Color.White * Math.Clamp(SprinklerCoverageAlpha, 0f, 1f),
					0.0f,
					Vector2.Zero,
					Game1.pixelZoom,
					SpriteEffects.None,
					0.01f
				);
			}
		}

		private void OnDayEnding(object? sender, DayEndingEventArgs e)
		{
			if (!Config.ActivateBeforeSleep)
				return;
			ActivateAllCollectiveSprinklers();
		}

		private void OnObjectListChanged(object? sender, ObjectListChangedEventArgs e)
		{
			SprinklerBehavior.ClearCacheForMap(new GameLocationMap(e.Location));

			if (Config.ActivateOnPlacement && SprinklerBehavior.AllowsIndependentSprinklerActivation)
			{
				foreach (var (_, @object) in e.Added)
					if (@object.IsSprinkler())
						ActivateSprinkler(@object, e.Location);
			}
			if (Config.ShowCoverageOnPlacement)
				DisplaySprinklerCoverage();
		}

		private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
		{
			SprinklerBehavior.ClearCache();
			SprinklerCoverageAlpha = 0f;
		}

		private void OnTerrainFeatureListChanged(object? sender, TerrainFeatureListChangedEventArgs e)
		{
			SprinklerBehavior.ClearCacheForMap(new GameLocationMap(e.Location));
		}

		private void OnLargeTerrainFeatureListChanged(object? sender, LargeTerrainFeatureListChangedEventArgs e)
		{
			SprinklerBehavior.ClearCacheForMap(new GameLocationMap(e.Location));
		}

		private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
		{
			if (!Config.ActivateOnAction && !Config.ShowCoverageOnAction)
				return;
			if (!Context.IsPlayerFree)
				return;
			if (!e.Button.IsActionButton())
				return;

			var tile = e.Cursor.GrabTile;
			var location = Game1.currentLocation;
			var @object = location.getObjectAtTile((int)tile.X, (int)tile.Y);
			if (@object == null || !@object.IsSprinkler())
				return;

			var heldItem = Game1.player.CurrentItem;
			if (heldItem?.ParentSheetIndex == PressureNozzleParentSheetIndex && @object.heldObject?.Value?.ParentSheetIndex != PressureNozzleParentSheetIndex)
				return;

			if (Config.ActivateOnAction && SprinklerBehavior.AllowsIndependentSprinklerActivation)
				ActivateSprinkler(@object, location);
			if (Config.ShowCoverageOnAction)
				DisplaySprinklerCoverage();
		}

		private void SetupConfig()
		{
			// TODO: add translation support
			var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

			configMenu?.Register(
				ModManifest,
				reset: () => Config = new ModConfig(),
				save: () =>
				{
					Helper.WriteConfig(Config);
					SetupSprinklerBehavior();
				}
			);

			string TranslateBehavior(ModConfig.SprinklerBehaviorEnum e)
			{
				return e switch
				{
					ModConfig.SprinklerBehaviorEnum.Cluster => "Vanilla > Cluster",
					ModConfig.SprinklerBehaviorEnum.ClusterWithoutVanilla => "Cluster",
					ModConfig.SprinklerBehaviorEnum.Flexible => "Vanilla > Flood fill",
					ModConfig.SprinklerBehaviorEnum.FlexibleWithoutVanilla => "Flood fill",
					ModConfig.SprinklerBehaviorEnum.Vanilla => "Vanilla",
					_ => throw new ArgumentException(),
				};
			}

			string TranslateFlexibleBehaviorTileWaterBalanceMode(FlexibleSprinklerBehaviorTileWaterBalanceMode e)
			{
				return e switch
				{
					FlexibleSprinklerBehaviorTileWaterBalanceMode.Relaxed => "Relaxed",
					FlexibleSprinklerBehaviorTileWaterBalanceMode.Exact => "Exact",
					FlexibleSprinklerBehaviorTileWaterBalanceMode.Restrictive => "Restrictive",
					_ => throw new ArgumentException(),
				};
			}

			string TranslateClusterBehaviorClusterOrdering(ClusterSprinklerBehaviorClusterOrdering e)
			{
				return e switch
				{
					ClusterSprinklerBehaviorClusterOrdering.SmallerFirst => "Smaller clusters first",
					ClusterSprinklerBehaviorClusterOrdering.BiggerFirst => "Bigger clusters first",
					ClusterSprinklerBehaviorClusterOrdering.All => "Treat clusters equally",
					_ => throw new ArgumentException(),
				};
			}

			string TranslateClusterBehaviorBetweenClusterBalanceMode(ClusterSprinklerBehaviorBetweenClusterBalanceMode e)
			{
				return e switch
				{
					ClusterSprinklerBehaviorBetweenClusterBalanceMode.Relaxed => "Relaxed",
					ClusterSprinklerBehaviorBetweenClusterBalanceMode.Restrictive => "Restrictive",
					_ => throw new ArgumentException(),
				};
			}

			string TranslateClusterBehaviorInClusterBalanceMode(ClusterSprinklerBehaviorInClusterBalanceMode e)
			{
				return e switch
				{
					ClusterSprinklerBehaviorInClusterBalanceMode.Relaxed => "Relaxed",
					ClusterSprinklerBehaviorInClusterBalanceMode.Exact => "Exact",
					ClusterSprinklerBehaviorInClusterBalanceMode.Restrictive => "Restrictive",
					_ => throw new ArgumentException(),
				};
			}

			configMenu?.AddTextOption(
				mod: ModManifest,
				name: () => "Sprinkler behavior",
				tooltip: () => "" +
				"> Cluster: Groups sprinklers by nearby clustered tiles and tries to water as much as possible.\n" +
				"   Note: Sprinklers will no longer be able to be activated manually.\n" +
				"> Flood fill: Custom-made algorithm. Tries to flood fill from the sprinkler/watered tiles.\n" +
				"   Will also change behavior if next to other sprinklers.\n" +
				"> Vanilla: Uses the default game behavior.",
				getValue: () => TranslateBehavior(Config.SprinklerBehavior),
				setValue: value => Config.SprinklerBehavior = I18nEnum.GetFromTranslation<ModConfig.SprinklerBehaviorEnum>(value, TranslateBehavior)!,
				allowedValues: I18nEnum.GetTranslations<ModConfig.SprinklerBehaviorEnum>(TranslateBehavior).ToArray()
			);

			configMenu?.AddBoolOption(
				mod: ModManifest,
				name: () => "Compatibility mode",
				tooltip: () => "Patch the game code in a more mod-compatible way, at the cost of some performance.",
				getValue: () => Config.CompatibilityMode,
				setValue: value => Config.CompatibilityMode = value
			);

			{
				configMenu?.AddSectionTitle(
					mod: ModManifest,
					text: () => "Cluster options"
				);

				configMenu?.AddTextOption(
					mod: ModManifest,
					name: () => "Cluster ordering",
					tooltip: () => "Which clusters should each sprinkler prefer when choosing which one to water more.",
					getValue: () => TranslateClusterBehaviorClusterOrdering(Config.ClusterBehaviorClusterOrdering),
					setValue: value => Config.ClusterBehaviorClusterOrdering = I18nEnum.GetFromTranslation<ClusterSprinklerBehaviorClusterOrdering>(value, TranslateClusterBehaviorClusterOrdering)!,
					allowedValues: I18nEnum.GetTranslations<ClusterSprinklerBehaviorClusterOrdering>(TranslateClusterBehaviorClusterOrdering).ToArray()
				);

				configMenu?.AddTextOption(
					mod: ModManifest,
					name: () => "Between cluster balance",
					tooltip: () => "Edge case handling for choosing which cluster to water more.\n\n> Relaxed: May water more tiles\n> Restrictive: May water less tiles",
					getValue: () => TranslateClusterBehaviorBetweenClusterBalanceMode(Config.ClusterBehaviorBetweenClusterBalanceMode),
					setValue: value => Config.ClusterBehaviorBetweenClusterBalanceMode = I18nEnum.GetFromTranslation<ClusterSprinklerBehaviorBetweenClusterBalanceMode>(value, TranslateClusterBehaviorBetweenClusterBalanceMode)!,
					allowedValues: I18nEnum.GetTranslations<ClusterSprinklerBehaviorBetweenClusterBalanceMode>(TranslateClusterBehaviorBetweenClusterBalanceMode).ToArray()
				);

				configMenu?.AddTextOption(
					mod: ModManifest,
					name: () => "In-cluster balance",
					tooltip: () => "Edge case handling for choosing which tiles in a cluster to water.\n\n> Relaxed: May water more tiles\n> Exact: Will water exactly as many tiles as it should, but those may be semi-random\n> Restrictive: May water less tiles",
					getValue: () => TranslateClusterBehaviorInClusterBalanceMode(Config.ClusterBehaviorInClusterBalanceMode),
					setValue: value => Config.ClusterBehaviorInClusterBalanceMode = I18nEnum.GetFromTranslation<ClusterSprinklerBehaviorInClusterBalanceMode>(value, TranslateClusterBehaviorInClusterBalanceMode)!,
					allowedValues: I18nEnum.GetTranslations<ClusterSprinklerBehaviorInClusterBalanceMode>(TranslateClusterBehaviorInClusterBalanceMode).ToArray()
				);
			}

			{
				configMenu?.AddSectionTitle(
					mod: ModManifest,
					text: () => "Flood fill options"
				);

				configMenu?.AddTextOption(
					mod: ModManifest,
					name: () => "Flood fill balance mode",
					tooltip: () => "Edge case handling for choosing which tiles to water.\n\n> Relaxed: May water more tiles\n> Exact: Will water exactly as many tiles as it should, but those may be semi-random\n> Restrictive: May water less tiles",
					getValue: () => TranslateFlexibleBehaviorTileWaterBalanceMode(Config.TileWaterBalanceMode),
					setValue: value => Config.TileWaterBalanceMode = I18nEnum.GetFromTranslation<FlexibleSprinklerBehaviorTileWaterBalanceMode>(value, TranslateFlexibleBehaviorTileWaterBalanceMode)!,
					allowedValues: I18nEnum.GetTranslations<FlexibleSprinklerBehaviorTileWaterBalanceMode>(TranslateFlexibleBehaviorTileWaterBalanceMode).ToArray()
				);
			}

			{
				configMenu?.AddSectionTitle(
					mod: ModManifest,
					text: () => "Activation"
				);

				configMenu?.AddBoolOption(
					mod: ModManifest,
					name: () => "Activate before sleep",
					getValue: () => Config.ActivateBeforeSleep,
					setValue: value => Config.ActivateBeforeSleep = value
				);

				configMenu?.AddBoolOption(
					mod: ModManifest,
					name: () => "Activate on placement",
					tooltip: () => "Note: this does not work with Cluster behavior.",
					getValue: () => Config.ActivateOnPlacement,
					setValue: value => Config.ActivateOnPlacement = value
				);

				configMenu?.AddBoolOption(
					mod: ModManifest,
					name: () => "Activate on action",
					tooltip: () => "Note: this does not work with Cluster behavior.",
					getValue: () => Config.ActivateOnAction,
					setValue: value => Config.ActivateOnAction = value
				);
			}

			{
				configMenu?.AddSectionTitle(
					mod: ModManifest,
					text: () => "Coverage"
				);

				configMenu?.AddNumberOption(
					mod: ModManifest,
					name: () => "Display time",
					tooltip: () => "How many seconds to show the coverage display for.",
					getValue: () => Config.CoverageTimeInSeconds,
					setValue: value => Config.CoverageTimeInSeconds = value,
					min: 1f
				);

				configMenu?.AddNumberOption(
					mod: ModManifest,
					name: () => "Alpha",
					tooltip: () => "Transparency of the coverage display.\n0: fully transparent\n1: fully opaque",
					getValue: () => Config.CoverageAlpha,
					setValue: value => Config.CoverageAlpha = value,
					min: 0f, max: 1f, interval: 0.05f
				);

				configMenu?.AddBoolOption(
					mod: ModManifest,
					name: () => "Show on placement",
					getValue: () => Config.ShowCoverageOnPlacement,
					setValue: value => Config.ShowCoverageOnPlacement = value
				);

				configMenu?.AddBoolOption(
					mod: ModManifest,
					name: () => "Show on action",
					getValue: () => Config.ShowCoverageOnAction,
					setValue: value => Config.ShowCoverageOnAction = value
				);
			}

			{
				configMenu?.AddSectionTitle(
					mod: ModManifest,
					text: () => "Sprinkler power",
					tooltip: () => "The values below will only be used when watering via the flood fill method."
				);

				configMenu?.AddNumberOption(
					mod: ModManifest,
					name: () => "Tier 1 (Basic)",
					tooltip: () => "How many tiles should tier 1 (Basic) sprinklers cover.",
					getValue: () => Config.Tier1Power,
					setValue: value => Config.Tier1Power = value,
					min: 0, interval: 1
				);

				configMenu?.AddNumberOption(
					mod: ModManifest,
					name: () => "Tier 2 (Quality)",
					tooltip: () => "How many tiles should tier 2 (Quality / Basic + Pressure Nozzle) sprinklers cover.",
					getValue: () => Config.Tier2Power,
					setValue: value => Config.Tier2Power = value,
					min: 0, interval: 1
				);

				configMenu?.AddNumberOption(
					mod: ModManifest,
					name: () => "Tier 3 (Iridium)",
					tooltip: () => "How many tiles should tier 3 (Iridium / Quality + Pressure Nozzle) sprinklers cover.",
					getValue: () => Config.Tier3Power,
					setValue: value => Config.Tier3Power = value,
					min: 0, interval: 1
				);

				configMenu?.AddNumberOption(
					mod: ModManifest,
					name: () => "Tier 4 (Iridium + Nozzle)",
					tooltip: () => "How many tiles should tier 4 (Iridium + Pressure Nozzle) sprinklers cover.",
					getValue: () => Config.Tier4Power,
					setValue: value => Config.Tier4Power = value,
					min: 0, interval: 1
				);

				configMenu?.AddNumberOption(
					mod: ModManifest,
					name: () => "Tier 5",
					tooltip: () => "How many tiles should tier 5 sprinklers (if you have mods with such a tier) cover.",
					getValue: () => Config.Tier5Power,
					setValue: value => Config.Tier5Power = value,
					min: 0, interval: 1
				);

				configMenu?.AddNumberOption(
					mod: ModManifest,
					name: () => "Tier 6",
					tooltip: () => "How many tiles should tier 6 sprinklers (if you have mods with such a tier) cover.",
					getValue: () => Config.Tier6Power,
					setValue: value => Config.Tier6Power = value,
					min: 0, interval: 1
				);

				configMenu?.AddNumberOption(
					mod: ModManifest,
					name: () => "Tier 7",
					tooltip: () => "How many tiles should tier 6 sprinklers (if you have mods with such a tier) cover.",
					getValue: () => Config.Tier7Power,
					setValue: value => Config.Tier7Power = value,
					min: 0, interval: 1
				);

				configMenu?.AddNumberOption(
					mod: ModManifest,
					name: () => "Tier 8",
					tooltip: () => "How many tiles should tier 6 sprinklers (if you have mods with such a tier) cover.",
					getValue: () => Config.Tier8Power,
					setValue: value => Config.Tier8Power = value,
					min: 0, interval: 1
				);
			}
		}

		private void SetupSprinklerBehavior()
		{
			SprinklerBehavior = Config.SprinklerBehavior switch
			{
				ModConfig.SprinklerBehaviorEnum.Cluster => new ClusterSprinklerBehavior(Config.ClusterBehaviorClusterOrdering, Config.ClusterBehaviorBetweenClusterBalanceMode, Config.ClusterBehaviorInClusterBalanceMode), // TODO: implement
				ModConfig.SprinklerBehaviorEnum.ClusterWithoutVanilla => new ClusterSprinklerBehavior(Config.ClusterBehaviorClusterOrdering, Config.ClusterBehaviorBetweenClusterBalanceMode, Config.ClusterBehaviorInClusterBalanceMode),
				ModConfig.SprinklerBehaviorEnum.Flexible => new FlexibleSprinklerBehavior(Config.TileWaterBalanceMode, new VanillaSprinklerBehavior()),
				ModConfig.SprinklerBehaviorEnum.FlexibleWithoutVanilla => new FlexibleSprinklerBehavior(Config.TileWaterBalanceMode, null),
				ModConfig.SprinklerBehaviorEnum.Vanilla => new VanillaSprinklerBehavior(),
				_ => throw new ArgumentException(),
			};
		}

		public void ActivateAllCollectiveSprinklers()
		{
			if (Game1.player.team.SpecialOrderRuleActive("NO_SPRINKLER"))
				return;
			foreach (GameLocation location in Game1.locations)
				ActivateCollectiveSprinklersInLocation(location);
		}

		public void ActivateCollectiveSprinklersInLocation(GameLocation location)
		{
			if (Game1.player.team.SpecialOrderRuleActive("NO_SPRINKLER"))
				return;
			var sprinklers = location.Objects.Values.Where(o => o.IsSprinkler());
			ActivateCollectiveSprinklers(sprinklers, location);
		}

		public void ActivateCollectiveSprinklers(IEnumerable<SObject> sprinklers, GameLocation location)
		{
			if (Game1.player.team.SpecialOrderRuleActive("NO_SPRINKLER"))
				return;
			var sprinklerEntries = sprinklers
				.Where(s => s.IsSprinkler())
				.Select(s => (sprinkler: s, position: new IntPoint((int)s.TileLocation.X, (int)s.TileLocation.Y), info: GetSprinklerInfo(s)))
				.ToList();
			if (sprinklerEntries.Count == 0)
				return;
			var anySprinkler = sprinklerEntries.First().sprinkler;
			var sprinklerTiles = SprinklerBehavior.GetSprinklerTiles(new GameLocationMap(location), sprinklerEntries.Select(e => (e.position, e.info)));
			foreach (var sprinklerTile in sprinklerTiles)
				anySprinkler.ApplySprinkler(location, new Vector2(sprinklerTile.X, sprinklerTile.Y));
			foreach (var (sprinkler, _, _) in sprinklerEntries)
				sprinkler.ApplySprinklerAnimation(location);
		}

		public void ActivateSprinkler(SObject sprinkler, GameLocation location)
		{
			if (!SprinklerBehavior.AllowsIndependentSprinklerActivation)
				return;
			if (Game1.player.team.SpecialOrderRuleActive("NO_SPRINKLER"))
				return;
			if (!sprinkler.IsSprinkler())
				return;

			foreach (var sprinklerTile in GetModifiedSprinklerCoverage(sprinkler, location))
				sprinkler.ApplySprinkler(location, new Vector2(sprinklerTile.X, sprinklerTile.Y));
			sprinkler.ApplySprinklerAnimation(location);
		}

		internal int GetSprinklerPower(SObject sprinkler, Vector2[] layout)
		{
			int? GetTier()
			{
				foreach (var sprinklerTierProvider in SprinklerTierProviders)
				{
					var tier = sprinklerTierProvider(sprinkler);
					if (tier != null)
						return tier;
				}

				// Line Sprinklers is patched, no need for custom handling here

				var radius = sprinkler.GetModifiedRadiusForSprinkler();
				return radius == -1 ? null : radius + 1;
			}

			var tier = GetTier();

			if (tier == null)
			{
				return layout.Length;
			}
			else
			{
				var powers = new[] { Config.Tier1Power, Config.Tier2Power, Config.Tier3Power, Config.Tier4Power, Config.Tier5Power, Config.Tier6Power, Config.Tier7Power, Config.Tier8Power };
				return tier.Value < powers.Length ? powers[tier.Value - 1] : layout.Length;
			}
		}
		internal SprinklerInfo GetSprinklerInfo(SObject sprinkler)
		{
			var layout = GetUnmodifiedSprinklerCoverage(sprinkler);
			var power = GetSprinklerPower(sprinkler, layout);
			return new SprinklerInfo(layout.ToHashSet(), power);
		}

		public int GetSprinklerPower(SObject sprinkler)
		{
			return GetSprinklerInfo(sprinkler).Power;
		}

		public int GetFloodFillSprinklerRange(int power)
		{
			return (int)Math.Floor(Math.Pow(power, 0.62) + 1);
		}

		public bool IsTileInRangeOfSprinkler(SObject sprinkler, GameLocation location, Vector2 tileLocation)
		{
			var info = GetSprinklerInfo(sprinkler);
			var manhattanDistance = ((int)tileLocation.X - (int)sprinkler.TileLocation.X) + ((int)tileLocation.Y - (int)sprinkler.TileLocation.Y);
			if (manhattanDistance > GetFloodFillSprinklerRange(info.Power))
			{
				if (!info.Layout.Contains(tileLocation - sprinkler.TileLocation))
					return false;
			}
			return GetModifiedSprinklerCoverage(sprinkler, location).Contains(tileLocation);
		}

		public bool IsTileInRangeOfSprinklers(IEnumerable<SObject> sprinklers, GameLocation location, Vector2 tileLocation)
		{
			var sortedSprinklers = sprinklers.OrderBy(s => (tileLocation - s.TileLocation).Length() * FlexibleSprinklers.Instance.GetSprinklerInfo(s).Power);
			foreach (var sprinkler in sortedSprinklers)
			{
				if (IsTileInRangeOfSprinkler(sprinkler, location, tileLocation))
					return true;
			}
			return false;
		}

		public Vector2[] GetModifiedSprinklerCoverage(SObject sprinkler, GameLocation location)
		{
			var wasVanillaQueryInProgress = VanillaPatches.IsVanillaQueryInProgress;
			VanillaPatches.IsVanillaQueryInProgress = false;
			VanillaPatches.CurrentLocation = location;
			var layout = sprinkler.GetSprinklerTiles().ToArray();
			VanillaPatches.IsVanillaQueryInProgress = wasVanillaQueryInProgress;
			return layout;
		}

		public Vector2[] GetUnmodifiedSprinklerCoverage(SObject sprinkler)
		{
			foreach (var sprinklerCoverageProvider in SprinklerCoverageProviders)
			{
				var coverage = sprinklerCoverageProvider(sprinkler);
				if (coverage != null)
					return coverage;
			}
			
			if (LineSprinklersApi != null)
			{
				if (LineSprinklersApi.GetSprinklerCoverage().TryGetValue(sprinkler.ParentSheetIndex, out Vector2[]? tilePositions))
					return tilePositions.Where(t => t != Vector2.Zero).ToArray();
			}

			if (BetterSprinklersApi != null)
			{
				if (BetterSprinklersApi.GetSprinklerCoverage().TryGetValue(sprinkler.ParentSheetIndex, out Vector2[]? tilePositions))
					return tilePositions.Where(t => t != Vector2.Zero).ToArray();
			}

			var wasVanillaQueryInProgress = VanillaPatches.IsVanillaQueryInProgress;
			VanillaPatches.IsVanillaQueryInProgress = true;
			var layout = sprinkler.GetSprinklerTiles()
				.Select(t => t - sprinkler.TileLocation)
				.Where(t => t != Vector2.Zero).ToArray();
			VanillaPatches.IsVanillaQueryInProgress = wasVanillaQueryInProgress;
			return layout;
		}

		public void RegisterSprinklerTierProvider(Func<SObject, int?> provider)
		{
			SprinklerTierProviders.Add(provider);
		}

		public void RegisterSprinklerCoverageProvider(Func<SObject, Vector2[]> provider)
		{
			SprinklerCoverageProviders.Add(provider);
		}

		public void DisplaySprinklerCoverage(float? seconds = null)
		{
			SprinklerCoverageAlpha = SprinklerCoverageAlphaDecrement * FPS * (seconds ?? Config.CoverageTimeInSeconds);
		}
	}
}