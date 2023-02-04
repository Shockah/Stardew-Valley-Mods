using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using System;
using System.Linq;
using System.Reflection;

namespace Shockah.Kokoro.Stardew
{
	public static class GameLocationExt
	{
		public static void RemoveAllPlaceables(this GameLocation location, IntPoint point)
		{
			Vector2 vector = new(point.X, point.Y);

			if (location is MineShaft shaft)
			{
				if (location.Objects.TryGetValue(vector, out var @object))
				{
					if (@object.Name.Equals("Stone"))
						shaft.SetStonesOnThisLevel(shaft.GetStonesOnThisLevel() - 1);
					location.Objects.Remove(vector);
				}
			}
			else
			{
				location.Objects.Remove(vector);
			}

			location.overlayObjects.Remove(vector);
			location.terrainFeatures.Remove(vector);

			var resourceClumpsToRemove = location.resourceClumps.Where(e => e.occupiesTile(point.X, point.Y)).ToList();
			foreach (var resourceClump in resourceClumpsToRemove)
				location.resourceClumps.Remove(resourceClump);

			var largeTerrainFeatureToRemove = location.getLargeTerrainFeatureAt(point.X, point.Y);
			if (largeTerrainFeatureToRemove is not null)
				location.largeTerrainFeatures.Remove(largeTerrainFeatureToRemove);
		}
	}

	public static class MineShaftExt
	{
		private static readonly Lazy<PropertyInfo> StonesOnThisLevelProperty = new(() => AccessTools.Property(typeof(MineShaft), "stonesLeftOnThisLevel"));
		private static readonly Lazy<Func<MineShaft, int>> StonesLeftOnThisLevelGetter = new(() => StonesOnThisLevelProperty.Value.EmitInstanceGetter<MineShaft, int>());
		private static readonly Lazy<Action<MineShaft, int>> StonesLeftOnThisLevelSetter = new(() => StonesOnThisLevelProperty.Value.EmitInstanceSetter<MineShaft, int>());

		private static readonly Lazy<Func<MineShaft, bool>> IsMonsterAreaGetter = new(() => AccessTools.Property(typeof(MineShaft), "isMonsterArea").EmitInstanceGetter<MineShaft, bool>());
		private static readonly Lazy<Func<MineShaft, bool>> IsDinoAreaGetter = new(() => AccessTools.Property(typeof(MineShaft), "isDinoArea").EmitInstanceGetter<MineShaft, bool>());

		public static int GetStonesOnThisLevel(this MineShaft location)
			=> StonesLeftOnThisLevelGetter.Value(location);

		public static void SetStonesOnThisLevel(this MineShaft location, int value)
			=> StonesLeftOnThisLevelSetter.Value(location, value);

		public static bool IsMonsterArea(this MineShaft location)
			=> IsMonsterAreaGetter.Value(location);

		public static bool IsDinoArea(this MineShaft location)
			=> IsDinoAreaGetter.Value(location);
	}
}