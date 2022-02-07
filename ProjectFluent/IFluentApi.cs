using StardewModdingAPI;
using StardewValley.GameData;
using System;
using static StardewValley.LocalizedContentManager;

namespace Shockah.ProjectFluent
{
	public interface IFluentApi
	{
		#region Strongly-typed APIs

		GameLocale CurrentLocale { get; }

		IFluent<Key> GetLocalizations<Key>(IManifest mod, string name, GameLocale locale);
		IFluent<Key> GetLocalizationsForCurrentLocale<Key>(IManifest mod, string name);
		IFluent<Key> GetLocalizations<Key>(IManifest mod, GameLocale locale);
		IFluent<Key> GetLocalizationsForCurrentLocale<Key>(IManifest mod);

		#endregion

		#region Weakly-typed APIs

		LanguageCode? CurrentLanguageCode { get; }
		ModLanguage CurrentModLanguage { get; }

		Func<Key, object, string> GetLocalizationFunction<Key>(IManifest mod, string name, LanguageCode builtInLanguageCode);
		Func<Key, object, string> GetLocalizationFunction<Key>(IManifest mod, string name, ModLanguage modLanguage);
		Func<Key, object, string> GetLocalizationFunctionForCurrentLocale<Key>(IManifest mod, string name);
		Func<Key, object, string> GetLocalizationFunction<Key>(IManifest mod, LanguageCode builtInLanguageCode);
		Func<Key, object, string> GetLocalizationFunction<Key>(IManifest mod, ModLanguage modLanguage);
		Func<Key, object, string> GetLocalizationFunctionForCurrentLocale<Key>(IManifest mod);

		#endregion
	}
}