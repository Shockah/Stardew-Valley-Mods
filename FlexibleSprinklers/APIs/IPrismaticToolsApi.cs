using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Shockah.FlexibleSprinklers
{
	interface IPrismaticToolsApi
	{
		int SprinklerIndex { get; }
		int SprinklerRange { get; }

		IEnumerable<Vector2> GetSprinklerCoverage(Vector2 origin);
	}
}