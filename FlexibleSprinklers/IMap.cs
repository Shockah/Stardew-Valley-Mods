using System;

namespace Shockah.FlexibleSprinklers
{
	internal enum SoilType { Dry, Wet, Sprinkler, NonWaterable, NonSoil }

	internal interface IMap: IEquatable<IMap>
	{
		SoilType this[IntPoint point] { get; }

		void WaterTile(IntPoint point);
	}
}