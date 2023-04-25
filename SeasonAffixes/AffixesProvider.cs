using System.Collections.Generic;
using System.Linq;

namespace Shockah.SeasonAffixes
{
	internal interface IAffixesProvider
	{
		IEnumerable<ISeasonAffix> Affixes { get; }
	}

	internal static class IAffixesProviderExt
	{
		public static IAffixesProvider Effective(this IAffixesProvider provider, OrdinalSeason season)
			=> new EffectiveAffixesProvider(provider, season);

		public static IAffixesProvider ApplicableToSeason(this IAffixesProvider provider, OrdinalSeason season)
			=> new ApplicableToSeasonAffixesProvider(provider, season);
	}

	internal sealed class AffixesProvider : IAffixesProvider
	{
		public IEnumerable<ISeasonAffix> Affixes { get; init; }

		public AffixesProvider(IEnumerable<ISeasonAffix> affixes)
		{
			this.Affixes = affixes;
		}
	}

	internal sealed class CompoundAffixesProvider : IAffixesProvider
	{
		private IEnumerable<IAffixesProvider> Providers { get; init; }

		public IEnumerable<ISeasonAffix> Affixes
		{
			get
			{
				foreach (var provider in Providers)
					foreach (var affix in provider.Affixes)
						yield return affix;
			}
		}

		public CompoundAffixesProvider(params IAffixesProvider[] providers) : this((IEnumerable<IAffixesProvider>)providers) { }

		public CompoundAffixesProvider(IEnumerable<IAffixesProvider> providers)
		{
			this.Providers = providers;
		}
	}

	internal sealed class EffectiveAffixesProvider : IAffixesProvider
	{
		private IAffixesProvider Wrapped { get; init; }
		private OrdinalSeason Season { get; init; }

		public IEnumerable<ISeasonAffix> Affixes =>
			Wrapped.Affixes
				.Where(affix => affix.GetPositivity(Season) > 0 || affix.GetNegativity(Season) > 0);

		public EffectiveAffixesProvider(IAffixesProvider wrapped, OrdinalSeason season)
		{
			this.Wrapped = wrapped;
			this.Season = season;
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