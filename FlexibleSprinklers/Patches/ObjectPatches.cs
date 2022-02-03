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
					prefix: new HarmonyMethod(typeof(ObjectPatches), nameof(GetSprinklerTiles_Prefix))
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

		private static bool GetSprinklerTiles_Prefix(Object __instance, ref List<Vector2> __result)
		{
			if (IsVanillaQueryInProgress)
				return true;
			
			var currentLocation = ObjectPatches.CurrentLocation ?? throw new System.InvalidOperationException("Location should not be null - potential mod conflict.");
			
			__result = FlexibleSprinklers.Instance.SprinklerBehavior.GetSprinklerTiles(
				new GameLocationMap(currentLocation),
				new IntPoint((int)__instance.TileLocation.X, (int)__instance.TileLocation.Y),
				FlexibleSprinklers.Instance.GetSprinklerInfo(__instance)
			).Select(e => new Vector2(e.X, e.Y)).ToList();

			return false;
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