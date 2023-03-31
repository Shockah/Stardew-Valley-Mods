using Shockah.Kokoro;
using Shockah.Kokoro.Map;
using System.Collections.Generic;

namespace Shockah.FlexibleSprinklers
{
	internal class VanillaSprinklerBehavior : ISprinklerBehavior.Independent
	{
		public IReadOnlyList<(IReadOnlySet<IntPoint>, float)> GetSprinklerTilesWithSteps(IMap<SoilType>.WithKnownSize map, SprinklerInfo sprinkler)
			=> new List<(IReadOnlySet<IntPoint>, float)> { (sprinkler.Coverage, 0f) };
	}
}