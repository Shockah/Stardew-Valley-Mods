using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData;
using System;

namespace Shockah.ProjectFluent
{
	public interface IFluentApi
	{
		IGameLocale CurrentLocale { get; }

		IGameLocale GetBuiltInLocale(LocalizedContentManager.LanguageCode languageCode);
		IGameLocale GetModLocale(ModLanguage language);

		IFluent<Key> GetLocalizations<Key>(IGameLocale locale, IManifest mod, string? name = null) where Key : notnull;
		IFluent<Key> GetLocalizationsForCurrentLocale<Key>(IManifest mod, string? name = null);

		IEnumFluent<EnumType> GetEnumFluent<EnumType>(IFluent<string> baseFluent, string keyPrefix = "") where EnumType: Enum;
	}
}