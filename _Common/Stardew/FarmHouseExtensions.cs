using StardewValley;
using StardewValley.Locations;
using System.Linq;

namespace Shockah.CommonModCode.Stardew
{
	public static class FarmHouseExtensions
	{
		public static Cellar? GetCellar(this FarmHouse farmhouse)
			=> Game1.getLocationFromName(farmhouse.GetCellarName()) as Cellar;
	}

	public static class CellarExtensions
	{
		public static FarmHouse? GetFarmHouse(this Cellar cellar)
			=> GameExt.GetAllLocations().OfType<FarmHouse>().FirstOrDefault(fh => fh.GetCellarName() == cellar.Name);
	}
}