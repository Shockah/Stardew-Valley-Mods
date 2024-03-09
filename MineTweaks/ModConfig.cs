using Newtonsoft.Json;

namespace Shockah.MineTweaks;

public sealed class ModConfig
{
	[JsonProperty] public MineTypeConfig Mine { get; internal set; } = new();
	[JsonProperty] public MineTypeConfig SkullCavern { get; internal set; } = new();
	[JsonProperty] public MineTypeConfig Volcano { get; internal set; } = new();
}