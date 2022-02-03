using System.Collections.Generic;
using System.Linq;

namespace Shockah.FlexibleSprinklers
{
	internal class VanillaSprinklerBehavior: ISprinklerBehavior
	{
		public ISet<IntPoint> GetSprinklerTiles(IMap map, IntPoint sprinklerPosition, SprinklerInfo info)
		{
			return info.Layout.Select(t => new IntPoint((int)t.X + sprinklerPosition.X, (int)t.Y + sprinklerPosition.Y)).ToHashSet();
		}
	}
}