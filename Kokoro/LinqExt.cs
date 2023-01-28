using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Shockah.Kokoro;

public static class LinqExt
{
	public static T? FirstOrNull<T>(this IEnumerable<T> self) where T : struct
		=> self.Select(e => new T?(e)).FirstOrDefault();

	public static T? FirstOrNull<T>(this IEnumerable<T> self, Func<T, bool> predicate) where T : struct
		=> self.Where(predicate).Select(e => new T?(e)).FirstOrDefault();

	public static T? LastOrNull<T>(this IEnumerable<T> self) where T : struct
		=> self.Select(e => new T?(e)).LastOrDefault();

	public static T? LastOrNull<T>(this IEnumerable<T> self, Func<T, bool> predicate) where T : struct
		=> self.Where(predicate).Select(e => new T?(e)).LastOrDefault();

	public static int? FirstIndex<T>(this IList<T> self, Func<T, bool> predicate)
	{
		int index = 0;
		foreach (var item in self)
		{
			if (predicate(item))
				return index;
			index++;
		}
		return null;
		}

		public static bool TryFirst<T>(this IEnumerable<T> self, Func<T, bool> predicate, [NotNullWhen(true)] out T? result) where T : class
		{
			foreach (var item in self)
			{
				if (predicate(item))
				{
					result = item;
					return true;
				}
			}

			result = null;
			return false;
		}

		public static bool TryFirst<T>(this IEnumerable<T> self, Func<T, bool> predicate, [NotNullWhen(true)] out T? result) where T : struct
		{
			foreach (var item in self)
			{
				if (predicate(item))
				{
					result = item;
					return true;
				}
			}

			result = null;
			return false;
	}

	public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> self) where T : class
	{
		foreach (var element in self)
			if (element is not null)
				yield return element;
	}

	public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> self) where T : struct
	{
		foreach (var element in self)
			if (element is not null)
				yield return element.Value;
	}
}