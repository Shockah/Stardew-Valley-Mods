using Newtonsoft.Json;
using Shockah.Kokoro;
using StardewModdingAPI;

namespace Shockah.JunimoWarp;

public class ModConfig : IVersioned.Modifiable
{
	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public ISemanticVersion? Version { get; set; }
	[JsonProperty] public bool RequiredEmptyChest { get; internal set; } = true;
}