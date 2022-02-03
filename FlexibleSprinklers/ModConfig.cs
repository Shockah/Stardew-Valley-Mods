namespace Shockah.FlexibleSprinklers
{
    internal class ModConfig
    {
        public enum SprinklerBehavior
        {
            Flexible, FlexibleWithoutVanilla, Vanilla
        }

        public SprinklerBehavior sprinklerBehavior { get; set; } = SprinklerBehavior.Flexible;
        public FlexibleSprinklerBehavior.TileWaterBalanceMode tileWaterBalanceMode { get; set; } = FlexibleSprinklerBehavior.TileWaterBalanceMode.Relaxed;
        public bool activateOnPlacement { get; set; } = true;
        public bool activateOnAction { get; set; } = true;
    }
}
