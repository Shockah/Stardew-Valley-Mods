using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shockah.ProjectFluent
{
	public class ProjectFluent: Mod
	{
		public static ProjectFluent Instance { get; private set; }
		public FluentApi Api { get; private set; }

		private Func<IManifest, string> getModDirectoryPath;

		private readonly IFluent<string> noOpFluent = new NoOpFluent();
		private IDictionary<(string modID, string name), IDictionary<GameLocale, IFluent<string>>> localizationCache = new Dictionary<(string modID, string name), IDictionary<GameLocale, IFluent<string>>>();

		public override void Entry(IModHelper helper)
		{
			Instance = this;
			Api = new FluentApi(this);
			helper.Events.GameLoop.GameLaunched += OnGameLaunched;

			var directoryPathGetter = AccessTools.PropertyGetter(Type.GetType("StardewModdingAPI.Framework.IModMetadata, StardewModdingAPI"), "DirectoryPath");
			getModDirectoryPath = (manifest) =>
			{
				var modInfo = helper.ModRegistry.Get(manifest.UniqueID);
				return (string)directoryPathGetter.Invoke(modInfo, null);
			};
		}

		public override object GetApi() => Api;

		private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			var harmony = new Harmony(ModManifest.UniqueID);
			ContentPatcherIntegration.Setup(harmony);
		}

		private IEnumerable<string> GetFilePathCandidates(IManifest mod, string name, GameLocale locale)
		{
			var baseModPath = getModDirectoryPath(mod);
			if (baseModPath == null)
				yield break;

			foreach (var relevantLocale in locale.GetRelevantLanguageCodes())
			{
				var fileNameWithoutExtension = $"{(String.IsNullOrEmpty(name) ? "" : $"{name}.")}{relevantLocale}";
				yield return Path.Combine(baseModPath, "i18n", $"{fileNameWithoutExtension}.ftl");
			}
		}

		#region APIs

		public GameLocale CurrentLocale
		{
			get
			{
				return LocalizedContentManager.CurrentLanguageCode switch
				{
					LocalizedContentManager.LanguageCode.mod => new GameLocale.Mod(LocalizedContentManager.CurrentModLanguage),
					_ => new GameLocale.BuiltIn(LocalizedContentManager.CurrentLanguageCode),
				};
			}
		}

		public IFluent<Key> GetLocalizations<Key>(IManifest mod, string name, GameLocale locale)
		{
			var rootKey = (modID: mod.UniqueID, name: name);
			if (!localizationCache.ContainsKey(rootKey))
				localizationCache[rootKey] = new Dictionary<GameLocale, IFluent<string>>();

			var specificCache = localizationCache[rootKey];
			if (!specificCache.ContainsKey(locale))
				specificCache[locale] = new FileResolvingFluent(locale, GetFilePathCandidates(mod, name, locale), noOpFluent);

			var baseFluent = specificCache[locale];
			return typeof(Key) == typeof(string) ? (IFluent<Key>)baseFluent : new MappingFluent<Key>(baseFluent);
		}

		#endregion
	}
}