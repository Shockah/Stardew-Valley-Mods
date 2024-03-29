﻿using StardewValley;
using StardewValley.Objects;
using System;
using System.Linq;

namespace Shockah.Kokoro.Stardew;

public static class ItemExt
{
	public static bool IsSameItem(this Item self, Item other, bool ignoringAmount = false)
	{
		int oldAmount = other.Stack;
		if (ignoringAmount)
			other.Stack = self.Stack;
		int compareToResult = self.CompareTo(other);
		other.Stack = oldAmount;

		if (compareToResult != 0)
			return false;

		if (self is ColoredObject coloredA && other is ColoredObject coloredB && coloredA.color.Value != coloredB.color.Value)
			return false;

		if (!self.modData.Pairs.OrderBy(kvp => kvp.Key).SequenceEqual(other.modData.Pairs.OrderBy(kvp => kvp.Key)))
			return false;

		return true;
	}
}