using System;

namespace Shockah.CommonModCode
{
	public static class WeakReferenceExt
	{
		public static T? GetTargetOrNull<T>(this WeakReference<T> self) where T : class
		{
			return self.TryGetTarget(out var target) ? target : null;
		}
	}
}
