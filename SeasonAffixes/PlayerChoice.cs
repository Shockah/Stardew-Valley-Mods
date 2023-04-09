using System.Collections.Generic;

namespace Shockah.SeasonAffixes
{
	internal record PlayerChoice
	{
		public record Choice(
			IReadOnlySet<ISeasonAffix> Affixes
		) : PlayerChoice;

		public record Reroll : PlayerChoice;

		public record Invalid : PlayerChoice;

		private PlayerChoice() { }
	}
}