using HarmonyLib;
using Microsoft.Xna.Framework;
using Shockah.CommonModCode;
using Shockah.CommonModCode.GMCM;
using Shockah.CommonModCode.Stardew;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using SObject = StardewValley.Object;

namespace Shockah.ImmersiveBeeHouses
{
	public class ImmersiveBeeHouses : Mod
	{
		private const int BeeHouseID = 10;

		internal static ImmersiveBeeHouses Instance { get; set; } = null!;
		public ModConfig Config { get; private set; } = null!;
		private IFluent<string> Fluent { get; set; } = null!;

		private bool IsGMCMRegistered = false;
		private bool IsVanillaQueryInProgress { get; set; } = false;

		public override void Entry(IModHelper helper)
		{
			Instance = this;
			Config = helper.ReadConfig<ModConfig>();

			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.GameLoop.DayStarted += OnDayStarted;
			helper.Events.World.ObjectListChanged += OnObjectListChanged;
		}

		public IReadOnlySet<(IntPoint Tile, Crop Crop)> GetFlowersAroundTile(GameLocation location, IntPoint tile, int range, bool includeSelf = false, Func<Crop, bool>? additionalCheck = null)
		{
			HashSet<(IntPoint Tile, Crop Crop)> results = new();
			for (int yy = -range; yy <= range; yy++)
			{
				int rangeLeft = range - Math.Abs(yy);
				for (int xx = -rangeLeft; xx <= rangeLeft; xx++)
				{
					if (xx == 0 && yy == 0 && !includeSelf)
						continue;
					if (!location.terrainFeatures.TryGetValue(new(tile.X + xx, tile.Y + yy), out var feature))
						continue;
					if (feature is not HoeDirt soil)
						continue;
					if (soil.crop is null)
						continue;
					if (new SObject(soil.crop.indexOfHarvest.Value, 1).Category != SObject.flowersCategory)
						continue;
					if (soil.crop.currentPhase.Value < soil.crop.phaseDays.Count - 1)
						continue;
					if (soil.crop.dead.Value)
						continue;
					if (additionalCheck is not null && !additionalCheck(soil.crop))
						continue;
					results.Add((Tile: new(tile.X + xx, tile.Y + yy), Crop: soil.crop));
				}
			}
			return results;
		}

