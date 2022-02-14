using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SObject = StardewValley.Object;

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
					original: AccessTools.Method(typeof(SObject), nameof(SObject.GetSprinklerTiles)),
					prefix: new HarmonyMethod(typeof(ObjectPatches), nameof(GetSprinklerTiles_Prefix)),
					postfix: new HarmonyMethod(typeof(ObjectPatches), nameof(GetSprinklerTiles_Postfix))
				);

				harmony.Patch(
					original: AccessTools.Method(typeof(SObject), nameof(SObject.placementAction)),
					prefix: new HarmonyMethod(typeof(ObjectPatches), nameof(placementAction_Prefix))
				);

				harmony.Patch(
					original: AccessTools.Method(typeof(SObject), nameof(SObject.IsInSprinklerRangeBroadphase)),
					prefix: new HarmonyMethod(typeof(ObjectPatches), nameof(IsInSprinklerRangeBroadphase_Prefix)),
					postfix: new HarmonyMethod(typeof(ObjectPatches), nameof(IsInSprinklerRangeBroadphase_Postfix))
				);

				harmony.Patch(
					original: AccessTools.Method(typeof(SObject), nameof(SObject.ApplySprinklerAnimation)),
					prefix: new HarmonyMethod(typeof(ObjectPatches), nameof(ApplySprinklerAnimation_Prefix))
				);

				foreach (var nestedType in typeof(SObject).GetTypeInfo().DeclaredNestedTypes)
				{
					if (!nestedType.DeclaredFields.Where(f => f.FieldType == typeof(SObject) && f.Name.EndsWith("__this")).Any())
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

				FlexibleSprinklers.Instance.Monitor.Log($"Could not patch base methods - FlexibleSprinklers probably won't work.\nReason: Cannot patch DayUpdate/PostFarmEventOvernightActions/Delegate.", LogLevel.Error);
				done:;
			}
			catch (Exception e)
			{
				FlexibleSprinklers.Instance.Monitor.Log($"Could not patch base methods - FlexibleSprinklers probably won't work.\nReason: {e}", LogLevel.Error);
			}
		}

		private static List<Vector2> GetSprinklerTiles_Result(SObject __instance)
		{
			if (CurrentLocation == null)
			{
				FlexibleSprinklers.Instance.Monitor.Log("Location should not be null - potential mod conflict.", LogLevel.Error);
				return new List<Vector2>();
			}

			return FlexibleSprinklers.Instance.SprinklerBehavior.GetSprinklerTiles(
				new GameLocationMap(CurrentLocation),
				new IntPoint((int)__instance.TileLocation.X, (int)__instance.TileLocation.Y),
				FlexibleSprinklers.Instance.GetSprinklerInfo(__instance)
			).Select(e => new Vector2(e.X, e.Y)).ToList();
		}

		private static bool GetSprinklerTiles_Prefix(SObject __instance, ref List<Vector2> __result)
		{
			if (IsVanillaQueryInProgress)
				return true;
			if (FlexibleSprinklers.Instance.Config.CompatibilityMode)
				return true;
			__result = GetSprinklerTiles_Result(__instance);
			return false;
		}

		private static void GetSprinklerTiles_Postfix(SObject __instance, ref List<Vector2> __result)
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

		private static bool IsInSprinklerRangeBoardphase_Result(Vector2 target)
		{
			if (CurrentLocation == null)
			{
				FlexibleSprinklers.Instance.Monitor.Log("Location should not be null - potential mod conflict.", LogLevel.Error);
				return true;
			}

			var wasVanillaQueryInProgress = IsVanillaQueryInProgress;
			IsVanillaQueryInProgress = true;
			var result = FlexibleSprinklers.Instance.IsTileInRangeOfSprinklers(CurrentLocation.Objects.Values.Where(o => o.IsSprinkler()), CurrentLocation, target);
			IsVanillaQueryInProgress = wasVanillaQueryInProgress;
			return result;
		}

		private static bool IsInSprinklerRangeBroadphase_Prefix(Vector2 target, ref bool __result)
		{
			if (FlexibleSprinklers.Instance.Config.CompatibilityMode)
				return true;
			__result = IsInSprinklerRangeBoardphase_Result(target);
			return false;
		}

		private static void IsInSprinklerRangeBroadphase_Postfix(Vector2 target, ref bool __result)
		{
			if (!FlexibleSprinklers.Instance.Config.CompatibilityMode)
				return;
			__result = IsInSprinklerRangeBoardphase_Result(target);
		}

		private static void ApplySprinklerAnimation_Prefix(SObject __instance, GameLocation location)
		{
			// remove all temporary sprites related to this sprinkler
			location.TemporarySprites.RemoveAll(sprite => sprite.id == __instance.TileLocation.X * 4000f + __instance.TileLocation.Y);
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