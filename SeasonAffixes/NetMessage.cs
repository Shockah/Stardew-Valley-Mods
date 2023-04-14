using System.Collections.Generic;

namespace Shockah.SeasonAffixes
{
	internal static class NetMessage
	{
		public record UpdateAffixChoiceMenuConfig(
			OrdinalSeason Season,
			bool Incremental,
			IReadOnlyList<IReadOnlySet<string>> Choices,
			int RerollsLeft = 0
		);

		public record UpdateActiveAffixes(
			IReadOnlySet<string> Affixes
		);

		public record AffixSetChoice(
			IReadOnlySet<string> Affixes
		);

		public record RerollChoice;
	}
}