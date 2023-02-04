using Newtonsoft.Json;
using Shockah.CommonModCode;
using StardewModdingAPI;
using System.Collections.Generic;

namespace Shockah.AdventuresInTheMines.Config
{
	public sealed class ModConfig : IVersioned.Modifiable
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public ISemanticVersion? Version { get; set; }

		public IList<MineLevelConditionedConfig<PopulateConfigEntry>> PopulateEntries { get; internal set; } = new List<MineLevelConditionedConfig<PopulateConfigEntry>>()
		{
			new(
				new(Chance: 0.25),
				new MineLevelConditions(new HashSet<MineType>() { MineType.Earth, MineType.Frost, MineType.Lava }, Dangerous: false, DarkArea: true)
			),
			new(
				new(Chance: 0.2),
				new MineLevelConditions(new HashSet<MineType>() { MineType.Earth, MineType.Frost, MineType.Lava }, Dangerous: false, DarkArea: false)
			),
			new(
				new(Chance: 0.3),
				new MineLevelConditions(new HashSet<MineType>() { MineType.Earth, MineType.Frost, MineType.Lava }, Dangerous: true, DarkArea: true)
			),
			new(
				new(Chance: 0.25),
				new MineLevelConditions(new HashSet<MineType>() { MineType.Earth, MineType.Frost, MineType.Lava }, Dangerous: true, DarkArea: false)
			),
			new(
				new(Chance: 0.2),
				new MineLevelConditions(MineType.SkullCavern, DarkArea: true)
			),
			new(
				new(Chance: 0.15),
				new MineLevelConditions(MineType.SkullCavern, DarkArea: false)
			)
		};

		[JsonProperty]
		public BrazierCombinationConfig BrazierCombination { get; internal set; } = new(
			Enabled: true,
			Entries: new List<MineLevelConditionedConfig<BrazierCombinationConfigEntry>>()
			{
				new(
					new(
						Weight: 0.35,
						BrazierCountWeightItems: new List<BrazierCombinationConfigEntryWeightItem>() { new(4, 3), new(1, 4) }
					),
					new MineLevelConditions(MineType.Earth, Dangerous: false)
				),
				new(
					new(
						Weight: 0.35,
						BrazierCountWeightItems: new List<BrazierCombinationConfigEntryWeightItem>() { new(1, 3), new(2, 4) }
					),
					new MineLevelConditions(MineType.Frost, Dangerous: false),
					new MineLevelConditions(MineType.Earth, Dangerous: true)
				),
				new(
					new(
						Weight: 0.35,
						BrazierCountWeightItems: new List<BrazierCombinationConfigEntryWeightItem>() { new(1, 3), new(4, 4), new(1, 5) }
					),
					new MineLevelConditions(MineType.Lava, Dangerous: false),
					new MineLevelConditions(MineType.Frost, Dangerous: true)
				),
				new(
					new(
						Weight: 0.35,
						BrazierCountWeightItems: new List<BrazierCombinationConfigEntryWeightItem>() { new(1, 4), new(1, 5) }
					),
					new MineLevelConditions(MineType.SkullCavern, Dangerous: false),
					new MineLevelConditions(MineType.Lava, Dangerous: true)
				),
				new(
					new(
						Weight: 0.35,
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
						Weight: 0.35,
						BrazierCountWeightItems: new List<BrazierSequenceConfigEntryWeightItem>() { new(1, 4) }
					),
					new MineLevelConditions(MineType.Earth, Dangerous: false)
				),
				new(
					new(
						Weight: 0.35,
						BrazierCountWeightItems: new List<BrazierSequenceConfigEntryWeightItem>() { new(1, 4), new(1, 5) }
					),
					new MineLevelConditions(MineType.Frost, Dangerous: false),
					new MineLevelConditions(MineType.Earth, Dangerous: true)
				),
				new(
					new(
						Weight: 0.35,
						BrazierCountWeightItems: new List<BrazierSequenceConfigEntryWeightItem>() { new(1, 5) }
					),
					new MineLevelConditions(MineType.Lava, Dangerous: false),
					new MineLevelConditions(MineType.Frost, Dangerous: true)
				),
				new(
					new(
						Weight: 0.35,
						BrazierCountWeightItems: new List<BrazierSequenceConfigEntryWeightItem>() { new(1, 5), new(1, 6) }
					),
					new MineLevelConditions(MineType.SkullCavern, Dangerous: false),
					new MineLevelConditions(MineType.Lava, Dangerous: true)
				),
				new(
					new(
						Weight: 0.35,
						BrazierCountWeightItems: new List<BrazierSequenceConfigEntryWeightItem>() { new(1, 6) }
					),
					new MineLevelConditions(MineType.SkullCavern, Dangerous: true)
				)
			}
		);

