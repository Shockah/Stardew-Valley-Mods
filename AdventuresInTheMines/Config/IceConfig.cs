using System.Collections.Generic;

namespace Shockah.AdventuresInTheMines.Config
{
	public sealed record IceConfig(
		bool Enabled,
		IList<MineLevelConditionedConfig<IceConfigEntry>> Entries
	);

	public sealed record IceConfigEntry(
		double Weight
	);
}