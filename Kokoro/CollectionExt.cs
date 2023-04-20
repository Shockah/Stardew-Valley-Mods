using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Shockah.Kokoro
{
	public static class CollectionExt
	{
		public static void Toggle<T>(this ISet<T> set, T element)
		{
			if (set.Contains(element))
				set.Remove(element);
			else
				set.Add(element);
		}

		public static void Push<TKey>(this ConditionalWeakTable<TKey, StructRef<int>> table, TKey key) where TKey : class
		{
			int counter = (table.TryGetValue(key, out var counterRef) ? counterRef.Value : 0) + 1;
			table.AddOrUpdate(key, new(counter));
		}

		public static void Pop<TKey>(this ConditionalWeakTable<TKey, StructRef<int>> table, TKey key) where TKey : class
		{
			int counter = (table.TryGetValue(key, out var counterRef) ? counterRef.Value : 0) - 1;
			if (counter == 0)
				table.Remove(key);
			else
				table.AddOrUpdate(key, new(counter));
		}

		public static bool PushPopAndDoIfFirst<TKey>(this ConditionalWeakTable<TKey, StructRef<int>> table, TKey key, Action function) where TKey : class
		{
			int counter = (table.TryGetValue(key, out var counterRef) ? counterRef.Value : 0) + 1;
			bool wasFirst = counter == 1;
			table.AddOrUpdate(key, new(counter));

			if (wasFirst)
				function();

			counter = (table.TryGetValue(key, out counterRef) ? counterRef.Value : 0) - 1;
			if (counter == 0)
				table.Remove(key);
			else
				table.AddOrUpdate(key, new(counter));

			return wasFirst;
		}

		public static bool TryPushPopAndDoIfFirst<TKey>(this ConditionalWeakTable<TKey, StructRef<int>> table, TKey key, Action function) where TKey : class
		{
			int counter = (table.TryGetValue(key, out var counterRef) ? counterRef.Value : 0) + 1;
			bool wasFirst = counter == 1;
			table.AddOrUpdate(key, new(counter));

			try
			{
				if (wasFirst)
					function();
			}
			finally
			{
				counter = (table.TryGetValue(key, out counterRef) ? counterRef.Value : 0) - 1;
				if (counter == 0)
					table.Remove(key);
				else
					table.AddOrUpdate(key, new(counter));
			}

			return wasFirst;
		}
	}
}