using System.Collections.Generic;

namespace Shockah.AdventuresInTheMines.Config
{
	public sealed record BrazierCombinationConfig(
		bool Enabled,
		IList<BrazierCombinationConfigEntry> Entries
	);

	public sealed record BrazierCombinationConfigEntry(
		IList<MineLevelConditions> Conditions,
		double Weight,
		IList<BrazierCombinationConfigEntryWeightItem> BrazierCountWeightItems
	);

	public sealed record BrazierCombinationConfigEntryWeightItem(
		double Weight,
		int BrazierCount
	);
}