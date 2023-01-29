using System;

namespace Shockah.CommonModCode
{
	public static class RandomExt
	{
		public static bool NextBool(this Random random)
			=> random.Next(2) == 0;
	}
}