using System.Collections.Generic;

namespace Shockah.FlexibleSprinklers
{
	internal interface ISprinklerBehavior
	{
		bool AllowsIndependentSprinklerActivation { get; }

		void ClearCache()
		{
		}

		void ClearCacheForMap(IMap map)
		{
		}

		ISet<IntPoint> GetSprinklerTiles(IMap map, IntPoint sprinklerPosition, SprinklerInfo info);

		ISet<IntPoint> GetSprinklerTiles(IMap map, IEnumerable<(IntPoint position, SprinklerInfo info)> sprinklers);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Nested in another interface")]
		public interface Independent: ISprinklerBehavior
		{
			bool ISprinklerBehavior.AllowsIndependentSprinklerActivation
				=> true;

			ISet<IntPoint> ISprinklerBehavior.GetSprinklerTiles(IMap map, IEnumerable<(IntPoint position, SprinklerInfo info)> sprinklers)
			{
				var tiles = new HashSet<IntPoint>();
				foreach (var (sprinklerPosition, info) in sprinklers)
					tiles.UnionWith(GetSprinklerTiles(map, sprinklerPosition, info));
				return tiles;
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Nested in another interface")]
		public interface Collective: ISprinklerBehavior
		{
			bool ISprinklerBehavior.AllowsIndependentSprinklerActivation
				=> false;

			ISet<IntPoint> ISprinklerBehavior.GetSprinklerTiles(IMap map, IntPoint sprinklerPosition, SprinklerInfo info)
			{
				return GetSprinklerTiles(map, new[] { (sprinklerPosition, info) });
			}
		}
	}
}