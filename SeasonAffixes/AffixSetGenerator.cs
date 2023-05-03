using Shockah.Kokoro;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.SeasonAffixes
{
	internal interface IAffixSetGenerator
	{
		IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(OrdinalSeason season);
	}

	internal static class IAffixSetGeneratorExt
	{
		public static IAffixSetGenerator NonConflictingWithCombinations(this IAffixSetGenerator affixSetGenerator)
			=> new NonConflictingWithCombinationsAffixSetGenerator(affixSetGenerator);

		public static IAffixSetGenerator WeightedRandom(this IAffixSetGenerator affixSetGenerator, Random random, IAffixSetWeightProvider weightProvider)
			=> new WeightedRandomAffixSetGenerator(affixSetGenerator, random, weightProvider);

		public static IAffixSetGenerator MaxAffixes(this IAffixSetGenerator affixSetGenerator, int max)
			=> new MaxAffixesAffixSetGenerator(affixSetGenerator, max);

		public static IAffixSetGenerator AsLittleAsPossible(this IAffixSetGenerator affixSetGenerator)
			=> new AsLittleAsPossibleAffixSetGenerator(affixSetGenerator);

		public static IAffixSetGenerator AvoidingDuplicatesBetweenChoices(this IAffixSetGenerator affixSetGenerator)
			=> new AvoidingDuplicatesBetweenChoicesAffixSetGenerator(affixSetGenerator);
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

		public IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(OrdinalSeason season)
		{
			var affixes = AffixesProvider.Affixes.ToList();
			return GetAllCombinations(affixes.ToDictionary(a => a.UniqueID, a => a.GetPositivity(season)), affixes.ToDictionary(a => a.UniqueID, a => a.GetNegativity(season)), affixes.Select(a => a.UniqueID).ToList(), new(), 0, 0)
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

	internal sealed class NonConflictingWithCombinationsAffixSetGenerator : IAffixSetGenerator
	{
		private IAffixSetGenerator AffixSetGenerator { get; init; }

		public NonConflictingWithCombinationsAffixSetGenerator(IAffixSetGenerator affixSetGenerator)
		{
			this.AffixSetGenerator = affixSetGenerator;
		}

		public IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(OrdinalSeason season)
		{
			return AffixSetGenerator.Generate(season).Where(combination =>
			{
				Dictionary<string, int> occurences = new();
				foreach (var affix in combination)
				{
					if (!occurences.ContainsKey(affix.UniqueID))
						occurences[affix.UniqueID] = 0;
					occurences[affix.UniqueID]++;
				}
				return !occurences.Values.Any(count => count > 1);
			});
		}
	}

	internal sealed class WeightedRandomAffixSetGenerator : IAffixSetGenerator
	{
		private IAffixSetGenerator AffixSetGenerator { get; init; }
		private Random Random { get; init; }
		private IAffixSetWeightProvider WeightProvider { get; init; }

		public WeightedRandomAffixSetGenerator(IAffixSetGenerator affixSetGenerator, Random random, IAffixSetWeightProvider weightProvider)
		{
			this.AffixSetGenerator = affixSetGenerator;
			this.Random = random;
			this.WeightProvider = weightProvider;
		}

		public IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(OrdinalSeason season)
		{
			List<WeightedItem<IReadOnlySet<ISeasonAffix>>> weightedItems = new();
			var maxWeight = 0.0;
			foreach (var choice in AffixSetGenerator.Generate(season))
			{
				var weight = WeightProvider.GetWeight(choice, season);
				maxWeight = Math.Max(maxWeight, weight);
				if (weight > 0)
					weightedItems.Add(new(weight, choice));
			}

			var weightedRandom = new WeightedRandom<IReadOnlySet<ISeasonAffix>>();
			foreach (var weightedItem in weightedItems)
				if (weightedItem.Weight >= maxWeight / 100)
					weightedRandom.Add(weightedItem);

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

		public IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(OrdinalSeason season)
			=> AffixSetGenerator.Generate(season).Where(affixes => affixes.Count <= Max);
	}

	internal sealed class AsLittleAsPossibleAffixSetGenerator : IAffixSetGenerator
	{
		private IAffixSetGenerator AffixSetGenerator { get; init; }

		public AsLittleAsPossibleAffixSetGenerator(IAffixSetGenerator affixSetGenerator)
		{
			this.AffixSetGenerator = affixSetGenerator;
		}

		public IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(OrdinalSeason season)
		{
			var remainingResults = AffixSetGenerator.Generate(season).ToList();

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

	internal sealed class AvoidingDuplicatesBetweenChoicesAffixSetGenerator : IAffixSetGenerator
	{
		private IAffixSetGenerator AffixSetGenerator { get; init; }

		public AvoidingDuplicatesBetweenChoicesAffixSetGenerator(IAffixSetGenerator affixSetGenerator)
		{
			this.AffixSetGenerator = affixSetGenerator;
		}

		public IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(OrdinalSeason season)
		{
			List<HashSet<string>> yielded = new();
			var remainingResults = AffixSetGenerator.Generate(season).ToList();

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