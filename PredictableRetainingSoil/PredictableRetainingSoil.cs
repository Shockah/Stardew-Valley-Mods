using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.TerrainFeatures;
using System;
using SObject = StardewValley.Object;

namespace Shockah.PredictableRetainingSoil
{
	public class PredictableRetainingSoil: Mod
	{
		private const int BasicRetainingSoilID = 370;
		private const int QualityRetainingSoilID = 371;
		private const int DeluxeRetainingSoilID = 920;

		internal static PredictableRetainingSoil Instance { get; set; }

		internal ModConfig Config { get; private set; }

		private bool isStayingWateredViaRetainingSoil = false;

		public override void Entry(IModHelper helper)
		{
			Instance = this;

			Config = helper.ReadConfig<ModConfig>();

			helper.Events.GameLoop.GameLaunched += OnGameLaunched;

			var harmony = new Harmony(ModManifest.UniqueID);
			try
			{
				harmony.Patch(
					original: AccessTools.Constructor(typeof(HoeDirt)),
					postfix: new HarmonyMethod(typeof(PredictableRetainingSoil), nameof(HoeDirt_ctor_Postfix))
				);
				harmony.Patch(
					original: AccessTools.Method(typeof(HoeDirt), nameof(HoeDirt.dayUpdate)),
					prefix: new HarmonyMethod(typeof(PredictableRetainingSoil), nameof(HoeDirt_dayUpdate_Prefix)),
					postfix: new HarmonyMethod(typeof(PredictableRetainingSoil), nameof(HoeDirt_dayUpdate_Postfix))
				);
				harmony.Patch(
					original: AccessTools.Method(typeof(HoeDirt), nameof(HoeDirt.plant)),
					postfix: new HarmonyMethod(typeof(PredictableRetainingSoil), nameof(HoeDirt_plant_Postfix))
				);
				harmony.Patch(
					original: AccessTools.Method(typeof(SObject), nameof(SObject.getDescription)),
					postfix: new HarmonyMethod(typeof(PredictableRetainingSoil), nameof(Object_getDescription_postfix))
				);
			}
			catch (Exception e)
			{
				Monitor.Log($"Could not patch methods - Predictable Retaining Soil probably won't work.\nReason: {e}", LogLevel.Error);
			}
		}

		private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			var sc = Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
			sc.RegisterCustomProperty(
				typeof(HoeDirt),
				"RetainingSoilDaysLeft",
				typeof(int),
				AccessTools.Method(typeof(HoeDirtExtensions), nameof(HoeDirtExtensions.GetRetainingSoilDaysLeft)),
				AccessTools.Method(typeof(HoeDirtExtensions), nameof(HoeDirtExtensions.SetRetainingSoilDaysLeft))
			);

			SetupConfig();
		}

		private void SetupConfig()
		{
			// TODO: add translation support
			var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

			configMenu.Register(
				ModManifest,
				reset: () => Config = new ModConfig(),
				save: () => Helper.WriteConfig(Config)
			);

			configMenu.AddSectionTitle(
				mod: ModManifest,
				text: () => "Days to retain water",
				tooltip: () => "0: will never retain water\n-1: will always retain water"
			);

			configMenu.AddNumberOption(
				mod: ModManifest,
				name: () => "Basic Retaining Soil",
				getValue: () => Config.BasicRetainingSoilDays,
				setValue: value => Config.BasicRetainingSoilDays = value,
				min: -1, interval: 1
			);

			configMenu.AddNumberOption(
				mod: ModManifest,
				name: () => "Quality Retaining Soil",
				getValue: () => Config.QualityRetainingSoilDays,
				setValue: value => Config.QualityRetainingSoilDays = value,
				min: -1, interval: 1
			);

			configMenu.AddNumberOption(
				mod: ModManifest,
				name: () => "Deluxe Retaining Soil",
				getValue: () => Config.DeluxeRetainingSoilDays,
				setValue: value => Config.DeluxeRetainingSoilDays = value,
				min: -1, interval: 1
			);
		}

		public int? GetRetainingSoilDays(int index)
		{
			// TODO: maybe add some API for other mods to add their own, but no idea what to do about config then (probably also make it part of the API)
			return index switch
			{
				BasicRetainingSoilID => Config.BasicRetainingSoilDays,
				QualityRetainingSoilID => Config.QualityRetainingSoilDays,
				DeluxeRetainingSoilID => Config.DeluxeRetainingSoilDays,
				_ => null,
			};
		}

		public bool IsRetainingSoil(int index)
		{
			return GetRetainingSoilDays(index) != null;
		}

		private static void HoeDirt_ctor_Postfix(HoeDirt __instance)
		{
			__instance.NetFields.AddFields(__instance.GetRetainingSoilDaysLeftNetField());
			__instance.state.fieldChangeVisibleEvent += (_, _, newValue) =>
			{
				if (newValue > 0 && !Instance.isStayingWateredViaRetainingSoil)
					__instance.RefreshRetainingSoilDaysLeft();
			};
		}

		private static void HoeDirt_dayUpdate_Prefix(HoeDirt __instance, ref int __state)
		{
			__state = __instance.state.Value;
		}

		private static void HoeDirt_dayUpdate_Postfix(HoeDirt __instance, ref int __state)
		{
			if (__instance.hasPaddyCrop())
				return;
			if (Instance.IsRetainingSoil(__instance.fertilizer.Value))
			{
				if (__instance.state.Value == 0)
				{
					Instance.isStayingWateredViaRetainingSoil = true;
					__instance.state.Value = __state;
					Instance.isStayingWateredViaRetainingSoil = false;
				}

				var retainingSoilDaysLeft = __instance.GetRetainingSoilDaysLeft();
				if (retainingSoilDaysLeft == -1)
					return;
				__instance.SetRetainingSoilDaysLeft(retainingSoilDaysLeft - 1);
				if (retainingSoilDaysLeft == 0)
					__instance.state.Value = 0;
			}
		}

		private static void HoeDirt_plant_Postfix(HoeDirt __instance, bool isFertilizer)
		{
			if (!isFertilizer)
				return;
			if (__instance.state.Value == 0)
				return;
			__instance.RefreshRetainingSoilDaysLeft();
		}

		private static void Object_getDescription_postfix(SObject __instance, ref string __result)
		{
			if (__instance.Category != SObject.fertilizerCategory)
				return;
			var retainingSoilDays = Instance.GetRetainingSoilDays(__instance.ParentSheetIndex);
			if (retainingSoilDays == null)
				return;

			// TODO: add translation support
			__result = retainingSoilDays.Value switch
			{
				-1 => "This soil will stay watered overnight.\nMix into tilled soil.",
				0 => "This soil will not stay watered overnight.\nMix into tilled soil.",
				1 => "This soil will stay watered overnight once.\nMix into tilled soil.",
				_ => $"This soil will stay watered overnight for {retainingSoilDays.Value} nights.\nMix into tilled soil.",
			};
		}
	}
}