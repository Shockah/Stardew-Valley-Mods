using Shockah.CommonModCode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Shockah.ProjectFluent
{
	internal class AssetManager
	{
		private static readonly string FluentPathsAssetPath = "Shockah.ProjectFluent/FluentPaths";
		internal static readonly string I18nPathsAssetPath = "Shockah.ProjectFluent/i18nPaths";

		private bool IsSetup { get; set; } = false;
		private Regex ModRegex { get; set; } = null!;

		private IList<(IContentPack pack, ContentPackContent content)> ContentPackContents { get; set; } = new List<(IContentPack pack, ContentPackContent content)>();
		private IList<WeakReference<AssetFileResolvingFluent>> Fluents { get; set; } = new List<WeakReference<AssetFileResolvingFluent>>();

		internal void Setup(IModHelper helper)
		{
			if (IsSetup)
				return;

			ModRegex = new Regex("\\{\\{\\s*?([a-z0-9_\\-\\.]+?)\\s*?\\}\\}", RegexOptions.IgnoreCase);

			helper.Events.Content.AssetRequested += OnAssetRequested;
			helper.Events.Content.AssetReady += OnAssetReady;
			helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;

			RegisterContentPacks(helper.ContentPacks, helper.GameContent);

			IsSetup = true;
		}

		private void RegisterContentPacks(IContentPackHelper helper, IGameContentHelper contentHelper)
		{
			ProjectFluent.Instance.Monitor.Log("Loading content packs...", LogLevel.Info);
			foreach (var pack in helper.GetOwned())
				RegisterContentPack(pack, contentHelper);
		}

		private void RegisterContentPack(IContentPack pack, IGameContentHelper contentHelper)
		{
			ProjectFluent.Instance.Monitor.Log($"Loading content pack `{pack.Manifest.UniqueID}`", LogLevel.Info);

			(IContentPack pack, ContentPackContent content)? existingEntry = ContentPackContents.FirstOrNull(e => e.pack.Manifest.UniqueID == pack.Manifest.UniqueID);
			if (existingEntry is not null)
			{
				ContentPackContents.Remove(existingEntry.Value);
				if ((existingEntry.Value.content.Fluent?.Count ?? 0) != 0)
					contentHelper.InvalidateCache(FluentPathsAssetPath);
				if ((existingEntry.Value.content.I18n?.Count ?? 0) != 0)
					contentHelper.InvalidateCache(I18nPathsAssetPath);
			}

			if (!pack.HasFile("content.json"))
				return;

			try
			{
				var content = pack.ReadJsonFile<ContentPackContent>("content.json");
				if (content is null)
					return;

				ContentPackContents.Add((pack: pack, content: content));
				if ((content.Fluent?.Count ?? 0) != 0)
					contentHelper.InvalidateCache(FluentPathsAssetPath);
				if ((content.I18n?.Count ?? 0) != 0)
					contentHelper.InvalidateCache(I18nPathsAssetPath);
			}
			catch (Exception ex)
			{
				ProjectFluent.Instance.Monitor.Log($"There was an error while reading `content.json` for the `{pack.Manifest.UniqueID}` content pack:\n{ex}", LogLevel.Error);
			}
		}

		internal IFluent<string> GetFluent(IGameLocale locale, IManifest mod, string? name = null)
		{
			var toRemove = Fluents.Where(r => !r.TryGetTarget(out _)).ToList();
			foreach (var reference in toRemove)
				Fluents.Remove(reference);

			foreach (var reference in Fluents)
			{
				if (!reference.TryGetTarget(out var cached))
					continue;
				if (cached.Locale.LanguageCode == locale.LanguageCode && cached.Mod.UniqueID == mod.UniqueID && cached.Name == name)
					return cached;
			}

			var fluent = new AssetFileResolvingFluent(locale, mod, name, ProjectFluent.Instance.FallbackFluentProvider.GetFallbackFluent(mod));
			var asset = Game1.content.Load<Dictionary<string, List<string>>>(FluentPathsAssetPath);
			fluent.OnAssetChanged(asset);
			Fluents.Add(new WeakReference<AssetFileResolvingFluent>(fluent));
			return fluent;
		}

		private string ReplacePathTokens(string path, string contextUniqueModID)
		{
			while (true)
			{
				string oldPath = path;

				{
					var match = ModRegex.Match(path);
					if (match.Success)
					{
						string content = match.Groups[1].Value;
						if (content.Equals("this", StringComparison.InvariantCultureIgnoreCase))
							content = contextUniqueModID;
					}
				}

				if (oldPath == path)
					return path;
			}
		}

		private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
		{
			if (e.Name.IsEquivalentTo(FluentPathsAssetPath))
			{
				e.LoadFrom(() =>
				{
					var asset = new Dictionary<string, List<string>>();
					foreach (var (pack, content) in ContentPackContents)
					{
						if (content.Fluent is null)
							continue;
						foreach (var (key, value) in content.Fluent)
						{
							if (!asset.TryGetValue(key, out var values))
							{
								values = new List<string>();
								asset[key] = values;
							}
							values.Add(value.Equals("<this>", StringComparison.InvariantCultureIgnoreCase) ? pack.Manifest.UniqueID : value);
						}
					}
					return asset;
				}, AssetLoadPriority.Medium);
			}
			else if (e.Name.IsEquivalentTo(I18nPathsAssetPath))
			{
				e.LoadFrom(() =>
				{
					var asset = new Dictionary<string, List<string>>();
					foreach (var (pack, content) in ContentPackContents)
					{
						if (content.I18n is null)
							continue;
						foreach (var (key, value) in content.I18n)
						{
							if (!asset.TryGetValue(key, out var values))
							{
								values = new List<string>();
								asset[key] = values;
							}
							values.Add(value.Equals("<this>", StringComparison.InvariantCultureIgnoreCase) ? pack.Manifest.UniqueID : value);
						}
					}
					return asset;
				}, AssetLoadPriority.Medium);
			}
		}

		private void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
		{
			if (e.Names.Any(n => n.IsEquivalentTo(FluentPathsAssetPath)))
			{
				foreach (var reference in Fluents)
				{
					if (!reference.TryGetTarget(out var cached))
						continue;
					cached.OnAssetChanged(null);
				}
			}
			else if (e.Names.Any(n => n.IsEquivalentTo(I18nPathsAssetPath)))
			{
				I18nIntegration.ReloadTranslations();
			}
		}

		private void OnAssetReady(object? sender, AssetReadyEventArgs e)
		{
			if (e.Name.IsEquivalentTo(FluentPathsAssetPath))
			{
				var asset = Game1.content.Load<Dictionary<string, List<string>>>(FluentPathsAssetPath);
				foreach (var reference in Fluents)
				{
					if (!reference.TryGetTarget(out var cached))
						continue;
					cached.OnAssetChanged(asset);
				}
			}
		}
	}
}