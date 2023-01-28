using StardewValley.Locations;

namespace Shockah.AdventuresInTheMines.Populators
{
	internal interface IMineShaftPopulator
	{
		void BeforePopulate(MineShaft location) { }
		void AfterPopulate(MineShaft location) { }
	}
}