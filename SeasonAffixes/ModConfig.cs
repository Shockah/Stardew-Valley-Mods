using Newtonsoft.Json;
using Shockah.Kokoro;
using StardewModdingAPI;
using System.Collections.Generic;

namespace Shockah.SeasonAffixes
{
	public class ModConfig : IVersioned.Modifiable
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

		[JsonProperty] public IList<AffixSetEntry> AffixSetEntries { get; internal set; } = new List<AffixSetEntry>() { new(1, 0, 2.0), new(1, 1, 8.0), new(1, 2, 5.0), new(2, 1, 3.0), new(2, 2, 2.0) };
		[JsonProperty] public int AffixRepeatPeriod { get; internal set; } = 2;
		[JsonProperty] public int AffixSetRepeatPeriod { get; internal set; } = 8;

		[JsonProperty] public float AgricultureValue { get; internal set; } = 2f;
		[JsonProperty] public float BurstingNoBombWeight { get; internal set; } = 0f;
		[JsonProperty] public float BurstingCherryBombWeight { get; internal set; } = 2f;
		[JsonProperty] public float BurstingBombWeight { get; internal set; } = 2f;
		[JsonProperty] public float BurstingMegaBombWeight { get; internal set; } = 1f;
		[JsonProperty] public float FairyTalesChance { get; internal set; } = 0.15f;
		[JsonIgnore] public float FortuneValue { get; internal set; } = 0.05f;
		[JsonProperty] public float InflationIncrease { get; internal set; } = 0.2f;
		[JsonProperty] public float InnovationDecrease { get; internal set; } = 0.25f;
		[JsonProperty] public float LoveValue { get; internal set; } = 2f;
		[JsonProperty] public float RanchingValue { get; internal set; } = 2f;
		[JsonProperty] public float ResilienceValue { get; internal set; } = 2f;
		[JsonProperty] public float RustIncrease { get; internal set; } = 0.5f;
		[JsonProperty] public int SilenceFriendshipGain { get; internal set; } = 0;
		[JsonProperty] public IDictionary<string, int> SkillLevelIncrease { get; internal set; } = new Dictionary<string, int>();
		[JsonProperty] public IDictionary<string, float> SkillXPIncrease { get; internal set; } = new Dictionary<string, float>();
		[JsonProperty] public float TenacityValue { get; internal set; } = 1.5f;
		[JsonProperty] public float ThunderChance { get; internal set; } = 2f;
		[JsonProperty] public float TreasuresChance { get; internal set; } = 0.25f;
		[JsonProperty] public float TreasuresChanceWithEnchantment { get; internal set; } = 0.5f;
		[JsonProperty] public float WildGrowthAdvanceChance { get; internal set; } = 1f;
		[JsonProperty] public float WildGrowthNewSeedChance { get; internal set; } = 0.5f;
	}
}