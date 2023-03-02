using Shockah.Kokoro;
using Shockah.Kokoro.Map;
using StardewValley.Locations;
using System;
using System.Runtime.CompilerServices;

namespace Shockah.AdventuresInTheMines
{
	public interface IReachableTileMapper
	{
		IMap<bool>.WithKnownSize MapReachableTiles(MineShaft location);
	}

	public sealed class ReachableTileMapper : IReachableTileMapper
	{
		private ILadderFinder LadderFinder { get; init; }
		private IMapOccupancyMapper MapOccupancyMapper { get; init; }

		public ReachableTileMapper(ILadderFinder ladderFinder, IMapOccupancyMapper mapOccupancyMapper)
		{
			this.LadderFinder = ladderFinder;
			this.MapOccupancyMapper = mapOccupancyMapper;
		}

		public IMap<bool>.WithKnownSize MapReachableTiles(MineShaft location)
		{
			var ladderPosition = LadderFinder.FindLadderPosition(location) ?? throw new ArgumentException($"The {location} location does not seem to have a ladder.");
			IntPoint belowLadderPosition = new(ladderPosition.X, ladderPosition.Y + 1);

			var occupancyMap = MapOccupancyMapper.MapOccupancy(location);
			return FloodFill.Run(occupancyMap, belowLadderPosition, (map, point) => map[point] != IMapOccupancyMapper.Tile.Blocked);
		}
	}

	public sealed class CachingReachableTileMapper : IReachableTileMapper
	{
		private IReachableTileMapper Wrapped { get; init; }
		private ConditionalWeakTable<MineShaft, IMap<bool>.WithKnownSize> Cache { get; init; } = new();

		public CachingReachableTileMapper(IReachableTileMapper wrapped)
		{
			this.Wrapped = wrapped;
		}

		public IMap<bool>.WithKnownSize MapReachableTiles(MineShaft location)
		{
			if (!Cache.TryGetValue(location, out var map))
			{
				map = Wrapped.MapReachableTiles(location);
				Cache.AddOrUpdate(location, map);
			}
			return map;
		}
	}

	public static class IReachableTileMapperExt
	{
		public static CachingReachableTileMapper Caching(this IReachableTileMapper self)
			=> (self as CachingReachableTileMapper) ?? new(self);
	}
}