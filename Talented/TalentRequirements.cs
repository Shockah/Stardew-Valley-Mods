using System.Collections.Generic;
using System.Linq;

namespace Shockah.Talented
{
	internal record AndTalentRequirements(
		IReadOnlyList<ITalentRequirements> Requirements
	) : ITalentRequirements
	{
		public bool AreSatisifed(IEnumerable<ITalent> talents)
			=> Requirements.All(r => r.AreSatisifed(talents));
	}

	internal record OrTalentRequirements(
		IReadOnlyList<ITalentRequirements> Requirements
	) : ITalentRequirements
	{
		public bool AreSatisifed(IEnumerable<ITalent> talents)
			=> Requirements.Any(r => r.AreSatisifed(talents));
	}

	internal record TagCountTalentRequirements(
		string Tag,
		int Count,
		bool CountEachRank
	) : ITalentRequirements
	{
		public bool AreSatisifed(IEnumerable<ITalent> talents)
			=> talents.Where(t => t.Definition.Tags.Contains(Tag)).Select(t => CountEachRank ? t.Rank : 1).Sum() >= Count;
	}

	internal record SpecificTalentTalentRequirements(
		string UniqueID,
		int MinimumRank
	) : ITalentRequirements
	{
		public bool AreSatisifed(IEnumerable<ITalent> talents)
			=> talents.Any(t => t.Definition.UniqueID == UniqueID && t.Rank >= MinimumRank);
	}
}