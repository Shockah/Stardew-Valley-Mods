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
        internal static GameLocation currentLocation;

        internal static void Apply(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Object), nameof(Object.GetSprinklerTiles)),
                prefix: new HarmonyMethod(typeof(ObjectPatches), nameof(GetSprinklerTiles_Prefix))
            );
            harmony.Patch(
               original: AccessTools.Method(typeof(Object), nameof(Object.ApplySprinklerAnimation)),
               prefix: new HarmonyMethod(typeof(ObjectPatches), nameof(ApplySprinklerAnimation_Prefix))
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

            // TODO: log error, no matching type (game code changed?)
            done:;
        }

        private static bool GetSprinklerTiles_Prefix(Object __instance, ref List<Vector2> __result)
        {
            var currentLocation = ObjectPatches.currentLocation ?? throw new System.InvalidOperationException("Location should not be null - potential mod conflict.");
            
            __result = FlexibleSprinklers.Instance.SprinklerBehavior.GetSprinklerTiles(
                new GameLocationMap(currentLocation),
                new IntPoint((int)__instance.TileLocation.X, (int)__instance.TileLocation.Y),
                FlexibleSprinklers.Instance.GetSprinklerInfo(__instance)
            ).Select(e => new Vector2(e.X, e.Y)).ToList();

            return false;
        }

        private static bool ApplySprinklerAnimation_Prefix(Object __instance, GameLocation location)
        {
            // TODO: re-implement animation
            return true;
        }

        private static bool DayUpdatePostFarmEventOvernightActionsDelegate_Prefix(object __instance)
        {
            var locationField = __instance.GetType().GetTypeInfo().DeclaredFields.First(f => f.FieldType == typeof(GameLocation) && f.Name == "location");
            currentLocation = (GameLocation?)locationField.GetValue(__instance);
            return true;
        }
    }
}