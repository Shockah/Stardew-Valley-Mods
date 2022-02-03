using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.FlexibleSprinklers
{
	internal static class ObjectPatches
	{
		internal static bool IsVanillaQueryInProgress = false;
		internal static GameLocation CurrentLocation;

		internal static void Apply(Harmony harmony)
		{
			try
			{
				harmony.Patch(
					original: AccessTools.Method(typeof(Object), nameof(Object.GetSprinklerTiles)),
					prefix: new HarmonyMethod(typeof(ObjectPatches), nameof(GetSprinklerTiles_Prefix)),
					postfix: new HarmonyMethod(typeof(ObjectPatches), nameof(GetSprinklerTiles_Postfix))
				);

				harmony.Patch(
					original: AccessTools.Method(typeof(Object), nameof(Object.placementAction)),
					prefix: new HarmonyMethod(typeof(ObjectPatches), nameof(placementAction_Prefix))
				);

				harmony.Patch(
					original: AccessTools.Method(typeof(Object), nameof(Object.IsInSprinklerRangeBroadphase)),
					prefix: new HarmonyMethod(typeof(ObjectPatches), nameof(IsInSprinklerRangeBroadphase_Prefix)),
					postfix: new HarmonyMethod(typeof(ObjectPatches), nameof(IsInSprinklerRangeBroadphase_Postfix))
				);

				foreach (var nestedType in typeof(Object).GetTypeInfo().DeclaredNestedTypes)
				{
					if (!nestedType.DeclaredFields.Where(f => f.FieldType == typeof(Object) && f.Name.EndsWith("__this")).Any())
						continue;
					if (!nestedType.DeclaredFields.Where(f => f.FieldType == typeof(GameLocation) && f.Name == "location").Any())
						continue;

					foreach (var method in nestedType.DeclaredMethods)
					{
						if (!method.Name.StartsWith("<DayUpdate>"))
							continue;

						harmony.Patch(
							original: method,
							prefix: new HarmonyMethod(typeof(ObjectPatches), nameof(DayUpdatePostFarmEventOvernightActionsDelegate_Prefix))
						);
						goto done;
					}
				}

				FlexibleSprinklers.Instance.Monitor.Log($"Could not patch base methods - FlexibleSprinklers probably won't work.\nReason: Cannot patch DayUpdate/PostFarmEventOvernightActions/Delegate.", StardewModdingAPI.LogLevel.Error);
				done:;
			}
			catch (System.Exception e)
			{
				FlexibleSprinklers.Instance.Monitor.Log($"Could not patch base methods - FlexibleSprinklers probably won't work.\nReason: {e}", StardewModdingAPI.LogLevel.Error);
			}
		}

		private static List<Vector2> GetSprinklerTiles_Result(Object __instance)
		{
			if (CurrentLocation == null)
			{
				FlexibleSprinklers.Instance.Monitor.Log("Location should not be null - potential mod conflict.", StardewModdingAPI.LogLevel.Error);
				return new List<Vector2>();
			}

			return FlexibleSprinklers.Instance.SprinklerBehavior.GetSprinklerTiles(
				new GameLocationMap(CurrentLocation),
				new IntPoint((int)__instance.TileLocation.X, (int)__instance.TileLocation.Y),
				FlexibleSprinklers.Instance.GetSprinklerInfo(__instance)
			).Select(e => new Vector2(e.X, e.Y)).ToList();
		}

		private static bool GetSprinklerTiles_Prefix(Object __instance, ref List<Vector2> __result)
		{
			if (IsVanillaQueryInProgress)
				return true;
			if (FlexibleSprinklers.Instance.Config.CompatibilityMode)
				return true;
			__result = GetSprinklerTiles_Result(__instance);
			return false;
		}

		private static void GetSprinklerTiles_Postfix(Object __instance, ref List<Vector2> __result)
		{
			if (IsVanillaQueryInProgress)
				return;
			if (!FlexibleSprinklers.Instance.Config.CompatibilityMode)
				return;
			__result = GetSprinklerTiles_Result(__instance);
		}

		private static bool placementAction_Prefix(GameLocation location, int x, int y, Farmer who)
		{
			CurrentLocation = location;
			return true;
		}

		private static bool IsInSprinklerRangeBoardphase_Result(Object instance, Vector2 target)
		{
			if (CurrentLocation == null)
			{
				FlexibleSprinklers.Instance.Monitor.Log("Location should not be null - potential mod conflict.", StardewModdingAPI.LogLevel.Error);
				return true;
			}

			var wasVanillaQueryInProgress = ObjectPatches.IsVanillaQueryInProgress;
			ObjectPatches.IsVanillaQueryInProgress = true;

			var sortedSprinklers = CurrentLocation.Objects.Values
				.Where(o => o.IsSprinkler())
				.OrderBy(s => (target - s.TileLocation).Length() * FlexibleSprinklers.Instance.GetSprinklerInfo(s).Power);

			ObjectPatches.IsVanillaQueryInProgress = wasVanillaQueryInProgress;

			foreach (var sprinkler in sortedSprinklers)
			{
				if (FlexibleSprinklers.Instance.GetModifiedSprinklerCoverage(sprinkler, CurrentLocation).Contains(target))
					return true;
			}
			return false;
		}

		private static bool IsInSprinklerRangeBroadphase_Prefix(Object __instance, Vector2 target, ref bool __result)
		{
			if (FlexibleSprinklers.Instance.Config.CompatibilityMode)
				return true;
			__result = IsInSprinklerRangeBoardphase_Result(__instance, target);
			return false;
		}

		private static void IsInSprinklerRangeBroadphase_Postfix(Object __instance, Vector2 target, ref bool __result)
		{
			if (!FlexibleSprinklers.Instance.Config.CompatibilityMode)
				return;
			__result = IsInSprinklerRangeBoardphase_Result(__instance, target);
		}

		private static bool DayUpdatePostFarmEventOvernightActionsDelegate_Prefix(object __instance)
		{
			var locationField = __instance.GetType().GetTypeInfo().DeclaredFields.First(f => f.FieldType == typeof(GameLocation) && f.Name == "location");
			CurrentLocation = (GameLocation)locationField.GetValue(__instance);
			IsVanillaQueryInProgress = false;
			return true;
		}
	}
}