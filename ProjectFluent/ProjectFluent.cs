﻿using HarmonyLib;
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
		private Harmony Harmony { get; set; } = null!;

		internal ModConfig Config { get; private set; } = null!;
		internal IFluent<string> Fluent { get; private set; } = null!;

		private IModDirectoryProvider ModDirectoryProvider { get; set; } = null!;
		private IFluentPathProvider FluentPathProvider { get; set; } = null!;
		private IModTranslationsProvider ModTranslationsProvider { get; set; } = null!;
		private IFallbackFluentProvider FallbackFluentProvider { get; set; } = null!;
		private IPathTokenReplacer PathTokenReplacer { get; set; } = null!;
		private IContentPackParser ContentPackParser { get; set; } = null!;
		private IContentPackManager.WithRegisteringCapability ContentPackManager { get; set; } = null!;
		private IModFluentPathProvider ModFluentPathProvider { get; set; } = null!;
		private II18nDirectoryProvider I18nDirectoryProvider { get; set; } = null!;
		private IFluentProvider FluentProvider { get; set; } = null!;

		public override void Entry(IModHelper helper)
		{
			Instance = this;
			Api = new FluentApi(this);
			Harmony = new Harmony(ModManifest.UniqueID);

			ModDirectoryProvider = new ModDirectoryProvider(helper.ModRegistry);
			FluentPathProvider = new FluentPathProvider();
			ModTranslationsProvider = new ModTranslationsProvider(helper.ModRegistry);
			FallbackFluentProvider = new FallbackFluentProvider(ModTranslationsProvider);
			PathTokenReplacer = new ModDirectoryPathTokenReplacer(helper.ModRegistry, ModDirectoryProvider);
			ContentPackParser = new ContentPackParser(ModManifest.Version, helper.ModRegistry);
			ContentPackManager = new ContentPackManager(Monitor, helper.ContentPacks, ContentPackParser);
			ModFluentPathProvider = new SerialModDirectoryFluentPathProvider(
				new ModFluentPathProvider(ModDirectoryProvider, FluentPathProvider),
				new ContentPackAdditionalModFluentPathProvider(helper.ModRegistry, ContentPackManager, FluentPathProvider, ModDirectoryProvider),
				new AssetAdditionalModFluentPathProvider(helper.Events.Content, FluentPathProvider, PathTokenReplacer),
				new ModFluentPathProvider(ModDirectoryProvider, FluentPathProvider, IGameLocale.Default)
			);
			I18nDirectoryProvider = new SerialI18nDirectoryProvider(
				new ContentPackI18nDirectoryProvider(helper.ModRegistry, ContentPackManager, ModDirectoryProvider),
				new AssetI18nDirectoryProvider(helper.Events.Content, PathTokenReplacer)
			);
			FluentProvider = new FluentProvider(FallbackFluentProvider, ModFluentPathProvider);


			Config = helper.ReadConfig<ModConfig>();
			Fluent = Api.GetLocalizationsForCurrentLocale(ModManifest);

			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			I18nIntegration.Setup(Monitor, Harmony, I18nDirectoryProvider);
		}

		public override object GetApi() => Api;

		private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
		{
			ContentPackManager.RegisterAllContentPacks();
			I18nIntegration.ReloadTranslations();
			ContentPatcherIntegration.Setup(Harmony);

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
			=> FluentProvider.GetFluent(locale, mod, name);

		#endregion
	}
}