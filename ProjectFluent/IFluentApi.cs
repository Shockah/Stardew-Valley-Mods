using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData;
using System;
using System.Collections.Generic;

namespace Shockah.ProjectFluent
{
	public interface IFluentApi
	{
		#region Locale

		IGameLocale CurrentLocale { get; }

		IGameLocale GetBuiltInLocale(LocalizedContentManager.LanguageCode languageCode);
		IGameLocale GetModLocale(ModLanguage language);

		#endregion

		#region Localizations

		IFluent<string> GetLocalizations(IGameLocale locale, IManifest mod, string? name = null);
		IFluent<string> GetLocalizationsForCurrentLocale(IManifest mod, string? name = null);

		#endregion

		#region Specialized types

		IEnumFluent<EnumType> GetEnumFluent<EnumType>(IFluent<string> baseFluent, string keyPrefix = "") where EnumType : Enum;
		IFluent<T> GetMappingFluent<T>(IFluent<string> baseFluent, Func<T, string> mapper);

		#endregion

		#region Custom Fluent functions

		public delegate object FluentFunction(IGameLocale locale, IManifest mod, IReadOnlyList<object> arguments);

		void RegisterFunction(IManifest mod, string name, FluentFunction function);
		void UnregisterFunction(IManifest mod, string name);

		#endregion
	}
}