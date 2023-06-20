using Newtonsoft.Json;
using Shockah.Kokoro;
using StardewModdingAPI;

namespace Shockah.PredictableRetainingSoil;

public class ModConfig : IVersioned.Modifiable
{
	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public ISemanticVersion? Version { get; set; }
	[JsonProperty] public int BasicRetainingSoilDays { get; set; } = 1;
	[JsonProperty] public int QualityRetainingSoilDays { get; set; } = 3;
	[JsonProperty] public int DeluxeRetainingSoilDays { get; set; } = -1;
}