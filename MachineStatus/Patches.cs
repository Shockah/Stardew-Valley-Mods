using HarmonyLib;
using Microsoft.Xna.Framework;
using Shockah.CommonModCode;
using StardewModdingAPI;
using StardewValley;
using System;
using xTile.Dimensions;
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
					postfix: new HarmonyMethod(typeof(Patches), nameof(Object_DayUpdate_Postfix)),
					monitor: Instance.Monitor
				);
				harmony.PatchVirtual(
					original: AccessTools.Method(typeof(SObject), nameof(SObject.checkForAction)),
					postfix: new HarmonyMethod(typeof(Patches), nameof(Object_checkForAction_Postfix)),
					monitor: Instance.Monitor
				);
				harmony.PatchVirtual(
					original: AccessTools.Method(typeof(SObject), nameof(SObject.performObjectDropInAction)),
					postfix: new HarmonyMethod(typeof(Patches), nameof(Object_performObjectDropInAction_Postfix)),
					monitor: Instance.Monitor
				);
				harmony.PatchVirtual(
					original: AccessTools.Method(typeof(SObject), nameof(SObject.performDropDownAction)),
					postfix: new HarmonyMethod(typeof(Patches), nameof(Object_performDropDownAction_Postfix)),
					monitor: Instance.Monitor
				);
				harmony.PatchVirtual(
					original: AccessTools.Method(typeof(SObject), nameof(SObject.onReadyForHarvest)),
					postfix: new HarmonyMethod(typeof(Patches), nameof(Object_onReadyForHarvest_Postfix)),
					monitor: Instance.Monitor
				);
				harmony.PatchVirtual(
					original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.checkAction)),
					postfix: new HarmonyMethod(typeof(Patches), nameof(GameLocation_checkAction_Postfix)),
					monitor: Instance.Monitor
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

		private static void Object_checkForAction_Postfix(SObject __instance, Farmer __0 /* who */, bool __1 /* justCheckingForActivity */)
		{
			if (__1)
				return;
			Instance.UpdateMachineState(__0.currentLocation, __instance);
		}

		private static void Object_performObjectDropInAction_Postfix(SObject __instance, bool __1 /* probe */, Farmer __2 /* who */)
		{
			if (__1)
				return;
			Instance.UpdateMachineState(__2.currentLocation, __instance);
		}

		private static void Object_performDropDownAction_Postfix(SObject __instance, Farmer __0 /* who */)
		{
			Instance.UpdateMachineState(__0.currentLocation, __instance);
		}

		private static void Object_onReadyForHarvest_Postfix(SObject __instance, GameLocation __0 /* environment */)
		{
			Instance.UpdateMachineState(__0, __instance);
		}

		private static void GameLocation_checkAction_Postfix(GameLocation __instance, Location __0 /* tileLocation */)
		{
			Vector2 key = new Vector2(__0.X, __0.Y);
			if (__instance.Objects.TryGetValue(key, out var @object))
				Instance.UpdateMachineState(__instance, @object);
		}

		private static void GameLocation_passTimeForObjects_Postfix(GameLocation __instance)
		{
			foreach (var @object in __instance.Objects.Values)
				Instance.UpdateMachineState(__instance, @object);
		}
	}
}
