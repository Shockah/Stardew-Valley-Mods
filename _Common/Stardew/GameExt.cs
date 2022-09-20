using StardewValley;
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

		public static IReadOnlyList<GameLocation> GetAllLocations()
		{
			List<GameLocation> locations = new();
			Utility.ForAllLocations(l => locations.Add(l));
			return locations;
		}
	}
}
