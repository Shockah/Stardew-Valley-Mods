using StardewModdingAPI;

namespace Shockah.ProjectFluent
{
	internal class I18nFluent: IFluent<string>
	{
		private readonly ITranslationHelper Translations;

		public I18nFluent(ITranslationHelper translations)
		{
			this.Translations = translations;
		}

		public bool ContainsKey(string key)
		{
			return Translations.Get(key).HasValue();
		}

		public string Get(string key, object? tokens)
		{
			return Translations.Get(key, tokens);
		}
	}
}