using Shockah.Kokoro;
using Shockah.Kokoro.Stardew;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.SeasonAffixes
{
	internal interface IAffixSetGenerator
	{
		IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(Season season, int year);
	}

	internal sealed class AllCombinationsAffixSetGenerator : IAffixSetGenerator
	{
		private IAffixesProvider AffixesProvider { get; init; }

		public AllCombinationsAffixSetGenerator(IAffixesProvider affixesProvider)
		{
			this.AffixesProvider = affixesProvider;
		}

		public IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(Season season, int year)
		{
			var affixes = AffixesProvider.Affixes.ToList();
			return GetAllCombinations(affixes.Select(a => a.UniqueID).ToList(), new())
				.ToHashSet()
				.Select(affixIds => affixIds.Select(id => SeasonAffixes.Instance.AllAffixes[id]!).ToHashSet());
		}

		private IEnumerable<IReadOnlySet<string>> GetAllCombinations(List<string> remainingAffixes, HashSet<string> current)
		{
			if (remainingAffixes.Count == 0)
			{
				yield return current;
				yield break;
			}

			var affix = remainingAffixes[0];
			var newRemainingAffixes = remainingAffixes.Skip(1).ToList();

			foreach (var result in GetAllCombinations(newRemainingAffixes, current))
				yield return result;
			foreach (var result in GetAllCombinations(newRemainingAffixes, new HashSet<string>(current) { affix }))
				yield return result;
		}
	}

	internal sealed class NonConflictingAffixSetGenerator : IAffixSetGenerator
	{
		private IAffixSetGenerator AffixSetGenerator { get; init; }

		public NonConflictingAffixSetGenerator(IAffixSetGenerator affixSetGenerator)
		{
			this.AffixSetGenerator = affixSetGenerator;
		}

		public IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(Season season, int year)
		{
			return AffixSetGenerator.Generate(season, year)
				.Where(c =>
				{
					var list = c.ToList();
					for (int i = 1; i < list.Count; i++)
						for (int j = 0; j < i; j++)
							if (list[i].Conflicts(list[j]))
								return false;
					return true;
				});
		}
	}

	internal sealed class FittingScoreAffixSetGenerator : IAffixSetGenerator
	{
		private IAffixSetGenerator AffixSetGenerator { get; init; }
		private int Positivity { get; init; }
		private int Negativity { get; init; }

		public FittingScoreAffixSetGenerator(IAffixSetGenerator affixSetGenerator, int positivity, int negativity)
		{
			this.AffixSetGenerator = affixSetGenerator;
			this.Positivity = positivity;
			this.Negativity = negativity;
		}

		public IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(Season season, int year)
		{
			return AffixSetGenerator.Generate(season, year)
				.Where(c => c.Sum(a => a.GetPositivity(season, year)) == Positivity && c.Sum(a => a.GetNegativity(season, year)) == Negativity);
		}
	}

	internal sealed class ShuffledAffixSetGenerator : IAffixSetGenerator
	{
		private IAffixSetGenerator AffixSetGenerator { get; init; }
		private Random Random { get; init; }

		public ShuffledAffixSetGenerator(IAffixSetGenerator affixSetGenerator, Random random)
		{
			this.AffixSetGenerator = affixSetGenerator;
			this.Random = random;
		}

		public IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(Season season, int year)
			=> AffixSetGenerator.Generate(season, year).Shuffled(Random);
	}

	internal sealed class AsLittleAsPossibleAffixSetGenerator : IAffixSetGenerator
	{
		private IAffixSetGenerator AffixSetGenerator { get; init; }

		public AsLittleAsPossibleAffixSetGenerator(IAffixSetGenerator affixSetGenerator)
		{
			this.AffixSetGenerator = affixSetGenerator;
		}

		public IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(Season season, int year)
		{
			var remainingResults = AffixSetGenerator.Generate(season, year).ToList();

			int currentAllowedCount = 0;
			while (remainingResults.Count != 0)
			{
				for (int i = 0; i < remainingResults.Count; i++)
				{
					if (remainingResults[i].Count != currentAllowedCount)
						continue;
					yield return remainingResults[i];
					remainingResults.RemoveAt(i--);
				}
				currentAllowedCount++;
			}
		}
	}

	internal sealed class AvoidingDuplicatesIfPossibleAffixSetGenerator : IAffixSetGenerator
	{
		private IAffixSetGenerator AffixSetGenerator { get; init; }

		public AvoidingDuplicatesIfPossibleAffixSetGenerator(IAffixSetGenerator affixSetGenerator)
		{
			this.AffixSetGenerator = affixSetGenerator;
		}

		public IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(Season season, int year)
		{
			List<HashSet<string>> yielded = new();
			var remainingResults = AffixSetGenerator.Generate(season, year).ToList();

			int allowedDuplicates = 0;
			while (remainingResults.Count != 0)
			{
				for (int i = 0; i < remainingResults.Count; i++)
				{
					var ids = remainingResults[i].Select(a => a.UniqueID).ToHashSet();
					foreach (var yieldedEntry in yielded)
					{
						if (yieldedEntry.Intersect(ids).Count() > allowedDuplicates)
							goto remainingResultsContinue;
					}
					yielded.Add(ids);
					yield return remainingResults[i];
					remainingResults.RemoveAt(i--);

					remainingResultsContinue:;
				}

				allowedDuplicates++;
			}
		}
	}
}