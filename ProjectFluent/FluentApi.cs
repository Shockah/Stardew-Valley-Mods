using StardewModdingAPI;
using StardewValley.GameData;
using System;
using static StardewValley.LocalizedContentManager;

namespace Shockah.ProjectFluent
{
	public class FluentApi: IFluentApi
	{
		private readonly ProjectFluent instance;

		internal FluentApi(ProjectFluent instance)
		{
			this.instance = instance;
		}

		#region Strongly-typed APIs

		public GameLocale CurrentLocale => instance.CurrentLocale;

		public IFluent<Key> GetLocalizations<Key>(IManifest mod, string name, GameLocale locale)
			=> instance.GetLocalizations<Key>(mod, name, locale);

		public IFluent<Key> GetLocalizationsForCurrentLocale<Key>(IManifest mod, string name)
			=> GetLocalizations<Key>(mod, name, CurrentLocale);

		public IFluent<Key> GetLocalizations<Key>(IManifest mod, GameLocale locale)
			=> GetLocalizations<Key>(mod, null, locale);

		public IFluent<Key> GetLocalizationsForCurrentLocale<Key>(IManifest mod)
			=> GetLocalizationsForCurrentLocale<Key>(mod, null);

		#endregion

		#region Weakly-typed APIs

		public LanguageCode? CurrentLanguageCode => (CurrentLocale as GameLocale.BuiltIn)?.BuiltInLanguageCode;

		public ModLanguage CurrentModLanguage => (CurrentLocale as GameLocale.Mod)?.Language;

		public Func<Key, object, string> GetLocalizationFunction<Key>(IManifest mod, string name, LanguageCode builtInLanguageCode)
		{
			var fluent = GetLocalizations<Key>(mod, name, new GameLocale.BuiltIn(builtInLanguageCode));
			return (key, tokens) => fluent.Get(key, tokens);
		}

		public Func<Key, object, string> GetLocalizationFunction<Key>(IManifest mod, string name, ModLanguage modLanguage)
		{
			var fluent = GetLocalizations<Key>(mod, name, new GameLocale.Mod(modLanguage));
			return (key, tokens) => fluent.Get(key, tokens);
		}

		public Func<Key, object, string> GetLocalizationFunctionForCurrentLocale<Key>(IManifest mod, string name)
		{
			var fluent = GetLocalizationsForCurrentLocale<Key>(mod, name);
			return (key, tokens) => fluent.Get(key, tokens);
		}

		public Func<Key, object, string> GetLocalizationFunction<Key>(IManifest mod, LanguageCode builtInLanguageCode)
			=> GetLocalizationFunction<Key>(mod, null, builtInLanguageCode);

		public Func<Key, object, string> GetLocalizationFunction<Key>(IManifest mod, ModLanguage modLanguage)
			=> GetLocalizationFunction<Key>(mod, null, modLanguage);

		public Func<Key, object, string> GetLocalizationFunctionForCurrentLocale<Key>(IManifest mod)
			=> GetLocalizationFunctionForCurrentLocale<Key>(mod, null);

		#endregion

		#region Temporary stuff until SMAPI works

		public Func<string, object, string> GetLocalizationFunctionForStringKeysForCurrentLocale(IManifest mod)
			=> GetLocalizationFunctionForCurrentLocale<string>(mod, null);

		#endregion
	}
}
