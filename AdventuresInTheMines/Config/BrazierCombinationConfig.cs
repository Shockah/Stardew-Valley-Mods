using System.Collections.Generic;

namespace Shockah.AdventuresInTheMines.Config
{
	public sealed record BrazierCombinationConfig(
		bool Enabled,
		IList<MineLevelConditionedConfig<BrazierCombinationConfigEntry>> Entries
	);

	public sealed record BrazierCombinationConfigEntry(
		double Weight,
		IList<BrazierCombinationConfigEntryWeightItem> BrazierCountWeightItems
	);

	public sealed record BrazierCombinationConfigEntryWeightItem(
		double Weight,
		int BrazierCount
	);
}