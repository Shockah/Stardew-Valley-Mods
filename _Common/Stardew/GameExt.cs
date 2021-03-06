using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.CommonModCode.Stardew
{
	public enum MultiplayerMode { SinglePlayer, Client, Server }
	
	public static class GameExt
	{
		public static MultiplayerMode GetMultiplayerMode()
			=> (MultiplayerMode)Game1.multiplayerMode;

		public static Farmer GetHostPlayer()
			=> Game1.getAllFarmers().First(p => p.slotCanHost);

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
