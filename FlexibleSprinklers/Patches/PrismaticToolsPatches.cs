using HarmonyLib;

namespace Shockah.FlexibleSprinklers
{
	internal static class PrismaticToolsPatches
	{
		private static readonly string PrismaticToolsFrameworkSprinklerInitializerQualifiedName = "PrismaticTools.Framework.SprinklerInitializer, PrismaticTools";

		internal static void Apply(Harmony harmony)
		{
			try
			{
				harmony.Patch(
					original: AccessTools.Method(System.Type.GetType(PrismaticToolsFrameworkSprinklerInitializerQualifiedName), "TimeEvents_AfterDayStarted"),
					prefix: new HarmonyMethod(typeof(PrismaticToolsPatches), nameof(TimeEvents_AfterDayStarted_Prefix))
				);
			}
			catch (System.Exception e)
			{
				FlexibleSprinklers.Instance.Monitor.Log($"Could not patch BetterSprinklers - they probably won't work.\nReason: {e}", StardewModdingAPI.LogLevel.Warn);
			}
		}

		internal static bool TimeEvents_AfterDayStarted_Prefix()
		{
			return false;
		}
	}
}