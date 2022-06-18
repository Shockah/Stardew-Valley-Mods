using HarmonyLib;
using StardewModdingAPI;
using System;
using System.Reflection;

namespace Shockah.ProjectFluent
{
	internal interface IModTranslationsProvider
	{
		ITranslationHelper? GetModTranslations(IManifest mod);
	}

	internal class ModTranslationsProvider: IModTranslationsProvider
	{
		private bool IsReflectionSetup { get; set; } = false;
		private Func<IManifest, ITranslationHelper?> GetModTranslationsDelegate { get; set; } = null!;

		private void SetupReflectionIfNeeded()
		{
			if (IsReflectionSetup)
				return;

			Type modMetadataType = AccessTools.TypeByName("StardewModdingAPI.Framework.IModMetadata, StardewModdingAPI")!;
			MethodInfo translationsGetter = AccessTools.PropertyGetter(modMetadataType, "Translations");
			GetModTranslationsDelegate = (manifest) => translationsGetter.Invoke(manifest, null) as ITranslationHelper;

			IsReflectionSetup = true;
		}

		public ITranslationHelper? GetModTranslations(IManifest mod)
		{
			SetupReflectionIfNeeded();
			return GetModTranslationsDelegate(mod);
		}
	}
}