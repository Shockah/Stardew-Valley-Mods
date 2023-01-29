using HarmonyLib;
using Netcode;
using StardewValley;
using StardewValley.Network;
using System;

namespace Shockah.CommonModCode.Stardew
{
	public static class MutexExt
	{
		private static bool IsReflectionSetup { get; set; } = false;
		private static Func<NetMutex, long> GetOwner { get; set; } = null!;

		private static void SetupReflectionIfNeeded()
		{
			if (IsReflectionSetup)
				return;

			var ownerField = AccessTools.Field(typeof(NetMutex), "owner");
			GetOwner = mutex => ((NetLong)ownerField.GetValue(mutex)!).Value;

			IsReflectionSetup = true;
		}

		public static Farmer? GetCurrentOwner(this NetMutex mutex)
		{
			SetupReflectionIfNeeded();
			long owner = GetOwner(mutex);
			if (owner == NetMutex.NoOwner)
				return null;
			foreach (var player in Game1.getAllFarmers())
				if (player.UniqueMultiplayerID == owner)
					return player;
			return null;
		}
	}
}