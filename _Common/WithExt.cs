using System;

namespace Shockah.CommonModCode
{
	public static class WithExt
	{
		public static T With<T>(this T self, Action<T> closure)
		{
			closure(self);
			return self;
		}

		public static T With<T, A>(this T self, A capture, Action<T, A> closure)
		{
			closure(self, capture);
			return self;
		}

		public static R Let<T, R>(this T self, Func<T, R> closure)
		{
			return closure(self);
		}
	}
}