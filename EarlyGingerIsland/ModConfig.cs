using Newtonsoft.Json;
using Shockah.CommonModCode;
using StardewModdingAPI;
using System.Collections.Generic;

namespace Shockah.EarlyGingerIsland
{
	public class ModConfig : IVersioned.Modifiable
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public ISemanticVersion? Version { get; set; }
		[JsonProperty] public int BoatTicketPrice { get; set; } = 1000;
		[JsonProperty] public bool AllowIslandFarmBeforeCC { get; set; } = false;
		[JsonProperty] public PlantingOnIslandFarmBeforeCC PlantingOnIslandFarmBeforeCC { get; set; } = PlantingOnIslandFarmBeforeCC.OnlyOneCrop;
		[JsonProperty] public int BoatFixHardwoodRequired { get; set; } = 200;
		[JsonProperty] public int BoatFixIridiumBarsRequired { get; set; } = 5;
		[JsonProperty] public int BoatFixBatteryPacksRequired { get; set; } = 5;
		[JsonProperty] public IList<UnlockCondition> UnlockConditions { get; set; } = new List<UnlockCondition>() { new(new(1, "spring", 1), 8), new(new(1, "winter", 1), 4), new(new(2, "winter", 1), 0) };
	}
}