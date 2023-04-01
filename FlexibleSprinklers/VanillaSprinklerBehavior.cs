using Shockah.Kokoro.Map;
using System.Collections.Generic;

namespace Shockah.FlexibleSprinklers
{
	internal class VanillaSprinklerBehavior : ISprinklerBehavior.Independent
	{
		public IReadOnlyList<WateringStep> GetSprinklerTilesWithSteps(IMap<SoilType>.WithKnownSize map, SprinklerInfo sprinkler)
			=> new List<WateringStep> { new(sprinkler.Coverage, 0f) };
	}
}