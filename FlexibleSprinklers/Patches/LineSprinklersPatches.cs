using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using System;
using System.Collections.Generic;
using SObject = StardewValley.Object;

namespace Shockah.FlexibleSprinklers
{
	internal static class LineSprinklersPatches
	{
		private static readonly string LineSprinklersModEntryQualifiedName = "LineSprinklers.ModEntry, LineSprinklers";

		private static bool IsDuringDayStarted = false;

		internal static void Apply(Harmony harmony)
		{
			try
			{
				harmony.Patch(
					original: AccessTools.Method(Type.GetType(LineSprinklersModEntryQualifiedName), "OnDayStarted"),
					prefix: new HarmonyMethod(typeof(LineSprinklersPatches), nameof(OnDayStarted_Prefix)),
					postfix: new HarmonyMethod(typeof(LineSprinklersPatches), nameof(OnDayStarted_Postfix))
				);

				harmony.Patch(
					original: AccessTools.Method(Type.GetType(LineSprinklersModEntryQualifiedName), "GetLocations"),
					prefix: new HarmonyMethod(typeof(LineSprinklersPatches), nameof(GetLocations_Prefix))
				);

				harmony.Patch(
					original: AccessTools.Method(typeof(SObject), nameof(SObject.IsSprinkler)),
					prefix: new HarmonyMethod(typeof(LineSprinklersPatches), nameof(Object_IsSprinkler_Prefix))
				);

				harmony.Patch(
					original: AccessTools.Method(typeof(SObject), nameof(SObject.GetModifiedRadiusForSprinkler)),
					prefix: new HarmonyMethod(typeof(LineSprinklersPatches), nameof(Object_GetModifiedRadiusForSprinkler_Prefix))
				);
			}
			catch (Exception e)
			{
				FlexibleSprinklers.Instance.Monitor.Log($"Could not patch LineSprinklers - they probably won't work.\nReason: {e}", StardewModdingAPI.LogLevel.Warn);
			}
		}

		internal static bool OnDayStarted_Prefix()
		{
			IsDuringDayStarted = true;
			return true;
		}

		internal static void OnDayStarted_Postfix()
		{
			IsDuringDayStarted = false;
		}

		internal static bool GetLocations_Prefix(ref IEnumerable<GameLocation> __result)
		{
			if (IsDuringDayStarted)
			{
				__result = new List<GameLocation>();
				return false;
			}
			else
			{
				return true;
			}
		}

		internal static bool Object_IsSprinkler_Prefix(SObject __instance, ref bool __result)
		{
			if (FlexibleSprinklers.Instance.LineSprinklersApi.GetSprinklerCoverage().ContainsKey(__instance.ParentSheetIndex))
			{
				__result = true;
				return false;
			}
			else
			{
				return true;
			}
		}

		internal static bool Object_GetModifiedRadiusForSprinkler_Prefix(SObject __instance, ref int __result)
		{
			if (FlexibleSprinklers.Instance.LineSprinklersApi.GetSprinklerCoverage().TryGetValue(__instance.ParentSheetIndex, out Vector2[] tilePositions))
			{
				__result = (int)Math.Sqrt(tilePositions.Length / 2) - 1;
				return false;
			}
			else
			{
				return true;
			}
		}
	}
}
