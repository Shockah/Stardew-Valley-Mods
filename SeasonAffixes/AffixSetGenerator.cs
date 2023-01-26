using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.SeasonAffixes
{
	internal interface IAffixSetGenerator
	{
		IReadOnlySet<ISeasonAffix> Generate(Random random);
	}

	internal class IncrementalAffixSetGenerator : IAffixSetGenerator
	{
		private IAffixProvider AffixProvider { get; init; }
		private IReadOnlySet<ISeasonAffix> LastAffixes { get; init; }

		public IncrementalAffixSetGenerator(IAffixProvider affixProvider, IReadOnlySet<ISeasonAffix> lastAffixes)
		{
			this.AffixProvider = affixProvider;
			this.LastAffixes = lastAffixes;
		}

		public IReadOnlySet<ISeasonAffix> Generate(Random random)
		{
			var newAffixes = LastAffixes.ToHashSet();
		}
	}

	internal class AffixSetGenerator : IAffixSetGenerator
	{
		public IReadOnlySet<ISeasonAffix> Generate(Random random)
		{
			throw new NotImplementedException();
		}
	}
}