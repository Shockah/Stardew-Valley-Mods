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
		ITalentTag Tag,
		int Count,
		bool CountEachRank
	) : ITalentRequirements
	{
		public bool AreSatisifed(IEnumerable<ITalent> talents)
		{
			bool Matches(ITalentTag tag)
			{
				if (tag == Tag)
					return true;
				if (tag.Parent is not null)
					return Matches(tag.Parent);
				return false;
			}

			return talents.Where(t => t.Definition.Tags.Any(tag => Matches(tag))).Select(t => CountEachRank ? t.Rank : 1).Sum() >= Count;
		}
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