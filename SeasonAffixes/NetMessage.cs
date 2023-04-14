using System.Collections.Generic;

namespace Shockah.SeasonAffixes
{
	internal static class NetMessage
	{
		public record QueueOvernightAffixChoice;

        public record UpdateAffixChoiceMenuConfig(
			OrdinalSeason Season,
			bool Incremental,
			List<HashSet<string>> Choices,
			int RerollsLeft = 0
		);

		public record UpdateActiveAffixes(
            HashSet<string> Affixes
		);

		public record ConfirmAffixSetChoice(
            HashSet<string>? Affixes
		);

		public record AffixSetChoice(
            HashSet<string> Affixes
		);

		public record RerollChoice;
	}
}