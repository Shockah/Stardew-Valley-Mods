using Shockah.Kokoro;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.FlexibleSprinklers
{
	internal class VanillaSprinklerBehavior : ISprinklerBehavior.Independent
	{
		public IReadOnlyList<(IReadOnlySet<IntPoint>, float)> GetSprinklerTilesWithSteps(IMap map, IntPoint sprinklerPosition, SprinklerInfo info)
			=> new List<(IReadOnlySet<IntPoint>, float)> { (info.Layout.Select(t => t + sprinklerPosition).ToHashSet(), 0f) };
	}
}