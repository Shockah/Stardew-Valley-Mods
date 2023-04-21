using Shockah.Kokoro;
using Shockah.SeasonAffixes.Affixes;
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
		public static IAffixSetGenerator NonConflicting(this IAffixSetGenerator affixSetGenerator, List<Func<IReadOnlySet<ISeasonAffix>, OrdinalSeason, bool>> affixConflictProviders)
			=> new NonConflictingAffixSetGenerator(affixSetGenerator, affixConflictProviders);

		public static IAffixSetGenerator NonConflictingWithCombinations(this IAffixSetGenerator affixSetGenerator)
			=> new NonConflictingWithCombinationsAffixSetGenerator(affixSetGenerator);

		public static IAffixSetGenerator Decombined(this IAffixSetGenerator affixSetGenerator)
			=> new DecombinedAffixSetGenerator(affixSetGenerator);

		public static IAffixSetGenerator WeightedRandom(this IAffixSetGenerator affixSetGenerator, Random random, Func<ISeasonAffix, double> weightProvider)
			=> new WeightedRandomAffixSetGenerator(affixSetGenerator, random, weightProvider);

		public static IAffixSetGenerator MaxAffixes(this IAffixSetGenerator affixSetGenerator, int max)
			=> new MaxAffixesAffixSetGenerator(affixSetGenerator, max);

		public static IAffixSetGenerator AsLittleAsPossible(this IAffixSetGenerator affixSetGenerator)
			=> new AsLittleAsPossibleAffixSetGenerator(affixSetGenerator);

		public static IAffixSetGenerator AvoidingDuplicatesBetweenChoices(this IAffixSetGenerator affixSetGenerator)
			=> new AvoidingDuplicatesBetweenChoicesAffixSetGenerator(affixSetGenerator);

		public static IAffixSetGenerator AvoidingChoiceHistoryDuplicates(this IAffixSetGenerator affixSetGenerator)
			=> new AvoidingChoiceHistoryDuplicatesAffixSetGenerator(affixSetGenerator);

		public static IAffixSetGenerator AvoidingSetChoiceHistoryDuplicates(this IAffixSetGenerator affixSetGenerator)
			=> new AvoidingSetChoiceHistoryDuplicatesAffixSetGenerator(affixSetGenerator);
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

	internal sealed class NonConflictingAffixSetGenerator : IAffixSetGenerator
	{
		private IAffixSetGenerator AffixSetGenerator { get; init; }
		private List<Func<IReadOnlySet<ISeasonAffix>, OrdinalSeason, bool>> AffixConflictProviders { get; init; }

		public NonConflictingAffixSetGenerator(IAffixSetGenerator affixSetGenerator, List<Func<IReadOnlySet<ISeasonAffix>, OrdinalSeason, bool>> affixConflictProviders)
		{
			this.AffixSetGenerator = affixSetGenerator;
			this.AffixConflictProviders = affixConflictProviders;
		}

		public IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(OrdinalSeason season)
		{
			var test = AffixSetGenerator.Generate(season).ToList();
			return test.Where(c => !AffixConflictProviders.Any(provider => provider(c, season)));
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

				void Record(ISeasonAffix affix)
				{
					if (affix is CombinedAffix combinedAffix)
					{
						foreach (var childAffix in combinedAffix.Affixes)
							Record(affix);
					}
					else
					{
						if (!occurences.ContainsKey(affix.UniqueID))
							occurences[affix.UniqueID] = 0;
						occurences[affix.UniqueID]++;
					}
				}

				foreach (var affix in combination)
					Record(affix);
				return !occurences.Values.Any(count => count > 1);
			});
		}
	}

	internal sealed class DecombinedAffixSetGenerator : IAffixSetGenerator
	{
		private IAffixSetGenerator AffixSetGenerator { get; init; }

		public DecombinedAffixSetGenerator(IAffixSetGenerator affixSetGenerator)
		{
			this.AffixSetGenerator = affixSetGenerator;
		}

		public IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(OrdinalSeason season)
		{
			return AffixSetGenerator.Generate(season).Select(combination =>
			{
				HashSet<ISeasonAffix> affixes = new();

				void Record(ISeasonAffix affix)
				{
					if (affix is CombinedAffix combinedAffix)
					{
						foreach (var childAffix in combinedAffix.Affixes)
							Record(affix);
					}
					else
					{
						affixes.Add(affix);
					}
				}

				foreach (var affix in combination)
					Record(affix);
				return affixes;
			});
		}
	}

	internal sealed class WeightedRandomAffixSetGenerator : IAffixSetGenerator
	{
		private IAffixSetGenerator AffixSetGenerator { get; init; }
		private Random Random { get; init; }
		private Func<ISeasonAffix, double> WeightProvider { get; init; }

		public WeightedRandomAffixSetGenerator(IAffixSetGenerator affixSetGenerator, Random random, Func<ISeasonAffix, double> weightProvider)
		{
			this.AffixSetGenerator = affixSetGenerator;
			this.Random = random;
			this.WeightProvider = weightProvider;
		}

		public IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(OrdinalSeason season)
		{
			var weightedRandom = new WeightedRandom<IReadOnlySet<ISeasonAffix>>();
			foreach (var choice in AffixSetGenerator.Generate(season))
			{
				var weight = choice.Average(a => a.GetProbabilityWeight(season) * WeightProvider(a));
				if (weight > 0)
					weightedRandom.Add(new(weight, choice));
			}
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

	internal sealed class AvoidingChoiceHistoryDuplicatesAffixSetGenerator : IAffixSetGenerator
	{
		private IAffixSetGenerator AffixSetGenerator { get; init; }

		public AvoidingChoiceHistoryDuplicatesAffixSetGenerator(IAffixSetGenerator affixSetGenerator)
		{
			this.AffixSetGenerator = affixSetGenerator;
		}

		public IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(OrdinalSeason season)
		{
			var remainingResults = new LinkedList<IReadOnlySet<ISeasonAffix>>(AffixSetGenerator.Generate(season));

			var node = remainingResults.First;
			while (node is not null)
			{
				foreach (var choiceAffix in node.Value)
					foreach (var step in SeasonAffixes.Instance.SaveData.AffixChoiceHistory)
						if (step.Any(saveAffix => saveAffix.UniqueID == choiceAffix.UniqueID))
							goto nodeLoopContinue;

				yield return node.Value;
				remainingResults.Remove(node);

				nodeLoopContinue:;
				node = node.Next;
			}

			foreach (var choice in remainingResults)
				yield return choice;
		}
	}

	internal sealed class AvoidingSetChoiceHistoryDuplicatesAffixSetGenerator : IAffixSetGenerator
	{
		private IAffixSetGenerator AffixSetGenerator { get; init; }

		public AvoidingSetChoiceHistoryDuplicatesAffixSetGenerator(IAffixSetGenerator affixSetGenerator)
		{
			this.AffixSetGenerator = affixSetGenerator;
		}

		public IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(OrdinalSeason season)
		{
			var remainingResults = new LinkedList<IReadOnlySet<ISeasonAffix>>(AffixSetGenerator.Generate(season));

			var node = remainingResults.First;
			while (node is not null)
			{
				var nodeIds = node.Value.Select(choiceAffix => choiceAffix.UniqueID).ToHashSet();
				foreach (var step in SeasonAffixes.Instance.SaveData.AffixSetChoiceHistory)
					if (step.Select(saveChoice => saveChoice.Select(saveAffix => saveAffix.UniqueID).ToHashSet()).Any(saveChoice => saveChoice.SetEquals(nodeIds)))
						goto nodeLoopContinue;

				yield return node.Value;
				remainingResults.Remove(node);

				nodeLoopContinue:;
				node = node.Next;
			}

			foreach (var choice in remainingResults)
				yield return choice;
		}
	}
}