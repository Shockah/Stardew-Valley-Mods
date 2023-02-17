using StardewValley;
using StardewValley.Locations;
using System.Linq;

namespace Shockah.Kokoro.Stardew
{
	public static class FarmHouseExt
	{
		public static Cellar? GetCellar(this FarmHouse farmhouse)
			=> Game1.getLocationFromName(farmhouse.GetCellarName()) as Cellar;
	}

	public static class CellarExt
	{
		public static FarmHouse? GetFarmHouse(this Cellar cellar)
			=> GameExt.GetAllLocations().OfType<FarmHouse>().FirstOrDefault(fh => fh.GetCellarName() == cellar.Name);
	}
}