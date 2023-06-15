using Newtonsoft.Json;
using Shockah.Kokoro;
using StardewModdingAPI;
using System.Collections.Generic;

namespace Shockah.SeasonAffixes;

public partial class ModConfig : IVersioned.Modifiable
{
	public record AffixSetEntry(
		int Positive = 0,
		int Negative = 0,
		double Weight = 1.0
	)
	{
		internal bool IsValid()
			=> Positive >= 0 && Negative >= 0 && (Positive + Negative) > 0 && Weight > 0;
	}

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public ISemanticVersion? Version { get; set; }

	[JsonProperty] public bool Incremental { get; internal set; } = false;
	[JsonProperty] public int Choices { get; internal set; } = 2;
	//[JsonProperty] public int RerollsPerSeason { get; internal set; } = 1;
	[JsonProperty] public IDictionary<string, double> AffixWeights { get; internal set; } = new Dictionary<string, double>();
	[JsonProperty] public ISet<string> PermanentAffixes { get; internal set; } = new HashSet<string>();

	[JsonProperty] public IList<AffixSetEntry> AffixSetEntries { get; internal set; } = new List<AffixSetEntry>() { new(1, 0, 2.0), new(1, 1, 8.0), new(1, 2, 5.0), new(2, 1, 3.0), new(2, 2, 2.0) };
	[JsonProperty] public int AffixRepeatPeriod { get; internal set; } = 2;
	[JsonProperty] public int AffixSetRepeatPeriod { get; internal set; } = 4;

	[JsonProperty] public bool WinterCrops { get; internal set; } = false;
}