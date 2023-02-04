using System.Collections.Generic;

namespace Shockah.AdventuresInTheMines.Config
{
	public sealed record DisarmableConfig(
		bool Enabled,
		IList<MineLevelConditionedConfig<DisarmableConfigEntry>> Entries
	);

	public sealed record DisarmableConfigEntry(
		double Weight,
		int MinButtonCount,
		int MaxButtonCount,
		IList<DisarmableConfigEntryWeightItem> WeightItems
	);

	public sealed record DisarmableConfigEntryWeightItem(
		double Weight,
		DisarmableConfigEntryWeightItemExplosion? Explosion = null,
		bool Rot = false
	);

	public sealed record DisarmableConfigEntryWeightItemExplosion(
		int Radius,
		int Damage
	);
}