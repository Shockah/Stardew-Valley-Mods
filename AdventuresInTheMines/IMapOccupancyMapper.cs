using Shockah.Kokoro;
using Shockah.Kokoro.Map;
using StardewValley.Locations;
using System.Runtime.CompilerServices;

#if DEBUG
using System;
#endif

namespace Shockah.AdventuresInTheMines
{
	public interface IMapOccupancyMapper
	{
		public enum Tile
		{
			Empty,
			Dirt,
			Passable,
			BelowLadder,
			Blocked
		}

		IMap<Tile>.WithKnownSize MapOccupancy(MineShaft location);
	}

#if DEBUG
	public static class IMapOccupancyMapperTileExt
	{
		private static char GetCharacterForTile(this IMapOccupancyMapper.Tile tile)
		{
			return tile switch
			{
				IMapOccupancyMapper.Tile.Empty => '.',
				IMapOccupancyMapper.Tile.Dirt => 'o',
				IMapOccupancyMapper.Tile.Passable => '/',
				IMapOccupancyMapper.Tile.BelowLadder => 'V',
				IMapOccupancyMapper.Tile.Blocked => '#',
				_ => throw new ArgumentException($"{nameof(IMapOccupancyMapper.Tile)} has an invalid value."),
			};
		}
	}
#endif

	public sealed class MapOccupancyMapper : IMapOccupancyMapper
	{
		private ILadderFinder LadderFinder { get; init; }

		public MapOccupancyMapper(ILadderFinder ladderFinder)
		{
			this.LadderFinder = ladderFinder;
		}

		public IMap<IMapOccupancyMapper.Tile>.WithKnownSize MapOccupancy(MineShaft location)
		{
			var ladderPosition = LadderFinder.FindLadderPosition(location);
			IntPoint? belowLadderPosition = ladderPosition is null ? null : new(ladderPosition.Value.X, ladderPosition.Value.Y + 1);

			return new ArrayMap<IMapOccupancyMapper.Tile>(point =>
			{
				if (point == belowLadderPosition)
					return IMapOccupancyMapper.Tile.BelowLadder;
				else if (location.isTileLocationOpenIgnoreFrontLayers(new(point.X, point.Y)) && location.isTileClearForMineObjects(point.X, point.Y))
					return IMapOccupancyMapper.Tile.Empty;
				else if (location.doesEitherTileOrTileIndexPropertyEqual(point.X, point.Y, "Type", "Back", "Dirt"))
					return IMapOccupancyMapper.Tile.Dirt;
				else if (location.isTileLocationOpenIgnoreFrontLayers(new(point.X, point.Y)) && location.isTilePlaceable(new(point.X, point.Y)))
					return IMapOccupancyMapper.Tile.Passable;
				else
					return IMapOccupancyMapper.Tile.Blocked;
			}, (int)(location.Map.DisplayWidth / 64f), (int)(location.Map.DisplayHeight / 64f));
		}
	}

	public sealed class CachingMapOccupancyMapper : IMapOccupancyMapper
	{
		private IMapOccupancyMapper Wrapped { get; init; }
		private ConditionalWeakTable<MineShaft, IMap<IMapOccupancyMapper.Tile>.WithKnownSize> Cache { get; init; } = new();

		public CachingMapOccupancyMapper(IMapOccupancyMapper wrapped)
		{
			this.Wrapped = wrapped;
		}

		public IMap<IMapOccupancyMapper.Tile>.WithKnownSize MapOccupancy(MineShaft location)
		{
			if (!Cache.TryGetValue(location, out var map))
			{
				map = Wrapped.MapOccupancy(location);
				Cache.AddOrUpdate(location, map);
			}
			return map;
		}
	}

	public static class IMapOccupancyMapperExt
	{
		public static CachingMapOccupancyMapper Caching(this IMapOccupancyMapper self)
			=> (self as CachingMapOccupancyMapper) ?? new(self);
	}
}