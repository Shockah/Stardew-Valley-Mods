using StardewValley;
using StardewValley.GameData;
using System.Collections.Generic;
using static StardewValley.LocalizedContentManager;

namespace Shockah.ProjectFluent
{
	public abstract class GameLocale
	{
		public static readonly GameLocale Default = new BuiltIn(LocalizedContentManager.LanguageCode.en);
		
		public abstract string LanguageCode { get; }

		public IEnumerable<string> GetRelevantLanguageCodes()
		{
			// source: https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI/Framework/Translator.cs

			// given locale
			yield return LanguageCode;

			// broader locales (like pt-BR => pt)
			var current = LanguageCode;
			while (true)
			{
				int dashIndex = current.LastIndexOf('-');
				if (dashIndex <= 0)
					break;

				current = current[..dashIndex];
				yield return current;
			}
		}

		public sealed class BuiltIn: GameLocale
		{
			internal LanguageCode BuiltInLanguageCode { get; private set; }
			public override string LanguageCode => BuiltInLanguageCode == LocalizedContentManager.LanguageCode.en ? "en-US" : Game1.content.LanguageCodeString(BuiltInLanguageCode);

			public BuiltIn(LanguageCode code)
			{
				this.BuiltInLanguageCode = code;
			}
		}

		public sealed class Mod: GameLocale
		{
			internal ModLanguage Language { get; private set; }
			public override string LanguageCode => Language.LanguageCode;

			public Mod(ModLanguage language)
			{
				this.Language = language;
			}
		}
	}
}