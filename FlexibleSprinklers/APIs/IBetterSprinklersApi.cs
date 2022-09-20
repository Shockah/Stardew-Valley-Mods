using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Shockah.FlexibleSprinklers
{
	public interface IBetterSprinklersApi
	{
		/// <summary>Get the relative tile coverage by supported sprinkler ID.</summary>
		IDictionary<int, Vector2[]> GetSprinklerCoverage();
	}
}
