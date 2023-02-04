using System.Collections.Generic;

namespace Shockah.AdventuresInTheMines.Config
{
	public sealed record BrazierLightUpConfig(
		bool Enabled,
		IList<MineLevelConditionedConfig<BrazierLightUpConfigEntry>> Entries
	);

	public sealed record BrazierLightUpConfigEntry(
		double Weight,
		int MinTorchCount,
		int MaxTorchCount,
		int MinInitialToggleCount,
		int MaxInitialToggleCount
	);
}