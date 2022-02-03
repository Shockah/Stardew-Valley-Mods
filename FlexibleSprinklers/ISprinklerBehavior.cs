using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Shockah.FlexibleSprinklers
{
    public interface ISprinklerBehavior
    {
        ISet<IntPoint> GetSprinklerTiles(IMap map, IntPoint sprinklerPosition, SprinklerInfo info);
    }
}