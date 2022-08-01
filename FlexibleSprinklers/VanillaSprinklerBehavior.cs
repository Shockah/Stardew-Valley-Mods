using Shockah.CommonModCode;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.FlexibleSprinklers
{
	internal class VanillaSprinklerBehavior : ISprinklerBehavior.Independent
	{
		public IList<(ISet<IntPoint>, float)> GetSprinklerTilesWithSteps(IMap map, IntPoint sprinklerPosition, SprinklerInfo info)
			=> new List<(ISet<IntPoint>, float)> { (info.Layout.Select(t => new IntPoint((int)t.X + sprinklerPosition.X, (int)t.Y + sprinklerPosition.Y)).ToHashSet(), 0f) };
	}
}