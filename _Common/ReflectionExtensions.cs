using System;

namespace Shockah.CommonModCode
{
	public static class ReflectionExtensions
	{
		public static string GetBestName(this Type type)
			=> type.FullName ?? type.Name;
	}
}
