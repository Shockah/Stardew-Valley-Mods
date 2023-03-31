using Shockah.Kokoro;
using Shockah.Kokoro.Map;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.FlexibleSprinklers
{
	internal interface ISprinklerBehavior
	{
		void ClearCache()
		{
		}

		void ClearCacheForMap(IMap<SoilType>.WithKnownSize map)
		{
		}

		IReadOnlyList<(IReadOnlySet<IntPoint>, float)> GetSprinklerTilesWithSteps(IMap<SoilType>.WithKnownSize map, IReadOnlySet<SprinklerInfo> sprinklers);

		IReadOnlySet<IntPoint> GetSprinklerTiles(IMap<SoilType>.WithKnownSize map, IReadOnlySet<SprinklerInfo> sprinklers)
			=> GetSprinklerTilesWithSteps(map, sprinklers).SelectMany(step => step.Item1).ToHashSet();

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Nested in another interface")]
		public interface Independent : ISprinklerBehavior
		{
			IReadOnlyList<(IReadOnlySet<IntPoint>, float)> GetSprinklerTilesWithSteps(IMap<SoilType>.WithKnownSize map, SprinklerInfo sprinkler);

			IReadOnlyList<(IReadOnlySet<IntPoint>, float)> ISprinklerBehavior.GetSprinklerTilesWithSteps(IMap<SoilType>.WithKnownSize map, IReadOnlySet<SprinklerInfo> sprinklers)
			{
				List<(IReadOnlySet<IntPoint>, float)> results = new();
				foreach (var sprinkler in sprinklers)
					foreach (var step in GetSprinklerTilesWithSteps(map, sprinkler))
						results.Add(step);
				return results.OrderBy(step => step.Item2).ToList();
			}

			IReadOnlySet<IntPoint> GetSprinklerTiles(IMap<SoilType>.WithKnownSize map, SprinklerInfo sprinkler)
				=> GetSprinklerTilesWithSteps(map, sprinkler).SelectMany(step => step.Item1).ToHashSet();
		}
	}
}