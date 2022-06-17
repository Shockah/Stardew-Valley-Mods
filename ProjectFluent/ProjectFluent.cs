using HarmonyLib;
using Shockah.CommonModCode.GMCM;
using Shockah.ProjectFluent.ContentPatcher;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;
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

		private Func<IManifest, string> GetModDirectoryPathDelegate { get; set; } = null!;
		private Func<IManifest, ITranslationHelper?> GetModTranslationsDelegate { get; set; } = null!;

		public ProjectFluent()
		{
			Harmony = new Harmony("Shockah.ProjectFluent");
			I18nIntegration.SetupEarly(Harmony);
		}

		public override void Entry(IModHelper helper)
		{
			Instance = this;
			Api = new FluentApi(this);
			AssetManager = new AssetManager();

			Config = helper.ReadConfig<ModConfig>();
			Fluent = Api.GetLocalizationsForCurrentLocale(ModManifest);

			var directoryPathGetter = AccessTools.PropertyGetter(Type.GetType("StardewModdingAPI.Framework.IModMetadata, StardewModdingAPI"), "DirectoryPath");
			GetModDirectoryPathDelegate = (manifest) =>
			{
				var modInfo = helper.ModRegistry.Get(manifest.UniqueID);
				return (string)directoryPathGetter.Invoke(modInfo, null)!;
			};

			var translationsGetter = AccessTools.PropertyGetter(Type.GetType("StardewModdingAPI.Framework.IModMetadata, StardewModdingAPI"), "Translations");
			GetModTranslationsDelegate = (manifest) =>
			{
				var modInfo = helper.ModRegistry.Get(manifest.UniqueID);
				return translationsGetter.Invoke(modInfo, null) as ITranslationHelper;
			};

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

			static IEnumerable<string> GetBuiltInLocales()
			{
				foreach (var value in Enum.GetValues(typeof(LocalizedContentManager.LanguageCode)))
				{
					var typedValue = (LocalizedContentManager.LanguageCode)value;
					if (typedValue == LocalizedContentManager.LanguageCode.mod)
						continue;
					yield return typedValue == LocalizedContentManager.LanguageCode.en ? "en-US" : Game1.content.LanguageCodeString(typedValue);
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
				new { Values = GetBuiltInLocales().Join() }
			);
		}

		internal IEnumerable<string> GetFilePathCandidates(IManifest mod, string? name, IGameLocale locale)
		{
			var baseModPath = GetModDirectoryPathDelegate(mod);
			if (baseModPath is null)
				yield break;
			foreach (var candidate in GetFilePathCandidates(Path.Combine(baseModPath, "i18n"), name, locale))
				yield return candidate;
		}

		internal IEnumerable<string> GetFilePathCandidates(string directory, string? name, IGameLocale locale)
		{
			foreach (var relevantLocale in locale.GetRelevantLocaleCodes())
			{
				string fileNameWithoutExtension = $"{(string.IsNullOrEmpty(name) ? "" : $"{name}.")}{relevantLocale}";
				yield return Path.Combine(directory, $"{fileNameWithoutExtension}.ftl");
			}
			foreach (var relevantLocale in new IGameLocale.BuiltIn(LocalizedContentManager.LanguageCode.en).GetRelevantLocaleCodes())
			{
				string fileNameWithoutExtension = $"{(string.IsNullOrEmpty(name) ? "" : $"{name}.")}{relevantLocale}";
				yield return Path.Combine(directory, $"{fileNameWithoutExtension}.ftl");
			}
		}

		internal string GetModDirectoryPath(IManifest mod)
			=> GetModDirectoryPathDelegate(mod);

		internal IFluent<string> GetFallbackFluent(IManifest mod)
		{
			var translations = GetModTranslationsDelegate(mod);
			return translations is null ? new NoOpFluent() : new I18nFluent(translations);
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