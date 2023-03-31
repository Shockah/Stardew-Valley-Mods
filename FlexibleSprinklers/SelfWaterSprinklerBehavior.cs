using Shockah.Kokoro;
using Shockah.Kokoro.Map;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.FlexibleSprinklers
{
	internal class SelfWaterSprinklerBehavior : ISprinklerBehavior
	{
		private readonly ISprinklerBehavior Wrapped;

		public SelfWaterSprinklerBehavior(ISprinklerBehavior wrapped)
		{
			this.Wrapped = wrapped;
		}

		public IReadOnlyList<(IReadOnlySet<IntPoint>, float)> GetSprinklerTilesWithSteps(IMap<SoilType>.WithKnownSize map, IReadOnlySet<SprinklerInfo> sprinklers)
		{
			var results = new List<(IReadOnlySet<IntPoint>, float)>
			{
				(sprinklers.SelectMany(s => s.OccupiedSpace.AllPointEnumerator()).ToHashSet(), 0f)
			};
			foreach (var wrappedResult in Wrapped.GetSprinklerTilesWithSteps(map, sprinklers))
				results.Add((wrappedResult.Item1, 0.2f + wrappedResult.Item2 * 0.8f));
			return results;
		}

		internal class Independent : ISprinklerBehavior.Independent
		{
			private readonly ISprinklerBehavior.Independent Wrapped;

			public Independent(ISprinklerBehavior.Independent wrapped)
			{
				this.Wrapped = wrapped;
			}

			public IReadOnlyList<(IReadOnlySet<IntPoint>, float)> GetSprinklerTilesWithSteps(IMap<SoilType>.WithKnownSize map, SprinklerInfo sprinkler)
			{
				var results = new List<(IReadOnlySet<IntPoint>, float)>
				{
					(sprinkler.OccupiedSpace.AllPointEnumerator().ToHashSet(), 0f)
				};
				foreach (var wrappedResult in Wrapped.GetSprinklerTilesWithSteps(map, sprinkler))
					results.Add((wrappedResult.Item1, 0.2f + wrappedResult.Item2 * 0.8f));
				return results;
			}
		}
	}
}