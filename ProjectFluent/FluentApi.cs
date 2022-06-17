using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData;
using System;

namespace Shockah.ProjectFluent
{
	public class FluentApi: IFluentApi
	{
		private readonly ProjectFluent Instance;

		internal FluentApi(ProjectFluent instance)
		{
			this.Instance = instance;
		}

		public IGameLocale CurrentLocale => Instance.CurrentLocale;

		public IGameLocale GetBuiltInLocale(LocalizedContentManager.LanguageCode languageCode)
			=> new IGameLocale.BuiltIn(languageCode);

		public IGameLocale GetModLocale(ModLanguage language)
			=> new IGameLocale.Mod(language);

		public IFluent<string> GetLocalizations(IGameLocale locale, IManifest mod, string? name = null)
			=> Instance.GetLocalizations(locale, mod, name);

		public IFluent<string> GetLocalizationsForCurrentLocale(IManifest mod, string? name = null)
			=> new CurrentLocaleFluent(mod, name);

		public IEnumFluent<EnumType> GetEnumFluent<EnumType>(IFluent<string> baseFluent, string keyPrefix) where EnumType : Enum
			=> new EnumFluent<EnumType>(baseFluent, keyPrefix);

		public IFluent<T> GetMappingFluent<T>(IFluent<string> baseFluent, Func<T, string> mapper)
			=> new MappingFluent<T>(baseFluent, mapper);
	}
}