		[JsonProperty]
		public BrazierLightUpConfig BrazierLightUp { get; internal set; } = new(
			Enabled: true,
			Entries: new List<MineLevelConditionedConfig<BrazierLightUpConfigEntry>>()
			{
				new(
					new(Weight: 0.35, MinTorchCount: 6, MaxTorchCount: 7, MinInitialToggleCount: 2, MaxInitialToggleCount: 3),
					new MineLevelConditions(MineType.Earth, Dangerous: false)
				),
				new(
					new(Weight: 0.35, MinTorchCount: 7, MaxTorchCount: 9, MinInitialToggleCount: 3, MaxInitialToggleCount: 5),
					new MineLevelConditions(MineType.Frost, Dangerous: false),
					new MineLevelConditions(MineType.Earth, Dangerous: true)
				),
				new(
					new(Weight: 0.35, MinTorchCount: 8, MaxTorchCount: 11, MinInitialToggleCount: 4, MaxInitialToggleCount: 7),
					new MineLevelConditions(MineType.Lava, Dangerous: false),
					new MineLevelConditions(MineType.Frost, Dangerous: true)
				),
				new(
					new(Weight: 0.35, MinTorchCount: 9, MaxTorchCount: 13, MinInitialToggleCount: 5, MaxInitialToggleCount: 9),
					new MineLevelConditions(MineType.SkullCavern, Dangerous: false),
					new MineLevelConditions(MineType.Lava, Dangerous: true)
				),
				new(
					new(Weight: 0.35, MinTorchCount: 10, MaxTorchCount: 15, MinInitialToggleCount: 6, MaxInitialToggleCount: 11),
					new MineLevelConditions(MineType.SkullCavern, Dangerous: true)
				)
			}
		);

		[JsonProperty]
		public IceConfig Ice { get; internal set; } = new(
			Enabled: true,
			Entries: new List<MineLevelConditionedConfig<IceConfigEntry>>()
			{
				new(
					new(Weight: 0),
					new MineLevelConditions(MonsterArea: true)
				),
				new(
					new(Weight: 1),
					new MineLevelConditions(MineType.Frost, Dangerous: false)
				),
				new(
					new(Weight: 0.3),
					new MineLevelConditions()
				)
			}
		);

		[JsonProperty]
		public DisarmableConfig Disarmable { get; internal set; } = new(
			Enabled: true,
			Entries: new List<MineLevelConditionedConfig<DisarmableConfigEntry>>()
			{
				new(
					new(
						Weight: 0, MinButtonCount: 0, MaxButtonCount: 0,
						WeightItems: new List<DisarmableConfigEntryWeightItem>()
					),
					new MineLevelConditions(MonsterArea: true)
				),
				new(
					new(
						Weight: 1, MinButtonCount: 1, MaxButtonCount: 1,
						WeightItems: new List<DisarmableConfigEntryWeightItem>()
						{
							new(1, Rot: true),
							new(1, Explosion: new(Radius: 3, Damage: 50))
						}
					),
					new MineLevelConditions(MineType.Earth, Dangerous: false)
				),
				new(
					new(
						Weight: 1, MinButtonCount: 2, MaxButtonCount: 3,
						WeightItems: new List<DisarmableConfigEntryWeightItem>()
						{
							new(2, Explosion: new(Radius: 4, Damage: 100)),
							new(1, Explosion: new(Radius: 4, Damage: 100), Rot: true)
						}
					),
					new MineLevelConditions(MineType.Frost, Dangerous: false),
					new MineLevelConditions(MineType.Earth, Dangerous: true)
				),
				new(
					new(
						Weight: 1, MinButtonCount: 3, MaxButtonCount: 5,
						WeightItems: new List<DisarmableConfigEntryWeightItem>()
						{
							new(1, Explosion: new(Radius: 5, Damage: 150)),
							new(1, Explosion: new(Radius: 5, Damage: 150), Rot: true)
						}
					),
					new MineLevelConditions(MineType.Lava, Dangerous: false),
					new MineLevelConditions(MineType.Frost, Dangerous: true)
				),
				new(
					new(
						Weight: 1, MinButtonCount: 2, MaxButtonCount: 6,
						WeightItems: new List<DisarmableConfigEntryWeightItem>()
						{
							new(1, Explosion: new(Radius: 6, Damage: 200), Rot: true)
						}
					),
					new MineLevelConditions(MineType.SkullCavern),
					new MineLevelConditions(MineType.Lava, Dangerous: true)
				)
			}
		);
	}
}