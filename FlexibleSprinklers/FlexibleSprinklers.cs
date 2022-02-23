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
		private readonly IList<Func<SObject, int?>> SprinklerTierProviders = new List<Func<SObject, int?>>();
		private readonly IList<Func<SObject, Vector2[]>> SprinklerCoverageProviders = new List<Func<SObject, Vector2[]>>();
		internal IList<Func<GameLocation, Vector2, bool?>> CustomWaterableTileProviders { get; private set; } = new List<Func<GameLocation, Vector2, bool?>>();
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

			RegisterCustomWaterableTileProvider((location, v) => (location is SlimeHutch && v.X == 16f && v.Y >= 6f && v.Y <= 9f) ? true : null);

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
			var sprinklerTiles = SprinklerBehavior.GetSprinklerTiles(new GameLocationMap(location, CustomWaterableTileProviders), sprinklers);
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
			SprinklerBehavior.ClearCacheForMap(new GameLocationMap(e.Location, CustomWaterableTileProviders));

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
			SprinklerBehavior.ClearCacheForMap(new GameLocationMap(e.Location, CustomWaterableTileProviders));
		}

		private void OnLargeTerrainFeatureListChanged(object? sender, LargeTerrainFeatureListChangedEventArgs e)
		{
			SprinklerBehavior.ClearCacheForMap(new GameLocationMap(e.Location, CustomWaterableTileProviders));
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

			configMenu?.AddEnumOption(
				ModManifest, Helper.Translation, "config.sprinklerBehavior",
				() => Config.SprinklerBehavior
			);

			configMenu?.AddBoolOption(
				ModManifest, Helper.Translation, "config.compatibilityMode",
				() => Config.CompatibilityMode
			);

			{
				configMenu?.AddSectionTitle(ModManifest, Helper.Translation, "config.cluster.section");

				configMenu?.AddEnumOption(
					ModManifest, Helper.Translation, "config.cluster.ordering",
					() => Config.ClusterBehaviorClusterOrdering
				);

				configMenu?.AddEnumOption(
					ModManifest, Helper.Translation, "config.cluster.betweenClusterBalance",
					() => Config.ClusterBehaviorBetweenClusterBalanceMode
				);

				configMenu?.AddEnumOption(
					ModManifest, Helper.Translation, "config.cluster.inClusterBalance",
					() => Config.ClusterBehaviorInClusterBalanceMode
				);
			}

			{
				configMenu?.AddSectionTitle(ModManifest, Helper.Translation, "config.floodFill.section");

				configMenu?.AddEnumOption(
					ModManifest, Helper.Translation, "config.floodFill.balanceMode",
					() => Config.TileWaterBalanceMode
				);
			}

			{
				configMenu?.AddSectionTitle(ModManifest, Helper.Translation, "config.activation.section");

				configMenu?.AddBoolOption(
					ModManifest, Helper.Translation, "config.activation.beforeSleep",
					() => Config.ActivateBeforeSleep
				);

				configMenu?.AddBoolOption(
					ModManifest, Helper.Translation, "config.activation.onPlacement",
					() => Config.ActivateOnPlacement
				);

				configMenu?.AddBoolOption(
					ModManifest, Helper.Translation, "config.activation.onAction",
					() => Config.ActivateOnAction
				);
			}

			{
				configMenu?.AddSectionTitle(ModManifest, Helper.Translation, "config.coverage.section");

				configMenu?.AddNumberOption(
					ModManifest, Helper.Translation, "config.coverage.displayTime",
					() => Config.CoverageTimeInSeconds,
					min: 1f
				);

				configMenu?.AddNumberOption(
					ModManifest, Helper.Translation, "config.coverage.alpha",
					() => Config.CoverageAlpha,
					min: 0f, max: 1f, interval: 0.05f
				);

				configMenu?.AddBoolOption(
					ModManifest, Helper.Translation, "config.coverage.onPlacement",
					() => Config.ShowCoverageOnPlacement
				);

				configMenu?.AddBoolOption(
					ModManifest, Helper.Translation, "config.coverage.onAction",
					() => Config.ShowCoverageOnAction
				);
			}

			{
				configMenu?.AddSectionTitle(ModManifest, Helper.Translation, "config.sprinklerPower.section");

				configMenu?.AddNumberOption(
					ModManifest, Helper.Translation, "config.sprinklerPower.tier1",
					() => Config.Tier1Power,
					min: 0
				);

				configMenu?.AddNumberOption(
					ModManifest, Helper.Translation, "config.sprinklerPower.tier2",
					() => Config.Tier2Power,
					min: 0
				);

				configMenu?.AddNumberOption(
					ModManifest, Helper.Translation, "config.sprinklerPower.tier3",
					() => Config.Tier3Power,
					min: 0
				);

				configMenu?.AddNumberOption(
					ModManifest, Helper.Translation, "config.sprinklerPower.tier4",
					() => Config.Tier4Power,
					min: 0
				);

				configMenu?.AddNumberOption(
					ModManifest, Helper.Translation, "config.sprinklerPower.tier5",
					() => Config.Tier5Power,
					min: 0
				);

				configMenu?.AddNumberOption(
					ModManifest, Helper.Translation, "config.sprinklerPower.tier6",
					() => Config.Tier6Power,
					min: 0
				);

				configMenu?.AddNumberOption(
					ModManifest, Helper.Translation, "config.sprinklerPower.tier7",
					() => Config.Tier7Power,
					min: 0
				);

				configMenu?.AddNumberOption(
					ModManifest, Helper.Translation, "config.sprinklerPower.tier8",
					() => Config.Tier8Power,
					min: 0
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
			var sprinklerTiles = SprinklerBehavior.GetSprinklerTiles(new GameLocationMap(location, CustomWaterableTileProviders), sprinklerEntries.Select(e => (e.position, e.info)));
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

		public void RegisterCustomWaterableTileProvider(Func<GameLocation, Vector2, bool?> provider)
		{
			CustomWaterableTileProviders.Add(provider);
		}

		public void DisplaySprinklerCoverage(float? seconds = null)
		{
			SprinklerCoverageAlpha = SprinklerCoverageAlphaDecrement * FPS * (seconds ?? Config.CoverageTimeInSeconds);
		}
	}
}