using Newtonsoft.Json;
using Shockah.Kokoro;
using StardewModdingAPI;
using System.Collections.Generic;

namespace Shockah.SeasonAffixes
{
	public class ModConfig : IVersioned.Modifiable
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public ISemanticVersion? Version { get; set; }

		[JsonProperty] public int Choices { get; internal set; } = 2;
		[JsonProperty] public int RerollsPerSeason { get; internal set; } = 1;
		[JsonProperty] public bool Incremental { get; internal set; } = false;

		[JsonProperty] public ISet<AffixSetEntry> AffixSetEntries { get; internal set; } = new HashSet<AffixSetEntry>() { new(1, 0, 2.0), new(1, 1, 8.0), new(1, 2, 5.0), new(2, 1, 3.0), new(2, 2, 2.0) };
		[JsonProperty] public int AffixRepeatPeriod { get; internal set; } = 2;
		[JsonProperty] public int AffixSetRepeatPeriod { get; internal set; } = 8;

		public readonly struct AffixSetEntry
		{
			public int Positive { get; init; }
			public int Negative { get; init; }
			public double Weight { get; init; }

			public AffixSetEntry(int positive, int negative, double weight)
			{
				this.Positive = positive;
				this.Negative = negative;
				this.Weight = weight;
			}
		}
	}
}