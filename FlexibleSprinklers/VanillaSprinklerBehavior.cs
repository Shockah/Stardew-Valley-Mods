using System.Collections.Generic;
using System.Linq;

namespace Shockah.FlexibleSprinklers
{
    public class VanillaSprinklerBehavior: ISprinklerBehavior
    {
        public ISet<IntPoint> GetSprinklerTiles(IMap map, IntPoint sprinklerPosition, SprinklerInfo info)
        {
            return info.Layout.Select(e => e + sprinklerPosition).ToHashSet();
        }
    }
}