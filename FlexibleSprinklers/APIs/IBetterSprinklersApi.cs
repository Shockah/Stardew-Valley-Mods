using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Shockah.FlexibleSprinklers
{
    /// <summary>The API which provides access to Better Sprinklers for other mods.</summary>
    public interface IBetterSprinklersApi
    {
        /// <summary>Get the relative tile coverage by supported sprinkler ID.</summary>
        IDictionary<int, Vector2[]> GetSprinklerCoverage();
    }
}
