using System.Collections.Generic;
using System.Linq;

namespace Shockah.FlexibleSprinklers
{
	internal static class SprinklerLayouts
	{
		internal static readonly ISet<IntPoint> Basic = IntPoint.NeighborOffsets.ToHashSet();
		internal static ISet<IntPoint> Quality => Box(1).ToHashSet();
		internal static ISet<IntPoint> Iridium => Box(2).ToHashSet();

		internal static ISet<IntPoint> Vanilla(int tier)
		{
			if (tier <= 1)
				return Basic;
			else
				return Box(tier - 1).ToHashSet();
		}

		private static IEnumerable<IntPoint> Box(int radius)
		{
			for (var y = -radius; y <= radius; y++)
			{
				for (var x = -radius; x <= radius; x++)
				{
					if (x != 0 || y != 0)
						yield return new IntPoint(x, y);
				}
			}
		}
	}
}