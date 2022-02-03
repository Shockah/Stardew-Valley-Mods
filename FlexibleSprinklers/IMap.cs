namespace Shockah.FlexibleSprinklers
{
	internal enum SoilType
	{
		Dry, Wet, Sprinkler, NonWaterable, NonSoil
	}

	internal interface IMap
	{
		SoilType this[IntPoint point]
		{
			get;
		}

		void WaterTile(IntPoint point);
	}
}