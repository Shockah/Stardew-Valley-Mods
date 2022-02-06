using Netcode;
using StardewValley.TerrainFeatures;
using System.Runtime.CompilerServices;

namespace Shockah.PredictableRetainingSoil
{
	public static class HoeDirtExtensions
	{
		private class Holder
		{
			internal static ConditionalWeakTable<HoeDirt, Holder> Values = new();

			public readonly NetInt RetainingSoilDaysLeft = new(0);
		}

		internal static NetInt GetRetainingSoilDaysLeftNetField(this HoeDirt instance)
		{
			return Holder.Values.GetOrCreateValue(instance).RetainingSoilDaysLeft;
		}

		public static int GetRetainingSoilDaysLeft(this HoeDirt instance)
		{
			return Holder.Values.TryGetValue(instance, out var holder) ? holder.RetainingSoilDaysLeft.Value : 0;
		}

		public static void SetRetainingSoilDaysLeft(this HoeDirt instance, int value)
		{
			Holder.Values.GetOrCreateValue(instance).RetainingSoilDaysLeft.Set(value);
		}
	}
}