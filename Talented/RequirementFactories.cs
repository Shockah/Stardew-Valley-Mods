using System.Collections.Generic;
using System.Linq;

namespace Shockah.Talented
{
	internal class RequirementFactories : ITalentedApi.IRequirementFactories
	{
		public ITalentRequirements All(params ITalentRequirements[] requirements)
			=> All((IEnumerable<ITalentRequirements>)requirements);

		public ITalentRequirements All(IEnumerable<ITalentRequirements> requirements)
			=> new AndTalentRequirements(requirements.ToList());

		public ITalentRequirements Any(params ITalentRequirements[] requirements)
			=> Any((IEnumerable<ITalentRequirements>)requirements);

		public ITalentRequirements Any(IEnumerable<ITalentRequirements> requirements)
			=> new OrTalentRequirements(requirements.ToList());

		public ITalentRequirements Tag(string tagUniqueID, int count)
			=> Tag(Talented.Instance.GetTalentTag(tagUniqueID)!, count);

		public ITalentRequirements Tag(ITalentTag tag, int count)
			=> new TagCountTalentRequirements(tag, count);

		public ITalentRequirements Talent(string talentUniqueID)
			=> Talent(Talented.Instance.GetTalent(talentUniqueID)!);

		public ITalentRequirements Talent(ITalent talent)
			=> new SpecificTalentTalentRequirements(talent);
	}
}
