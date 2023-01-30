using Shockah.CommonModCode;
using StardewValley.Locations;
using System.Runtime.CompilerServices;

namespace Shockah.AdventuresInTheMines
{
	public interface ILadderFinder
	{
		IntPoint? FindLadderPosition(MineShaft location);
	}

	public sealed class LadderFinder : ILadderFinder
	{
		private const int LadderTileIndex = 115;

		public IntPoint? FindLadderPosition(MineShaft location)
		{
			for (int y = 0; y < location.Map.DisplayHeight / 64f; y++)
				for (int x = 0; x < location.Map.DisplayWidth / 64f; x++)
					if (location.Map.GetLayer("Buildings").Tiles[new(x, y)]?.TileIndex == LadderTileIndex)
						return new(x, y);
			return null;
		}
	}

	public sealed class CachingLadderFinder : ILadderFinder
	{
		private ILadderFinder Wrapped { get; init; }
		private ConditionalWeakTable<MineShaft, NullableStructRef<IntPoint>> Cache { get; init; } = new();

		public CachingLadderFinder(ILadderFinder wrapped)
		{
			this.Wrapped = wrapped;
		}

		public IntPoint? FindLadderPosition(MineShaft location)
		{
			if (!Cache.TryGetValue(location, out var position))
			{
				position = Wrapped.FindLadderPosition(location);
				Cache.AddOrUpdate(location, position);
			}
			return position;
		}
	}

	public static class ILadderFinderExt
	{
		public static CachingLadderFinder Caching(this ILadderFinder self)
			=> (self as CachingLadderFinder) ?? new(self);
	}
}