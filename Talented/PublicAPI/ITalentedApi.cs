using System.Collections.Generic;

namespace Shockah.Talented
{
	public interface ITalentedApi
	{
		IRequirementFactories Factories { get; }

		public interface IRequirementFactories
		{
			ITalentRequirements Talent(string uniqueID, int minimumRank = 1);
			ITalentRequirements Talent(ITalentDefinition definition, int minimumRank = 1);

			ITalentRequirements Tag(string tag, int count, bool countEachRank);

			ITalentRequirements All(params ITalentRequirements[] requirements);
			ITalentRequirements All(IEnumerable<ITalentRequirements> requirements);

			ITalentRequirements Any(params ITalentRequirements[] requirements);
			ITalentRequirements Any(IEnumerable<ITalentRequirements> requirements);
		}
	}
}