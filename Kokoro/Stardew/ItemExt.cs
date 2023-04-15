using StardewValley;
using System;
using System.Linq;

namespace Shockah.Kokoro.Stardew
{
	public static class ItemExt
	{
		public static bool IsSameItem(this Item self, Item other)
			=> self.CompareTo(other) == 0
			&& self.modData.Pairs.OrderBy(kvp => kvp.Key).SequenceEqual(other.modData.Pairs.OrderBy(kvp => kvp.Key));
	}
}