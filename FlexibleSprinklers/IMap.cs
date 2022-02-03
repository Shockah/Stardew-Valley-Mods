namespace Shockah.FlexibleSprinklers
{
    public enum SoilType
    {
        Dry, Wet, Sprinkler, NonWaterable, NonSoil
    }

    public interface IMap
    {
        SoilType this[IntPoint point]
        {
            get;
        }

        void WaterTile(IntPoint point);
    }
}