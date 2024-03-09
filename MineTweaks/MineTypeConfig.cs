using Newtonsoft.Json;

namespace Shockah.MineTweaks;

public sealed class MineTypeConfig
{
	[JsonProperty] public float StoneChanceMultiplier { get; internal set; } = 1f;
	[JsonProperty] public float MonsterChanceMultiplier { get; internal set; } = 1f;
	[JsonProperty] public float ItemChanceMultiplier { get; internal set; } = 1f;
	[JsonProperty] public float GemStoneChanceMultiplier { get; internal set; } = 1f;
	[JsonProperty] public float MonsterMuskChanceMultiplier { get; internal set; } = 2f;
}