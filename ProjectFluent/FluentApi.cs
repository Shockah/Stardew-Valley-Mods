using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData;

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

		public IFluent<Key> GetLocalizations<Key>(IGameLocale locale, IManifest mod, string? name = null) where Key : notnull
			=> Instance.GetLocalizations<Key>(locale, mod, name);

		public IFluent<Key> GetLocalizationsForCurrentLocale<Key>(IManifest mod, string? name = null)
			=> new CurrentLocaleFluent<Key>(mod, name);

		IEnumFluent<EnumType> IFluentApi.GetEnumFluent<EnumType>(IFluent<string> baseFluent, string keyPrefix)
			=> new EnumFluent<EnumType>(baseFluent, keyPrefix);
	}
}
