using StardewValley;
using System.Linq;

namespace Shockah.CommonModCode.Stardew
{
	public static class GameLocationExt
	{
		public static void RemoveAllPlaceables(this GameLocation location, IntPoint point)
		{
			location.Objects.Remove(new(point.X, point.Y));

			var resourceClumpsToRemove = location.resourceClumps.Where(e => e.occupiesTile(point.X, point.Y)).ToList();
			foreach (var resourceClump in resourceClumpsToRemove)
				location.resourceClumps.Remove(resourceClump);

			var largeTerrainFeatureToRemove = location.getLargeTerrainFeatureAt(point.X, point.Y);
			if (largeTerrainFeatureToRemove is not null)
				location.largeTerrainFeatures.Remove(largeTerrainFeatureToRemove);
		}
	}
}