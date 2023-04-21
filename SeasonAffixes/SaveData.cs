using StardewModdingAPI;
using System.Collections.Generic;

namespace Shockah.SeasonAffixes
{
	internal sealed class SerializedSaveData
	{
		public ISemanticVersion Version { get; set; }
		public List<string> ActiveAffixes { get; set; } = new();
		public List<List<string>> AffixChoiceHistory { get; set; } = new();
		public List<List<List<string>>> AffixSetChoiceHistory { get; set; } = new();

		public SerializedSaveData(ISemanticVersion version)
		{
			this.Version = version;
		}
	}

	internal sealed class SaveData
	{
		public ISet<ISeasonAffix> ActiveAffixes { get; init; } = new HashSet<ISeasonAffix>();
		public IList<ISet<ISeasonAffix>> AffixChoiceHistory { get; init; } = new List<ISet<ISeasonAffix>>();
		public IList<ISet<ISet<ISeasonAffix>>> AffixSetChoiceHistory { get; init; } = new List<ISet<ISet<ISeasonAffix>>>();
	}
}