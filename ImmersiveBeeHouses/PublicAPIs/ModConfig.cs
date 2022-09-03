using Newtonsoft.Json;

namespace Shockah.ImmersiveBeeHouses
{
	public class ModConfig
	{
		[JsonProperty] public float DaysToProduce { get; internal set; } = 8f;
		[JsonProperty] public float FlowerCoefficient { get; internal set; } = 0.5f;
		[JsonProperty] public bool CompatibilityMode { get; internal set; } = true;
	}
}