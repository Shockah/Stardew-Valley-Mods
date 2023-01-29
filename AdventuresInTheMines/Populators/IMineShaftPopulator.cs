using StardewValley.Locations;
using StardewValley.Objects;
using System;

namespace Shockah.AdventuresInTheMines.Populators
{
	internal interface IMineShaftPopulator
	{
		double Prepare(MineShaft location, Random random);
		void BeforePopulate(MineShaft location, Random random);
		void AfterPopulate(MineShaft location, Random random);
		void OnUpdateTicking(MineShaft location) { }
		void OnUpdateTicked(MineShaft location) { }

		bool HandleChestOpen(MineShaft location, Chest chest) => false;
	}
}