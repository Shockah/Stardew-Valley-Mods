using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.CommonModCode
{
	public static class LinqExtensions
	{
		public static T? FirstOrNull<T>(this IEnumerable<T> self) where T: struct
			=> self.Select(e => new T?(e)).FirstOrDefault();

		public static T? FirstOrNull<T>(this IEnumerable<T> self, Func<T, bool> predicate) where T: struct
			=> self.Where(predicate).Select(e => new T?(e)).FirstOrDefault();
	}
}