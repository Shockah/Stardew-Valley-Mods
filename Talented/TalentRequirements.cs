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
		int Count
	) : ITalentRequirements
	{
		public bool AreSatisifed(IEnumerable<ITalent> talents)
			=> talents.Count(t => t.Matches(Tag)) >= Count;
	}

	internal record SpecificTalentTalentRequirements(
		ITalent Talent
	) : ITalentRequirements
	{
		public bool AreSatisifed(IEnumerable<ITalent> talents)
			=> talents.Contains(Talent);
	}
}