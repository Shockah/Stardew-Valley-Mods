using HarmonyLib;
using Shockah.CommonModCode;
using StardewModdingAPI;
using StardewValley;
using System;
using SObject = StardewValley.Object;

namespace Shockah.MachineStatus
{
	public static class Patches
	{
		private static MachineStatus Instance => MachineStatus.Instance;
		
		internal static void Apply(Harmony harmony)
		{
			try
			{
				harmony.PatchVirtual(
					original: AccessTools.Method(typeof(SObject), nameof(SObject.DayUpdate)),
					postfix: new HarmonyMethod(typeof(Patches), nameof(Object_DayUpdate_Postfix))
				);
				harmony.PatchVirtual(
					original: AccessTools.Method(typeof(SObject), nameof(SObject.updateWhenCurrentLocation)),
					postfix: new HarmonyMethod(typeof(Patches), nameof(Object_updateWhenCurrentLocation_Postfix))
				);
				harmony.PatchVirtual(
					original: AccessTools.Method(typeof(SObject), nameof(SObject.checkForAction)),
					postfix: new HarmonyMethod(typeof(Patches), nameof(Object_checkForAction_Postfix))
				);
				harmony.PatchVirtual(
					original: AccessTools.Method(typeof(SObject), nameof(SObject.performObjectDropInAction)),
					postfix: new HarmonyMethod(typeof(Patches), nameof(Object_performObjectDropInAction_Postfix))
				);
				harmony.PatchVirtual(
					original: AccessTools.Method(typeof(SObject), nameof(SObject.performDropDownAction)),
					postfix: new HarmonyMethod(typeof(Patches), nameof(Object_performDropDownAction_Postfix))
				);
				harmony.PatchVirtual(
					original: AccessTools.Method(typeof(SObject), nameof(SObject.onReadyForHarvest)),
					postfix: new HarmonyMethod(typeof(Patches), nameof(Object_onReadyForHarvest_Postfix))
				);
				harmony.Patch(
					original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.passTimeForObjects)),
					postfix: new HarmonyMethod(typeof(Patches), nameof(GameLocation_passTimeForObjects_Postfix))
				);
			}
			catch (Exception e)
			{
				Instance.Monitor.Log($"Could not patch methods - Machine Status probably won't work.\nReason: {e}", LogLevel.Error);
			}
		}

		private static void Object_DayUpdate_Postfix(SObject __instance, GameLocation __0 /* location */)
		{
			Instance.UpdateMachineState(__0, __instance);
		}

		private static void Object_updateWhenCurrentLocation_Postfix(SObject __instance, GameLocation __1 /* environment */)
		{
			Instance.UpdateMachineState(__1, __instance);
		}

		private static void Object_checkForAction_Postfix(SObject __instance, Farmer __0 /* who */, bool __1 /* justCheckingForActivity */)
		{
			if (__1)
				return;
			Instance.UpdateMachineState(__0.currentLocation, __instance);
		}

		private static void Object_performObjectDropInAction_Postfix(SObject __instance, bool __0 /* probe */, Farmer __1 /* who */)
		{
			if (__0)
				return;
			Instance.UpdateMachineState(__1.currentLocation, __instance);
		}

		private static void Object_performDropDownAction_Postfix(SObject __instance, Farmer __0 /* who */)
		{
			Instance.UpdateMachineState(__0.currentLocation, __instance);
		}

		private static void Object_onReadyForHarvest_Postfix(SObject __instance, GameLocation __0 /* environment */)
		{
			Instance.UpdateMachineState(__0, __instance);
		}

		private static void GameLocation_passTimeForObjects_Postfix(GameLocation __instance)
		{
			foreach (var @object in __instance.Objects.Values)
				Instance.UpdateMachineState(__instance, @object);
		}
	}
}
