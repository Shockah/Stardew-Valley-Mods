using StardewValley.Locations;
using System;

namespace Shockah.AdventuresInTheMines.Populators
{
	internal interface IMineShaftPopulator
	{
		double Prepare(MineShaft location, Random random);
		void BeforePopulate(MineShaft location, Random random);
		void AfterPopulate(MineShaft location, Random random);
	}
}