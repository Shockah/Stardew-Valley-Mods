using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using System.Collections.Generic;

namespace Shockah.CommonModCode.Stardew
{
	public static class GameExtensions
	{
		public static IEnumerable<GameLocation> GetAllLocations()
		{
			IEnumerable<GameLocation> GetLocationAndSublocations(GameLocation location)
			{
				yield return location;
				if (location is BuildableGameLocation buildable)
					foreach (Building building in buildable.buildings)
						if (building.indoors.Value is not null)
							foreach (GameLocation nested in GetLocationAndSublocations(building.indoors.Value))
								yield return nested;
			}

			foreach (GameLocation location in Game1.locations)
				foreach (GameLocation nested in GetLocationAndSublocations(location))
					yield return nested;
		}
	}
}
