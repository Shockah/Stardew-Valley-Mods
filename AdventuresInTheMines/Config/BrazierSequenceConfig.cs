using System.Collections.Generic;

namespace Shockah.AdventuresInTheMines.Config
{
	public sealed record BrazierSequenceConfig(
		bool Enabled,
		IList<MineLevelConditionedConfig<BrazierSequenceConfigEntry>> Entries
	);

	public sealed record BrazierSequenceConfigEntry(
		double Weight,
		IList<BrazierSequenceConfigEntryWeightItem> BrazierCountWeightItems
	);

	public sealed record BrazierSequenceConfigEntryWeightItem(
		double Weight,
		int BrazierCount
	);
}