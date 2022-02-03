using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Shockah.FlexibleSprinklers
{
	internal interface ISprinklerBehavior
	{
		ISet<IntPoint> GetSprinklerTiles(IMap map, IntPoint sprinklerPosition, SprinklerInfo info);
	}
}