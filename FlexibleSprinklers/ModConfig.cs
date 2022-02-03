namespace Shockah.FlexibleSprinklers
{
	internal class ModConfig
	{
		internal enum SprinklerBehaviorEnum
		{
			Flexible, FlexibleWithoutVanilla, Vanilla
		}

		public SprinklerBehaviorEnum SprinklerBehavior { get; set; } = SprinklerBehaviorEnum.Flexible;
		public FlexibleSprinklerBehavior.TileWaterBalanceMode TileWaterBalanceMode { get; set; } = FlexibleSprinklerBehavior.TileWaterBalanceMode.Relaxed;
		public bool ActivateOnPlacement { get; set; } = true;
		public bool ActivateOnAction { get; set; } = true;
		public int Tier1Power { get; set; } = 4;
		public int Tier2Power { get; set; } = 3 * 3 - 1;
		public int Tier3Power { get; set; } = 5 * 5 - 1;
		public int Tier4Power { get; set; } = 7 * 7 - 1;
		public int Tier5Power { get; set; } = 9 * 9 - 1;
		public int Tier6Power { get; set; } = 11 * 11 - 1;
		public int Tier7Power { get; set; } = 13 * 13 - 1;
		public int Tier8Power { get; set; } = 15 * 15 - 1;
		public bool CompatibilityMode { get; set; } = true;
	}
}
