using Newtonsoft.Json;
using Shockah.Kokoro;
using StardewModdingAPI;

namespace Shockah.AdvancedCrafting;

public class ModConfig : IVersioned.Modifiable
{
	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public ISemanticVersion? Version { get; set; }

	[JsonProperty] public SharingMode SkillSharing { get; internal set; } = SharingMode.PerPlayer;
}
