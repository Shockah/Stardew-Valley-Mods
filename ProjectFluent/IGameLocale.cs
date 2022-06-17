using StardewValley;
using StardewValley.GameData;
using System;
using System.Collections.Generic;

namespace Shockah.ProjectFluent
{
	public interface IGameLocale
	{
		static readonly IGameLocale Default = new BuiltIn(LocalizedContentManager.LanguageCode.en);

		string LanguageCode { get; }

		bool IsBuiltInLocale
			=> this is BuiltIn;

		bool IsModLocale
			=> this is Mod;

		public sealed class BuiltIn: IGameLocale
		{
			internal LocalizedContentManager.LanguageCode BuiltInLanguageCode { get; private set; }
			public string LanguageCode => BuiltInLanguageCode == LocalizedContentManager.LanguageCode.en ? "en-US" : Game1.content.LanguageCodeString(BuiltInLanguageCode);

			public BuiltIn(LocalizedContentManager.LanguageCode code)
			{
				if (code == LocalizedContentManager.LanguageCode.mod)
					throw new ArgumentException("`mod` is not a valid built-in locale.");
				this.BuiltInLanguageCode = code;
			}
		}

		public sealed class Mod: IGameLocale
		{
			internal ModLanguage Language { get; private set; }
			public string LanguageCode => Language.LanguageCode;

			public Mod(ModLanguage language)
			{
				this.Language = language;
			}
		}
	}

	internal static class IGameLocaleExtensions
	{
		internal static IEnumerable<string> GetRelevantLocaleCodes(this IGameLocale self)
		{
			// source: https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI/Framework/Translator.cs

			// given locale
			yield return self.LanguageCode;

			// broader locales (like pt-BR => pt)
			var current = self.LanguageCode;
			while (true)
			{
				int dashIndex = current.LastIndexOf('-');
				if (dashIndex <= 0)
					break;

				current = current[..dashIndex];
				yield return current;
			}
		}
	}
}