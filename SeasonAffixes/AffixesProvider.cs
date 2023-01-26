using Shockah.Kokoro.Stardew;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.SeasonAffixes
{
	internal interface IAffixesProvider
	{
		IEnumerable<ISeasonAffix> Affixes { get; }
	}

	internal class AllAffixesProvider : IAffixesProvider
	{
		private ISeasonAffixesApi Api { get; init; }

		public IEnumerable<ISeasonAffix> Affixes =>
			Api.AllAffixes.Values;

		public AllAffixesProvider(ISeasonAffixesApi api)
		{
			this.Api = api;
		}
	}

	internal class ApplicableToSeasonAffixesProvider : IAffixesProvider
	{
		private IAffixesProvider Wrapped { get; init; }
		private Season Season { get; init; }
		private int Year { get; init; }

		public IEnumerable<ISeasonAffix> Affixes =>
			Wrapped.Affixes
				.Where(affix => affix.GetProbabilityWeight(Season, Year) > 0);

		public ApplicableToSeasonAffixesProvider(IAffixesProvider wrapped, Season season, int year)
		{
			this.Wrapped = wrapped;
			this.Season = season;
			this.Year = year;
		}
	}
}