using StardewValley.Locations;

namespace Shockah.AdventuresInTheMines.Populators
{
	internal interface IMineShaftPopulator
	{
		bool BeforePopulate(MineShaft location) => false;
		void AfterPopulate(MineShaft location) { }
	}
}