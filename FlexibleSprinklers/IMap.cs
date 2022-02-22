using System;

namespace Shockah.FlexibleSprinklers
{
	internal enum SoilType { Waterable, Sprinkler, NonWaterable }

	internal interface IMap: IEquatable<IMap>
	{
		SoilType this[IntPoint point] { get; }

		void WaterTile(IntPoint point);
	}
}