		private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
		{
			var fluentApi = Helper.ModRegistry.GetApi<IFluentApi>("Shockah.ProjectFluent")!;
			Fluent = fluentApi.GetLocalizationsForCurrentLocale(ModManifest);

			var harmony = new Harmony(ModManifest.UniqueID);
			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(SObject), nameof(SObject.DayUpdate)),
				prefix: new HarmonyMethod(typeof(ImmersiveBeeHouses), nameof(SObject_DayUpdate_Prefix)),
				postfix: new HarmonyMethod(typeof(ImmersiveBeeHouses), nameof(SObject_DayUpdate_Postfix))
			);
			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(SObject), nameof(SObject.performDropDownAction)),
				prefix: new HarmonyMethod(typeof(ImmersiveBeeHouses), nameof(SObject_performDropDownAction_Prefix)),
				postfix: new HarmonyMethod(typeof(ImmersiveBeeHouses), nameof(SObject_performDropDownAction_Postfix))
			);
			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(SObject), nameof(SObject.checkForAction)),
				prefix: new HarmonyMethod(typeof(ImmersiveBeeHouses), nameof(SObject_checkForAction_Prefix)),
				postfix: new HarmonyMethod(typeof(ImmersiveBeeHouses), nameof(SObject_checkForAction_Postfix))
			);
			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(Utility), nameof(Utility.findCloseFlower), new Type[] { typeof(GameLocation), typeof(Vector2), typeof(int), typeof(Func<Crop, bool>) }),
				prefix: new HarmonyMethod(typeof(ImmersiveBeeHouses), nameof(Utility_findCloseFlower_Prefix)),
				postfix: new HarmonyMethod(typeof(ImmersiveBeeHouses), nameof(Utility_findCloseFlower_Postfix))
			);

			var sc = Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore")!;
			sc.RegisterCustomProperty(
				typeof(SObject),
				"BeeHouseStartingMinutesUntilReady",
				typeof(int),
				AccessTools.Method(typeof(SObjectExtensions), nameof(SObjectExtensions.GetBeeHouseStartingMinutesUntilReady)),
				AccessTools.Method(typeof(SObjectExtensions), nameof(SObjectExtensions.SetBeeHouseStartingMinutesUntilReady))
			);

			SetupConfig();
		}

		private void OnDayStarted(object? sender, DayStartedEventArgs e)
		{
			if (GameExt.GetMultiplayerMode() == MultiplayerMode.Client)
				return;

			foreach (var location in GameExt.GetAllLocations())
				foreach (var @object in location.Objects.Values)
					if (IsBeeHouse(@object))
						UpdateBeeHouseMinutesUntilReady(location, @object);
		}

		private void OnObjectListChanged(object? sender, ObjectListChangedEventArgs e)
		{
			if (GameExt.GetMultiplayerMode() == MultiplayerMode.Client)
				return;

			foreach (var @object in e.Removed)
			{
				if (IsBeeHouse(@object.Value))
					@object.Value.SetBeeHouseStartingMinutesUntilReady(0);
			}
			foreach (var @object in e.Added)
			{
				if (IsBeeHouse(@object.Value))
					UpdateBeeHouseMinutesUntilReady(e.Location, @object.Value);
			}
		}

		private void SetupConfig()
		{
			var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
			if (IsGMCMRegistered)
				configMenu?.Unregister(ModManifest);

			configMenu?.Register(
				ModManifest,
				reset: () => Config = new ModConfig(),
				save: () =>
				{
					Helper.WriteConfig(Config);
					SetupConfig();
				}
			);
			IsGMCMRegistered = true;

			configMenu?.AddBoolOption(
				mod: ModManifest,
				name: () => Fluent["config-compatibilityMode"],
				tooltip: () => Fluent["config-compatibilityMode.tooltip"],
				getValue: () => Config.CompatibilityMode,
				setValue: value => Config.CompatibilityMode = value
			);

			configMenu?.AddNumberOption(
				mod: ModManifest,
				name: () => Fluent["config-daysToProduce"],
				tooltip: () => Fluent["config-daysToProduce.tooltip"],
				getValue: () => Config.DaysToProduce,
				setValue: value => Config.DaysToProduce = value,
				min: 0.2f, max: 30f, interval: 0.2f
			);

			configMenu?.AddNumberOption(
				mod: ModManifest,
				name: () => Fluent["config-flowerCoefficient"],
				tooltip: () => Fluent["config-flowerCoefficient.tooltip"],
				getValue: () => Config.FlowerCoefficient,
				setValue: value => Config.FlowerCoefficient = value,
				min: 0.1f, max: 2f, interval: 0.01f
			);

			configMenu?.AddGraphView(
				mod: ModManifest,
				name: () => "asdf",
				configuration: new(
					inline: false,
					height: 360,
					xAxisTitle: () => "Flowers",
					yAxisTitle: () => "Days",
					data: () => Array.Empty<Vector2>()
				)
			);
		}

		private string GetDurationString(int minutes)
		{
			int hours = minutes / 60;
			minutes %= 60;

			int days = hours / 24;
			hours %= 24;

			return Fluent.Get("duration", new { Days = days, Hours = hours, Minutes = minutes });
		}

		private bool IsBeeHouse(SObject @object)
			=> @object.bigCraftable.Value && @object.ParentSheetIndex == BeeHouseID;

		private int GetModdedMinutesUntilReadyForFlowerCount(int flowerCount)
		{
			float flowerMultiplier = (float)Math.Pow(flowerCount + 1, Config.FlowerCoefficient);
			int modifiedStartingMinutesUntilReady = (int)Math.Ceiling(Config.DaysToProduce * 24 * 60 / flowerMultiplier);
			int tenRoundedModifiedStartingMinutesUntilReady = ((int)Math.Ceiling(modifiedStartingMinutesUntilReady / 10f)) * 10;
			return tenRoundedModifiedStartingMinutesUntilReady;
		}

		private void UpdateBeeHouseMinutesUntilReady(GameLocation location, SObject @object, bool reset = false)
		{
			if (!IsBeeHouse(@object))
				return;

			int originalStartingMinutesUntilReady = (int)(Config.DaysToProduce * 24 * 60);
			int flowerCount = GetFlowersAroundTile(location, new((int)@object.TileLocation.X, (int)@object.TileLocation.Y), range: 5, additionalCheck: crop => !crop.forageCrop.Value).Count;
			int tenRoundedModifiedStartingMinutesUntilReady = GetModdedMinutesUntilReadyForFlowerCount(flowerCount);

			int oldStartingMinutesUntilReady = @object.GetBeeHouseStartingMinutesUntilReady();
			if (oldStartingMinutesUntilReady == 0 || reset)
				oldStartingMinutesUntilReady = originalStartingMinutesUntilReady;
			float progress = 1f * @object.MinutesUntilReady / oldStartingMinutesUntilReady;

			int newMinutesUntilReady = (int)Math.Ceiling(tenRoundedModifiedStartingMinutesUntilReady * progress);
			int tenRoundedNewMinutesUntilReady = ((int)Math.Ceiling(newMinutesUntilReady / 10f)) * 10;
			@object.SetBeeHouseStartingMinutesUntilReady(tenRoundedModifiedStartingMinutesUntilReady);
			@object.MinutesUntilReady = tenRoundedNewMinutesUntilReady;
		}

		private static void SObject_DayUpdate_Prefix(SObject __instance, ref SObjectDayUpdateState __state)
		{
			__state = new(__instance.readyForHarvest.Value, __instance.MinutesUntilReady, __instance.heldObject.Value?.getOne() as SObject);
		}

		private static void SObject_DayUpdate_Postfix(SObject __instance, ref SObjectDayUpdateState __state, GameLocation location)
		{
			if (Instance.IsVanillaQueryInProgress)
				return;
			if (!Instance.IsBeeHouse(__instance))
				return;

			if ((__state.HeldObject is null && __instance.heldObject.Value is not null) || (__state.ReadyForHarvest && !__instance.readyForHarvest.Value))
				Instance.UpdateBeeHouseMinutesUntilReady(location, __instance);
		}

		private static void SObject_performDropDownAction_Prefix(SObject __instance, ref SObjectDayUpdateState __state)
		{
			__state = new(__instance.readyForHarvest.Value, __instance.MinutesUntilReady, __instance.heldObject.Value?.getOne() as SObject);
		}

		private static void SObject_performDropDownAction_Postfix(SObject __instance, ref SObjectDayUpdateState __state, Farmer who)
		{
			if (Instance.IsVanillaQueryInProgress)
				return;
			if (!Instance.IsBeeHouse(__instance))
				return;

			if ((__state.HeldObject is null && __instance.heldObject.Value is not null) || (__state.ReadyForHarvest && !__instance.readyForHarvest.Value))
				Instance.UpdateBeeHouseMinutesUntilReady(who.currentLocation, __instance, reset: true);
		}

		private static void SObject_checkForAction_Prefix(SObject __instance, ref SObjectDayUpdateState __state)
		{
			__state = new(__instance.readyForHarvest.Value, __instance.MinutesUntilReady, __instance.heldObject.Value?.getOne() as SObject);
		}

		private static void SObject_checkForAction_Postfix(SObject __instance, ref SObjectDayUpdateState __state, Farmer who, bool justCheckingForActivity)
		{
			if (justCheckingForActivity)
				return;
			if (Instance.IsVanillaQueryInProgress)
				return;
			if (!Instance.IsBeeHouse(__instance))
				return;

			if ((__state.HeldObject is null && __instance.heldObject.Value is not null) || (__state.ReadyForHarvest && !__instance.readyForHarvest.Value))
				Instance.UpdateBeeHouseMinutesUntilReady(who.currentLocation, __instance, reset: true);
		}

		private static bool Utility_findCloseFlower_Prefix(ref Crop? __result, GameLocation location, Vector2 startTileLocation, int range, Func<Crop, bool>? additional_check)
		{
			if (Instance.Config.CompatibilityMode)
				return true;
			__result = Utility_findCloseFlower_Result(location, startTileLocation, range, additional_check);
			return false;
		}

		private static void Utility_findCloseFlower_Postfix(ref Crop? __result, GameLocation location, Vector2 startTileLocation, int range, Func<Crop, bool>? additional_check)
		{
			if (!Instance.Config.CompatibilityMode)
				return;
			__result = Utility_findCloseFlower_Result(location, startTileLocation, range, additional_check);
		}

		private static Crop? Utility_findCloseFlower_Result(GameLocation location, Vector2 startTileLocation, int range, Func<Crop, bool>? additional_check = null)
		{
			var flowers = Instance.GetFlowersAroundTile(location, new((int)startTileLocation.X, (int)startTileLocation.Y), range: 5);
			if (flowers.Count == 0)
				return null;
			else
				return flowers.ToList()[Game1.random.Next(flowers.Count)].Crop;
		}

		private record SObjectDayUpdateState(bool ReadyForHarvest, int MinutesUntilReady, SObject? HeldObject);
	}
}