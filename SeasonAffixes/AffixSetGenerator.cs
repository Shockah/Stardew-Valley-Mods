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
		private int? Positivity { get; init; }
		private int? Negativity { get; init; }

		public AllCombinationsAffixSetGenerator(IAffixesProvider affixesProvider, int? positivity, int? negativity)
		{
			this.AffixesProvider = affixesProvider;
			this.Positivity = positivity;
			this.Negativity = negativity;
		}

		public IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(Season season, int year)
		{
			var affixes = AffixesProvider.Affixes.ToList();
			return GetAllCombinations(affixes.ToDictionary(a => a.UniqueID, a => a.GetPositivity(season, year)), affixes.ToDictionary(a => a.UniqueID, a => a.GetNegativity(season, year)), affixes.Select(a => a.UniqueID).ToList(), new(), 0, 0)
				.ToHashSet()
				.Select(affixIds => affixIds.Select(id => SeasonAffixes.Instance.GetAffix(id)!).ToHashSet());
		}

		private IEnumerable<IReadOnlySet<string>> GetAllCombinations(Dictionary<string, int> affixPositivity, Dictionary<string, int> affixNegativity, List<string> remainingAffixes, HashSet<string> current, int currentPositivity, int currentNegativity)
		{
			if (remainingAffixes.Count == 0)
			{
				if (Positivity is not null && currentPositivity < Positivity.Value)
					yield break;
				if (Negativity is not null && currentNegativity < Negativity.Value)
					yield break;
				yield return current;
				yield break;
			}
			if (Positivity is not null && currentPositivity > Positivity.Value)
				yield break;
			if (Negativity is not null && currentNegativity > Negativity.Value)
				yield break;

			var affixID = remainingAffixes[0];
			var newRemainingAffixes = remainingAffixes.Skip(1).ToList();

			foreach (var result in GetAllCombinations(affixPositivity, affixNegativity, newRemainingAffixes, current, currentPositivity, currentNegativity))
				yield return result;
			foreach (var result in GetAllCombinations(affixPositivity, affixNegativity, newRemainingAffixes, new HashSet<string>(current) { affixID }, currentPositivity + affixPositivity[affixID], currentNegativity + affixNegativity[affixID]))
				yield return result;
		}
	}

	internal sealed class NonConflictingAffixSetGenerator : IAffixSetGenerator
	{
		private IAffixSetGenerator AffixSetGenerator { get; init; }
		private List<Func<ISeasonAffix, ISeasonAffix, Season, int, bool>> AffixConflictHandlers { get; init; }

		public NonConflictingAffixSetGenerator(IAffixSetGenerator affixSetGenerator, List<Func<ISeasonAffix, ISeasonAffix, Season, int, bool>> affixConflictHandlers)
		{
			this.AffixSetGenerator = affixSetGenerator;
			this.AffixConflictHandlers = affixConflictHandlers;
		}

		public IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(Season season, int year)
		{
			var test = AffixSetGenerator.Generate(season, year).ToList();
			return test
				.Where(c =>
				{
					var list = c.ToList();
					for (int i = 1; i < list.Count; i++)
					{
						for (int j = 0; j < i; j++)
						{
							if (AffixConflictHandlers.Any(h => h(list[i], list[j], season, year)))
								return false;
							if (AffixConflictHandlers.Any(h => h(list[j], list[i], season, year)))
								return false;
						}
					}
					return true;
				});
		}
	}

	internal sealed class WeightedRandomAffixSetGenerator : IAffixSetGenerator
	{
		private IAffixSetGenerator AffixSetGenerator { get; init; }
		private Random Random { get; init; }

		public WeightedRandomAffixSetGenerator(IAffixSetGenerator affixSetGenerator, Random random)
		{
			this.AffixSetGenerator = affixSetGenerator;
			this.Random = random;
		}

		public IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(Season season, int year)
		{
			var weightedRandom = new WeightedRandom<IReadOnlySet<ISeasonAffix>>();
			foreach (var choice in AffixSetGenerator.Generate(season, year))
				weightedRandom.Add(new(choice.Average(a => a.GetProbabilityWeight(season, year)), choice));
			while (weightedRandom.Items.Count != 0)
				yield return weightedRandom.Next(Random, consume: true);
		}
	}

	internal sealed class MaxAffixesAffixSetGenerator : IAffixSetGenerator
	{
		private IAffixSetGenerator AffixSetGenerator { get; init; }
		private int Max { get; init; }

		public MaxAffixesAffixSetGenerator(IAffixSetGenerator affixSetGenerator, int max)
		{
			this.AffixSetGenerator = affixSetGenerator;
			this.Max = max;
		}

		public IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(Season season, int year)
			=> AffixSetGenerator.Generate(season, year).Where(affixes => affixes.Count <= Max);
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