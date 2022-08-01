using StardewModdingAPI;

namespace Shockah.ProjectFluent
{
	internal interface IFallbackFluentProvider
	{
		IFluent<string> GetFallbackFluent(IManifest mod);
	}

	internal class FallbackFluentProvider : IFallbackFluentProvider
	{
		private IModTranslationsProvider ModTranslationsProvider { get; set; }

		public FallbackFluentProvider(IModTranslationsProvider modTranslationsProvider)
		{
			this.ModTranslationsProvider = modTranslationsProvider;
		}

		public IFluent<string> GetFallbackFluent(IManifest mod)
		{
			var translations = ModTranslationsProvider.GetModTranslations(mod);
			return translations is null ? new NoOpFluent() : new I18nFluent(translations);
		}
	}
}