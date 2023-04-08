using System.Collections.Generic;

namespace Shockah.SeasonAffixes
{
	internal record AffixChoiceMenuConfig(
		OrdinalSeason Season,
		bool Incremental,
		IReadOnlyList<IReadOnlySet<ISeasonAffix>>? Choices,
		int RerollsLeft = 0
	);
}