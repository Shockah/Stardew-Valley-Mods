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
			Entries: new List<BrazierCombinationConfigEntry>()
			{
				new(
					Conditions: new List<MineLevelConditions>() { new(MineType.Earth, Dangerous: false) },
					Weight: 1,
					BrazierCountWeightItems: new List<BrazierCombinationConfigEntryWeightItem>() { new(4, 3), new(1, 4) }
				),
				new(
					Conditions: new List<MineLevelConditions>() { new(MineType.Frost, Dangerous: false), new(MineType.Earth, Dangerous: true) },
					Weight: 1,
					BrazierCountWeightItems: new List<BrazierCombinationConfigEntryWeightItem>() { new(1, 3), new(2, 4) }
				),
				new(
					Conditions: new List<MineLevelConditions>() { new(MineType.Lava, Dangerous: false), new(MineType.Frost, Dangerous: true) },
					Weight: 1,
					BrazierCountWeightItems: new List<BrazierCombinationConfigEntryWeightItem>() { new(1, 3), new(4, 4), new(1, 5) }
				),
				new(
					Conditions: new List<MineLevelConditions>() { new(MineType.SkullCavern, Dangerous: false), new(MineType.Lava, Dangerous: true) },
					Weight: 1,
					BrazierCountWeightItems: new List<BrazierCombinationConfigEntryWeightItem>() { new(1, 4), new(1, 5) }
				),
				new(
					Conditions: new List<MineLevelConditions>() { new(MineType.SkullCavern, Dangerous: true) },
					Weight: 1,
					BrazierCountWeightItems: new List<BrazierCombinationConfigEntryWeightItem>() { new(1, 4), new(2, 5) }
				)
			}
		);
	}
}