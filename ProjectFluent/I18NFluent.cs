using StardewModdingAPI;

namespace Shockah.ProjectFluent
{
	internal class I18NFluent: IFluent<string>
	{
		private readonly ITranslationHelper Translations;

		public I18NFluent(ITranslationHelper translations)
		{
			this.Translations = translations;
		}

		public string Get(string key, object? tokens)
		{
			return Translations.Get(key, tokens);
		}
	}
}