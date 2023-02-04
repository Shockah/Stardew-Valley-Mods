using Newtonsoft.Json;
using Shockah.CommonModCode;
using StardewModdingAPI;
using System.Collections.Generic;

namespace Shockah.AdventuresInTheMines.Config
{
	public sealed class ModConfig : IVersioned.Modifiable
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public ISemanticVersion? Version { get; set; }

		[JsonProperty] public BrazierCombinationConfig BrazierCombination { get; internal set; } = new(
			Enabled: true,
			Entries: new List<MineLevelConditionedConfig<BrazierCombinationConfigEntry>>()
			{
				new(
					new(
						Weight: 1,
						BrazierCountWeightItems: new List<BrazierCombinationConfigEntryWeightItem>() { new(4, 3), new(1, 4) }
					),
					new MineLevelConditions(MineType.Earth, Dangerous: false)
				),
				new(
					new(
						Weight: 1,
						BrazierCountWeightItems: new List<BrazierCombinationConfigEntryWeightItem>() { new(1, 3), new(2, 4) }
					),
					new MineLevelConditions(MineType.Frost, Dangerous: false),
					new MineLevelConditions(MineType.Earth, Dangerous: true)
				),
				new(
					new(
						Weight: 1,
						BrazierCountWeightItems: new List<BrazierCombinationConfigEntryWeightItem>() { new(1, 3), new(4, 4), new(1, 5) }
					),
					new MineLevelConditions(MineType.Lava, Dangerous: false),
					new MineLevelConditions(MineType.Frost, Dangerous: true)
				),
				new(
					new(
						Weight: 1,
						BrazierCountWeightItems: new List<BrazierCombinationConfigEntryWeightItem>() { new(1, 4), new(1, 5) }
					),
					new MineLevelConditions(MineType.SkullCavern, Dangerous: false),
					new MineLevelConditions(MineType.Lava, Dangerous: true)
				),
				new(
					new(
						Weight: 1,
						BrazierCountWeightItems: new List<BrazierCombinationConfigEntryWeightItem>() { new(1, 4), new(2, 5) }
					),
					new MineLevelConditions(MineType.SkullCavern, Dangerous: true)
				)
			}
		);

		[JsonProperty]
		public BrazierSequenceConfig BrazierSequence { get; internal set; } = new(
			Enabled: true,
			Entries: new List<MineLevelConditionedConfig<BrazierSequenceConfigEntry>>()
			{
				new(
					new(
						Weight: 1,
						BrazierCountWeightItems: new List<BrazierSequenceConfigEntryWeightItem>() { new(1, 4) }
					),
					new MineLevelConditions(MineType.Earth, Dangerous: false)
				),
				new(
					new(
						Weight: 1,
						BrazierCountWeightItems: new List<BrazierSequenceConfigEntryWeightItem>() { new(1, 4), new(1, 5) }
					),
					new MineLevelConditions(MineType.Frost, Dangerous: false),
					new MineLevelConditions(MineType.Earth, Dangerous: true)
				),
				new(
					new(
						Weight: 1,
						BrazierCountWeightItems: new List<BrazierSequenceConfigEntryWeightItem>() { new(1, 5) }
					),
					new MineLevelConditions(MineType.Lava, Dangerous: false),
					new MineLevelConditions(MineType.Frost, Dangerous: true)
				),
				new(
					new(
						Weight: 1,
						BrazierCountWeightItems: new List<BrazierSequenceConfigEntryWeightItem>() { new(1, 5), new(1, 6) }
					),
					new MineLevelConditions(MineType.SkullCavern, Dangerous: false),
					new MineLevelConditions(MineType.Lava, Dangerous: true)
				),
				new(
					new(
						Weight: 1,
						BrazierCountWeightItems: new List<BrazierSequenceConfigEntryWeightItem>() { new(1, 6) }
					),
					new MineLevelConditions(MineType.SkullCavern, Dangerous: true)
				)
			}
		);
	}
}