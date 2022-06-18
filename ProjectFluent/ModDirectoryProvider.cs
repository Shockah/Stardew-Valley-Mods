using HarmonyLib;
using StardewModdingAPI;
using System;
using System.Reflection;

namespace Shockah.ProjectFluent
{
	internal interface IModDirectoryProvider
	{
		string GetModDirectoryPath(IManifest mod);
	}

	internal class ModDirectoryProvider: IModDirectoryProvider
	{
		private bool IsReflectionSetup { get; set; } = false;
		private Func<IManifest, string> GetModDirectoryPathDelegate { get; set; } = null!;

		private void SetupReflectionIfNeeded()
		{
			if (IsReflectionSetup)
				return;

			Type modMetadataType = AccessTools.TypeByName("StardewModdingAPI.Framework.IModMetadata, StardewModdingAPI")!;
			MethodInfo directoryPathGetter = AccessTools.PropertyGetter(modMetadataType, "DirectoryPath");
			GetModDirectoryPathDelegate = (manifest) => (string)directoryPathGetter.Invoke(manifest, null)!;

			IsReflectionSetup = true;
		}

		public string GetModDirectoryPath(IManifest mod)
		{
			SetupReflectionIfNeeded();
			return GetModDirectoryPathDelegate(mod);
		}
	}
}