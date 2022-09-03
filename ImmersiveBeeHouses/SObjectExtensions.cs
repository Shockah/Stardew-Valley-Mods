using Netcode;
using System.Runtime.CompilerServices;
using SObject = StardewValley.Object;

namespace Shockah.ImmersiveBeeHouses
{
	public static class SObjectExtensions
	{
		private class Holder
		{
			internal static ConditionalWeakTable<SObject, Holder> Values { get; private set; } = new();

			public NetInt BeeHouseStartingMinutesUntilReady { get; private set; } = new(0);
		}

		internal static NetInt GetBeeHouseStartingMinutesUntilReadyNetField(this SObject self)
			=> Holder.Values.GetOrCreateValue(self).BeeHouseStartingMinutesUntilReady;

		public static int GetBeeHouseStartingMinutesUntilReady(this SObject instance)
			=> Holder.Values.TryGetValue(instance, out var holder) ? holder.BeeHouseStartingMinutesUntilReady.Value : 0;

		public static void SetBeeHouseStartingMinutesUntilReady(this SObject instance, int value)
			=> Holder.Values.GetOrCreateValue(instance).BeeHouseStartingMinutesUntilReady.Set(value);
	}
}