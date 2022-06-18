using HarmonyLib;
using Shockah.CommonModCode.GMCM;
using Shockah.ProjectFluent.ContentPatcher;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.ProjectFluent
{
	public class ProjectFluent: Mod
	{
		public static ProjectFluent Instance { get; private set; } = null!;
		public IFluentApi Api { get; private set; } = null!;
		private AssetManager AssetManager { get; set; } = null!;

		private Harmony Harmony { get; set; }
		internal ModConfig Config { get; private set; } = null!;
		internal IFluent<string> Fluent { get; private set; } = null!;

		internal IModDirectoryProvider ModDirectoryProvider { get; set; } = null!;
		internal IFluentPathProvider FluentPathProvider { get; set; } = null!;
		internal IModDirectoryFluentPathProvider ModDirectoryFluentPathProvider { get; set; } = null!;
		internal IModTranslationsProvider ModTranslationsProvider { get; set; } = null!;
		internal IFallbackFluentProvider FallbackFluentProvider { get; set; } = null!;

		public ProjectFluent()
		{
			Harmony = new Harmony("Shockah.ProjectFluent");
			I18nIntegration.SetupEarly(Harmony);
		}

		public override void Entry(IModHelper helper)
		{
			Instance = this;
			ModDirectoryProvider = new ModDirectoryProvider();
			FluentPathProvider = new FluentPathProvider();
			ModDirectoryFluentPathProvider = new ModDirectoryFluentPathProvider(ModDirectoryProvider, FluentPathProvider);
			ModTranslationsProvider = new ModTranslationsProvider();
			FallbackFluentProvider = new FallbackFluentProvider(ModTranslationsProvider);

			Api = new FluentApi(this);
			AssetManager = new AssetManager();

			Config = helper.ReadConfig<ModConfig>();
			Fluent = Api.GetLocalizationsForCurrentLocale(ModManifest);

			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			AssetManager.Setup(helper);
			I18nIntegration.Setup(Harmony);
			I18nIntegration.ReloadTranslations();
		}

		public override object GetApi() => Api;

		private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
		{
			var harmony = new Harmony(ModManifest.UniqueID);
			ContentPatcherIntegration.Setup(harmony);

			SetupConfig();
		}

		private void SetupConfig()
		{
			var api = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
			if (api is null)
				return;
			GMCMI18nHelper helper = new(
				api, ModManifest, new FluentTranslationSet<string>(Fluent),
				namePattern: "{Key}",
				tooltipPattern: "{Key}.tooltip",
				valuePattern: "{Key}.{Value}"
			);

			static IEnumerable<string> GetBuiltInLocaleCodes()
			{
				foreach (LocalizedContentManager.LanguageCode value in Enum.GetValues<LocalizedContentManager.LanguageCode>())
				{
					if (value == LocalizedContentManager.LanguageCode.mod)
						continue;
					yield return value == LocalizedContentManager.LanguageCode.en ? "en-US" : Game1.content.LanguageCodeString(value);
				}
			}

			api.Register(
				ModManifest,
				reset: () => Config = new ModConfig(),
				save: () => Helper.WriteConfig(Config)
			);

			helper.AddEnumOption(
				keyPrefix: "config-contentPatcherPatchingMode",
				property: () => Config.ContentPatcherPatchingMode
			);

			helper.AddTextOption(
				keyPrefix: "config-localeOverride",
				property: () => Config.CurrentLocaleOverride
			);

			helper.AddParagraph(
				"config-localeOverrideSubtitle",
				new { Values = GetBuiltInLocaleCodes().Join() }
			);
		}

		#region APIs

		public IGameLocale CurrentLocale
		{
			get
			{
				return LocalizedContentManager.CurrentLanguageCode switch
				{
					LocalizedContentManager.LanguageCode.mod => new IGameLocale.Mod(LocalizedContentManager.CurrentModLanguage),
					_ => new IGameLocale.BuiltIn(LocalizedContentManager.CurrentLanguageCode),
				};
			}
		}

		public IFluent<string> GetLocalizations(IGameLocale locale, IManifest mod, string? name = null)
			=> AssetManager.GetFluent(locale, mod, name);

		#endregion
	}
}