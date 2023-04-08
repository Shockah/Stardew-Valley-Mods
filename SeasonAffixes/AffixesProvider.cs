using System.Collections.Generic;
using System.Linq;

namespace Shockah.SeasonAffixes
{
	internal interface IAffixesProvider
	{
		IEnumerable<ISeasonAffix> Affixes { get; }
	}

	internal sealed class AllAffixesProvider : IAffixesProvider
	{
		private ISeasonAffixesApi Api { get; init; }

		public IEnumerable<ISeasonAffix> Affixes =>
			Api.AllAffixes.Values;

		public AllAffixesProvider(ISeasonAffixesApi api)
		{
			this.Api = api;
		}
	}

	internal sealed class ApplicableToSeasonAffixesProvider : IAffixesProvider
	{
		private IAffixesProvider Wrapped { get; init; }
		private OrdinalSeason Season { get; init; }

		public IEnumerable<ISeasonAffix> Affixes =>
			Wrapped.Affixes
				.Where(affix => affix.GetProbabilityWeight(Season) > 0);

		public ApplicableToSeasonAffixesProvider(IAffixesProvider wrapped, OrdinalSeason season)
		{
			this.Wrapped = wrapped;
			this.Season = season;
		}
	}
}