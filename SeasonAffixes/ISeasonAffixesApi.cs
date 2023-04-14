using System;
using System.Collections.Generic;

namespace Shockah.SeasonAffixes
{
	public interface ISeasonAffixesApi
	{
		ModConfig Config { get; }

		IReadOnlyDictionary<string, ISeasonAffix> AllAffixes { get; }
		IReadOnlySet<ISeasonAffix> ActiveAffixes { get; }

		ISeasonAffix? GetAffix(string uniqueID);

		void RegisterAffix(ISeasonAffix affix);
		void UnregisterAffix(ISeasonAffix affix);
		void RegisterAffixConflictProvider(Func<IReadOnlySet<ISeasonAffix>, OrdinalSeason, bool> provider);

		void ActivateAffix(ISeasonAffix affix);
		void DeactivateAffix(ISeasonAffix affix);
		void DeactivateAllAffixes();

		IReadOnlyList<ISeasonAffix> GetUIOrderedAffixes(OrdinalSeason season, IEnumerable<ISeasonAffix> affixes);
		string GetSeasonName(IReadOnlyList<ISeasonAffix> affixes);
		string GetSeasonDescription(IReadOnlyList<ISeasonAffix> affixes);

        void QueueOvernightAffixChoice();

		IReadOnlySet<ISeasonAffix> GetAllPossibleAffixesForSeason(OrdinalSeason season);
	}
}