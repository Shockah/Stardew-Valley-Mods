using Shockah.Kokoro;
using System.Collections.Generic;

namespace Shockah.FlexibleSprinklers
{
	internal readonly struct WateringStep
	{
		public readonly IReadOnlySet<IntPoint> Tiles { get; init; }
		public readonly float Time { get; init; }

		public WateringStep(IReadOnlySet<IntPoint> tiles, float time)
		{
			this.Tiles = tiles;
			this.Time = time;
		}
	}
}