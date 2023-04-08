using System.Collections.Generic;

namespace Shockah.SeasonAffixes
{
	internal record SerializedSaveData(
		IReadOnlyList<string> ActiveAffixes,
		IReadOnlyList<IReadOnlyList<string>> AffixChoiceHistory,
		IReadOnlyList<IReadOnlyList<IReadOnlyList<string>>> AffixSetChoiceHistory
	);

	internal sealed class SaveData
	{
		public ISet<ISeasonAffix> ActiveAffixes { get; init; } = new HashSet<ISeasonAffix>();
		public IList<ISet<ISeasonAffix>> AffixChoiceHistory { get; init; } = new List<ISet<ISeasonAffix>>();
		public IList<ISet<ISet<ISeasonAffix>>> AffixSetChoiceHistory { get; init; } = new List<ISet<ISet<ISeasonAffix>>>();
	}
}