using Shockah.Kokoro.UI;
using System;
using System.Collections.Generic;

namespace Shockah.SeasonAffixes
{
	public interface ISeasonAffixesApi
	{
		ModConfig Config { get; }

		IReadOnlyDictionary<string, ISeasonAffix> AllAffixes { get; }
		IEnumerable<(ISeasonAffix Combined, IReadOnlySet<ISeasonAffix> Affixes)> AffixCombinations { get; }
		IReadOnlySet<ISeasonAffix> ActiveAffixes { get; }

		ISeasonAffix? GetAffix(string uniqueID);

		void RegisterAffix(ISeasonAffix affix);
		void UnregisterAffix(ISeasonAffix affix);
		void RegisterVisualAffixCombination(IReadOnlySet<ISeasonAffix> affixes, Func<string> localizedName, Func<string> localizedDescription, Func<TextureRectangle> icon);
		void RegisterAffixCombination(IReadOnlySet<ISeasonAffix> affixes, Func<string> localizedName, Func<string> localizedDescription, Func<TextureRectangle> icon, Func<OrdinalSeason, double>? probabilityWeightProvider = null);
		void UnregisterAffixCombination(IReadOnlySet<ISeasonAffix> affixes);
		void RegisterAffixConflictProvider(Func<IReadOnlySet<ISeasonAffix>, OrdinalSeason, bool> provider);
		void UnregisterAffixConflictProvider(Func<IReadOnlySet<ISeasonAffix>, OrdinalSeason, bool> provider);

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