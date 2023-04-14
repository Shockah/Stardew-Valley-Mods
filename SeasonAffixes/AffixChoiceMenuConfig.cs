using System.Collections.Generic;

namespace Shockah.SeasonAffixes
{
	internal record AffixChoiceMenuConfig(
		OrdinalSeason Season,
		bool Incremental,
		IReadOnlyList<IReadOnlySet<ISeasonAffix>> Choices,
		int RerollsLeft
	)
	{
		public AffixChoiceMenuConfig WithChoices(IReadOnlyList<IReadOnlySet<ISeasonAffix>> choices)
			=> new(Season, Incremental, choices, RerollsLeft);

		public AffixChoiceMenuConfig WithRerollsLeft(int rerollsLeft)
			=> new(Season, Incremental, Choices, rerollsLeft);
	};
}