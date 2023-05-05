using Shockah.Kokoro;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

		public static IAffixSetGenerator AsLittleAsPossible(this IAffixSetGenerator affixSetGenerator)
			=> new AsLittleAsPossibleAffixSetGenerator(affixSetGenerator);

		public static IAffixSetGenerator AvoidingDuplicatesBetweenChoices(this IAffixSetGenerator affixSetGenerator)
			=> new AvoidingDuplicatesBetweenChoicesAffixSetGenerator(affixSetGenerator);

		public static IAffixSetGenerator Benchmarking(this IAffixSetGenerator affixSetGenerator, IMonitor monitor, string tag, LogLevel logLevel = LogLevel.Debug)
			=> new BenchmarkingAffixSetGenerator(affixSetGenerator, monitor, tag, logLevel);
	}

	internal sealed class AllCombinationsAffixSetGenerator : IAffixSetGenerator
	{
		private IAffixesProvider AffixesProvider { get; init; }
		private IAffixScoreProvider ScoreProvider { get; init; }
		private int? Positivity { get; init; }
		private int? Negativity { get; init; }
		private int? MaxAffixes { get; init; }

		public AllCombinationsAffixSetGenerator(IAffixesProvider affixesProvider, IAffixScoreProvider scoreProvider, int? positivity, int? negativity, int? maxAffixes)
		{
			this.AffixesProvider = affixesProvider;
			this.ScoreProvider = scoreProvider;
			this.Positivity = positivity;
			this.Negativity = negativity;
			this.MaxAffixes = maxAffixes;
		}

		public IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(OrdinalSeason season)
			=> GetAllCombinations(season, AffixesProvider.Affixes.ToList(), new(), 0, 0).Distinct();

		private IEnumerable<IReadOnlySet<ISeasonAffix>> GetAllCombinations(OrdinalSeason season, List<ISeasonAffix> remainingAffixes, HashSet<ISeasonAffix> current, int currentPositivity, int currentNegativity)
		{
			if (remainingAffixes.Count == 0)
			{
				if (Positivity is not null && currentPositivity < Positivity.Value)
					yield break;
				if (Negativity is not null && currentNegativity < Negativity.Value)
					yield break;
				if (MaxAffixes is not null && current.Count >= MaxAffixes.Value)
					yield break;
				yield return current;
				yield break;
			}
			if (Positivity is not null && currentPositivity > Positivity.Value)
				yield break;
			if (Negativity is not null && currentNegativity > Negativity.Value)
				yield break;
			if (MaxAffixes is not null && current.Count >= MaxAffixes.Value)
				yield return current;

			var newAffix = remainingAffixes[0];
			var newRemainingAffixes = remainingAffixes.Skip(1).ToList();

			foreach (var result in GetAllCombinations(season, newRemainingAffixes, current, currentPositivity, currentNegativity))
				yield return result;
			foreach (var result in GetAllCombinations(season, newRemainingAffixes, new HashSet<ISeasonAffix>(current) { newAffix }, currentPositivity + ScoreProvider.GetPositivity(newAffix, season), currentNegativity + ScoreProvider.GetNegativity(newAffix, season)))
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

	internal sealed class BenchmarkingAffixSetGenerator : IAffixSetGenerator
	{
		private IAffixSetGenerator AffixSetGenerator { get; init; }
		private IMonitor Monitor { get; init; }
		private string Tag { get; init; }
		private LogLevel LogLevel { get; init; }

		public BenchmarkingAffixSetGenerator(IAffixSetGenerator affixSetGenerator, IMonitor monitor, string tag, LogLevel logLevel = LogLevel.Debug)
		{
			this.AffixSetGenerator = affixSetGenerator;
			this.Monitor = monitor;
			this.Tag = tag;
			this.LogLevel = logLevel;
		}

		public IEnumerable<IReadOnlySet<ISeasonAffix>> Generate(OrdinalSeason season)
		{
			Monitor.Log($"[{Tag}] Generating affix sets...", LogLevel);
			Stopwatch stopwatch = Stopwatch.StartNew();
			int index = 0;

			foreach (var result in AffixSetGenerator.Generate(season))
			{
				if (index < 10 || (index < 100 && index % 10 == 0) || (index < 1000 && index % 100 == 0) || (index < 10000 && index % 1000 == 0) || (index < 100000 && index % 10000 == 0))
					Monitor.Log($"> [{Tag}] Generated affix set #{index + 1}, took {stopwatch.ElapsedMilliseconds}ms", LogLevel);
				yield return result;
				index++;
			}
			Monitor.Log($"> [{Tag}] Done generating affix sets after {index} results, took {stopwatch.ElapsedMilliseconds}ms.", LogLevel);
		}
	}
}