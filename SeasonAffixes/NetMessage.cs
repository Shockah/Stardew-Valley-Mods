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

		public record Broadcast<T>(
			long PlayerID,
			T Message
		);

		public record AffixSetChoice(
			IReadOnlySet<string> Affixes
		);

		public record RerollChoice;
	}
}