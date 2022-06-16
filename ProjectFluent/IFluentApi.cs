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

		IFluent<string> GetLocalizations(IGameLocale locale, IManifest mod, string? name = null);
		IFluent<string> GetLocalizationsForCurrentLocale(IManifest mod, string? name = null);

		IEnumFluent<EnumType> GetEnumFluent<EnumType>(IFluent<string> baseFluent, string keyPrefix = "") where EnumType : Enum;
	}
}