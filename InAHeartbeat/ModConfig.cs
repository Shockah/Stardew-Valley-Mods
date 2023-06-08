using Newtonsoft.Json;

namespace Shockah.InAHeartbeat;

public sealed class ModConfig
{
	[JsonProperty] public ActionConfig Date { get; internal set; } = new(2000, 1750, 1500, 1250);
	[JsonProperty] public ActionConfig Marry { get; internal set; } = new(2500, 2250, 2000, 1750);
	[JsonProperty] public int BouquetFlowersRequired { get; internal set; } = 5;
	[JsonProperty] public int BouquetFlowerTypesRequired { get; internal set; } = 2;
	[JsonProperty] public int PendantGemsRequiredForRegular { get; internal set; } = 2;
	[JsonProperty] public int PendantGemsRequiredForSilver { get; internal set; } = 4;
	[JsonProperty] public int PendantGemsRequiredForGold { get; internal set; } = 6;
	[JsonProperty] public int PendantGemsRequiredForIridium { get; internal set; } = 8;
}