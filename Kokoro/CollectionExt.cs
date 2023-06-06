using System.Collections.Generic;

namespace Shockah.Kokoro;

public static class CollectionExt
{
	public static void Toggle<T>(this ISet<T> set, T element)
	{
		if (set.Contains(element))
			set.Remove(element);
		else
			set.Add(element);
	}
}