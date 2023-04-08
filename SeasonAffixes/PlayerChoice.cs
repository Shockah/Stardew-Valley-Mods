using System.Collections.Generic;

namespace Shockah.SeasonAffixes
{
	internal record PlayerChoice
	{
		public record Choice(
			IReadOnlyList<ISeasonAffix> Affixes
		) : PlayerChoice;

		public record Reroll() : PlayerChoice;

		private PlayerChoice() { }
	}
}