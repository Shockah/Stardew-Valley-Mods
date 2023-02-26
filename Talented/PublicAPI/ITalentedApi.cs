using System.Collections.Generic;

namespace Shockah.Talented
{
	public interface ITalentedApi
	{
		IRequirementFactories RequirementFactories { get; }

		void RegisterTalentTag(ITalentTag tag);
		void UnregisterTalentTag(ITalentTag tag);

		void RegisterTalent(ITalent talent);
		void UnregisterTalent(ITalent talent);

		IReadOnlyList<ITalentTag> RootTalentTags { get; }
		IReadOnlyList<ITalentTag> AllTalentTags { get; }
		ITalentTag? GetTalentTag(string uniqueID);
		IReadOnlyList<ITalentTag> GetChildTalentTags(ITalentTag parent);
		IReadOnlyList<ITalent> GetTalents(ITalentTag tag);

		ITalent? GetTalent(string uniqueID);
		IReadOnlyList<ITalent> GetTalents();

		bool IsTalentActive(ITalent talent);
		IReadOnlyList<ITalent> GetActiveTalents();
		IReadOnlyDictionary<ITalentTag, int> GetEarnedTalentPoints();
		IReadOnlyDictionary<ITalentTag, int> GetSpentTalentPoints();
		IReadOnlyDictionary<ITalentTag, int> GetAvailableTalentPoints();

		bool ActivateTalent(ITalent talent);
		IReadOnlySet<ITalent> DeactivateTalent(ITalent talent);

		public interface IRequirementFactories
		{
			ITalentRequirements Talent(string talentUniqueID);
			ITalentRequirements Talent(ITalent talent);

			ITalentRequirements Tag(string tagUniqueID, int count);
			ITalentRequirements Tag(ITalentTag tag, int count);

			ITalentRequirements All(params ITalentRequirements[] requirements);
			ITalentRequirements All(IEnumerable<ITalentRequirements> requirements);

			ITalentRequirements Any(params ITalentRequirements[] requirements);
			ITalentRequirements Any(IEnumerable<ITalentRequirements> requirements);
		}
	}
}